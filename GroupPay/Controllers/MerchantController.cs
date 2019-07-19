using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Crypto;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace GroupPay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MerchantController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _siteConfig;
        private readonly ILogger<MerchantController> _logger;
        private const int pageSize = 20;
        private readonly IDistributedCache _cache;

        public MerchantController(DataAccessor dataAccessor, IDistributedCache cache, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._siteConfig = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._logger = loggerFactory.CreateLogger<MerchantController>();
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpPost]
        [Authorize(Roles = "MerchantWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<Merchant>>> Post([FromBody]Merchant merchant)
        {
            if (!merchant.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<Merchant>(Constants.WebApiErrors.InvalidData, "bad merchant data"));
            }

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(
                    "insert into `merchant`(`name`, `app_key`, `app_pwd`) values(@name, @appKey, @appPwd)",
                    p =>
                    {
                        p.Add("@name", MySqlDbType.VarChar).Value = merchant.Name;
                        p.Add("@appKey", MySqlDbType.VarChar).Value = merchant.AppKey;
                        p.Add("@appPwd", MySqlDbType.VarChar).Value = merchant.AppSecret;
                    });
                merchant.Id = await transaction.GetLastInsertId32();
                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<Merchant>(merchant));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MerchantWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<Merchant>> Put([FromRoute]int id, [FromBody]Merchant merchant)
        {
            if (!merchant.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<Merchant>(Constants.WebApiErrors.InvalidData, "bad merchant data"));
            }

            Merchant existingOne = await this._dataAccessor.GetOne<Merchant>("select * from `merchant` where `id`=@id", p => p.Add("@id", MySqlDbType.Int32).Value = id);
            if (existingOne == null)
            {
                return this.NotFound();
            }

            merchant.Id = id;
            await _dataAccessor.Execute(
                "update `merchant` set `name`=@name, `app_key`=@appKey, `app_pwd`=@appPwd where `id`=@id",
                p =>
                {
                    p.Add("@name", MySqlDbType.VarChar).Value = merchant.Name;
                    p.Add("@appKey", MySqlDbType.VarChar).Value = merchant.AppKey;
                    p.Add("@appPwd", MySqlDbType.VarChar).Value = merchant.AppSecret;
                    p.Add("@id", MySqlDbType.Int32).Value = merchant.Id;
                });
            return this.Ok(new WebApiResult<Merchant>(merchant));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "MerchantWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<Merchant>>> Delete([FromRoute]long id)
        {
            Merchant merchant = await this._dataAccessor.GetOne<Merchant>("select * from `merchant` where `id`=@id", p => p.Add("@id", MySqlDbType.Int32).Value = id);
            if (merchant == null)
            {
                return this.NotFound(this.CreateErrorResult<Merchant>(Constants.WebApiErrors.ObjectNotFound, "merchant not found"));
            }

            await this._dataAccessor.Execute("delete from `merchant` where `id`=@id", p => p.Add("@id", MySqlDbType.Int32).Value = id);
            return this.NoContent();
        }

        [HttpGet]
        [Authorize(Roles = "MerchantReader,PaymentWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<Merchant>>>> Get()
        {
            List<Merchant> merchants = await this._dataAccessor.GetAll<Merchant>("select id, name from merchant order by `id`");
            return this.Ok(new WebApiResult<List<Merchant>>(merchants));
        }

        [HttpGet("Info")]
        [Authorize(Roles = "Agent,AgentMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<Merchant>>> Info([FromQuery] string referrerId = "")
        {
            if (this.HttpContext.User.HasRole("AgentMgmt"))
            {
                if (string.IsNullOrEmpty(referrerId) || referrerId == "null")
                {
                    return this.Ok(new WebApiResult<Merchant>(new Merchant()));
                }
                else
                {
                    Merchant merchant = await this._dataAccessor.GetOne<Merchant>("select wechat_ratio,ali_ratio,bank_ratio from `merchant` where `user_id`=@id",
                    p => p.Add("@id", MySqlDbType.Int64).Value = BitConverter.ToInt64(referrerId.DecodeWithBase64())) ?? new Merchant();
                    return this.Ok(new WebApiResult<Merchant>(merchant));
                }
            }
            else
            {
                Merchant merchant = await this._dataAccessor.GetOne<Merchant>("select * from `merchant` where `user_id`=@id",
                    p => p.Add("@id", MySqlDbType.Int64).Value = this.HttpContext.User.GetId());
                return this.Ok(new WebApiResult<Merchant>(merchant));
            }
        }

        [HttpGet("{userId}/Report")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "Agent,AgentMgmt,ReportReader")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<Report>>> GetOneReport(
            [FromRoute]long userId,
            [FromQuery]string startTime,
            [FromQuery]string endTime,
            [FromQuery]bool all = true,
            [FromQuery]string accountName = "",
            [FromQuery]long accountId = 0,
            [FromQuery]string accountNickName = "")
        {
            List<string> whereBuilder = new List<string>();
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            if (!string.IsNullOrEmpty(accountName))
            {
                whereBuilder.Add(" ua.`account_name` like @accountName");
                parameters.Add(new MySqlParameter("@accountName", MySqlDbType.VarChar) { Value = "%" + accountName + "%" });
            }

            if (accountId != 0)
            {
                whereBuilder.Add(" ua.`id` = @accountId");
                parameters.Add(new MySqlParameter("@accountName", MySqlDbType.Int64) { Value = accountId });
            }

            if (!string.IsNullOrEmpty(accountNickName))
            {
                whereBuilder.Add(" ua.`nick_name` = @accountNickName");
                parameters.Add(new MySqlParameter("@accountNickName", MySqlDbType.VarChar) { Value = accountNickName });
            }

            List<MerchantReport> childsData = new List<MerchantReport>();
            if (!all)
            {
                whereBuilder.Add(" ur.`is_direct` = 1");
            }
            whereBuilder.Add(" ur.`upper_level_id` = @upper_level_id");
            parameters.Add(new MySqlParameter("@upper_level_id", MySqlDbType.Int64) { Value = userId });
            
            List<long> childs = await _dataAccessor.GetAll("select ua.id from user_account as ua join user_relation as ur on ua.id=ur.user_id where " + string.Join(" and", whereBuilder),
                    new SimpleRowMapper<long>(async reader =>
                    {
                        if (await reader.IsDBNullAsync(0))
                        {
                            return 0;
                        }

                        return reader.GetInt64(0);
                    }), p => p.AddRange(parameters.ToArray()));

            foreach (long id in childs)
            {
                childsData.Add(await GetReport(id, startTime, endTime, userId));
            }

            return this.Ok(new WebApiResult<Report>(new Report()
            {
                SelfData = await GetReport(userId, startTime, endTime),
                ChildData = new DataPage<MerchantReport>()
                {
                    Records = childsData
                }
            }));
        }

        [HttpPost("WithDraw")]
        [Authorize(Roles = "Agent", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<MerchantWireOutOrder>>> WithDraw([FromBody] MerchantWireOutOrder mOrder, [FromQuery]string token, [FromQuery]string captcha)
        {
            UserAccount user = await this._dataAccessor.GetOne<UserAccount>("select * from `user_account` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = this.HttpContext.User.GetId());
            if (user == null)
            {
                return this.StatusCode(404, "merchant owner not found");
            }

            if (string.IsNullOrEmpty(captcha))
            {
                return this.BadRequest(this.CreateErrorResult<UserToken>(Constants.WebApiErrors.InvalidData, "captcha is required"));
            }

            // Check captcha
            string storedCaptcha = string.Empty;
            if (!string.IsNullOrEmpty(token))
            {
                storedCaptcha = await this._cache.GetStringAsync(token);
                await this._cache.RemoveAsync(token);
            }
            else
            {
                storedCaptcha = this.HttpContext.Session.GetString(Constants.Web.CaptchaCode);
            }

            if (!captcha.EqualsIgnoreCase(storedCaptcha))
            {
                return this.BadRequest(this.CreateErrorResult<UserToken>(Constants.WebApiErrors.InvalidData, "bad captcha"));
            }

            if (user.Balance * 100 >= mOrder.Amount)
            {
                mOrder.Status = WireOutOrderStatus.pending;
                mOrder.UserId = this.HttpContext.User.GetId();
                mOrder.CreateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
                {
                    await transaction.Execute(@"INSERT INTO `merchant_wireout_order`
                        (`status`,`user_id`,`operator`,`account_provider`,`account_holder`,`account_name`,`amount`,`create_time`)
                        VALUES(@status,@id,@operator,@provider,@holder,@name,@amount,@nowtime)",
                        p =>
                        {
                            p.Add("@status", MySqlDbType.Int32).Value = (int)mOrder.Status;
                            p.Add("@id", MySqlDbType.Int64).Value = mOrder.UserId;
                            p.Add("@operator", MySqlDbType.Int64).Value = mOrder.UserId;
                            p.Add("@provider", MySqlDbType.VarChar).Value = mOrder.AccountProvider;
                            p.Add("@holder", MySqlDbType.VarChar).Value = mOrder.AccountHolder;
                            p.Add("@name", MySqlDbType.VarChar).Value = mOrder.AccountName;
                            p.Add("@amount", MySqlDbType.Int32).Value = mOrder.Amount;
                            p.Add("@nowtime", MySqlDbType.Int64).Value = mOrder.CreateTimestamp;
                        });
                    mOrder.Id = await transaction.GetLastInsertId32();

                    await transaction.Execute("update `user_account` set `balance` = `balance` - @amount/100 where `id`=@id",
                        p =>
                        {
                            p.Add("@amount", MySqlDbType.Int32).Value = mOrder.Amount;
                            p.Add("@id", MySqlDbType.Int64).Value = mOrder.UserId;
                        });

                    // Add a transaction log
                    await transaction.Execute(
                        "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                        p =>
                        {
                            p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.WireOut;
                            p.Add("@paymentId", MySqlDbType.Int64).Value = mOrder.Id;
                            p.Add("@userId", MySqlDbType.Int64).Value = mOrder.UserId;
                            p.Add("@amount", MySqlDbType.Int64).Value = -mOrder.Amount;
                            p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                            p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance - mOrder.Amount / 100.0);
                            p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        });

                    await transaction.Commit();
                }
                return this.Ok(new WebApiResult<MerchantWireOutOrder>(mOrder));
            }
            else
            {
                return this.StatusCode(403, "balance is not enough");
            }
        }

        [HttpGet("WithDrawSettle/{orderId}")]
        [Authorize(Roles = "AgentSettler", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<MerchantWireOutOrder>>> WithDrawSettleInfo([FromRoute] long orderId)
        {
            return this.Ok(new WebApiResult<MerchantWireOutOrder>(await this._dataAccessor.GetOne<MerchantWireOutOrder>("select mw.*, ua.nick_name as name, ou.account_name as operator_name from `merchant_wireout_order` as mw join `user_account` as ua on mw.user_id = ua.id join `user_account` as ou on mw.operator=ou.id where mw.`id`=@id", p => p.Add("@id", MySqlDbType.Int64).Value = orderId)));
        }

        [HttpGet("WithDrawCount")]
        [Authorize(Roles = "AgentSettler", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<long>>> WithDrawSettle([FromQuery] WireOutOrderStatus? type = null)
        {
            if (type == null)
            {
                return this.Ok(new WebApiResult<long>((long)await this._dataAccessor.ExecuteScalar("select count(1) from `merchant_wireout_order`")));
            }
            else
            {
                return this.Ok(new WebApiResult<long>((long)await this._dataAccessor.ExecuteScalar("select count(1) from `merchant_wireout_order` where `status`=@status", p => p.Add("@status", MySqlDbType.Int32).Value = (int)type)));
            }
        }

        [HttpGet("WithDrawSettle")]
        [Authorize(Roles = "AgentSettler", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<DataPage<MerchantWireOutOrder>>>> WithDrawSettle(
            [FromQuery] long userId, 
            [FromQuery]int page, 
            [FromQuery] string startTime, 
            [FromQuery] string endTime, 
            [FromQuery] WireOutOrderStatus status = WireOutOrderStatus.all)
        {
            page = page < 1 ? 1 : page;
            long totalRecord = 0;
            long startTimestamp = startTime.ToTimeStamp(this._siteConfig.TimeZoneOffset);
            long endTimestamp = endTime.ToTimeStamp(this._siteConfig.TimeZoneOffset);
            if (startTimestamp == 0)
            {
                startTimestamp = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            sqlBuilder.Append("where mw.`create_time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (endTimestamp != 0)
            {
                sqlBuilder.Append(" and mw.`create_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = endTimestamp });
            }

            if (status != WireOutOrderStatus.all)
            {
                sqlBuilder.Append(" and mw.`status` = @status");
                parameters.Add(new MySqlParameter("@status", MySqlDbType.Int32) { Value = (int)status });
            }

            if (userId != 0)
            {
                sqlBuilder.Append(" and mw.`user_id` < @userId");
                parameters.Add(new MySqlParameter("@userId", MySqlDbType.Int64) { Value = userId });
            }
            else
            {
                totalRecord = (long)await this._dataAccessor.ExecuteScalar(
                    @"select count(1) from `merchant_wireout_order` as mw " + sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray()));
            }

            

            sqlBuilder.Append(" limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int32) { Value = (page - 1) * pageSize });

            List<MerchantWireOutOrder> orderlist = await this._dataAccessor.GetAll<MerchantWireOutOrder>("select mw.*, ua.nick_name as name, ou.account_name as operator_name from `merchant_wireout_order` as mw join `user_account` as ua on mw.user_id = ua.id join `user_account` as ou on mw.operator=ou.id " + sqlBuilder.ToString(), p => p.AddRange(parameters.ToArray()));

            return this.Ok(new WebApiResult<DataPage<MerchantWireOutOrder>>(new DataPage<MerchantWireOutOrder>()
            {
                Page = page,
                Records = orderlist,
                TotalPages = (int)Math.Ceiling(totalRecord / (double)pageSize),
                TotalRecords = totalRecord
            }));
        }

        [HttpPut("WithDrawSettle/{orderId}")]
        [Authorize(Roles = "AgentSettler", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult> WithDrawSettle([FromRoute] int orderId, [FromForm] WireOutOrderStatus status)
        {
            MerchantWireOutOrder order = await this._dataAccessor.GetOne<MerchantWireOutOrder>("select * from `merchant_wireout_order` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = orderId);
            if (order == null)
            {
                return this.StatusCode(404, "merchant order not found");
            }

            UserAccount operatorUser = await this._dataAccessor.GetOne<UserAccount>("select * from `user_account` where `id`=@id", p => p.Add("@id", MySqlDbType.Int64).Value = this.User.GetId());

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(@"update `merchant_wireout_order` set `operator`=@operator,`status`=@status,`settle_time`=@settle_time where `id`=@id;",
                    p =>
                    {
                        p.Add("@operator", MySqlDbType.Int64).Value = operatorUser.Id;
                        p.Add("@status", MySqlDbType.Int32).Value = (int)status;
                        p.Add("@settle_time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        p.Add("@id", MySqlDbType.Int32).Value = orderId;
                    });

                if (status == WireOutOrderStatus.settle)
                {
                    await transaction.Execute("update `user_account` set `pending_balance` = `pending_balance` + @amount/100 where `id`=@id",
                    p =>
                    {
                        p.Add("@amount", MySqlDbType.Int32).Value = order.Amount;
                        p.Add("@id", MySqlDbType.Int64).Value = operatorUser.Id;
                    });

                    // Add a transaction log
                    await transaction.Execute(
                        "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                        p =>
                        {
                            p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.WireOut;
                            p.Add("@paymentId", MySqlDbType.Int64).Value = order.Id;
                            p.Add("@userId", MySqlDbType.Int64).Value = operatorUser.Id;
                            p.Add("@amount", MySqlDbType.Int64).Value = order.Amount;
                            p.Add("@balBefore", MySqlDbType.Double).Value = operatorUser.Balance;
                            p.Add("@balAfter", MySqlDbType.Double).Value = (operatorUser.Balance + order.Amount / 100.0);
                            p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        });
                }
                await transaction.Commit();
            }
            return this.StatusCode(200, "merchant order success settle");
        }

        private async Task<MerchantReport> GetReport(long userId, string startTime, string endTime, long upperId = -1)
        {
            long startTimestamp = startTime.ToTimeStamp(this._siteConfig.TimeZoneOffset);
            long endTimestamp = endTime.ToTimeStamp(this._siteConfig.TimeZoneOffset);
            if (startTimestamp == 0)
            {
                startTimestamp = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            sqlBuilder.Append("where p.`create_time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (endTimestamp != 0)
            {
                sqlBuilder.Append(" and p.`create_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = endTimestamp });
            }

            StringBuilder userSeleter = new StringBuilder();
            List<MySqlParameter> userParameters = new List<MySqlParameter>();
            userSeleter.Append(" u.`id`=@uid");
            userParameters.Add(new MySqlParameter("@uid", MySqlDbType.Int64) { Value = userId });

            sqlBuilder.Append(" and" + userSeleter.ToString());
            parameters.AddRange(userParameters);
            MerchantData merchant = await this._dataAccessor.GetOne<MerchantData>(
                @"select u.`account_name`, u.`id` as user_id, u.`balance`, ur.`memo` as jobName, u.`role_id` as roleType, u.`nick_name`,
                        m.`wechat_ratio_static` as wechat_ratio, m.`ali_ratio_static` as ali_ratio, m.`bank_ratio_static` as bank_ratio 
                        from `user_account` as u
                        join `user_role` as ur on u.`role_id` = ur.`id` 
                        left join merchant as m on m.`user_id` = u.`id` where" + userSeleter.ToString(),
                p => p.AddRange(userParameters.ToArray())); ;
            List<ChannelData> channelDatas = new List<ChannelData>();
            if (merchant.RoleType == (int)UserRoleType.Agent)
            {
                if (upperId != -1)
                {
                    parameters.Add(new MySqlParameter("@upperId", MySqlDbType.Int64) { Value = upperId });
                }
                channelDatas = await this._dataAccessor.GetAll<ChannelData>(
                @"select sum(case when p.status = 3 then p.`amount` else 0 end) as amount," +
                        (upperId != -1 ? "sum(tl.amount)+0E0 as commission," : "sum(case when p.status = 3 then p.`amount` * p.`ratio` / 100 else 0 end) as commission,") +
                        @"count(if(p.status=3, 1, null)) as success, count(1) as total,
                        p.channel as channelType,
                        cc.name as channelName
                        from `payment` as p 
                        join `merchant` as m on m.`id` = p.`merchant_id`
                        join `user_account` as u on u.`id` = m.`user_id`
                        join `collect_channel` as cc on p.`channel` = cc.id "+
                        (upperId != -1 ? "join `transaction_log` as tl on tl.`payment_id` = p.id and tl.`amountFrom` = u.id and tl.`user_id` = @upperId " : "") + sqlBuilder.ToString() + " group by p.channel",
                p => p.AddRange(parameters.ToArray()));
            }

            return new MerchantReport()
            {
                Merchant = merchant,
                Channels = channelDatas
            };
        }

        public class Report
        {
            public DataPage<MerchantReport> ChildData { get; set; }

            public MerchantReport SelfData { get; set; }
        }

        public class WithDrawReport
        {
            public DataPage<MerchantReport> DataPage { get; set; }
        }

        public class MerchantData
        {   
            [Column("balance")]
            public double Balance { get; set; }

            [Column("user_id")]
            public long AccountId { get; set; }

            [Column("account_name")]
            public string AccountName { get; set; }

            [Column("nick_name")]
            public string NickName { get; set; }

            [Column("roleType")]
            public int RoleType { get; set; }

            [Column("jobName")]
            public string JobName { get; set; }

            [Column("wechat_ratio")]
            public double WechatRatio { get; set; }

            [Column("ali_ratio")]
            public double AliRatio { get; set; }

            [Column("bank_ratio")]
            public double BankRatio { get; set; }

            public string PromotionCode => BitConverter.GetBytes(this.AccountId).ToUrlSafeBase64();
        }

        public class ChannelData
        {
            [Column("amount")]
            public decimal Amount { get; set; }

            [Column("commission")]
            public double Commission { get; set; }

            [Column("success")]
            public long Success { get; set; }
            
            [Column("total")]
            public long Total { get; set; }

            [Column("channelType")]
            public CollectChannelType Type { get; set; }

            [Column("channelName")]
            public string Name { get; set; }
            
            public ChannelProvider Provider
            {
                get
                {
                    switch (Type)
                    {
                        case CollectChannelType.Ali:
                        case CollectChannelType.AliH5:
                        case CollectChannelType.AliWap:
                        case CollectChannelType.AliRed:
                            return ChannelProvider.AliPay;
                        case CollectChannelType.Wechat:
                        case CollectChannelType.WechatH5:
                        case CollectChannelType.WechatWap:
                            return ChannelProvider.Wechat;
                        default:
                            return ChannelProvider.Bank;
                    }
                }
            }
        }
        
        public class MerchantReport
        {
            public MerchantData Merchant { get; set; }
            
            public List<ChannelData> Channels { get; set; }
        }
    }
}
