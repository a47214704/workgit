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
    public class TransactionLogController : ControllerBase
    {
        private const int pageSize = 20;
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _siteConfig;
        private readonly ILogger<TransactionLogController> _logger;
        public TransactionLogController(DataAccessor dataAccessor, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._siteConfig = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._logger = loggerFactory.CreateLogger<TransactionLogController>();
        }

        [HttpGet("Report")]
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
            List<ReportItem> summary = null;

            sqlBuilder.Append("where tl.`time` >= @startTime");
            parameters.Add(new MySqlParameter("@startTime", MySqlDbType.Int64) { Value = startTimestamp });

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime.ToTimeZoneDateTime(this._siteConfig.TimeZoneOffset), out dateTime))
            {
                endTimestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                sqlBuilder.Append(" and tl.`time` < @endTime");
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
                    @"select count(distinct u.`id`) from `transaction_log` as tl join `user_account` as u on u.`id` = tl.`user_id`" + sqlBuilder.ToString(),
                    p => p.AddRange(parameters.ToArray())) / (double)pageSize);

                summary = await this._dataAccessor.GetAll<ReportItem>(
                    "select tl.`type`, sum(tl.`amount`) as amount from `transaction_log` as tl " + sqlBuilder.ToString() +
                            " group by tl.`type`",
                    p => p.AddRange(parameters.ToArray()));
            }

            sqlBuilder.Append(" group by u.`account_name` limit @pageSize offset @offset");
            parameters.Add(new MySqlParameter("@pageSize", MySqlDbType.Int32) { Value = pageSize });
            parameters.Add(new MySqlParameter("@offset", MySqlDbType.Int32) { Value = (page - 1) * pageSize });

            List<ReportPage> reportData = await this._dataAccessor.GetAll<ReportPage>(
                @"select u.`account_name`, 
                        sum(case when tl.`type` = 0 then tl.`amount` else 0 end) as modify_amount,
                        sum(case when tl.`type` = 1 then tl.`amount` else 0 end) as redeem_amount,
                        sum(case when tl.`type` = 2 then tl.`amount` else 0 end) as refund_amount,
                        sum(case when tl.`type` = 3 then tl.`amount` else 0 end) as refill_amount,
                        sum(case when tl.`type` = 6 then tl.`amount` else 0 end) as rewards_amount,
                        sum(case when tl.`type` = 7 then tl.`amount` else 0 end) as commission_amount,
                        sum(case when tl.`type` = 8 then tl.`amount` else 0 end) as manualRedeem_amount
                        from `transaction_log` as tl 
                        join `user_account` as u on u.`id` = tl.`user_id` " + sqlBuilder.ToString(), 
                p => p.AddRange(parameters.ToArray()));
            this._logger.LogInformation($"get transaction log by time {startTimestamp} ~ {endTimestamp}");
            return this.Ok(new WebApiResult<Report>(new Report
            {
                DataPage = new DataPage<ReportPage>
                {
                    Page = page,
                    TotalPages = totalPage,
                    Records = reportData
                },
                Summary = summary
            }));
        }

        public class ReportItem
        {
            [Column("type")]
            public TransactionType Type { get; set; }

            [Column("amount")]
            public decimal Amount { get; set; }
        }

        public class Report
        {
            public DataPage<ReportPage> DataPage { get; set; }

            public List<ReportItem> Summary { get; set; }
        }

        public class ReportPage
        {
            [Column("account_name")]
            public string AccountName { get; set; }

            [Column("redeem_amount")]
            public decimal RedeemAmount { get; set; }

            [Column("refund_amount")]
            public decimal RefundAmount { get; set; }

            [Column("refill_amount")]
            public decimal RefillAmount { get; set; }

            [Column("rewards_amount")]
            public decimal RewardsAmount { get; set; }

            [Column("commission_amount")]
            public decimal CommissionAmount { get; set; }

            [Column("manualRedeem_amount")]
            public decimal ManualRedeemAmount { get; set; }

            [Column("modify_amount")]
            public decimal ModifyAmount { get; set; }
            
        }
    }
}