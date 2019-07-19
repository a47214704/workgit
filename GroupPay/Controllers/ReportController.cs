using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace GroupPay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private const int pageSize = 20;
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _siteConfig;
        private readonly ILogger<ReportController> _logger;
        public ReportController(DataAccessor dataAccessor, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._siteConfig = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._logger = loggerFactory.CreateLogger<ReportController>();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "ReportReader")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<Report>>> Get(
            [FromQuery]int page,
            [FromQuery]string startTime,
            [FromQuery]string endTime,
            [FromQuery]string accountName)
        {
            page = page < 1 ? 1 : page;
            int totalPage = 0;
            long startTimestamp;
            long endTimestamp = 0;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out DateTime dateTime))
            {
                startTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else
            {
                startTimestamp = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<PaymentItem> summary = null;

            sqlBuilder.Append("where p.`status` = @status and p.`settle_time` >= @startTime");
            parameters.Add(new MySqlParameter("@status", MySqlDbType.Int32) { Value = PaymentStatus.Settled });
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out dateTime))
            {
                endTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                sqlBuilder.Append(" and p.`settle_time` < @endTime");
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
                    @"select count(distinct ci.`user_id`) from `payment` as p 
                            join `collect_instrument` as ci on p.`ciid` = ci.`id` " + sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray())) / (double)pageSize);

                summary = await this._dataAccessor.GetAll<PaymentItem>(
                    "select p.`channel`, sum(p.`amount`) as amount from `payment` as p " + sqlBuilder.ToString() +
                            " group by p.`channel`",
                    p => p.AddRange(parameters.ToArray()));
            }

            sqlBuilder.Append(" group by u.`account_name` limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int32) { Value = (page - 1) * pageSize });

            List<PaymentReport> reportData = await this._dataAccessor.GetAll<PaymentReport>(
                @"select u.`account_name`, 
                        sum(case when p.`channel` = 1 then p.`amount` else 0 end) as wechat_amount,
                        sum(case when p.`channel` = 2 then p.`amount` else 0 end) as alipay_amount,
                        sum(case when p.`channel` = 3 then p.`amount` else 0 end) as unionpay_amount,
                        sum(case when p.`channel` = 4 then p.`amount` else 0 end) as aliRedEnvelope_amount,
                        sum(case when p.`channel` = 5 then p.`amount` else 0 end) as uBank_amount,
                        sum(case when p.`channel` = 6 then p.`amount` else 0 end) as aliToCard_amount,
                        sum(case when p.`channel` = 7 then p.`amount` else 0 end) as aliWap_amount,
                        sum(case when p.`channel` = 8 then p.`amount` else 0 end) as wechatWap_amount,
                        sum(case when p.`channel` = 9 then p.`amount` else 0 end) as aliH5_amount,
                        sum(case when p.`channel` = 10 then p.`amount` else 0 end) as wechatH5_amount
                        from `payment` as p 
                        join `collect_instrument` as ci on p.`ciid` = ci.`id` 
                        join `user_account` as u on u.`id` = ci.`user_id` " + sqlBuilder.ToString(), 
                p => p.AddRange(parameters.ToArray()));
            this._logger.LogInformation($"get report by time {startTimestamp} ~ {endTimestamp}");
            return this.Ok(new WebApiResult<Report>(new Report
            {
                DataPage = new DataPage<PaymentReport>
                {
                    Page = page,
                    TotalPages = totalPage,
                    Records = reportData
                },
                Summary = summary
            }));
        }

        [HttpGet("successRate")]
        [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "SelfMgmt")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<SuccessRate>>>> Get(
            [FromQuery]string startTime,
            [FromQuery]string endTime) {
            long startTimestamp;
            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out DateTime dateTime))
            {
                startTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else
            {
                startTimestamp = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            }

            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            if (this.HttpContext.User.HasRole("Agent"))
            {
                sqlBuilder.Append(" join `merchant` as m on m.`id`=p.`merchant_id` and m.`user_id`=@id");
                parameters.Add(new MySqlParameter("@id", MySqlDbType.Int64) { Value = this.HttpContext.User.GetId() });
            }

            if (parameters.Count > 0)
            {
                sqlBuilder.Append(" and");
            }
            else
            {
                sqlBuilder.Append(" where");
            }

            sqlBuilder.Append(" p.`create_time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out dateTime))
            {
                sqlBuilder.Append(" and p.`create_time` < @endTime");
                parameters.Add(new MySqlParameter("@endTime", MySqlDbType.Int64) { Value = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds() });
            }
            
            List<SuccessRate> totals = await this._dataAccessor.GetAll<SuccessRate>(
                    "select count(1) as total, count(if(p.status=3, 1, null)) as success, cc.name from `payment` as p join collect_channel as cc on cc.id = p.channel" + sqlBuilder.ToString() + " group by channel",
                    p => p.AddRange(parameters.ToArray()));

            return this.Ok(new WebApiResult<List<SuccessRate>>(totals));
        }

        public class PaymentItem
        {
            [Column("channel")]
            public int Channel { get; set; }

            [Column("amount")]
            public decimal Amount { get; set; }
        }

        public class Report
        {
            public DataPage<PaymentReport> DataPage { get; set; }

            public List<PaymentItem> Summary { get; set; }
        }

        public class SuccessRate
        {
            [Column("name")]
            public string Name { get; set; }
            
            [Column("total")]
            public long Total { get; set; }

            [Column("success")]
            public long Success { get; set; }
        }
    }
}