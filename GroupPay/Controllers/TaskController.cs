using Core;
using Core.Data;
using GroupPay.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _config;
        private readonly ILogger<TaskController> _logger;
        private readonly AgencyCommissionService _agencyCommissionService;
        private readonly PaymentDispatcher _paymentDispatcher;
        private readonly IDistributedCache _cache;

        public TaskController(DataAccessor dataAccessor,SiteConfig config, ILoggerFactory loggerFactory, AgencyCommissionService agencyCommissionService, PaymentDispatcher paymentDispatcher, IDistributedCache cache)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._config = config ?? throw new ArgumentNullException(nameof(config));
            this._logger = loggerFactory.CreateLogger<TaskController>();
            this._agencyCommissionService = agencyCommissionService ?? throw new ArgumentNullException(nameof(agencyCommissionService));
            this._paymentDispatcher = paymentDispatcher ?? throw new ArgumentNullException(nameof(paymentDispatcher));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet("Settle")]
        public async Task<ActionResult> Settle([FromQuery]string key, [FromQuery]long startTime = 0, [FromQuery]long endTime = 0)
        {
            var ip = this.HttpContext.RemoteAddr();
            if (ip == "127.0.0.1" || ip == "::1")
            {
                switch (key)
                {
                    case "Award":
                        await this._agencyCommissionService.AwardDaliyTask(startTime, endTime);
                        break;
                    case "Commission":
                        await this._agencyCommissionService.CommissionDaliyTask(startTime, endTime);
                        break;
                    case "Evaluation":
                        await this._paymentDispatcher.EvaluationTask();
                        break;
                    case "MerchantRatio":
                        await this._dataAccessor.Execute("update merchant set wechat_ratio_static = wechat_ratio, ali_ratio_static = ali_ratio, bank_ratio_static = bank_ratio");
                        break;
                }
                return this.StatusCode(200, $"{DateTimeOffset.UtcNow.ToDateValue(this._config.TimeZone)} {key} Daily Settle Task ok");
            }
            else
            {
                return this.StatusCode(400, $"fail auth, {ip} is tried");
            }
        }
    }
}
