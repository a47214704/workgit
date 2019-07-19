using Core;
using Core.Crypto;
using Core.Data;
using GroupPay.Models;
using GroupPay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private const int DefaultPageSize = 20;
        private readonly ILogger<UserController> _logger;
        private readonly DataAccessor _dataAccessor;
        private readonly IDistributedCache _cache;
        private readonly SiteConfig _config;
        private readonly AgencyCommissionService _agencyCommissionService;

        public UserController(ILoggerFactory loggerFactory, DataAccessor dataAccessor, IDistributedCache cache, SiteConfig siteConfig, AgencyCommissionService agencyCommissionService)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this._logger = loggerFactory.CreateLogger<UserController>();
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this._config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._agencyCommissionService = agencyCommissionService ?? throw new ArgumentNullException(nameof(agencyCommissionService));
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "UserReader,Agent,AgentMgmt")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<DataPage<UserAccount>>>> Get(
            [FromQuery]int pageSize,
            [FromQuery]int page,
            [FromQuery]string pageToken,
            [FromQuery]int roleId,
            [FromQuery]int status,
            [FromQuery]string accountName)
        {
            pageSize = pageSize > 0 ? pageSize : DefaultPageSize;
            page = page > 0 ? page : 0;
            int basePage = 0;
            long baseId = 0;
            if (!string.IsNullOrEmpty(pageToken))
            {
                string[] values = pageToken.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 2)
                {
                    int.TryParse(values[0], out basePage);
                    long.TryParse(values[1], out baseId);
                }
            }

            StringBuilder whereBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            if (!this.User.HasRole("UserReader"))
            {
                return this.StatusCode(403, "fail auth");
            }
            whereBuilder.Append(" where ua.`role_id`<>@agentType");
            parameters.Add(new MySqlParameter("@agentType", MySqlDbType.Int32) { Value = (int)UserRoleType.Agent });
            parameters.Add(new MySqlParameter("@commissionWeek", MySqlDbType.Int32) { Value = DateTimeOffset.UtcNow.AddDays(-7).ToDateValue(this._config.TimeZone) });
            parameters.Add(new MySqlParameter("@awardWeek", MySqlDbType.Int32) { Value = DateTimeOffset.UtcNow.AddDays(-1).ToDateValue(this._config.TimeZone) });
            
            
            if (roleId > 0)
            {
                whereBuilder.Append(" and ua.`role_id`=@roleId");
                parameters.Add(new MySqlParameter("@roleId", MySqlDbType.Int32) { Value = roleId });
            }

            if (status != 0)
            {
                whereBuilder.Append(" and ua.`status`=@status");
                parameters.Add(new MySqlParameter("@status", MySqlDbType.Int32) { Value = status });
            }

            List<UserAccount> summary = null;
            if (!string.IsNullOrEmpty(accountName))
            {
                whereBuilder.Append(" and ua.`account_name` like @accountName");
                parameters.Add(new MySqlParameter("@accountName", MySqlDbType.VarChar) { Value = "%" + accountName + "%" });
            }
            else
            {
                summary = await this._dataAccessor.GetAll<UserAccount>("SELECT ua.balance, ua.pending_balance,"
                + " sum(case when (ac.type = 1 and ac.week >= @commissionWeek) then ac.commission else 0 end) as commission, sum(case when (ac.type = 2 and ac.week =@awardWeek) then ac.commission else 0 end) as award"
                + " FROM user_account as ua left join agency_commission as ac on ua.id = ac.user_id and ac.cashed = 0 "
                + whereBuilder.ToString() + " group by ua.id",
                p => p.AddRange(parameters.ToArray()));
            }

            int totalPages = 0;
            long totalRecords = 0;

            totalRecords = (long)await this._dataAccessor.ExecuteScalar(
                "select count(*) from `user_account` as ua" + whereBuilder.ToString(),
                p => p.AddRange(parameters.ToArray()));

            totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            int deltaPage = page;
            if (baseId > 0 && page > basePage)
            {
                if (parameters.Count == 0)
                {
                    whereBuilder.Append(" where");
                }
                else
                {
                    whereBuilder.Append(" and");
                }

                whereBuilder.Append(" ua.`id`>@baseId");
                parameters.Add(new MySqlParameter("@baseId", MySqlDbType.Int64) { Value = baseId });
                deltaPage = page - (basePage + 1);
            }

            whereBuilder.Append(" group by ua.`id` order by ua.`id` limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int64) { Value = deltaPage > 0 ? deltaPage * pageSize : 0 });
            List<UserAccount> users = await this._dataAccessor.GetAll<UserAccount>(
                "select ua.*, sum(case when (ac.type = 1 and ac.week>=@commissionWeek) then ac.commission else 0 end) as commission, sum(case when (ac.type = 2 and ac.week=@awardWeek) then ac.commission else 0 end) as award from `user_account` as ua left join agency_commission as ac on ac.user_id=ua.id and ac.cashed=0" +
                whereBuilder.ToString(),
                p => p.AddRange(parameters.ToArray()));
            UserAccount lastRecord = users.LastOrDefault();
            return this.Ok(new WebApiResult<Report>(new Report()
            {
                DataPage = new DataPage<UserAccount>()
                {
                    Page = page,
                    TotalPages = totalPages,
                    Records = users,
                    TotalRecords = totalRecords,
                    PageToken = lastRecord != null ? (page + "," + lastRecord.Id) : string.Empty
                },
                Summary = summary
            }));
        }
        
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "SelfMgmt,UserReader")]
        public async Task<ActionResult<WebApiResult<UserAccount>>> Get([FromRoute]long id, [FromQuery]bool promotionUrl = false, [FromQuery]bool isAgent = false)
        {
            if (!this.HttpContext.User.HasRole("UserReader") && id != this.HttpContext.User.GetId())
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidCredentials, "access not allowed on target user"));
            }

            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                isAgent ? "select ua.*, m.wechat_ratio as m_wechat_ratio, m.ali_ratio as m_ali_ratio, m.bank_ratio as m_bank_ratio, m.channel_enabled as m_channel_enabled from user_account as ua join merchant as m on ua.id = m.user_id where ua.`id`=@userId" : "select * from user_account where `id`=@userId",
                p => p.Add("@userId", MySqlDbType.Int64).Value = id);
            if (user == null)
            {
                return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
            }

            user.UpperAccountName = await this._dataAccessor.GetOne("select ua.account_name from user_relation as ur" +
                " join user_account as ua on ua.id=ur.upper_level_id where ur.user_id=@id and ur.is_direct=1",
                new SimpleRowMapper<string>(async reader => {
                    if (await reader.IsDBNullAsync(0))
                    {
                        return "";
                    }
                    return reader.GetString(0);
                }),
                p => p.Add("@id", MySqlDbType.Int64).Value = user.Id);
            
            if (promotionUrl)
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
                {
                    user.PromotionUrl = this._config.RegisterUrl + "?referrer=" + user.PromotionCode;
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        using (HttpResponseMessage response = await client.GetAsync(this._config.UrlShortenService + "?url=" + user.PromotionUrl))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                WebApiResult<string> result = JsonConvert.DeserializeObject<WebApiResult<string>>(await response.Content.ReadAsStringAsync());
                                user.PromotionUrl = result.Result;
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        this._logger.LogError(CoreEvents.IncomingRequest.ServerError, err, "unableToCreateShortenUrl:{url}", user.PromotionUrl);
                    }
                }
            }

            return this.Ok(new WebApiResult<UserAccount>(user));
        }

        [HttpGet("Me")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "SelfMgmt")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<ActionResult<WebApiResult<UserAccount>>> GetMe([FromQuery]bool promotionUrl = false)
        {
            return this.Get(this.HttpContext.User.GetId(), promotionUrl);
        }

        [HttpHead]
        public async Task<ActionResult<WebApiResult<UserAccount>>> Head([FromQuery]string account, [FromQuery]string token, [FromQuery]string captcha)
        {
            if (string.IsNullOrEmpty(captcha))
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "captcha is required"));
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
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad captcha"));
            }

            UserAccount userAccount = await this._dataAccessor.GetOne<UserAccount>(
                "select * from `user_account` where `account_name`=@account",
                p => p.Add("@account", MySqlDbType.VarChar).Value = account);
            if (userAccount != null)
            {
                return this.Ok();
            }

            return this.NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<WebApiResult<UserAccount>>> Post([FromBody]UserAccount userAccount, [FromQuery]string token, [FromQuery]string captcha, [FromQuery]string referrer)
        {
            if (!userAccount.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            if (string.IsNullOrEmpty(captcha))
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "captcha is required"));
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
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad captcha"));
            }

            // check for existing
            List<UserAccount> existingOne = await this._dataAccessor.GetAll<UserAccount>(
            "select * from `user_account` where `account_name`=@account or `wechat_account`=@wechat",
            p =>
                {
                    p.Add("@account", MySqlDbType.VarChar).Value = userAccount.AccountName;
                    p.Add("@wechat", MySqlDbType.VarChar).Value = userAccount.WechatAccount;
                });

            if (existingOne.Any(item => item.AccountName == userAccount.AccountName))
            {
                return this.Conflict(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectConflict, "account already registered"));
            }

            if (existingOne.Any(item => item.WechatAccount == userAccount.WechatAccount) && userAccount.Merchant == null)
            {
                return this.Conflict(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectConflict, "wechat already registered"));
            }

            List<MySqlParameter> parameters = new List<MySqlParameter>();

            if (userAccount.Merchant == null)
            {
                // Check if the system is well configured
                UserRole role = await this._dataAccessor.GetOne<UserRole>(
                    "select * from `user_role` where `name`=@name",
                    p => p.Add("@name", MySqlDbType.VarChar).Value = this._config.UserRole);
                if (role == null)
                {
                    this._logger.LogWarning("UserRoleNotConfigured:{0}", this.TraceActivity());
                    return this.StatusCode(503, this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ServiceNotReady, "service is not yet configured"));
                }

                userAccount.Role = new UserRole()
                {
                    Id = role.Id
                };
                parameters.Add(new MySqlParameter("@roleId", MySqlDbType.Int32) { Value = role.Id });
            }
            else
            {
                parameters.Add(new MySqlParameter("@roleId", MySqlDbType.Int32) { Value = (int)UserRoleType.Agent });
                referrer = string.IsNullOrEmpty(referrer) ? BitConverter.GetBytes(this.HttpContext.User.GetId()).ToUrlSafeBase64() : referrer; 
            }

            userAccount.CreateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            userAccount.PasswordLastSet = userAccount.CreateTimestamp;
            parameters.Add(new MySqlParameter("@accountName", MySqlDbType.VarChar) { Value = userAccount.AccountName });
            parameters.Add(new MySqlParameter("@password", MySqlDbType.VarChar) { Value = userAccount.Password.ToSha256().ToHexString() });
            parameters.Add(new MySqlParameter("@wechaAccount", MySqlDbType.VarChar) { Value = userAccount.WechatAccount });
            parameters.Add(new MySqlParameter("@nickName", MySqlDbType.VarChar) { Value = userAccount.NickName });
            parameters.Add(new MySqlParameter("@createTime", MySqlDbType.Int64) { Value = userAccount.CreateTimestamp });
            parameters.Add(new MySqlParameter("@pwdLastSet", MySqlDbType.Int64) { Value = userAccount.PasswordLastSet });

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(
                    "insert into `user_account`(`role_id`, `account_name`, `password`, `wechat_account`, `create_time`, `password_last_set`, `nick_name`) values(@roleId, @accountName, @password, @wechaAccount, @createTime, @pwdLastSet, @nickName)",
                    p => p.AddRange(parameters.ToArray()));

                userAccount.Id = await transaction.GetLastInsertId();

                if (userAccount.Merchant != null)
                {
                    await transaction.Execute(
                        "insert INTO `merchant`(`name`,`app_key`,`app_pwd`,`user_id`,`wechat_ratio`,`ali_ratio`,`bank_ratio`,`channel_enabled`,`channel_limit`)VALUES(@nickName,@app_key,@app_pwd,@user_id,@wechatRatio,@aliRatio,@bankRatio,@channelEnabled,@channelLimit)",
                        p =>
                        {
                            p.Add("@nickName", MySqlDbType.VarChar).Value = userAccount.NickName;
                            p.Add("@app_pwd", MySqlDbType.VarChar).Value = CryptoExtensions.ToHexString(CryptoExtensions.ToSha256(userAccount.Id.ToString() + DateTime.Now.ToString()));
                            p.Add("@app_key", MySqlDbType.VarChar).Value = CryptoExtensions.ToHexString(CryptoExtensions.ToMd5(userAccount.Id.ToString()));
                            p.Add("@user_id", MySqlDbType.Int64).Value = userAccount.Id;
                            p.Add("@wechatRatio", MySqlDbType.Double).Value = userAccount.Merchant.WechatRatio;
                            p.Add("@aliRatio", MySqlDbType.Double).Value = userAccount.Merchant.AliRatio;
                            p.Add("@bankRatio", MySqlDbType.Double).Value = userAccount.Merchant.BankRatio;
                            p.Add("@channelEnabled", MySqlDbType.Int32).Value = userAccount.Merchant.ChannelEnabled;
                            p.Add("@channelLimit", MySqlDbType.VarChar).Value = "";
                        });
                }

                if (!string.IsNullOrEmpty(referrer))
                {
                    try
                    {
                        long referrerId = BitConverter.ToInt64(referrer.DecodeWithBase64());
                        UserAccount user = await this._dataAccessor.GetOne<UserAccount>("select * from `user_account` where `id`=@id", p => p.Add("@id", MySqlDbType.Int64).Value = referrerId);
                        if (user == null)
                        {
                            this._logger.LogWarning("invalidReferrer:{0},{1}", this.TraceActivity(), referrerId);
                        }
                        else
                        {
                            await transaction.Execute(
                                "insert into `user_relation`(`user_id`, `upper_level_id`, `is_direct`) values(@userId, @upperLevelId, 1)",
                                p =>
                                {
                                    p.Add("@userId", MySqlDbType.Int64).Value = userAccount.Id;
                                    p.Add("@upperLevelId", MySqlDbType.Int64).Value = referrerId;
                                });
                            await transaction.Execute(
                                "insert into `user_relation`(`user_id`, `upper_level_id`, `is_direct`) select @userId as `user_id`, `upper_level_id`, 0 as `is_direct` from `user_relation` where `user_id`=@id",
                                p =>
                                {
                                    p.Add("@userId", MySqlDbType.Int64).Value = userAccount.Id;
                                    p.Add("@id", MySqlDbType.Int64).Value = referrerId;
                                });
                            if (!user.HasSubAccounts)
                            {
                                await transaction.Execute(
                                    "update `user_account` set `has_sub_account`=1 where `id`=@id",
                                    p => p.Add("@id", MySqlDbType.Int64).Value = referrerId);
                            }

                        }
                    }
                    catch (Exception exception)
                    {
                        this._logger.LogWarning("failedToSaveReferrer:{0},{1}", this.TraceActivity(), exception);
                    }
                }
                await transaction.Commit();
            }

            
            return this.Ok(new WebApiResult<UserAccount>(Constants.WebApiErrors.RequiredMoreData, userAccount));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SelfMgmt,UserWriter,Agent,AgentMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserAccount>>> Put([FromRoute]long id, [FromBody]UserAccount userAccount)
        {
            bool adminRole = this.HttpContext.User.HasRole("UserWriter") || this.HttpContext.User.HasRole("Agent") || this.HttpContext.User.HasRole("AgentMgmt");
            if (!adminRole && id != this.HttpContext.User.GetId())
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidCredentials, "access not allowed on target user"));
            }
            
            long userId = adminRole ? id : HttpContext.User.GetId();
            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                this.HttpContext.User.HasRole("Agent") ? "select ua.* from `user_account` as ua join `user_relation` as ur on ua.`id` = ur.`user_id` where ua.`id`=@id and (ur.`upper_level_id`=@uid or ur.`user_id`=@uid)" : "select * from `user_account` where `id`=@id",
                p => 
                {
                    p.Add("@id", MySqlDbType.Int64).Value = userId;
                    if (this.HttpContext.User.HasRole("Agent"))
                    {
                        p.Add("@uid", MySqlDbType.Int64).Value = this.HttpContext.User.GetId();
                    }
                });

            if (user == null)
            {
                return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
            }
            
            user.Status = userAccount.Status != AccountStatus.None ? userAccount.Status : user.Status;
            user.Role.Id = (userAccount.Role == null || userAccount.Role.Id == 0) ? user.Role.Id : userAccount.Role.Id;
            user.MerchantId = userAccount.MerchantId == 0 ? user.MerchantId : userAccount.MerchantId;
            user.NickName = string.IsNullOrEmpty(userAccount.NickName) ? user.NickName : userAccount.NickName;
            user.Email = string.IsNullOrEmpty(userAccount.Email) ? user.Email : userAccount.Email;

            StringBuilder sqlBuilder = new StringBuilder("update `user_account` set `status` = @status, `role_id` = @roleId, `merchant_id` = @merchant_id , `nick_name` = @nickName");

            List<MySqlParameter> parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@status", MySqlDbType.Int32) { Value = user.Status },
                new MySqlParameter("@roleId", MySqlDbType.Int32) { Value = user.Role.Id },
                new MySqlParameter("@merchant_id", MySqlDbType.Int32) { Value = user.MerchantId },
                new MySqlParameter("@nickName", MySqlDbType.VarChar) { Value = user.NickName }
            };

            if (!string.IsNullOrEmpty(userAccount.Password))
            {
                sqlBuilder.Append(", `password` = @password");
                parameters.Add(new MySqlParameter("@password", MySqlDbType.VarChar) { Value = userAccount.Password.ToSha256().ToHexString() });
            }
            
            sqlBuilder.Append(" where `id` = @id");
            parameters.Add(new MySqlParameter("@id", MySqlDbType.Int64) { Value = userId });

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                if (user.Role.Id == (int)UserRoleType.Agent && userAccount.Merchant != null)
                {
                    await transaction.Execute(
                        "update `merchant` set `name`=@name,`wechat_ratio`=@wechat_ratio,`ali_ratio`=@ali_ratio,`bank_ratio`=@bank_ratio,`channel_enabled`=@channelEnabled where `user_id`=@id",
                        p =>
                        {
                            p.Add("@name", MySqlDbType.VarChar).Value = user.NickName;
                            p.Add("@wechat_ratio", MySqlDbType.Double).Value = userAccount.Merchant.WechatRatio;
                            p.Add("@ali_ratio", MySqlDbType.Double).Value = userAccount.Merchant.AliRatio;
                            p.Add("@bank_ratio", MySqlDbType.Double).Value = userAccount.Merchant.BankRatio;
                            p.Add("@channelEnabled", MySqlDbType.Int32).Value = userAccount.Merchant.ChannelEnabled;
                            p.Add("@id", MySqlDbType.Int64).Value = user.Id;
                        });
                }

                await transaction.Execute(sqlBuilder.ToString(), p => p.AddRange(parameters.ToArray()));
                await transaction.Commit();
            }
                
            return this.Ok(new WebApiResult<UserAccount>(user));
        }
        
        [HttpPost("{id}/AddCredit")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "BalanceMgr")]
        public async Task<ActionResult<WebApiResult<UserAccount>>> AddCredit([FromRoute]long id, [FromBody]AddCreditRequest request)
        {
            if (request == null || Math.Abs(request.Credit) < 0.001)
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                "select * from `user_account` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = id);
            if (user == null)
            {
                return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
            }

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(
                    "update `user_account` set `balance`=`balance`+@credit where `id`=@id",
                    p =>
                    {
                        p.Add("@credit", MySqlDbType.Double).Value = request.Credit;
                        p.Add("@id", MySqlDbType.Int64).Value = user.Id;
                    });

                // Add a transaction log
                await transaction.Execute(
                    "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `operator_id`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @operator)",
                    p =>
                    {
                        p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Modify;
                        p.Add("@paymentId", MySqlDbType.Int64).Value = 0;
                        p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                        p.Add("@amount", MySqlDbType.Int32).Value = (int)(request.Credit * 100);
                        p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                        p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance + request.Credit);
                        p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        p.Add("@operator", MySqlDbType.Int64).Value = this.HttpContext.User.GetId();
                    });
                await transaction.Commit();
            }

            user.Balance += request.Credit;
            return this.Ok(new WebApiResult<UserAccount>(user));
        }

        [HttpPost("{id}/SecurityAnswers")]
        [Authorize(Roles = "SelfMgmt,UserWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserAccount>>> SaveSecurityAnswers([FromRoute]long id, [FromBody]List<SecurityAnswer> answers)
        {
            if (answers == null || answers.Count == 0)
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            bool adminRole = this.HttpContext.User.HasRole("UserWriter");
            if (!adminRole && id != this.HttpContext.User.GetId())
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidCredentials, "access not allowed on target user"));
            }

            long userId = adminRole ? id : HttpContext.User.GetId();
            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                "select * from `user_account` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = userId);
            if (user == null)
            {
                return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
            }

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                foreach (SecurityAnswer answer in answers)
                {
                    SecurityQuestion question = await this._dataAccessor.GetOne<SecurityQuestion>(
                        "select * from `security_question` where `id`=@id",
                        p => p.Add("@id", MySqlDbType.Int64).Value = answer.Question.Id);
                    if (question == null)
                    {
                        return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad question id " + answer.Question.Id.ToString()));
                    }

                    SecurityAnswer existingOne = await transaction.GetOne<SecurityAnswer>(
                        "select * from `security_answer` where `user_id`=@userId and `question_id`=@questionId",
                        p =>
                        {
                            p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                            p.Add("@questionId", MySqlDbType.Int32).Value = question.Id;
                        });
                    if (existingOne != null)
                    {
                        await transaction.Execute(
                            "update `security_answer` set `answer`=@answer where `id`=@id",
                            p =>
                            {
                                p.Add("@answer", MySqlDbType.VarChar).Value = answer.Answer;
                                p.Add("@id", MySqlDbType.Int64).Value = existingOne.Id;
                            });
                    }
                    else
                    {
                        await transaction.Execute(
                            "insert into `security_answer`(`user_id`, `question_id`, `answer`) values(@userId, @qid, @answer)",
                            p =>
                            {
                                p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                                p.Add("@qid", MySqlDbType.Int32).Value = question.Id;
                                p.Add("@answer", MySqlDbType.VarChar).Value = answer.Answer;
                            });
                    }
                }

                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<UserAccount>(user));
        }

        [HttpPost("{id}/UpdatePassword")]
        [Authorize(Roles = "SelfMgmt,CredMgr", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserAccount>>> UpdatePassword([FromRoute]long id, [FromBody]UpdatePasswordRequest updateRequest)
        {
            if (updateRequest == null ||
                string.IsNullOrEmpty(updateRequest.OldPassword) ||
                string.IsNullOrEmpty(updateRequest.NewPassword))
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            bool adminRole = this.HttpContext.User.HasRole("CredMgr");
            if (!adminRole && id != this.HttpContext.User.GetId())
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidCredentials, "access not allowed on target user"));
            }

            long userId = adminRole ? id : HttpContext.User.GetId();
            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                "select * from `user_account` where `id`=@id and `password`=@password",
                p =>
                {
                    p.Add("@id", MySqlDbType.Int64).Value = this.HttpContext.User.GetId();
                    p.Add("@password", MySqlDbType.VarChar).Value = updateRequest.OldPassword.ToSha256().ToHexString();
                });
            if (user == null)
            {
                return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
            }

            await this._dataAccessor.Execute(
                "update `user_account` set `password`=@password, `password_last_set`=@pwdLastSet where `id`=@id",
                p =>
                {
                    p.Add("@password", MySqlDbType.VarChar).Value = updateRequest.NewPassword.ToSha256().ToHexString();
                    p.Add("@pwdLastSet", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    p.Add("@id", MySqlDbType.Int64).Value = this.HttpContext.User.GetId();
                });
            return this.Ok(new WebApiResult<UserAccount>(user));
        }

        [HttpPost("Login")]
        public async Task<ActionResult<WebApiResult<UserToken>>> Login([FromBody]UserAccount userAccount, [FromQuery]string token, [FromQuery]string captcha)
        {
            if (userAccount == null || string.IsNullOrEmpty(userAccount.AccountName) || string.IsNullOrEmpty(userAccount.Password))
            {
                return this.BadRequest(this.CreateErrorResult<UserToken>(Constants.WebApiErrors.InvalidData, "invalid request data"));
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

            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                "select * from `user_account` where `account_name`=@accountName and `password`=@password",
                p =>
                {
                    p.Add("@accountName", MySqlDbType.VarChar).Value = userAccount.AccountName;
                    p.Add("@password", MySqlDbType.VarChar).Value = userAccount.Password.ToSha256().ToHexString();
                });
            if (user == null)
            {
                return this.Unauthorized(this.CreateErrorResult<UserToken>(Constants.WebApiErrors.InvalidCredentials, "invalid username or bad password"));
            }

            if (user.Status == AccountStatus.NotYetVerified)
            {
                return this.Unauthorized(this.CreateErrorResult<UserToken>(Constants.WebApiErrors.InvalidCredentials, "user account not yet verified"));
            }

            await this._dataAccessor.Execute(
                "update `user_account` set `last_login_timestamp`=@lastLogin where `id`=@id",
                p =>
                {
                    p.Add("@lastLogin", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    p.Add("@id", MySqlDbType.Int64).Value = user.Id;
                });

            await this._dataAccessor.Execute(
                "insert into `login_log`(`user_id`, `timestamp`, `browser`, `ip`) values(@userId, @ts, @browser, @ip)",
                p =>
                {
                    p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                    p.Add("@ts", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    p.Add("@browser", MySqlDbType.VarChar).Value = this.Request.Headers["User-Agent"].ToString();
                    p.Add("@ip", MySqlDbType.VarChar).Value = this.HttpContext.RemoteAddr();
                });

            List<string> perms = await this._dataAccessor.GetAll<string>(
                "select `perm` from `role_perm` where `role_id`=@roleId",
                new SimpleRowMapper<string>((reader) => Task.FromResult(reader.GetString(0))),
                p => p.Add("@roleId", MySqlDbType.Int32).Value = user.Role.Id);
            StringBuilder ticketBuilder = new StringBuilder();
            ticketBuilder.Append(user.Id);
            ticketBuilder.Append(',');
            ticketBuilder.Append(user.AccountName);
            ticketBuilder.Append(',');
            ticketBuilder.Append(user.Role.Id);
            ticketBuilder.Append(',');
            ticketBuilder.Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (perms != null && perms.Count > 0)
            {
                foreach (string perm in perms)
                {
                    ticketBuilder.Append(',');
                    ticketBuilder.Append(perm);
                }
            }

            List<SecurityAnswer> answers = await this._dataAccessor.GetAll<SecurityAnswer>(
                "select * from `security_answer` where `user_id`=@userId",
                p => p.Add("@userId", MySqlDbType.Int64).Value = user.Id);

            string ticket = Encoding.UTF8.GetBytes(ticketBuilder.ToString()).EncryptWithAes256(this._config.SecretKey, user.Id.ToString()).ToUrlSafeBase64();
            string error = Constants.WebApiErrors.Success;
            if (user.PasswordLastSet < this._config.ForcePwdChangeBefore)
            {
                error = Constants.WebApiErrors.PasswordChangeRequired;
            }
            else if (answers.Count == 0)
            {
                error = Constants.WebApiErrors.RequiredMoreData;
            }

            return this.Ok(new WebApiResult<UserToken>(error, new UserToken
            {
                UserId = user.Id,
                Token = user.Id.ToString() + "." + ticket
            }));
        }

        [HttpGet("{id}/Team")]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<TeamReport>>> GetTeam([FromRoute]long id)
        {
            long userId = this.HttpContext.User.GetId();
            if (userId != id)
            {
                return this.BadRequest(this.CreateErrorResult<TeamReport>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "GetTeamReport", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                TeamReport teamReport = (await this._dataAccessor.GetOne<TeamReport>(
                    context,
                    "select cast(sum(case when ua.`has_sub_account` = 0 then 1 else 0 end) as signed) as m_total," +
                    "cast(sum(case when ua.`has_sub_account` = 0 and ua.`create_time` >= @wts then 1 else 0 end) as signed) as m_this_week," +
                    "cast(sum(case when ua.`has_sub_account` = 0 and ua.`create_time` >= @wts then 1 else 0 end) as signed) as m_this_month," +
                    "cast(sum(case when ua.`has_sub_account` = 1 then 1 else 0 end) as signed) as a_total," +
                    "cast(sum(case when ua.`has_sub_account` = 1 and ua.`create_time` >= @mts then 1 else 0 end) as signed) as a_this_week," +
                    "cast(sum(case when ua.`has_sub_account` = 1 and ua.`create_time` >= @mts then 1 else 0 end) as signed) as a_this_month" +
                    " from `user_account` as ua join user_relation as ur on ua.`id`=ur.`user_id` where ur.`upper_level_id`=@uid and ur.`is_direct`=1",
                    p =>
                    {
                        p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        p.Add("@wts", MySqlDbType.Int64).Value = now.RoundWeek(this._config.TimeZone).ToUnixTimeMilliseconds();
                        p.Add("@mts", MySqlDbType.Int64).Value = now.RoundMonth(this._config.TimeZone).ToUnixTimeMilliseconds();
                    })) ?? new TeamReport();
                teamReport.Total = await this._dataAccessor.GetOne(
                    context,
                    "select count(`user_id`) from `user_relation` where `upper_level_id`=@uid",
                    new SimpleRowMapper<int>(async reader =>
                    {
                        if (await reader.IsDBNullAsync(0))
                        {
                            return 0;
                        }

                        return reader.GetInt32(0);
                    }),
                    p => p.Add("@uid", MySqlDbType.Int64).Value = userId);
                return this.Ok(new WebApiResult<TeamReport>(teamReport));
            }
        }

        [HttpGet("{id}/Members")]
        [Authorize(Roles = "SelfMgmt,UserReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<DataPage<UserAccount>>>> ListDirectMembers(
            [FromRoute]long id,
            [FromQuery]int page,
            [FromQuery]string pageToken)
        {
            long userId = this.HttpContext.User.GetId();
            if (userId != id && !this.HttpContext.User.HasRole("UserReader"))
            {
                return this.BadRequest(this.CreateErrorResult<DataPage<UserAccount>>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "ListDirectMembers", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                return await this.GetDirectUserAccounts(context, Constants.Web.DefaultPageSize, page, pageToken, id, false);
            }
        }

        [HttpGet("{id}/Agents")]
        [Authorize(Roles = "SelfMgmt,UserReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<DataPage<UserAccount>>>> ListDirectAgents(
            [FromRoute]long id,
            [FromQuery]int page,
            [FromQuery]string pageToken)
        {
            long userId = this.HttpContext.User.GetId();
            if (userId != id && !this.HttpContext.User.HasRole("UserReader"))
            {
                return this.BadRequest(this.CreateErrorResult<DataPage<UserAccount>>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "ListDirectAgents", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                return await this.GetDirectUserAccounts(context, Constants.Web.DefaultPageSize, page, pageToken, id, true);
            }
        }

        [HttpGet("{id}/Revenue")]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<RevenueReport>>> GetRevenueReport([FromRoute]long id)
        {
            long userId = this.HttpContext.User.GetId();
            if (userId != id)
            {
                return this.BadRequest(this.CreateErrorResult<RevenueReport>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "GetRevenueReport", this.TraceActivity()))
            {
                DateTimeOffset today = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone);
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                using (DataTransaction transaction = await this._dataAccessor.CreateTransaction(context))
                {
                    return this.Ok(new WebApiResult<RevenueReport>(await _agencyCommissionService.GenerateRevenueReport(transaction, userId, today.ToUnixTimeMilliseconds())));
                }
            }
        }

        [HttpGet("{id}/CommissionBalance")]
        [Authorize(Roles = "SelfMgmt,UserReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<AgencyCommission>>> GetCommissionBalance([FromRoute]long id)
        {
            long userId = this.HttpContext.User.GetId();
            if (this.HttpContext.User.HasRole("UserReader"))
            {
                userId = id;
            }

            if (userId != id)
            {
                return this.BadRequest(this.CreateErrorResult<AgencyCommission>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "GetCommissionBalance", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                int earliestDate = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone).AddDays(-7).ToDateValue(this._config.TimeZone);

                double totalCommission = await this._dataAccessor.GetOne(
                        context,
                        "select sum(`commission`) as commission from `agency_commission` where `user_id`=@uid and `cashed`=0 and `type` = 1 and `week` >= @earliestDate",
                        new SimpleRowMapper<double>(async reader =>
                        {
                            if (await reader.IsDBNullAsync(0))
                            {
                                return 0d;
                            }

                            return reader.GetDouble(0);
                        }),
                        p => {
                            p.Add("@uid", MySqlDbType.Int64).Value = userId;
                            p.Add("@earliestDate", MySqlDbType.Int32).Value = earliestDate;
                        });
                return this.Ok(new WebApiResult<AgencyCommission>(new AgencyCommission
                {
                    UserId = userId,
                    Commission = totalCommission,
                    Cashed = false
                }));
            }
        }

        [HttpGet("{id}/CommissionCashRecords")]
        [Authorize(Roles = "SelfMgmt,UserReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<AgencyCommission>>>> ListCommissionCashRecords([FromRoute]long id)
        {
            long userId = this.HttpContext.User.GetId();
            if (this.HttpContext.User.HasRole("UserReader"))
            {
                userId = id;
            }

            if (userId != id)
            {
                return this.BadRequest(this.CreateErrorResult<AgencyCommission>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "ListCommissionCashRecords", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset thisWeek = now.RoundWeek(this._config.TimeZone);
                DateTimeOffset earliestWeek = thisWeek.AddDays(-7 * this._config.UserLogsWindow);
                int earliestWeekDate = earliestWeek.ToDateValue(this._config.TimeZone);
                List<AgencyCommission> records = await this._dataAccessor.GetAll<AgencyCommission>(
                    context,
                    "select * from `agency_commission` where `user_id`=@uid and `week`>=@week and `cashed`=1 and type=1 and `commission` > 0.001",
                    p =>
                    {
                        p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        p.Add("@week", MySqlDbType.Int32).Value = earliestWeekDate;
                    });

                return this.Ok(new WebApiResult<List<AgencyCommission>>(records));
            }
        }

        [HttpPost("{id}/CashCommission")]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserAccount>>> CashCommission([FromRoute]long id)
        {
            long userId = this.HttpContext.User.GetId();
            if (userId != id)
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidCredentials, "bad user id"));
            }
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "GetCommissionBalance", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                using (DataTransaction transaction = await this._dataAccessor.CreateTransaction(context))
                {
                    UserAccount userAccount = await transaction.GetOne<UserAccount>(
                        "select * from `user_account` where `id`=@uid for update",
                        p =>
                        {
                            p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        });
                    if (userAccount == null)
                    {
                        return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user account not found"));
                    }
                    double totalCommission = await transaction.GetOne(
                                "select sum(`commission`) as commission from `agency_commission` where `user_id`=@uid and `cashed`=0 and `type`=1",
                                new SimpleRowMapper<double>(async reader =>
                                {
                                    if (await reader.IsDBNullAsync(0))
                                    {
                                        return 0d;
                                    }

                                    return reader.GetDouble(0);
                                }),
                                p => p.Add("@uid", MySqlDbType.Int64).Value = userId);
                    if (totalCommission < 1)
                    {
                        return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "no commission"));
                    }

                    await transaction.Execute(
                        "update `agency_commission` set `cashed`=1, `cash_time`=@ts where `user_id`=@uid and `cashed`=0 and `type`=1",
                        p =>
                        {
                            p.Add("@ts", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            p.Add("@uid", MySqlDbType.Int64).Value = userId;
                        });
                    await transaction.Execute(
                        "update `user_account` set `balance`=`balance`+@balance where `id`=@uid",
                        p =>
                        {
                            p.Add("@uid", MySqlDbType.Int64).Value = userId;
                            p.Add("@balance", MySqlDbType.Int64).Value = (int)(totalCommission);
                        });

                    // Add a transaction log
                    await transaction.Execute(
                        "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `operator_id`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @operator)",
                        p =>
                        {
                            p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Commission;
                            p.Add("@paymentId", MySqlDbType.Int64).Value = 0;
                            p.Add("@userId", MySqlDbType.Int64).Value = userId;
                            p.Add("@amount", MySqlDbType.Int32).Value = (int)(totalCommission * 100);
                            p.Add("@balBefore", MySqlDbType.Double).Value = userAccount.Balance;
                            p.Add("@balAfter", MySqlDbType.Double).Value = (userAccount.Balance + totalCommission);
                            p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            p.Add("@operator", MySqlDbType.Int64).Value = 0;
                        });
                    await transaction.Commit();
                    userAccount.Balance += totalCommission;
                    return this.Ok(new WebApiResult<UserAccount>(userAccount));
                }
            }
        }

        [HttpPost("{id}/Evaluation")]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserAccount>>> Evaluation(
            [FromRoute]long id,
            [FromForm]int point,
            [FromForm]UserEvaluationType type,
            [FromForm]string note,
            [FromForm]string captcha)
        {
            // Check captcha
            string storedCaptcha = this.HttpContext.Session.GetString(Constants.Web.CaptchaCode);

            if (!captcha.EqualsIgnoreCase(storedCaptcha))
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad captcha"));
            }

            UserAccount user = null;
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                user = await transaction.GetOne<UserAccount>("select * from user_account where id=@uid",
                            p => p.Add("@uid", MySqlDbType.Int64).Value = id);

                if (user == null)
                {
                    this._logger.LogInformation($"user:{id} is not found");
                    return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
                }

                //modify point
                await transaction.Execute("update `user_account` set `evaluation_point`=`evaluation_point`+@point where id=@uid",
                    p =>
                    {
                        p.Add("@point", MySqlDbType.Int32).Value = point;
                        p.Add("@uid", MySqlDbType.Int64).Value = id;
                    });

                // Add a point log
                await transaction.Execute(
                    "insert into `evaluation_log`(`type`, `user_id`, `point`, `point_before`, `point_after`, `time`, `note`) values(@type, @userId, @point, @pointBefore, @pointAfter, @time, @note)",
                    p =>
                    {
                        p.Add("@type", MySqlDbType.Int32).Value = (int)type;
                        p.Add("@userId", MySqlDbType.Int64).Value = id;
                        p.Add("@point", MySqlDbType.Int32).Value = point;
                        p.Add("@pointBefore", MySqlDbType.Int32).Value = user.Point;
                        p.Add("@pointAfter", MySqlDbType.Int32).Value = user.Point + point;
                        p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        p.Add("@note", MySqlDbType.VarChar).Value = note;
                    });
                
                await transaction.Commit();
                user.Point = user.Point + point;
            }
            return this.Ok(new WebApiResult<UserAccount>(user));
        }

        private async Task<ActionResult<WebApiResult<DataPage<UserAccount>>>> GetDirectUserAccounts(CallContext callContext, int pageSize, int page, string pageToken, long userId, bool listAgent)
        {
            pageSize = pageSize > 0 ? pageSize : Constants.Web.DefaultPageSize;
            page = page > 0 ? page : 0;
            int basePage = 0;
            long baseId = 0;
            if (!string.IsNullOrEmpty(pageToken))
            {
                string[] values = pageToken.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 2)
                {
                    int.TryParse(values[0], out basePage);
                    long.TryParse(values[1], out baseId);
                }
            }

            int totalPages = (int)Math.Ceiling((long)await this._dataAccessor.ExecuteScalar(
                callContext,
                "select count(ua.`id`) from `user_account` as ua join `user_relation` as ur on ua.`id`=ur.`user_id` where `ur`.`upper_level_id`=@uid and ur.`is_direct`=1 and ua.`has_sub_account`=@listAgent",
                p =>
                {
                    p.Add("@uid", MySqlDbType.Int64).Value = userId;
                    p.Add("@listAgent", MySqlDbType.Bit).Value = listAgent;
                }) / (double)pageSize); ;


            StringBuilder sqlBuilder = new StringBuilder("select ua.`id`, ua.`account_name`, ua.`nick_name`, ua.`balance` from `user_account` as ua join `user_relation` as ur on ua.`id`=ur.`user_id` where `ur`.`upper_level_id`=@uid and ur.`is_direct`=1 and ua.`has_sub_account`=@listAgent");
            List<MySqlParameter> parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@uid", MySqlDbType.Int64){ Value = userId },
                new MySqlParameter("@listAgent", MySqlDbType.Bit){ Value = listAgent }
            };

            long totalRecords = (long)await this._dataAccessor.ExecuteScalar("select count(1) from `user_account` as ua join `user_relation` as ur on ua.`id`=ur.`user_id` where `ur`.`upper_level_id`=@uid and ur.`is_direct`=1",p=>p.Add("uid", MySqlDbType.Int64).Value = userId);

            int deltaPage = page;
            if (baseId > 0 && page > basePage)
            {
                sqlBuilder.Append(" and");
                sqlBuilder.Append(" ua.`id`>@baseId");
                parameters.Add(new MySqlParameter("@baseId", MySqlDbType.Int64) { Value = baseId });
                deltaPage = page - (basePage + 1);
            }

            sqlBuilder.Append(" order by ua.`id` limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int64) { Value = deltaPage > 0 ? deltaPage * pageSize : 0 });
            List<UserAccount> users = await this._dataAccessor.GetAll<UserAccount>(
                sqlBuilder.ToString(),
                p => p.AddRange(parameters.ToArray()));
            UserAccount lastRecord = users.LastOrDefault();
            return this.Ok(new WebApiResult<DataPage<UserAccount>>(new DataPage<UserAccount>
            {
                Page = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Records = users,
                PageToken = lastRecord != null ? (page + "," + lastRecord.Id) : string.Empty
            }));
        }
        
        public class AddCreditRequest
        {
            public double Credit { get; set; }
        }

        public class Report
        {
            public DataPage<UserAccount> DataPage { get; set; }

            public List<UserAccount> Summary { get; set; }
        }
        
    }
}
