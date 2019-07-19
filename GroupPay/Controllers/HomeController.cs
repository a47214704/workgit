using Microsoft.AspNetCore.Mvc;
using Core;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using GroupPay.Models;
using Core.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Core.Crypto;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;

namespace GroupPay.Controllers
{
    public class HomeController : Controller
    {
        private readonly SiteConfig _config;
        private readonly IDistributedCache _cache;
        private readonly DataAccessor _dataAccessor;

        public HomeController(IDistributedCache cache, DataAccessor dataAccessor, SiteConfig siteConfig)
        {
            this._config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        [Authorize(Roles = "InstrumentOwner")]
        public async Task<IActionResult> Index()
        {
            return View(await this.GetCurrentUser());
        }

        [Authorize(Roles = "PaymentReader")]
        public IActionResult ListPayments()
        {
            return View();
        }

        [Authorize(Roles = "PaymentWriter")]
        public IActionResult Reorder()
        {
            return View();
        }

        [Authorize(Roles = "PaymentWriter")]
        public IActionResult CreateOrder()
        {
            return View();
        }

        [Authorize(Roles = "UserReader,Agent,AgentMgmt")]
        public IActionResult ListAgents()
        {
            return View();
        }

        [Authorize(Roles = "UserReader")]
        public IActionResult ListUsers()
        {
            return View();
        }

        [Authorize(Roles = "ReportReader")]
        public IActionResult Report()
        {
            return View();
        }

        [Authorize(Roles = "Agent,ReportReader")]
        public IActionResult MerchantReport()
        {
            return View();
        }

        [Authorize(Roles = "RechargeReader")]
        public IActionResult RechargeReport()
        {
            return View();
        }

        [Authorize(Roles = "ReportReader")]
        public IActionResult TransactionLog()
        {
            return View();
        }

        [Authorize(Roles = "AgentSettler")]
        public IActionResult WireOutLog()
        {
            return View();
        }

        [Authorize(Roles = "ReportReader")]
        public IActionResult RechargeList()
        {
            return View();
        }

        [Authorize(Roles = "SelfMgmt")]
        public IActionResult SelfMgmt()
        {
            return View();
        }

        [Authorize(Roles = "UserMgmt")]
        public IActionResult UserEdit()
        {
            return View();
        }

        [Authorize(Roles = "SelfMgmt")]
        public IActionResult SelfMgmtSetPassword()
        {
            return View();
        }

        [Authorize(Roles = "AgentMgmt,Agent")]
        public IActionResult AgentReport()
        {
            return View();
        }
               
        [Authorize(Roles = "AgentMgmt,Agent")]
        public IActionResult AgentEdit()
        {
            return View();
        }

        [Authorize(Roles = "UserMgmt")]
        public IActionResult AddUser()
        {
            return View();
        }

        [Authorize(Roles = "Agent,AgentMgmt")]
        public IActionResult AddAgent()
        {
            return View();
        }

        [Authorize(Roles = "SelfMgmt")]
        public IActionResult AddChildUser()
        {
            return View();
        }

        [Authorize(Roles = "SelfMgmt")]
        public IActionResult ListChildUsers()
        {
            return View();
        }

        [Authorize(Roles = "ConfigWriter")]
        public IActionResult CommissionRatios()
        {
            return this.View();
        }

        [Authorize(Roles = "ConfigWriter")]
        public IActionResult AwardConfigs()
        {
            return this.View();
        }

        [Authorize(Roles = "ConfigWriter")]
        public IActionResult SiteConfigs()
        {
            return this.View();
        }

        [Authorize(Roles = "ConfigWriter")]
        public IActionResult CollectChannel()
        {
            return this.View();
        }

        [Authorize(Roles = "ConfigWriter")]
        public IActionResult UserEvaluation()
        {
            return this.View();
        }

        [Authorize(Roles = "UserMgmt")]
        public IActionResult AddEvaluation()
        {
            return this.View();
        }

        [Authorize(Roles = "SelfMgmt")]
        public async Task<IActionResult> Console()
        {
            if (this.HttpContext.User.HasRole("InstrumentOwner"))
            {
                return this.RedirectToAction("Index");
            }

            return View(await this.GetCurrentUser());
        }

        public async Task<IActionResult> Login([FromForm]LoginRequest loginRequest, [FromQuery]string returnUrl)
        {
            if (this.Request.Method == HttpMethods.Get)
            {
                return View();
            }

            if (loginRequest == null)
            {
                this.ViewData["Error"] = "请求无效";
                return View(loginRequest);
            }

            if (string.IsNullOrEmpty(loginRequest.Username))
            {
                this.ViewData["Error"] = "用户名不能为空";
                return View(loginRequest);
            }

            if (string.IsNullOrEmpty(loginRequest.Password))
            {
                this.ViewData["Error"] = "密码不能为空";
                return View(loginRequest);
            }

            if (string.IsNullOrEmpty(loginRequest.Captcha))
            {
                this.ViewData["Error"] = "验证码不能为空";
                return View(loginRequest);
            }

            if (!loginRequest.Captcha.EqualsIgnoreCase(this.HttpContext.Session.GetString(Constants.Web.CaptchaCode)))
            {
                this.HttpContext.Session.Remove(Constants.Web.CaptchaCode);
                this.ViewData["Error"] = "验证码错误";
                return View(loginRequest);
            }

            UserAccount user = await this._dataAccessor.GetOne<UserAccount>(
                "select * from `user_account` where `account_name`=@accountName and `password`=@password",
                p =>
                {
                    p.Add("@accountName", MySqlDbType.VarChar).Value = loginRequest.Username;
                    p.Add("@password", MySqlDbType.VarChar).Value = loginRequest.Password.ToSha256().ToHexString();
                });
            if (user == null)
            {
                this.ViewData["Error"] = "用户名或者密码错误";
                return View(loginRequest);
            }

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
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.AccountName),
                new Claim(ClaimTypes.Sid, user.Role.Id.ToString())
            };
            foreach (string perm in perms)
            {
                claims.Add(new Claim(ClaimTypes.Role, perm));
            }

            ClaimsIdentity identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            await this.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return this.RedirectToAction(principal.HasRole("InstrumentOwner") ? "Index" : "Console");
        }

        public Task Logout()
        {
            return this.HttpContext.SignOutAsync(new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            });
        }

        public IActionResult Register([FromQuery]string referrer)
        {
            return this.View(new RegisterPageData
            {
                Token = Guid.NewGuid().ToString(),
                AppDownloadUrl = this._config.AppDownloadUrl,
                Referrer = referrer ?? string.Empty
            });
        }

        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<FileStreamResult> CaptchaImage([FromQuery]string token)
        {
            int width = 200;
            int height = 60;
            CaptchaResult result = Captcha.GenerateCaptchaImage(width, height);
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.SetString(Constants.Web.CaptchaCode, result.CaptchaCode);
            }
            else
            {
                await this._cache.SetAsync(token, Encoding.UTF8.GetBytes(result.CaptchaCode), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }

            Stream s = new MemoryStream(result.CaptchaByteData);

            return new FileStreamResult(s, "image/png");
        }

        public async Task<IActionResult> Chat()
        {
            long userId = this.HttpContext.User.GetId();
            ChatSvcTicketContent ticketContent = new ChatSvcTicketContent
            {
                Manager = this.HttpContext.User.HasRole("UserReader") ? 1 : 0,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            if (userId == 0)
            {
                // Anonymous
                ticketContent.UserId = new Random((int)DateTime.UtcNow.Ticks).Next(1000, 9999);
                ticketContent.NickName = "游客";
                if (this.Request.Cookies.TryGetValue("vistorName", out string userName))
                {
                    ticketContent.UserName = userName;
                }
                else
                {
                    ticketContent.UserName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ticketContent.UserId}";
                    if (ticketContent.UserName.Length > 11)
                    {
                        ticketContent.UserName = ticketContent.UserName.Substring(0, 11);
                    }

                    this.Response.Cookies.Append("visitorName", ticketContent.UserName);
                }
            }
            else
            {
                UserAccount userAccount = await this.GetCurrentUser();
                ticketContent.UserId = userAccount.Id;
                ticketContent.UserName = userAccount.AccountName;
                ticketContent.NickName = string.IsNullOrEmpty(userAccount.NickName) ? userAccount.AccountName : userAccount.NickName;
            }

            return this.View(new ChatSvcConnector
            {
                Ticket = Convert.ToBase64String(JsonConvert.SerializeObject(ticketContent).UnsafeEncrypt(this._config.ChatServiceKey)),
                ServiceEndpoint = this._config.ChatServiceEndpoint,
                UserName = ticketContent.UserName,
                IsCsRepresentative = ticketContent.Manager > 0
            });
        }

        private Task<UserAccount> GetCurrentUser()
        {
            return this._dataAccessor.GetOne<UserAccount>("select * from `user_account` where `id`=@id", p => p.Add("@id", MySqlDbType.Int64).Value = this.HttpContext.User.GetId());
        }

        public class LoginRequest
        {
            public string Username { get; set; }

            public string Password { get; set; }

            public string Captcha { get; set; }
        }

        public class RegisterPageData
        {
            public string Token { get; set; }

            public string AppDownloadUrl { get; set; }

            public string Referrer { get; set; }
        }

        public class ChatSvcConnector
        {
            public string Ticket { get; set; }

            public string ServiceEndpoint { get; set; }

            public string UserName { get; set; }

            public bool IsCsRepresentative { get; set; }
        }

        public class ChatSvcTicketContent
        {
            [JsonProperty("userId")]
            public long UserId { get; set; }

            [JsonProperty("userName")]
            public string UserName { get; set; }

            [JsonProperty("nickname")]
            public string NickName { get; set; }

            [JsonProperty("manager")]
            public int Manager { get; set; }

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; }
        }
    }
}
