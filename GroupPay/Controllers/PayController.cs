using Core;
using Core.Crypto;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayController : Controller
    {
        private readonly SiteConfig _config;
        private readonly DataAccessor _dataAccessor;
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PayController> _logger;

        private readonly string AppSecret = "2a33942a0571f46c567f7bcad0b174ac";
        private readonly string Key = "bb4cfda04fc32388c03d23b735dd5031";

        public PayController(IDistributedCache cache, DataAccessor dataAccessor, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            this._config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this._httpClient = new HttpClient();
            this._logger = loggerFactory.CreateLogger<PayController>();
        }

        [HttpGet("PaymentCache")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<PaymentRequest>>> PaymentCache([FromQuery] string appkey, [FromQuery]string id)
        {
            return this.Ok(new WebApiResult<PaymentRequest>(JsonConvert.DeserializeObject<PaymentRequest>(await this._cache.GetStringAsync("PayRequest_" + appkey + "_" + id))));
        }

        [HttpGet("Menu/{id}")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<Merchant>>> Menu([FromRoute] string Id)
        {
            Merchant merchant = await this._dataAccessor.GetOne<Merchant>("select channel_enabled, channel_limit from `merchant` where `app_key`=@id",
                p => p.Add("@id", MySqlDbType.VarChar).Value = Id);
            List<CollectChannel> collectChannels = await this._dataAccessor.GetAll<CollectChannel>("select * from `collect_channel`");
            foreach (CollectChannel cc in collectChannels)
            {
                if (!cc.Enabled)
                {
                    merchant.ChannelEnabledList[(int)cc.Id - 1] = false;
                }
            }
            return this.Ok(new WebApiResult<Merchant>(merchant));
        }

        [HttpPost("Menu")]
        public async Task<IActionResult> Menu([FromForm]PaymentRequest payment)
        {
            string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment);
            if (string.IsNullOrEmpty(errorMsg))
            {
                ViewBag.MId = payment.AppKey;
                ViewBag.Id = payment.MerchantReferenceNumber;
                ViewBag.Amount = payment.Amount / 100;
                ViewBag.CallBackUrl = payment.CallBackUrl;
                await this._cache.SetStringAsync("PayRequest_"+ payment.AppKey + "_" + payment.MerchantReferenceNumber, JsonConvert.SerializeObject(payment),new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
            }
            else
            {
                ViewBag.ErrorMsg = errorMsg;
                this.HttpContext.Session.Remove("PayRequest_" + payment.AppKey + "_" + payment.MerchantReferenceNumber);
            }
            return View();
        }

        [HttpGet("AliScanRedirect")]
        public async Task<IActionResult> AliScanRedirect([FromQuery] string appkey, [FromQuery]string id)
        {
            string payRequest = await this._cache.GetStringAsync("PayRequest_" + appkey + "_" + id);
            if (!string.IsNullOrEmpty(payRequest))
            {
                PaymentRequest payment = JsonConvert.DeserializeObject<PaymentRequest>(payRequest);
                ViewBag.CallBackUrl = payment.CallBackUrl;
                string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment, (int)CollectChannelType.AliWap);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    ViewBag.ErrorMsg = errorMsg;
                    this.HttpContext.Session.Remove("PayRequest_" + appkey + "_" + id);
                }
            }
            else
            {
                ViewBag.ErrorMsg = "无效的订单请求!";
            }
            return View();
        }

        [HttpGet("AliScanWap/{id}")]
        public async Task<IActionResult> AliScanWap([FromRoute]string id)
        {
            Payment payment = await this._dataAccessor.GetOne<Payment>(
                @"select p.*, c.`id` as ci_id, c.`user_id` as ci_user_id, c.`qr_code` as ci_qr_code from `payment` as p
                    join `collect_instrument` as c on p.`ciid`=c.`id`
                    where p.`mrn`=@id and p.`status` = 2 and c.`channel_id`=7",
                p => p.Add("@id", MySqlDbType.VarChar).Value = id);

            if (payment == null)
            {
                return Content("找不到该订单");
            }

            if (string.IsNullOrEmpty(payment.Instrument.QrCode))
            {
                return Content("错误的支付通道");
            }

            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>(
                "select * from `collect_channel` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = payment.Channel);

            long countDown = payment.AcceptTimestamp + channel.ValidTime * 60 * 1000 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (countDown <= 0)
            {
                return Content("订单已过期");
            }
            ViewBag.CountDown = countDown / 1000;
            ViewBag.QrCode = payment.Instrument.QrCode;
            ViewBag.Amount = (float)payment.Amount / 100;
            ViewBag.RefNumber = payment.MerchantReferenceNumber;
            return View();
        }

        [HttpGet("AliScan")]
        public async Task<IActionResult> AliScan([FromQuery] string appkey, [FromQuery]string id)
        {
            string payRequest = await this._cache.GetStringAsync("PayRequest_" + appkey + "_" + id);
            if (!string.IsNullOrEmpty(payRequest))
            {
                PaymentRequest payment = JsonConvert.DeserializeObject<PaymentRequest>(payRequest);
                ViewBag.CallBackUrl = payment.CallBackUrl;
                string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment, (int)CollectChannelType.Ali);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    ViewBag.ErrorMsg = errorMsg;
                    this.HttpContext.Session.Remove("PayRequest_" + appkey + "_" + id);
                }
            }
            else
            {
                ViewBag.ErrorMsg = "无效的订单请求!";
            }
            return View();
        }

        [HttpGet("WechatRedirect")]
        public async Task<IActionResult> WechatRedirect([FromQuery] string appkey, [FromQuery]string id)
        {
            string payRequest = await this._cache.GetStringAsync("PayRequest_" + appkey + "_" + id);
            if (!string.IsNullOrEmpty(payRequest))
            {
                PaymentRequest payment = JsonConvert.DeserializeObject<PaymentRequest>(payRequest);
                string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment, (int)CollectChannelType.WechatWap);
                ViewBag.CallBackUrl = payment.CallBackUrl;
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    ViewBag.ErrorMsg = errorMsg;
                    this.HttpContext.Session.Remove("PayRequest_" + appkey + "_" + id);
                }
            }
            else
            {
                ViewBag.ErrorMsg = "无效的订单请求!";
            }
            return View();
        }

        [HttpGet("WechatWap/{id}")]
        public async Task<IActionResult> WechatWap([FromRoute]string id)
        {
            Payment payment = await this._dataAccessor.GetOne<Payment>(
                @"select p.*, c.`id` as ci_id, c.`user_id` as ci_user_id, c.`qr_code` as ci_qr_code from `payment` as p
                    join `collect_instrument` as c on p.`ciid`=c.`id`
                    where p.`mrn`=@id and p.`status` = 2 and c.`channel_id`=8",
                p => p.Add("@id", MySqlDbType.VarChar).Value = id);

            if (payment == null)
            {
                return Content("找不到该订单");
            }

            if (string.IsNullOrEmpty(payment.Instrument.QrCode))
            {
                return Content("错误的支付通道");
            }

            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>(
                "select * from `collect_channel` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = payment.Channel);

            long countDown = payment.AcceptTimestamp + channel.ValidTime * 60 * 1000 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (countDown <= 0)
            {
                return Content("订单已过期");
            }

            ViewBag.CountDown = (int)(countDown / 1000);
            ViewBag.QrCode = payment.Instrument.QrCode;
            ViewBag.Amount = (float)payment.Amount / 100;
            ViewBag.RefNumber = payment.MerchantReferenceNumber;
            return View();
        }

        [HttpGet("Wechat")]
        public async Task<IActionResult> Wechat([FromQuery] string appkey, [FromQuery]string id)
        {
            string payRequest = await this._cache.GetStringAsync("PayRequest_" + appkey + "_" + id);
            if (!string.IsNullOrEmpty(payRequest))
            {
                PaymentRequest payment = JsonConvert.DeserializeObject<PaymentRequest>(payRequest);
                ViewBag.CallBackUrl = payment.CallBackUrl;
                string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment, (int)CollectChannelType.Wechat);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    ViewBag.ErrorMsg = errorMsg;
                    this.HttpContext.Session.Remove("PayRequest_" + appkey + "_" + id);
                }
            }
            else
            {
                ViewBag.ErrorMsg = "无效的订单请求!";
            }
            return View();
        }

        [HttpGet("Bank")]
        public async Task<IActionResult> Bank([FromQuery] string appkey, [FromQuery]string id)
        {
            string payRequest = await this._cache.GetStringAsync("PayRequest_" + appkey + "_" + id);
            if (!string.IsNullOrEmpty(payRequest))
            {
                PaymentRequest payment = JsonConvert.DeserializeObject<PaymentRequest>(payRequest);
                ViewBag.CallBackUrl = payment.CallBackUrl;
                string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment, (int)CollectChannelType.Card);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    ViewBag.ErrorMsg = errorMsg;
                    this.HttpContext.Session.Remove("PayRequest_" + appkey + "_" + id);
                }
            }
            else
            {
                ViewBag.ErrorMsg = "无效的订单请求!";
            }
            return View();
        }

        [HttpGet("AliBankRedirect")]
        public async Task<IActionResult> AliBankRedirect([FromQuery]string id)
        {
            this.HttpContext.Request.Cookies.TryGetValue("PayRequest_" + id, out string payRequest);
            if (!string.IsNullOrEmpty(payRequest))
            {
                PaymentRequest payment = JsonConvert.DeserializeObject<PaymentRequest>(payRequest);
                ViewBag.CallBackUrl = payment.CallBackUrl;
                string errorMsg = await CheckPaymentOuth(payment.MerchantReferenceNumber, payment, (int)CollectChannelType.AliToCard);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    ViewBag.ErrorMsg = errorMsg;
                }
            }
            else
            {
                ViewBag.ErrorMsg = "无效的订单请求!";
            }
            return View();
        }


        [HttpGet("AliBank/{id}")]
        public async Task<IActionResult> AliBank([FromRoute]string id)
        {
            Dictionary<string, string> redirectURI = new Dictionary<string, string>
            {
                { "appId", "09999988" },
                { "actionType", "toCard" },
                { "sourceId", "bill" }
            };

            Payment payment = await this._dataAccessor.GetOne<Payment>(
                @"select p.*, c.`id` as ci_id, c.`user_id` as ci_user_id, c.`account_provider` as ci_account_provider, c.`account_name` as ci_account_name, c.`account_holder` as ci_account_holder from `payment` as p
                    join `collect_instrument` as c on p.`ciid`=c.`id`
                    where p.`mrn`=@id and p.`status` = 2 and c.`channel_id`=6",
                p =>p.Add("@id", MySqlDbType.VarChar).Value = id);

            if (payment == null)
            {
                return Content("找不到该订单");
            }

            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>(
                "select * from `collect_channel` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int64).Value = payment.Channel);

            if (payment.AcceptTimestamp + channel.ValidTime * 60 * 1000 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <= 0)
            {
                return Content("订单已过期");
            }

            float payAmount = (float)payment.Amount / 100;

            redirectURI.Add("cardNo", payment.Instrument.AccountName);
            redirectURI.Add("bankAccount", payment.Instrument.AccountHolder);
            redirectURI.Add("money", payAmount.ToString());
            redirectURI.Add("amount", payAmount.ToString());
            redirectURI.Add("bankMark", Constants.BankMark.GetValueOrDefault(payment.Instrument.AccountProvider));
            redirectURI.Add("bankName", HttpUtility.UrlEncode(payment.Instrument.AccountProvider));

            return Redirect(QueryHelpers.AddQueryString("alipays://platformapi/startapp", redirectURI));
        }
        
        /// <summary>
        /// 淘金宝充值通道
        /// </summary>
        /// <param name="userId">userId</param>
        /// <returns></returns>
        [HttpPost("selfRecharge")]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> SelfRecharge([FromBody]Payment payment, [FromQuery]string captcha, [FromQuery]string token)
        {
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

            long userId = this.HttpContext.User.GetId();
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                long rechargeCount = await transaction.GetOne("select count(1) from `recharge`",
                new SimpleRowMapper<long>(async reader =>
                {
                    if (await reader.IsDBNullAsync(0))
                    {
                        return 0;
                    }

                    return reader.GetInt64(0);
                }));

                if (payment.Amount <= 100)
                {
                    return this.BadRequest(this.CreateErrorResult<Payment>(Constants.WebApiErrors.InvalidData, "fail amount"));
                }

                int amount = payment.Amount - 100 - (int)(rechargeCount % 100);
                string orderIdStr = DateTime.Now.ToString("yyyyMMddHHmmssfff") + new Random().Next(10).ToString();
                string signatureContent = string.Format(
                    "Amount={0}&Channel={1}&Key={2}&MerchantReferenceNumber={3}&NotifyUrl={4}",
                    amount,
                    payment.Channel,
                    AppSecret,
                    orderIdStr,
                    this._config.BaseUrl + "/api/Pay/Notify").ToMd5().ToHexString();
                long.TryParse(orderIdStr, out long orderId);
                await transaction.Execute("insert into recharge(`id`,`channel`,`amount`,`user_id`,`create_time`)values(@orderId,@channel,@amount,@user_id,@create_time)",
                p =>
                {
                    p.Add("@orderId", MySqlDbType.Int64).Value = orderId;
                    p.Add("@channel", MySqlDbType.Int32).Value = payment.Channel;
                    p.Add("@amount", MySqlDbType.Int32).Value = amount;
                    p.Add("@user_id", MySqlDbType.Int64).Value = userId;
                    p.Add("@create_time", MySqlDbType.Int32).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                });
                await transaction.Commit();

                return this.Ok(new WebApiResult<RechargeResponse>(new RechargeResponse
                {
                    Key = Key,
                    Sign = signatureContent,
                    Payment = new Payment()
                    {
                        Amount = amount,
                        Channel = payment.Channel,
                        MerchantReferenceNumber = orderIdStr,
                        NotifyUrl = this._config.BaseUrl + "/api/Pay/Notify"
                    }
                }));
            }
        }

        /// <summary>
        /// 淘金宝充值回调
        /// </summary>
        /// <param name="amount">amount</param>
        /// <param name="merchantReferenceNumber">merchantReferenceNumber</param>
        /// <param name="sig">sign</param>
        /// <returns></returns>
        [HttpPost("Notify")]
        public async Task<IActionResult> Notify([FromForm]int amount, [FromForm]string merchantReferenceNumber, [FromForm]string sig)
        {
            string signatureContent = string.Format(
                "Amount={0}&MerchantReferenceNumber={1}&Key={2}",
                amount,
                merchantReferenceNumber,
                AppSecret).ToMd5().ToHexString();
            this._logger.LogInformation("notify,Amount={0}&MerchantReferenceNumber={1}", amount,
                merchantReferenceNumber);

            if (signatureContent == sig)
            {
                Recharge recharge = null;
                using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
                {
                    recharge = await transaction.GetOne<Recharge>("select * from recharge where id=@mrn",
                    p =>
                    {
                        p.Add("@mrn", MySqlDbType.String).Value = merchantReferenceNumber;
                    });

                    if (recharge == null)
                    {
                        this._logger.LogInformation("notify,recharge{0} not found", merchantReferenceNumber);
                        return this.BadRequest(this.CreateErrorResult<Payment>(Constants.WebApiErrors.ObjectNotFound, "recharge not found"));
                    }

                    if (recharge.SettleTime != 0)
                    {
                        this._logger.LogInformation("notify,recharge{0} already settled", merchantReferenceNumber);
                        return this.Ok(new WebApiResult<Recharge>(recharge));
                    }

                    UserAccount user = await this._dataAccessor.GetOne<UserAccount>("select * from user_account where id = @userId",
                    p =>
                    {
                        p.Add("@userId", MySqlDbType.Int64).Value = recharge.UserId;
                    });

                    if (user == null)
                    {
                        this._logger.LogInformation("notify,UserId{0},recharge{1} not found", recharge.UserId, recharge.Id);
                        return this.BadRequest(this.CreateErrorResult<Payment>(Constants.WebApiErrors.ObjectNotFound, "user not found"));
                    }

                    await transaction.Execute("update user_account set balance = balance + @amount/100 where id = @userId",
                    p =>
                    {
                        p.Add("@amount", MySqlDbType.Int64).Value = amount;
                        p.Add("@userId", MySqlDbType.Int64).Value = recharge.UserId;
                    });

                    long nowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    await transaction.Execute("update recharge set settle_time = @settleTime,amount = @amount where id = @mrn",
                    p =>
                    {
                        p.Add("@settleTime", MySqlDbType.Int64).Value = nowTime;
                        p.Add("@amount", MySqlDbType.Int32).Value = amount;
                        p.Add("@mrn", MySqlDbType.String).Value = merchantReferenceNumber;
                    });

                    await transaction.Execute(
                        "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                        p =>
                        {
                            p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Refill;
                            p.Add("@paymentId", MySqlDbType.Int64).Value = recharge.Id;
                            p.Add("@userId", MySqlDbType.Int64).Value = recharge.UserId;
                            p.Add("@amount", MySqlDbType.Int32).Value = amount;
                            p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                            p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance + recharge.Amount / 100.0);
                            p.Add("@time", MySqlDbType.Int64).Value = nowTime;
                        });
                    await transaction.Commit();
                }
                    
                return this.Ok(new WebApiResult<Recharge>(recharge));
            }
            else
            {
                return this.BadRequest(this.CreateErrorResult<Recharge>(Constants.WebApiErrors.InvalidCredentials, "bad sign"));
            }
        }

        private async Task<string> CheckPaymentOuth(string id, PaymentRequest payment, int type = 0)
        {
            string errorMsg = "";
            if (string.IsNullOrEmpty(payment.AppKey) || string.IsNullOrEmpty(payment.Sign) || id != payment.MerchantReferenceNumber)
            {
                errorMsg = "无效的订单请求!";
            }
            else
            {
                Merchant merchant = await this._dataAccessor.GetOne<Merchant>(
                    "select * from `merchant` where `app_key`=@key",
                    p => p.Add("@key", MySqlDbType.VarChar).Value = payment.AppKey);
                if (merchant == null)
                {
                    return "无效商户!";
                }

                Payment checkPayment = await this._dataAccessor.GetOne<Payment>(
                       @"select * from `payment` where `merchant_id` = @mid and `mrn`=@id",
                       p =>
                       {
                           p.Add("@id", MySqlDbType.VarChar).Value = id;
                           p.Add("@mid", MySqlDbType.Int32).Value = merchant.Id;
                       });

                if (checkPayment != null)
                {
                    errorMsg = "订单号重复!";
                }
                
                if (type != 0)
                {
                    var limit = merchant.ChannelLimit.GetValueOrDefault(Enum.GetName(typeof(CollectChannelType), type));
                    if (limit.Item1 > (payment.Amount / 100) || limit.Item2 < (payment.Amount / 100))
                    {
                        return $"请选择适合的通道!此通道限额{limit.Item1}~{limit.Item2}";
                    }

                    if (!merchant.ChannelEnabledList[type - 1])
                    {
                        return "通道未开启!";
                    }
                }
            }
            return errorMsg;
        }

        class RechargeResponse
        {
            public string Key { get; set; }
            public string Sign { get; set; }
            public Payment Payment { get; set; }
        }

        public class PaymentRequest : Payment
        {
            public string AppKey { get; set; }
            public string Sign { get; set; }
        }
    }
}