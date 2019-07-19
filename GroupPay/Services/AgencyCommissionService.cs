using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroupPay.Services
{
    public class AgencyCommissionService
    {
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _config;
        private readonly ILogger<AgencyCommissionService> _logger;

        public AgencyCommissionService(DataAccessor dataAccessor, IDistributedCache cache, SiteConfig config, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            this._logger = loggerFactory.CreateLogger<AgencyCommissionService>();
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<List<AwardConfig>> GetConfigList()
        {
            return await this._dataAccessor.GetAll<AwardConfig>("select * from `award_config` order by `condition`");
        }

        public async Task<RevenueReport> GenerateRevenueReport(DataTransaction transaction,long userId, long startTime, long endTime = -1)
        {
            // Revenue from self
            CollectRevenueReport selfWechatReport = await GetCollectRevenue(transaction, userId, ChannelProvider.Wechat, startTime, endTime);
            CollectRevenueReport selfAliReport = await GetCollectRevenue(transaction, userId, ChannelProvider.AliPay, startTime, endTime);
            CollectRevenueReport selfBankReport = await GetCollectRevenue(transaction, userId, ChannelProvider.Bank, startTime, endTime);

            RevenueReport report = new RevenueReport
            {
                SelfWechatRevenue = selfWechatReport.Revenue,
                SelfBankRevenue = selfBankReport.Revenue,
                SelfAliRevenue = selfAliReport.Revenue,
                AliRatio = selfAliReport.Ratio,
                BankRatio = selfBankReport.Ratio,
                WechatRatio = selfWechatReport.Ratio
            };

            List<CollectRevenueReport> agentReports = await transaction.GetAll<CollectRevenueReport>(
                @"select re.`revenue`, re.`user_id`, cr.`ratio`/1000 + 0E0 as `ratio` from 
                (select sum(p.`amount`/100) + 0E0 as revenue, ci.`user_id` as `user_id` 
                    from (
                        select ci.id,ur.`user_id` from `user_relation` as ur 
                        join `collect_instrument` as ci on ci.`user_id`=ur.`user_id`
	                    where ur.`upper_level_id` = @uid and ur.`is_direct`=1 group by ur.`user_id`,ci.id
	                    union
	                    select ci.id,ur.`user_id` from `user_relation` as ur 
                        join `user_relation` as ur1 on ur1.`upper_level_id`=ur.`user_id`
	                    join `collect_instrument` as ci on ci.`user_id`=ur1.`user_id`
	                    where ur.`upper_level_id` = @uid and ur.`is_direct`=1 group by ur.`user_id`,ci.id
                    ) as ci 
                    join `payment` as p on p.`ciid`=ci.`id`
                    where p.`settle_time` >=@startTime and p.`status` in (3,4)" + (endTime > 0 ? " and p.`settle_time`<=@endTime" : string.Empty) +
                    @" group by ci.`user_id`) as re 
                    join `commission_ratio` as cr on re.`revenue` >= cr.`lbound` and (re.`revenue` < cr.`ubound` or cr.`ubound`=-1)",
                p =>
                {
                    p.Add("@startTime", MySqlDbType.Int64).Value = startTime;
                    p.Add("@uid", MySqlDbType.Int64).Value = userId;
                    if (endTime > 0)
                    {
                        p.Add("@endTime", MySqlDbType.Int64).Value = endTime;
                    }
                });
            report.AgencyCommission = agentReports.Sum(r => r.Revenue * r.Ratio);
            report.AgentRevenue = agentReports.Sum(r => r.Revenue);

            List<CommissionRatio> ratios = await transaction.GetAll<CommissionRatio>(
                "select * from `commission_ratio` order by `lbound`");

            foreach (CommissionRatio ratio in ratios)
            {
                if (report.TotalRevenue >= ratio.LowerBound && (report.TotalRevenue < ratio.UpperBound || ratio.UpperBound == -1))
                {
                    report.RankRatio = ratio.Ratio / 1000;
                    break;
                }
            }

            return report;
        }

        public async Task<AgencyCommission> GetAwardStatus(long userId, long startTime, long endTime, bool save = false)
        {
            int lastDate = new DateTimeOffset(startTime.ToDateTime()).ToDateValue(this._config.TimeZone);
            this._logger.LogInformation("GenerateingAwardReport_"+ lastDate + "_" + userId);

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                AgencyCommission award = await transaction.GetOne<AgencyCommission>(
                "select * from `agency_commission` where `user_id`=@uid and `week`=@week and `type`=2",
                p =>
                {
                    p.Add("@uid", MySqlDbType.Int64).Value = userId;
                    p.Add("@week", MySqlDbType.Int32).Value = lastDate;
                });

                if (award != null)
                {
                    return award;
                }
                else
                {
                    award = new AgencyCommission
                    {
                        UserId = userId,
                        Commission = 0,
                        Revenue = 0
                    };
                }

                var user = await transaction.GetOne<UserAccount>("select * from `user_account` where id=@uid",
                        p => p.Add("@uid", MySqlDbType.Int64).Value = userId);

                if (user == null)
                {
                    throw new AwardException(AwardError.UserNotFound);
                }
                
                int totalAmount = await transaction.GetOne("select sum(p.`amount`) as amount from `payment` as p join `collect_instrument` as ci on p.ciid=ci.id where ci.`user_id`=@uid and p.`status`=3 and p.`create_time`>=@tss and p.`create_time`<@tse",
                    new SimpleRowMapper<int>(async reader =>
                    {
                        if (await reader.IsDBNullAsync(0))
                        {
                            return 0;
                        }

                        return reader.GetInt32(0);
                    }),
                    p => {
                        p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        p.Add("@tss", MySqlDbType.Int64).Value = startTime;
                        p.Add("@tse", MySqlDbType.Int64).Value = endTime;
                    });

                award.Revenue = totalAmount;
                List<AwardConfig> awardConfigs = await GetConfigList();

                foreach (AwardConfig config in awardConfigs)
                {
                    if (config.AwardCondition * 100 > award.Revenue)
                    {
                        break;
                    }
                    award.Commission = config.Bouns;
                }

                if (save)
                {
                    await transaction.Execute("insert into `agency_commission`(`user_id`, `type`, `week`, `revenue`, `commission`, `cashed`, `cash_time`) values (@uid, @type, @week, @revenue, @commission, @cashed, @ts)", p =>
                    {
                        p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        p.Add("@type", MySqlDbType.Int32).Value = 2;
                        p.Add("@week", MySqlDbType.Int32).Value = lastDate;
                        p.Add("@revenue", MySqlDbType.Double).Value = award.Revenue;
                        p.Add("@commission", MySqlDbType.Double).Value = award.Commission;
                        p.Add("@cashed", MySqlDbType.Bit).Value = award.Commission == 0;
                        p.Add("@ts", MySqlDbType.Int64).Value = award.Commission == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : 0;
                    });
                }
                await transaction.Commit();
                this._logger.LogInformation("GenerateingAwardReport_" + lastDate + "_" + userId + " End");
                return award;
            }
        }

        public async Task GetCommissionStatus(long userId, long startTime, long endTime)
        {
            int lastDate = new DateTimeOffset(startTime.ToDateTime()).ToDateValue(this._config.TimeZone);
            this._logger.LogInformation("GenerateingCommissionReport_" + lastDate + "_" + userId);

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                AgencyCommission commission = await _dataAccessor.GetOne<AgencyCommission>(
                    "select * from `agency_commission` where `user_id`=@uid and `week`=@week and `type`=1",
                    p =>
                    {
                        p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        p.Add("@week", MySqlDbType.Int32).Value = lastDate;
                    });
                if (commission == null)
                {
                    RevenueReport revenueReport = await this.GenerateRevenueReport(transaction, userId, startTime, endTime);

                    await transaction.Execute("insert into `agency_commission`(`user_id`, `type`, `week`, `revenue`, `commission`, `cashed`, `cash_time`) values (@uid, @type, @week, @revenue, @commission, @cashed, @ts)",
                        p =>
                        {
                            p.Add("@uid", MySqlDbType.Int64).Value = userId;
                            p.Add("@type", MySqlDbType.Int32).Value = 1;
                            p.Add("@week", MySqlDbType.Int32).Value = lastDate;
                            p.Add("@revenue", MySqlDbType.Double).Value = revenueReport.TotalRevenue;
                            p.Add("@commission", MySqlDbType.Double).Value = revenueReport.TotalCommission;
                            p.Add("@cashed", MySqlDbType.Bit).Value = revenueReport.TotalCommission < 0.01;
                            p.Add("@ts", MySqlDbType.Int64).Value = revenueReport.TotalCommission < 0.01 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : 0;
                        });
                    await transaction.Commit();
                }
            }
            this._logger.LogInformation("GenerateingCommissionReport_" + lastDate + "_" + userId + "End");
        }

        public async Task<long> GetLastSettleTimeStamp(long defaultTime, string keyword)
        {
            //get user commission last settle time 
            ConfigItem lastSettleTime = await _dataAccessor.GetOne<ConfigItem>("select * from `site_config` where name=@key",
                p=>p.Add("@key",MySqlDbType.VarChar).Value=keyword);
            long lastSettleTimeStamp = defaultTime;
            if (lastSettleTime != null)
            {
                long.TryParse(lastSettleTime.Value, out lastSettleTimeStamp);
            }
            return lastSettleTimeStamp;
        }

        public async Task AwardDaliyTask(long startTime = 0, long endTime = 0)
        {
            using (DataTransaction transaction = await _dataAccessor.CreateTransaction())
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset today = now.RoundDay(this._config.TimeZone);
                long todayStamp = today.AddDays(-1).ToUnixTimeMilliseconds();
                endTime = endTime == 0 ? today.ToUnixTimeMilliseconds() : endTime;
                long lastSettleTimeStamp = startTime == 0 ? await GetLastSettleTimeStamp(todayStamp, "AwardSettleLastTime") : startTime;
                this._logger.LogInformation($"start login users in {startTime}/{lastSettleTimeStamp} ~ {endTime}");
                //get yesterday login users
                List<long> users = await transaction.GetAll("select id from `user_account` where role_id=100",
                    new SimpleRowMapper<long>(reader => Task.FromResult(reader.GetInt64(0))));

                this._logger.LogInformation($"start Generate Award Report in users:{string.Join(",", users)}");
                await Task.WhenAll(users.Select(u => this.GetAwardStatus(u, lastSettleTimeStamp, endTime, true)).ToArray());
                    
                await transaction.Execute("insert into `site_config`(`name`,`display_name`,`value`) value('AwardSettleLastTime',@displayName,@value) ON DUPLICATE KEY UPDATE `display_name`=@displayName, `value`=@value;",
                p =>
                {
                    p.Add("@displayName", MySqlDbType.VarChar).Value = "奖励最后结算时间";
                    p.Add("@value", MySqlDbType.VarChar).Value = endTime;
                });

                await transaction.Commit();
            }
        }

        public async Task CommissionDaliyTask(long startTime = 0, long endTime = 0)
        {
            using (DataTransaction transaction = await _dataAccessor.CreateTransaction())
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset today = now.RoundDay(this._config.TimeZone);
                long todayStamp = today.AddDays(-1).ToUnixTimeMilliseconds();
                endTime = endTime == 0 ? today.ToUnixTimeMilliseconds() : endTime;
                long lastSettleTimeStamp = startTime == 0 ? await GetLastSettleTimeStamp(todayStamp, "CommissionSettleLastTime") : startTime;
                this._logger.LogInformation($"start login users in {startTime}/{lastSettleTimeStamp} ~ {endTime}");
                //get yesterday login users
                List<long> users = await transaction.GetAll("select id from `user_account` where role_id=100",
                    new SimpleRowMapper<long>(reader => Task.FromResult(reader.GetInt64(0))));

                this._logger.LogInformation($"start Generate Revenue Report in users:{string.Join(",", users)}");
                await Task.WhenAll(users.Select(u => this.GetCommissionStatus(u, lastSettleTimeStamp, endTime)).ToArray());

                await transaction.Execute("insert into `site_config`(`name`,`display_name`,`value`) value('CommissionSettleLastTime',@displayName,@value) ON DUPLICATE KEY UPDATE `display_name`=@displayName, `value`=@value;",
                p =>
                {
                    p.Add("@displayName", MySqlDbType.VarChar).Value = "利润最后结算时间";
                    p.Add("@value", MySqlDbType.VarChar).Value = endTime;
                });

                await transaction.Commit();
            }
        }

        private async Task<CollectRevenueReport> GetCollectRevenue(DataTransaction transaction, long userId, ChannelProvider provider, long startTime, long endTime = -1)
        {
            return await transaction.GetOne<CollectRevenueReport>(
                "select sum(p.`amount`/100) + 0E0 as revenue, cc.`ratio`/1000 + 0E0  as ratio from `payment` as p " +
                "join `collect_instrument` as ci on p.`ciid`=ci.`id` " +
                "join `collect_channel` as cc on ci.`channel_id`=cc.`id` " +
                "where p.`settle_time` >=@startTime and cc.`provider`=@provider and ci.`user_id`=@uid and p.`status` in (3,4)" + (endTime > 0 ? " and p.`settle_time`<=@endTime" : string.Empty) + " group by cc.`ratio`",
                p =>
                {
                    p.Add("@startTime", MySqlDbType.Int64).Value = startTime;
                    p.Add("@provider", MySqlDbType.Int32).Value = provider;
                    p.Add("@uid", MySqlDbType.Int64).Value = userId;
                    if (endTime > 0)
                        p.Add("@endTime", MySqlDbType.Int64).Value = endTime;
                }) ?? new CollectRevenueReport
                {
                    Revenue = 0,
                    Ratio = 0
                };
        }

        class CollectRevenueReport
        {
            [Column("user_id")]
            public long UserId { get; set; }

            [Column("revenue")]
            public double Revenue { get; set; }

            [Column("ratio")]
            public double Ratio { get; set; }
        }
    }
}
