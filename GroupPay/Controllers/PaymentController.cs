using Core;
using Core.Crypto;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GroupPay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, int> s_paymentAmountSequence = new ConcurrentDictionary<string, int>();

        private const int pageSize = 20;
        private readonly DataAccessor _dataAccessor;
        private readonly PaymentDispatcher _dispatcher;
        private readonly ILogger<PaymentController> _logger;
        private readonly IDistributedCache _cache;
        private readonly int _pendingTimeout;
        private readonly int _settleTimeout;
        private readonly SiteConfig _siteConfig;
        private readonly HttpClient _httpClient = new HttpClient(
            new HttpClientHandler()
            {
                UseCookies = false,
                MaxConnectionsPerServer = 100,
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = (arg1, arg2, arg3, arg4) => true
            });
        private readonly Random _random = new Random();

        public PaymentController(DataAccessor dataAccessor, ILoggerFactory loggerFactory, PaymentDispatcher paymentDispatcher, SiteConfig siteConfig, IDistributedCache cache)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (siteConfig == null)
            {
                throw new ArgumentNullException(nameof(siteConfig));
            }

            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._logger = loggerFactory.CreateLogger<PaymentController>();
            this._dispatcher = paymentDispatcher ?? throw new ArgumentNullException(nameof(paymentDispatcher));
            this._pendingTimeout = siteConfig?.PaymentPendingTimeout ?? 30; // default to 30 seconds
            if (this._pendingTimeout == 0)
            {
                this._pendingTimeout = 30;
            }

            this._settleTimeout = siteConfig?.PaymentSettleTimeout ?? 1800; // default to 30 minutes
            this._siteConfig = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet("{id}/Notify")]
        public async Task<ActionResult<WebApiResult<CollectDetails>>> NotifyUser(
            [FromRoute]long id,
            [FromHeader(Name = "x-app-key")]string appKey,
            [FromHeader(Name = "x-signature")]string signature)
        {
            if (string.IsNullOrEmpty(appKey))
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "x-app-key is required"));
            }

            if (string.IsNullOrEmpty(signature))
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "x-signature is required"));
            }

            // Check if we can find the merchant
            Merchant merchant = await this._dataAccessor.GetOne<Merchant>(
                "select * from `merchant` where `app_key`=@appKey",
                p => p.Add("@appKey", MySqlDbType.VarChar).Value = appKey);
            if (merchant == null)
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.ObjectNotFound, "merchant not found"));
            }

            // Check signature
            string signatureContent = string.Format(
                "Key={0}&RefNumber={1}",
                merchant.AppSecret,
                id);
            if (signature != signatureContent.ToMd5().ToHexString())
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidCredentials, "bad signature"));
            }

            // try to find the payment object
            Payment payment = await this._dataAccessor.GetOne<Payment>(
                "select p.*, c.`id` as `ci_id`, c.`user_id` as `ci_user_id` from payment as p" +
                " join collect_instrument as c on p.`ciid`=c.`id`" +
                " where p.`id`=@id",
                p => p.Add("@id", MySqlDbType.VarChar).Value = id);
            if (payment == null)
            {
                return this.NotFound(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.ObjectNotFound, "payment not found"));
            }

            this._dispatcher.NotifyUserToSettle(payment);
            return this.Accepted(new WebApiResult<CollectDetails>(new CollectDetails
            {
                RefNumber = payment.Id.ToString()
            }));
        }

        [HttpPost]
        public async Task<ActionResult<WebApiResult<CollectDetails>>> Post(
            [FromForm]Payment payment,
            [FromHeader(Name = "x-app-key")]string appKey,
            [FromHeader(Name = "x-signature")]string signature)
        {
            if (string.IsNullOrEmpty(appKey))
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "x-app-key is required"));
            }

            if (string.IsNullOrEmpty(signature))
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "x-signature is required"));
            }

            if (!payment.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "bad payment parameters"));
            }

            // Check if we can find the merchant
            Merchant merchant = await this._dataAccessor.GetOne<Merchant>(
                "select * from `merchant` where `app_key`=@appKey",
                p => p.Add("@appKey", MySqlDbType.VarChar).Value = appKey);
            if (merchant == null)
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.ObjectNotFound, "merchant not found"));
            }

            // Check signature
            string signatureContent = string.Format(
                $"Amount={payment.Amount}&CallBackUrl={payment.CallBackUrl}&Key={merchant.AppSecret}&MerchantReferenceNumber={payment.MerchantReferenceNumber}&NotifyUrl={payment.NotifyUrl}"
                );
            if (signature != signatureContent.ToMd5().ToHexString())
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidCredentials, "bad signature"));
            }

            //check channel is opened
            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>(
                "select * from `collect_channel` where `id`=@cid and `enabled` = true",
                p => p.Add("@cid", MySqlDbType.Int32).Value = payment.Channel);
            if (channel == null)
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.ObjectNotFound, "available channel not found"));
            }

            // Now, create a payment object

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                payment.OriginAmount = payment.Amount;

                // get amount sequence which is between 1-49
                payment.Amount -= s_paymentAmountSequence.AddOrUpdate($"{merchant.Id}-{payment.Channel}", 0, (key, oldValue) => (oldValue + 1) % 49) + 1;

                //channel type of bank card need sub 1 more dollar
                if (payment.Channel == (int)CollectChannelType.AliToCard || payment.Channel == (int)CollectChannelType.Card || payment.Channel == (int)CollectChannelType.UBank)
                {
                    payment.Amount -= 100;
                }

                payment.Merchant = merchant;
                payment.CreateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                payment.Status = PaymentStatus.Pending;
                await transaction.Execute(
                    "insert into `payment`(`channel`, `merchant_id`, `mrn`, `amount`, `notify_url`, `status`, `create_time`, `origin_amount`, `callback_url`) values(@channel, @merchantId, @mrn, @amount, @notifyUrl, @status, @createTime, @originAmount, @callbackUrl)",
                    p =>
                    {
                        p.Add("@channel", MySqlDbType.Int32).Value = payment.Channel;
                        p.Add("@merchantId", MySqlDbType.Int32).Value = merchant.Id;
                        p.Add("@mrn", MySqlDbType.VarChar).Value = payment.MerchantReferenceNumber;
                        p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount;
                        p.Add("@notifyUrl", MySqlDbType.VarChar).Value = payment.NotifyUrl;
                        p.Add("@status", MySqlDbType.Int32).Value = (int)payment.Status;
                        p.Add("@createTime", MySqlDbType.Int64).Value = payment.CreateTimestamp;
                        p.Add("@originAmount", MySqlDbType.Int32).Value = payment.OriginAmount;
                        p.Add("@callbackUrl", MySqlDbType.VarChar).Value = payment.CallBackUrl;
                    });
                payment.Id = await transaction.GetLastInsertId();
                await transaction.Commit();
            }

            // Try to dispatch the payment object
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(this._pendingTimeout)))
                {
                    CollectInstrument collectInstrument = await this._dispatcher.Dispatch(payment, cts.Token);
                    this._logger.LogInformation($"payment:{payment.Id} response by amount:{payment.Amount} QR:{collectInstrument.QrCode}, AccountName:{collectInstrument.AccountHolder}, bankName:{collectInstrument.AccountProvider}, accountNumber:{ collectInstrument.AccountName}");
                    return this.Ok(new WebApiResult<CollectDetails>(new CollectDetails
                    {
                        RefNumber = payment.Id.ToString(),
                        QrCodeUrl = collectInstrument.QrCode,
                        Amount = payment.Amount,
                        BankName = collectInstrument.AccountProvider,
                        AccountName = collectInstrument.AccountHolder,
                        AccountNumber = collectInstrument.AccountName
                    }));
                }
            }
            catch (OperationCanceledException timeoutException)
            {
                await this.UpdatePaymentAsFailed(payment.Id);
                this._logger.LogError("paymentPendingTimeout:{0},{1}", this.TraceActivity(), timeoutException);
                return this.StatusCode(503, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.ServiceNotReady, "service is not ready, please try again"));
            }
            catch (PaymentDispatchException dispatchException)
            {
                await this.UpdatePaymentAsFailed(payment.Id);
                this._logger.LogError("paymentDispatchFailed:{0},{1}", this.TraceActivity(), dispatchException.Error);
                if (dispatchException.Error == DispatchError.NoEligibleUser)
                {
                    return this.StatusCode(503, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.ServiceNotReady, "unable to handle the request currently, please try again"));
                }
                else
                {
                    return this.StatusCode(500, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Unknown, "unknown system error, please try again"));
                }
            }
            catch (Exception exception)
            {
                await this.UpdatePaymentAsFailed(payment.Id);
                this._logger.LogError("unknownSystemError:{0},{1}", this.TraceActivity(), exception);
                return this.StatusCode(500, this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.Unknown, "system error, please contact administrator"));
            }
        }

        [HttpPost("Create")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "PaymentWriter")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<Payment>>> Create(
            [FromForm] int amount,
            [FromForm] long userId, 
            [FromForm] int merchentId,
            [FromForm] int channelId,
            [FromQuery] string captcha)
        {
            // Check captcha
            string storedCaptcha = await this._cache.GetStringAsync("createPayment_"+this.User.GetId());
            await this._cache.RemoveAsync("createPayment_" + this.User.GetId());

            if (!captcha.EqualsIgnoreCase(storedCaptcha))
            {
                return this.BadRequest(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.InvalidData, "bad captcha"));
            }

            if (amount <= 0)
            {
                return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "amount cant less then 0"));
            }
            Payment payment = new Payment();
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                long nowTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                UserAccount member = await transaction.GetOne<UserAccount>(@"select id, balance, merchant_id from user_account where id = @id for update",
                    p => p.Add("@id", MySqlDbType.Int64).Value = userId);
                if (member == null)
                {
                    return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "user not found"));
                }

                if (member.Balance * 100 < amount)
                {
                    return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "user balance not enough"));
                }

                if (member.MerchantId != merchentId && member.MerchantId != 0)
                {
                    return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "user is not support this merchent"));
                }

                UserAccount targetMertOwner = await transaction.GetOne<UserAccount>("select ua.id as id, m.id as m_id, m.app_pwd as m_app_pwd, m.wechat_ratio_static as m_wechat_ratio, m.ali_ratio_static as m_ali_ratio, m.bank_ratio_static as m_bank_ratio from `merchant` as m join user_account as ua on m.user_id = ua.id where m.`id`=@id",
                    p => p.Add("@id", MySqlDbType.Int32).Value = merchentId);
                if (targetMertOwner == null)
                {
                    return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "merchent not found"));
                }

                double ratio = 0;
                ChannelProvider paymentType = ChannelProvider.Bank;
                switch (channelId)
                {
                    case (int)CollectChannelType.Ali:
                    case (int)CollectChannelType.AliH5:
                    case (int)CollectChannelType.AliWap:
                    case (int)CollectChannelType.AliRed:
                        ratio = targetMertOwner.Merchant.AliRatio;
                        paymentType = ChannelProvider.AliPay;
                        break;
                    case (int)CollectChannelType.Wechat:
                    case (int)CollectChannelType.WechatH5:
                    case (int)CollectChannelType.WechatWap:
                        ratio = targetMertOwner.Merchant.WechatRatio;
                        paymentType = ChannelProvider.Wechat;
                        break;
                    default:
                        ratio = targetMertOwner.Merchant.BankRatio;
                        paymentType = ChannelProvider.Bank;
                        break;
                }

                long todayTs = DateTimeOffset.UtcNow.RoundDay(this._siteConfig.TimeZone).ToUnixTimeMilliseconds();
                long collectAccountId = (long)(await transaction.ExecuteScalar(@"select id from `collect_instrument` as ci 
                                                        where ci.user_id = @uid and ci.`status`= 2 and ci.`channel_id`= @channel_id
                                                        and ci.`daily_limit`*100 - @amount >= coalesce((select sum(`amount`) from `payment` where                         `ciid`= ci.`id` and `accept_time`>= @todayTs and `status` in (2, 3)), 0)",
                                                        p =>
                                                        {
                                                            p.Add("@uid", MySqlDbType.Int64).Value = userId;
                                                            p.Add("@channel_id", MySqlDbType.Int32).Value = channelId;
                                                            p.Add("@amount", MySqlDbType.Int32).Value = amount;
                                                            p.Add("@todayTs", MySqlDbType.Int64).Value = todayTs;
                                                        }) ?? 0);
                if (collectAccountId == 0)
                {
                    return this.BadRequest(this.CreateErrorResult<CollectDetails>(Constants.WebApiErrors.InvalidData, "user collect account balance is not enough"));
                }

                payment.Channel = channelId;
                payment.MerchantReferenceNumber = "人工新增订单";
                payment.Amount = amount;
                payment.Status = PaymentStatus.Settled;
                payment.CreateTimestamp = nowTimeStamp;
                payment.AcceptTimestamp = nowTimeStamp;
                payment.SettleTimestamp = nowTimeStamp;
                await transaction.Execute(
                                    "insert into `payment`(`ciid`, `channel`, `merchant_id`, `mrn`, `amount`, `status`, `create_time`, `origin_amount`, `accept_time`, `settle_time`, `ratio`, `notify_url`, `callback_url`) values(@ciid, @channel, @merchantId, @mrn, @amount, @status, @createTime, @originAmount, @accept_time, @settle_time, @ratio, '', '')",
                                    p =>
                                    {
                                        p.Add("@ciid", MySqlDbType.Int64).Value = collectAccountId;
                                        p.Add("@channel", MySqlDbType.Int32).Value = payment.Channel;
                                        p.Add("@merchantId", MySqlDbType.Int32).Value = merchentId;
                                        p.Add("@mrn", MySqlDbType.VarChar).Value = payment.MerchantReferenceNumber;
                                        p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount;
                                        p.Add("@originAmount", MySqlDbType.Int32).Value = payment.Amount;
                                        p.Add("@status", MySqlDbType.Int32).Value = (int)payment.Status;
                                        p.Add("@createTime", MySqlDbType.Int64).Value = payment.CreateTimestamp;
                                        p.Add("@accept_time", MySqlDbType.Int64).Value = payment.AcceptTimestamp;
                                        p.Add("@settle_time", MySqlDbType.Int64).Value = payment.SettleTimestamp;
                                        p.Add("@ratio", MySqlDbType.Double).Value = ratio;
                                    });
                payment.Id = await transaction.GetLastInsertId();
                await transaction.Execute(
                            "update `user_account` set `balance`=`balance`-@amount where `id`=@id",
                            p =>
                            {
                                p.Add("@amount", MySqlDbType.Double).Value = amount / 100.0;
                                p.Add("@id", MySqlDbType.Int64).Value = userId;
                            });

                // Add a transaction log
                await transaction.Execute(
                    "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                    p =>
                    {
                        p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.ManualRedeem;
                        p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                        p.Add("@userId", MySqlDbType.Int64).Value = userId;
                        p.Add("@amount", MySqlDbType.Int32).Value = -amount;
                        p.Add("@balBefore", MySqlDbType.Double).Value = member.Balance;
                        p.Add("@balAfter", MySqlDbType.Double).Value = (member.Balance - amount / 100.0);
                        p.Add("@time", MySqlDbType.Int64).Value = nowTimeStamp;
                    });
                
                async Task SettleAgent(double paymentRatio, UserAccount child)
                {
                    UserAccount UserAgent = await transaction.GetOne<UserAccount>("select ua.id, ua.balance, ua.role_id as role_id, m.wechat_ratio_static as m_wechat_ratio, m.ali_ratio_static as m_ali_ratio, m.bank_ratio_static as m_bank_ratio " +
                        "from user_account as ua " +
                        "join user_relation as ur on ua.id = ur.upper_level_id " +
                        "left join merchant as m on ua.id = m.user_id where ur.is_direct = 1 and ur.user_id = @uid", p => p.Add("@uid", MySqlDbType.Int64).Value = child.Id);

                    if (UserAgent != null)
                    {
                        switch (UserAgent.Role.Id)
                        {
                            case (int)UserRoleType.Agent:
                                double selfRatio = 0;
                                switch (paymentType)
                                {
                                    case ChannelProvider.AliPay:
                                        selfRatio = child.Merchant.AliRatio - UserAgent.Merchant.AliRatio;
                                        break;
                                    case ChannelProvider.Wechat:
                                        selfRatio = child.Merchant.WechatRatio - UserAgent.Merchant.WechatRatio;
                                        break;
                                    default:
                                        selfRatio = child.Merchant.BankRatio - UserAgent.Merchant.BankRatio;
                                        break;
                                }

                                await transaction.Execute(
                                    "update `user_account` set `balance`=`balance`+@amount/100 where `id`=@id",
                                    p =>
                                    {
                                        p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (selfRatio / 100);
                                        p.Add("@id", MySqlDbType.Int64).Value = UserAgent.Id;
                                    });

                                await transaction.Execute(
                                   "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `amountFrom`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @amountFrom)",
                                   p =>
                                   {
                                       p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Commission;
                                       p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                       p.Add("@userId", MySqlDbType.Int64).Value = UserAgent.Id;
                                       p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (selfRatio / 100);
                                       p.Add("@balBefore", MySqlDbType.Double).Value = UserAgent.Balance;
                                       p.Add("@balAfter", MySqlDbType.Double).Value = UserAgent.Balance + payment.Amount * (selfRatio / 100) / 100;
                                       p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                       p.Add("@amountFrom", MySqlDbType.Int64).Value = targetMertOwner.Id;
                                   });

                                await SettleAgent(paymentRatio - selfRatio, UserAgent);
                                break;
                            case (int)UserRoleType.AgentMaster:
                            case (int)UserRoleType.SA:
                                await transaction.Execute(
                                    "update `user_account` set `balance`=`balance`+@amount/100 where `id`=@id",
                                    p =>
                                    {
                                        p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (paymentRatio / 100);
                                        p.Add("@id", MySqlDbType.Int64).Value = UserAgent.Id;
                                    });

                                await transaction.Execute(
                                   "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `amountFrom`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @amountFrom)",
                                   p =>
                                   {
                                       p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Commission;
                                       p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                       p.Add("@userId", MySqlDbType.Int64).Value = UserAgent.Id;
                                       p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (paymentRatio / 100);
                                       p.Add("@balBefore", MySqlDbType.Double).Value = UserAgent.Balance;
                                       p.Add("@balAfter", MySqlDbType.Double).Value = UserAgent.Balance + payment.Amount * (paymentRatio / 100) / 100;
                                       p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                       p.Add("@amountFrom", MySqlDbType.Int64).Value = targetMertOwner.Id;
                                   });
                                break;
                        }
                    }
                }

                await transaction.Execute(
                    "update `user_account` set `balance`=`balance`+@amount/100 where `id`=@id",
                    p =>
                    {
                        p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (1 - ratio / 100);
                        p.Add("@id", MySqlDbType.Int64).Value = targetMertOwner.Id;
                    });

                await transaction.Execute(
                    "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `amountFrom`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @amountFrom)",
                    p =>
                    {
                        p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.WireIn;
                        p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                        p.Add("@userId", MySqlDbType.Int64).Value = targetMertOwner.Id;
                        p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (1 - ratio / 100);
                        p.Add("@balBefore", MySqlDbType.Double).Value = targetMertOwner.Balance;
                        p.Add("@balAfter", MySqlDbType.Double).Value = targetMertOwner.Balance + payment.Amount * (1 - ratio / 100) / 100;
                        p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        p.Add("@amountFrom", MySqlDbType.Int64).Value = member.Id;
                    });
                await SettleAgent(ratio, targetMertOwner);

                await transaction.Commit();
            }
            return this.Ok(new WebApiResult<Payment>(payment));
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "InstrumentOwner,PaymentReader")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<NotificationService.PaymentItem>>>> Get([FromQuery]PaymentStatus status)
        {
            long userId = this.HttpContext.User.GetId();
            List<NotificationService.PaymentItem> payments = await this._dataAccessor.GetAll<NotificationService.PaymentItem>(
                @"select p.`id` as `id`, c.`name` as `channel`, p.`amount` as `amount`, p.`mrn` as `mrn`, p.`accept_time` as `expiration` from `payment` as p
                        join `collect_channel` as c on p.`channel`=c.`id` 
                        join `collect_instrument` as ci on ci.`id`=p.`ciid`
                        where p.`status`=@status
                         and ci.`user_id`=@userId",
                p =>
                {
                    p.Add("@status", MySqlDbType.Int32).Value = (int)status;
                    p.Add("@userId", MySqlDbType.Int64).Value = userId;
                });
            payments.ForEach(p =>
            {
                if (p.Expiration > 0)
                {
                    p.Expiration = Math.Max(0, p.Expiration + this._settleTimeout * 1000 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
            });
            return this.Ok(new WebApiResult<List<NotificationService.PaymentItem>>(payments));
        }

        [HttpGet("listPayments")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "PaymentReader")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<DataPage<Payment>>>> Get(
            [FromQuery]string orderId,
            [FromQuery]string platformOrderId,
            [FromQuery]string startTime,
            [FromQuery]string endTime,
            [FromQuery]int status,
            [FromQuery]int channel,
            [FromQuery]string accountName,
            [FromQuery]int page)
        {
            bool isAgent = this.HttpContext.User.HasRole("Agent");
            page = page > 1 ? page : 1;
            int totalPages = 0;
            long totalRecords = 0;
            long startTimestamp;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out DateTime dateTime))
            {
                startTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else
            {
                startTimestamp = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            sqlBuilder.Append("where p.`create_time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out dateTime))
            {
                sqlBuilder.Append(" and p.`create_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds() });
            }

            if (!string.IsNullOrEmpty(orderId) && long.TryParse(orderId, out long id))
            {
                sqlBuilder.Append(" and p.`id` = @orderId");
                parameters.Add(new MySqlParameter("@orderId", MySqlDbType.Int64) { Value = id });
            }

            if (!string.IsNullOrEmpty(platformOrderId))
            {
                sqlBuilder.Append(" and p.`mrn` like @platformOrderId");
                parameters.Add(new MySqlParameter("@platformOrderId", MySqlDbType.VarChar) { Value = "%" + platformOrderId });
            }

            if (channel > 0)
            {
                sqlBuilder.Append(" and p.`channel` = @channel");
                parameters.Add(new MySqlParameter("@channel", MySqlDbType.Int32) { Value = channel });
            }

            if (status > 0)
            {
                sqlBuilder.Append(" and p.`status` = @status");
                parameters.Add(new MySqlParameter("@status", MySqlDbType.Int32) { Value = status });
            }

            if (!string.IsNullOrEmpty(accountName))
            {
                sqlBuilder.Append(" and u.`account_name` like @accountName");
                parameters.Add(new MySqlParameter("@accountName", MySqlDbType.VarChar) { Value = "%" + accountName });
            }
            
            if (isAgent)
            {
                Merchant merchant = await this._dataAccessor.GetOne<Merchant>("select * from merchant where user_id=@id",
                    p => p.Add(new MySqlParameter("@id", MySqlDbType.Int64) { Value = this.HttpContext.User.GetId() }));
                sqlBuilder.Append(" and p.`merchant_id` = @merchant_id");
                parameters.Add(new MySqlParameter("@merchant_id", MySqlDbType.Int32) { Value = merchant.Id});
            }

            totalRecords = (long)await this._dataAccessor.ExecuteScalar(
                    @"select count(*) from `payment` as p 
                        left join `collect_instrument` as ci on p.`ciid` = ci.`id` 
                        left join `user_account` as u on ci.`user_id` = u.`id` " + sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray()));

            totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            sqlBuilder.Append(" order by p.`id` desc limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int64) { Value = (page - 1) * pageSize });
            List<Payment> payments = await this._dataAccessor.GetAll<Payment>(
                @"select p.*, u.`account_name` from `payment` as p 
                        left join `collect_instrument` as ci on p.`ciid` = ci.`id` 
                        left join `user_account` as u on ci.`user_id` = u.`id` " + sqlBuilder.ToString(),
                p => p.AddRange(parameters.ToArray()));

            return this.Ok(new WebApiResult<DataPage<Payment>>(new DataPage<Payment>
            {
                Page = page,
                TotalPages = totalPages,
                Records = payments,
                TotalRecords = totalRecords
            }));
        }

        [HttpPost("{id}/Settle")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "InstrumentOwner,PaymentWriter")]
        public async Task<ActionResult<WebApiResult<Payment>>> Settle([FromRoute]long id, [FromForm]int? amount)
        {
            long userId = this.HttpContext.User.GetId();

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "SettlePayment", this.TraceActivity()))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                using (DataTransaction transaction = await this._dataAccessor.CreateTransaction(context))
                {
                    Payment payment = await transaction.GetOne<Payment>(
                        @"select p.*, c.`id` as ci_id, c.`user_id` as ci_user_id from `payment` as p
                          join `collect_instrument` as c on p.`ciid`=c.`id`
                          where p.`id`=@id and p.`status` in (2, 5)",
                        p => p.Add("@id", MySqlDbType.Int64).Value = id);
                    int originAmount = payment.Amount;
                    if (payment == null ||
                        payment.Instrument == null ||
                        (this.HttpContext.User.HasRole("InstrumentOwner") && (payment.Instrument.UserId != userId || payment.Status == PaymentStatus.Aborted)))
                    {
                        return this.NotFound(this.CreateErrorResult<Payment>(Constants.WebApiErrors.ObjectNotFound, "payment not found"));
                    }

                    if (this.HttpContext.User.HasRole("PaymentWriter") && amount.HasValue)
                    {
                        payment.Amount = amount.Value;
                    }

                    bool paymentExpired = payment.Status == PaymentStatus.Aborted;
                    UserAccount user = await transaction.GetOne<UserAccount>(
                        "select * from `user_account` where `id`=@userId for update",
                        p => p.Add("@userId", MySqlDbType.Int64).Value = payment.Instrument.UserId);
                    if (user == null)
                    {
                        this._logger.LogError("InsufficientPendingBalance:{user},{amount}", payment.Instrument.Id, payment.Amount / 100.0);
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, this.CreateErrorResult<Payment>(Constants.WebApiErrors.Unknown, "Unexpected insufficient balance"));
                    }

                    if ((!paymentExpired && user.PendingBalance < payment.Amount) || (paymentExpired && (user.Balance - payment.Amount / 100.0) < -0.001))
                    {
                        this._logger.LogError("InsufficientPendingBalance:{user},{pending},{amount}", user.Id, user.PendingBalance, payment.Amount / 100.0);
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, this.CreateErrorResult<Payment>(Constants.WebApiErrors.Unknown, "Unexpected insufficient balance"));
                    }

                    // Update user balance
                    if (paymentExpired)
                    {
                        this._logger.LogInformation($"user:{user.Id} settle payment:{payment.Id} over timeout");
                        await transaction.Execute(
                            "update `user_account` set `balance`=`balance`-@amount,`pending_payments`=`pending_payments`-1 where `id`=@id",
                            p =>
                            {
                                p.Add("@amount", MySqlDbType.Double).Value = payment.Amount / 100.0;
                                p.Add("@id", MySqlDbType.Int64).Value = user.Id;
                            });

                        // Add a transaction log
                        await transaction.Execute(
                            "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                            p =>
                            {
                                p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.ManualRedeem;
                                p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                                p.Add("@amount", MySqlDbType.Int32).Value = -payment.Amount;
                                p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                                p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance - payment.Amount / 100.0);
                                p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            });
                    }
                    else
                    {
                        if (originAmount != payment.Amount)
                        {
                            this._logger.LogInformation($"payment:{payment:Id} change amount from {originAmount} to {payment.Amount}, balance from {user.Balance} to {user.Balance + (originAmount - payment.Amount) / 100.0}");
                            await transaction.Execute(
                            "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                            p =>
                            {
                                p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Refund;
                                p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                p.Add("@userId", MySqlDbType.Int64).Value = user.Id;
                                p.Add("@amount", MySqlDbType.Int32).Value = -payment.Amount;
                                p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                                p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance + (originAmount - payment.Amount) / 100.0);
                                p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            });
                        }

                        this._logger.LogInformation($"user:{user.Id} settle payment:{payment.Id}, before_pendingBalance:{user.PendingBalance}, after_pendingBalance:{user.PendingBalance - originAmount}");

                        await transaction.Execute(
                            "update `user_account` set `pending_balance`=`pending_balance`-@amount,`pending_payments`=`pending_payments`-1,`balance`=`balance`+@deltaAmount where `id`=@id",
                            p =>
                            {
                                p.Add("@amount", MySqlDbType.Int32).Value = originAmount;
                                p.Add("@deltaAmount", MySqlDbType.Int32).Value = (originAmount - payment.Amount) / 100;
                                p.Add("@id", MySqlDbType.Int64).Value = user.Id;
                            });
                    }

                    // get merchant
                    UserAccount mertOwner = await transaction.GetOne<UserAccount>(
                        "select ua.id as id, m.id as m_id, m.app_pwd as m_app_pwd, m.wechat_ratio_static as m_wechat_ratio, m.ali_ratio_static as m_ali_ratio, m.bank_ratio_static as m_bank_ratio from `merchant` as m join user_account as ua on m.user_id = ua.id where m.`id`=@id",
                        p => p.Add("@id", MySqlDbType.Int32).Value = payment.Merchant.Id);

                    double ratio = 0;
                    ChannelProvider paymentType = ChannelProvider.Bank;
                    if (mertOwner != null)
                    {
                        switch (payment.Channel)
                        {
                            case (int)CollectChannelType.Ali:
                            case (int)CollectChannelType.AliH5:
                            case (int)CollectChannelType.AliWap:
                            case (int)CollectChannelType.AliRed:
                                ratio = mertOwner.Merchant.AliRatio;
                                paymentType = ChannelProvider.AliPay;
                                break;
                            case (int)CollectChannelType.Wechat:
                            case (int)CollectChannelType.WechatH5:
                            case (int)CollectChannelType.WechatWap:
                                ratio = mertOwner.Merchant.WechatRatio;
                                paymentType = ChannelProvider.Wechat;
                                break;
                            default:
                                ratio = mertOwner.Merchant.BankRatio;
                                paymentType = ChannelProvider.Bank;
                                break;
                        }
                    }
                    else
                    {
                        this._logger.LogError("merchantNotFound:{0},{1}", this.TraceActivity(), payment.Merchant.Id);
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, this.CreateErrorResult<Payment>(Constants.WebApiErrors.Unknown, "MerchantNotFound"));
                    }

                    // Mark payment as settled
                    payment.Status = PaymentStatus.Settled;
                    payment.SettleTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    await transaction.Execute(
                        "update `payment` set `status`=@status, `amount`=@amount, `settle_time`=@settleTime, `ratio`=@ratio where `id`=@id",
                        p =>
                        {
                            p.Add("@status", MySqlDbType.Int32).Value = (int)payment.Status;
                            p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount;
                            p.Add("@settleTime", MySqlDbType.Int64).Value = payment.SettleTimestamp;
                            p.Add("@ratio", MySqlDbType.Double).Value = ratio;
                            p.Add("@id", MySqlDbType.Int64).Value = payment.Id;
                        });

                    async Task SettleAgent(double paymentRatio, UserAccount child)
                    {
                        UserAccount UserAgent = await transaction.GetOne<UserAccount>("select ua.id, ua.balance, ua.role_id as role_id, m.wechat_ratio_static as m_wechat_ratio, m.ali_ratio_static as m_ali_ratio, m.bank_ratio_static as m_bank_ratio " +
                            "from user_account as ua " +
                            "join user_relation as ur on ua.id = ur.upper_level_id " +
                            "left join merchant as m on ua.id = m.user_id where ur.is_direct = 1 and ur.user_id = @uid", p => p.Add("@uid", MySqlDbType.Int64).Value = child.Id);

                        if (UserAgent != null)
                        {
                            switch (UserAgent.Role.Id)
                            {
                                case (int)UserRoleType.Agent:
                                    double selfRatio = 0;
                                    switch (paymentType)
                                    {
                                        case ChannelProvider.AliPay:
                                            selfRatio = child.Merchant.AliRatio - UserAgent.Merchant.AliRatio;
                                            break;
                                        case ChannelProvider.Wechat:
                                            selfRatio = child.Merchant.WechatRatio - UserAgent.Merchant.WechatRatio;
                                            break;
                                        default:
                                            selfRatio = child.Merchant.BankRatio - UserAgent.Merchant.BankRatio;
                                            break;
                                    }

                                    await transaction.Execute(
                                        "update `user_account` set `balance`=`balance`+@amount/100 where `id`=@id",
                                        p =>
                                        {
                                            p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (selfRatio / 100);
                                            p.Add("@id", MySqlDbType.Int64).Value = UserAgent.Id;
                                        });

                                    await transaction.Execute(
                                       "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `amountFrom`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @amountFrom)",
                                       p =>
                                       {
                                           p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Commission;
                                           p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                           p.Add("@userId", MySqlDbType.Int64).Value = UserAgent.Id;
                                           p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (selfRatio / 100);
                                           p.Add("@balBefore", MySqlDbType.Double).Value = UserAgent.Balance;
                                           p.Add("@balAfter", MySqlDbType.Double).Value = UserAgent.Balance + payment.Amount * (selfRatio / 100) / 100;
                                           p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                           p.Add("@amountFrom", MySqlDbType.Int64).Value = mertOwner.Id;
                                       });

                                    await SettleAgent(paymentRatio - selfRatio, UserAgent);
                                    break;
                                case (int)UserRoleType.AgentMaster:
                                case (int)UserRoleType.SA:
                                    await transaction.Execute(
                                        "update `user_account` set `balance`=`balance`+@amount/100 where `id`=@id",
                                        p =>
                                        {
                                            p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (paymentRatio / 100);
                                            p.Add("@id", MySqlDbType.Int64).Value = UserAgent.Id;
                                        });

                                    await transaction.Execute(
                                       "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `amountFrom`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @amountFrom)",
                                       p =>
                                       {
                                           p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Commission;
                                           p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                           p.Add("@userId", MySqlDbType.Int64).Value = UserAgent.Id;
                                           p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (paymentRatio / 100);
                                           p.Add("@balBefore", MySqlDbType.Double).Value = UserAgent.Balance;
                                           p.Add("@balAfter", MySqlDbType.Double).Value = UserAgent.Balance + payment.Amount * (paymentRatio / 100) / 100;
                                           p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                           p.Add("@amountFrom", MySqlDbType.Int64).Value = mertOwner.Id;
                                       });
                                    break;
                            }
                        }
                    }

                    await transaction.Execute(
                        "update `user_account` set `balance`=`balance`+@amount/100 where `id`=@id",
                        p =>
                        {
                            p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (1 - ratio / 100);
                            p.Add("@id", MySqlDbType.Int64).Value = mertOwner.Id;
                        });

                    await transaction.Execute(
                        "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`, `amountFrom`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time, @amountFrom)",
                        p =>
                        {
                            p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.WireIn;
                            p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                            p.Add("@userId", MySqlDbType.Int64).Value = mertOwner.Id;
                            p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount * (1 - ratio / 100);
                            p.Add("@balBefore", MySqlDbType.Double).Value = mertOwner.Balance;
                            p.Add("@balAfter", MySqlDbType.Double).Value = mertOwner.Balance + payment.Amount * (1 - ratio / 100) / 100;
                            p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            p.Add("@amountFrom", MySqlDbType.Int64).Value = payment.Instrument.UserId;
                        });
                    await SettleAgent(ratio, mertOwner);
                    this.NotifyMerchat(mertOwner.Merchant, payment).Forget();

                    await transaction.Commit();

                    return this.Ok(new WebApiResult<Payment>(payment));
                }
            }
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "PaymentReader")]
        public async Task<ActionResult<WebApiResult<Payment>>> Get([FromRoute]long id)
        {
            Payment payment = await this._dataAccessor.GetOne<Payment>(
                @"select p.*, c.`id` as ci_id, c.`user_id` as ci_user_id from `payment` as p
                join `collect_instrument` as c on p.`ciid`=c.`id` where p.`id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = id);
            if (payment == null)
            {
                return this.NotFound(this.CreateErrorResult<UserAccount>(Constants.WebApiErrors.ObjectNotFound, "payment not found"));
            }

            return this.Ok(new WebApiResult<Payment>(payment));
        }

        private async Task NotifyMerchat(Merchant merchant, Payment payment)
        {
            string content = $"Amount={payment.Amount}&MerchantReferenceNumber={payment.MerchantReferenceNumber}&OriginAmount={payment.OriginAmount}";
            string sig = (content + "&Key=" + merchant.AppSecret).ToMd5().ToHexString();
            content = content + "&Sig=" + sig;
            int retries = 0;
            while (retries < 3)
            {
                try
                {
                    string redirectRequestData = "requestURL=" + Convert.ToBase64String(System.Text.Encoding.GetEncoding("utf-8").GetBytes(payment.NotifyUrl)) + "&method=POST&data=" + Convert.ToBase64String(System.Text.Encoding.GetEncoding("utf-8").GetBytes(content));
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "http://www.boxiq.cn/resend3.php")
                    {
                        Content = new StringContent(redirectRequestData, Encoding.UTF8, "application/x-www-form-urlencoded")
                    };
                    HttpResponseMessage responseMessage = await this._httpClient.SendAsync(requestMessage);
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        this._logger.LogWarning("merchantNotificationFailure:{0},{1},{2}", payment.Id, responseMessage.StatusCode, retries);
                    }
                    else
                    {
                        this._logger.LogInformation("merchantNotified:{0},{1}", payment.Id, retries);
                        break;
                    }
                }
                catch (Exception exception)
                {
                    this._logger.LogError("failedToSendMerchantNotification:{0},{1},{2}", payment.Id, retries, exception);
                }

                ++retries;
                await Task.Delay((int)(Math.Pow(2, retries) * 2000 * this._random.Next(50, 100) / 100.0));
            }
        }

        private Task UpdatePaymentAsFailed(long paymentId)
        {
            return this._dataAccessor.Execute(
                "update `payment` set `status`=@status where `id`=@paymentId",
                p =>
                {
                    p.Add("@status", MySqlDbType.Int32).Value = (int)PaymentStatus.Aborted;
                    p.Add("@paymentId", MySqlDbType.Int64).Value = paymentId;
                });
        }
    }
}

