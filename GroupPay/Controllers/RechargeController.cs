using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RechargeController : Controller
    {
        private const int pageSize = 20;
        private readonly SiteConfig _config;
        private readonly DataAccessor _dataAccessor;
        private readonly ILogger<RechargeController> _logger;
        public RechargeController(DataAccessor dataAccessor, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            this._config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._logger = loggerFactory.CreateLogger<RechargeController>();
        }

        [HttpGet]
        [Authorize(Roles = "SelfMgmt,RechargeReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<Recharge>>>> Get(
            [FromQuery]long userId = -1,
            [FromQuery]string startTime = "",
            [FromQuery]string endTime = "")
        {
            if (userId == -1 && this.HttpContext.User.HasRole("SelfMgmt"))
            {
                userId = this.HttpContext.User.GetId();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            long startTimestamp;
            long endTimestamp = 0;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime.ToTimeZoneDateTime(this._config.TimeZoneOffset), out DateTime dateTime))
            {
                startTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else
            {
                startTimestamp = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            }

            sqlBuilder.Append("where `user_id`=@userId and `create_time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });
            parameters.Add(new MySqlParameter("@userId", MySqlDbType.Int64) { Value = userId });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._config.TimeZoneOffset), out dateTime))
            {
                endTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                sqlBuilder.Append(" and `create_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = endTimestamp });
            }

            List<Recharge> recharges = await this._dataAccessor.GetAll<Recharge>("select * from recharge " + sqlBuilder.ToString() + " order by create_time desc",
            p => p.AddRange(parameters.ToArray()));

            return this.Ok(new WebApiResult<List<Recharge>>(recharges));
        }

        [HttpGet("Report")]
        [Authorize(Roles = "RechargeReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<Recharge>>>> Get(
            [FromQuery]int page,
            [FromQuery]string startTime,
            [FromQuery]string endTime,
            [FromQuery]string accountName)
        {
            page = page < 1 ? 1 : page;
            int totalPage = 0;
            long startTimestamp;
            long endTimestamp = 0;
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
            RechargeReport summary = null;

            sqlBuilder.Append("where r.`create_time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._config.TimeZoneOffset), out dateTime))
            {
                endTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                sqlBuilder.Append(" and r.`create_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = endTimestamp });
            }

            if (!string.IsNullOrEmpty(accountName))
            {
                sqlBuilder.Append(" and u.`account_name` = @accountName");
                parameters.Add(new MySqlParameter("@accountName", MySqlDbType.VarChar) { Value = accountName });
            }
            else
            {
                totalPage = (int)Math.Ceiling((long)await this._dataAccessor.ExecuteScalar(
                    @"select count(distinct r.`user_id`) from `recharge` as r "+ sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray())) / (double)pageSize);

                List<Recharge> summaryPayment = await this._dataAccessor.GetAll<Recharge>(
                    "select * from `recharge` as r " + sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray()));

                summary = new RechargeReport()
                {
                    SubmitAmount = summaryPayment.Sum(x => x.Amount),
                    SubmitCount = summaryPayment.Count(),
                    SettleAmount = summaryPayment.Where(x => x.SettleTime != 0).Sum(x=>x.Amount),
                    SettleCount = summaryPayment.Where(x => x.SettleTime != 0).Count()
                };
            }

            sqlBuilder.Append(" group by u.`account_name`, u.`id` limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int32) { Value = (page - 1) * pageSize });

            List<RechargeReport> reportData = await this._dataAccessor.GetAll<RechargeReport>(
                @"select u.`id` as user_id,u.`account_name`, 
                        sum(r.`amount`) as submit_amount,
                        count(1) as submit_count,
                        sum(case when r.`settle_time` is null then 0 else r.`amount` end) as settle_amount,
                        sum(case when r.`settle_time` is null then 0 else 1 end) as settle_count
                        from `recharge` as r
                        join `user_account` as u on u.`id` = r.`user_id` " + sqlBuilder.ToString(),
                p => p.AddRange(parameters.ToArray()));

            return this.Ok(new WebApiResult<Report>(new Report
            {
                DataPage = new DataPage<RechargeReport>
                {
                    Page = page,
                    TotalPages = totalPage,
                    Records = reportData
                },
                Summary = summary
            }));
        }

        public class Report
        {
            public DataPage<RechargeReport> DataPage { get; set; }

            public RechargeReport Summary { get; set; }
        }

        public class RechargeReport
        {
            [Column("user_id")]
            public long UserId { get; set; }
            [Column("account_name")]
            public string AccountName { get; set; }
            [Column("submit_amount")]
            public decimal SubmitAmount { get; set; }
            [Column("submit_count")]
            public long SubmitCount { get; set; }
            [Column("settle_amount")]
            public decimal SettleAmount { get; set; }
            [Column("settle_count")]
            public decimal SettleCount { get; set; }
        }
    }
}