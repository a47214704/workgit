using Core;
using Core.Data;
using GroupPay.Models;
using GroupPay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AwardController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _config;
        private readonly ILogger<AwardController> _logger;
        private const int pageSize = 20;
        private readonly AgencyCommissionService _agencyCommissionService;
        private readonly IDistributedCache _cache;

        public AwardController(DataAccessor dataAccessor,SiteConfig config, IDistributedCache cache, ILoggerFactory loggerFactory, AgencyCommissionService agencyCommissionService)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._config = config ?? throw new ArgumentNullException(nameof(config));
            this._logger = loggerFactory.CreateLogger<AwardController>();
            this._agencyCommissionService = agencyCommissionService ?? throw new ArgumentNullException(nameof(agencyCommissionService));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<AwardConfig>>> Post([FromBody]AwardConfig awardConfig)
        {
            if (!awardConfig.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<AwardConfig>(Constants.WebApiErrors.InvalidData, "incomplete award object"));
            }

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(Sql.Insert, parameters =>
                {
                    parameters.Add("@award_bouns", MySqlDbType.Int32).Value = awardConfig.Bouns;
                    parameters.Add("@award_condition", MySqlDbType.Int32).Value = awardConfig.AwardCondition;
                    parameters.Add("@modify_time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                });
                awardConfig.Id = await transaction.GetLastInsertId32();
                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<AwardConfig>(awardConfig));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<AwardConfig>>> Put([FromRoute]int id, [FromBody]AwardConfig awardConfig)
        {
            if (!awardConfig.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<AwardConfig>(Constants.WebApiErrors.InvalidData, "incomplete award object"));
            }

            AwardConfig existingOne = await this._dataAccessor.GetOne<AwardConfig>("select * from `award_config` where `id`=@id", parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });

            if (existingOne == null)
            {
                return this.NotFound(this.CreateErrorResult<AwardConfig>(Constants.WebApiErrors.ObjectNotFound, "award not found"));
            }

            awardConfig.Id = id;
            await this._dataAccessor.Execute(Sql.Update, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int64).Value = awardConfig.Id;
                parameters.Add("@bouns", MySqlDbType.Int32).Value = awardConfig.Bouns;
                parameters.Add("@condition", MySqlDbType.Int32).Value = awardConfig.AwardCondition;
                parameters.Add("@modify_time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            });
            return this.Ok(new WebApiResult<AwardConfig>(awardConfig));
        }

        [HttpGet]
        [Authorize(Roles = "InstrumentOwner,ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<AwardConfig>>>> Get()
        {
            return this.Ok(new WebApiResult<List<AwardConfig>>(await _agencyCommissionService.GetConfigList()));
        }
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<AwardConfig>>> Delete(int id)
        {
            AwardConfig award = await this._dataAccessor.GetOne<AwardConfig>("select * from `award_config` where `id`=@id", parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            if (award == null)
            {
                return this.NotFound(this.CreateErrorResult<AwardConfig>(Constants.WebApiErrors.ObjectNotFound, "award not found"));
            }

            await this._dataAccessor.Execute(Sql.Delete, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            return this.NoContent();
        }

        [HttpPost("Withdraw")]
        [Authorize(Roles = "InstrumentOwner", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<AgencyCommission>>> Withdraw()
        {
            long userId = this.HttpContext.User.GetId();
            AgencyCommission award;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset today = now.RoundDay(this._config.TimeZone);
            DateTimeOffset yesterday = today.AddDays(-1);
            int lastDate = yesterday.ToDateValue(this._config.TimeZone);
            try
            {
                award = await this._dataAccessor.GetOne<AgencyCommission>("select * from `agency_commission` where `user_id`=@id and `type`=2 and `week`=@lastdate",
                p=> 
                {
                    p.Add("@id", MySqlDbType.Int64).Value = userId;
                    p.Add("@lastdate", MySqlDbType.Int32).Value = lastDate;
                });
                if (award == null)
                {
                    throw new AwardException(AwardError.UnReadySettle);
                }
            }
            catch (AwardException awardException)
            {
                switch (awardException.Error)
                {
                    case AwardError.UnReadySettle:
                        return this.StatusCode(403, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Pending, "award is unReady settle"));
                    case AwardError.UserNotFound:
                        return this.StatusCode(404, this.CreateErrorResult<AgencyCommission>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
                    case AwardError.Pending:
                        return this.StatusCode(102, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Pending, "award is settleing"));
                    default:
                        await this._cache.SetStringAsync("GenerateingAwardReport_" + lastDate + "_" + userId, "false");
                        return this.StatusCode(500, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Unknown, "unknown system error, please try again"));
                }
            }

            if (award.Cashed)
            {
                return this.StatusCode(403, this.CreateErrorResult<AgencyCommission>(Constants.WebApiErrors.ObjectConflict, "yesterday already withdraw"));
            }
            
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                var user = await transaction.GetOne<UserAccount>("select * from `user_account` where id=@uid",
                    p => p.Add("@uid", MySqlDbType.Int64).Value = userId);

                if (user == null)
                {
                    return this.StatusCode(404, this.CreateErrorResult<AgencyCommission>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
                }

                await transaction.Execute(Sql.WithDrawSettle, parameters =>
                {
                    parameters.Add("@uid", MySqlDbType.Int64).Value = award.UserId;
                    parameters.Add("@cash_time", MySqlDbType.Int64).Value = now.ToUniversalTime().ToUnixTimeMilliseconds();
                    parameters.Add("@lastDate", MySqlDbType.Int32).Value = lastDate;
                });

                await transaction.Execute(
                "update `user_account` set `balance`=`balance`+@bouns where id=@uid",
                p =>
                {
                    p.Add("@bouns", MySqlDbType.Int32).Value = award.Commission;
                    p.Add("@uid", MySqlDbType.Int64).Value = user.Id;
                });

                 // Add a transaction log
                await transaction.Execute(
                    "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                    p =>
                    {
                        p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Rewards;
                        p.Add("@paymentId", MySqlDbType.Int64).Value = 0;
                        p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                        p.Add("@amount", MySqlDbType.Int32).Value = award.Commission * 100;
                        p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                        p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance + award.Commission);
                        p.Add("@time", MySqlDbType.Int64).Value = now.ToUniversalTime().ToUnixTimeMilliseconds();
                    });

                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<AgencyCommission>(award));
        }

        [HttpGet("Withdraw")]
        [Authorize(Roles = "InstrumentOwner", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<AgencyCommission>>> WithdrawStatus()
        {
            long userId = this.HttpContext.User.GetId();
            DateTimeOffset today = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone);
            AgencyCommission award;
            try
            {
                award = await _agencyCommissionService.GetAwardStatus(userId, today.ToUnixTimeMilliseconds(), today.AddDays(1).ToUnixTimeMilliseconds());
            }
            catch (AwardException awardException)
            {
                switch (awardException.Error)
                {
                    case AwardError.UserNotFound:
                        return this.StatusCode(404, this.CreateErrorResult<AgencyCommission>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
                    case AwardError.Pending:
                        return this.StatusCode(102, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Pending, "award is settleing"));
                    default:
                        await this._cache.SetStringAsync("GenerateingAwardReport_" + today.ToDateValue(this._config.TimeZone) + "_" + userId, "false");
                        return this.StatusCode(500, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Unknown, "unknown system error, please try again"));
                }
            }
            return this.Ok(new WebApiResult<AgencyCommission>(award));
        }

        [HttpGet("WithdrawList")]
        [Authorize(Roles = "InstrumentOwner", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<AgencyCommission>>>> WithdrawList()
        {
            long userId = this.HttpContext.User.GetId();
            List<AgencyCommission> awardConfigs = await this._dataAccessor.GetAll<AgencyCommission>(Sql.WithDrawRecord,p=> p.Add("@uid", MySqlDbType.Int64).Value = userId);
            return this.Ok(new WebApiResult<List<AgencyCommission>>(awardConfigs));
        }

        [HttpGet("WithdrawReport")]
        [Authorize(Roles = "ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<AgencyCommission>>>> WithdrawRecord(
            [FromQuery]int page,
            [FromQuery]string startTime,
            [FromQuery]string endTime,
            [FromQuery]string accountName)
        {
            page = page < 1 ? 1 : page;
            int totalPage = 0;
            long startTimestamp;
            long endTimestamp = 0;
            double summary = 0;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime.ToTimeZoneDateTime(this._config.TimeZoneOffset), out DateTime dateTime))
            {
                startTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else
            {
                startTimestamp = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            sqlBuilder.Append("where ac.`cash_time` >= @startTime and ac.type=2");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._config.TimeZoneOffset), out dateTime))
            {
                endTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                sqlBuilder.Append(" and ac.`cash_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = endTimestamp });
            }

            if (!string.IsNullOrEmpty(accountName))
            {
                sqlBuilder.Append(" and u.`account_name` like @accountName");
                parameters.Add(new MySqlParameter("@accountName", MySqlDbType.VarChar) { Value = "%" + accountName + "%" });
            }

            totalPage = (int)Math.Ceiling((long)await this._dataAccessor.ExecuteScalar(
                    @"select count(1) from `agency_commission` as ac join `user_account` as u on u.`id`=ac.`user_id` " + sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray())) / (double)pageSize);

            summary = await this._dataAccessor.GetOne(
                "select sum(ac.`commission`) as summary from `agency_commission` as ac join `user_account` as u on u.`id`=ac.`user_id` " + sqlBuilder.ToString(),
                new SimpleRowMapper<double>(async reader =>
                {
                    if (await reader.IsDBNullAsync(0))
                    {
                        return 0d;
                    }

                    return reader.GetDouble(0);
                }),
                p => p.AddRange(parameters.ToArray()));

            sqlBuilder.Append(" order by ac.`cash_time` desc");
            sqlBuilder.Append(" limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int32) { Value = (page - 1) * pageSize });

            List<AgencyCommission> award = await this._dataAccessor.GetAll<AgencyCommission>("select u.id as user_id, u.`account_name` as account_name, ac.`commission` as commission, ac.`week` as week from `agency_commission` as ac join `user_account` as u on u.`id`=ac.`user_id` " + sqlBuilder.ToString(), p => p.AddRange(parameters.ToArray()));
            return this.Ok(new WebApiResult<Report>(new Report() {
                DataPage = new DataPage<AgencyCommission>() {
                    Page = page,
                    TotalPages = totalPage,
                    Records = award
                },
                Summary = summary
            }));
        }

        public class Report
        {
            public DataPage<AgencyCommission> DataPage { get; set; }

            public double Summary { get; set; }
        }

        private static class Sql
        {
            internal const string Insert = "insert into `award_config`(`bouns`,`modify_time`,`condition`)values(@award_bouns, @modify_time, @award_condition)";
            internal const string Update = "update `award_config` set `bouns`=@bouns,`condition`=@condition, `modify_time`=@modify_time where `id`=@id";
            internal const string Delete = "delete `award_config` where `id`=@id";
            internal const string WithDrawSettle = "update `agency_commission` set `cashed` = 1, `cash_time` = @cash_time where `user_id` = @uid and `week` = @lastDate and `type` = 2";
            internal const string WithDrawRecord = "select * from `agency_commission` where `user_id`=@uid and `type` = 2 and `cashed` = 1 order by `week` desc";
        }
    }
}
