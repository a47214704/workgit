using System;
using System.Collections.Generic;
using System.Threading;
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
    [ApiController]
    [Route("/api/[controller]")]
    public class CommissionRatioController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;
        private readonly ILogger _logger;

        public CommissionRatioController(DataAccessor dataAccessor, ILoggerFactory loggerFactory)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._logger = loggerFactory?.CreateLogger<CommissionRatioController>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<CommissionRatio>>> Post([FromBody]CommissionRatio commissionRatio)
        {
            if (!commissionRatio.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<CommissionRatio>(Constants.WebApiErrors.InvalidData, "invalid commission ratio"));
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "UpdateOrCreateCommissionRatio", this.TraceActivity()))
            {
                List<CommissionRatio> ratios = await this._dataAccessor.GetAll<CommissionRatio>("select * from `commission_ratio` order by `lbound`");
                foreach (CommissionRatio ratio in ratios)
                {
                    if ((commissionRatio.LowerBound <= ratio.LowerBound && (ratio.LowerBound < commissionRatio.UpperBound || commissionRatio.UpperBound < 0)) ||
                        ((commissionRatio.UpperBound >= ratio.UpperBound || commissionRatio.UpperBound < 0) && (ratio.UpperBound > commissionRatio.LowerBound || ratio.UpperBound < 0)))
                    {
                        if (commissionRatio.LowerBound == ratio.LowerBound && commissionRatio.UpperBound == ratio.UpperBound)
                        {
                            // duplicate, do update
                            await this._dataAccessor.Execute(
                                "update `commission_ratio` set `ratio`=@ratio where `lbound`=@lbound and `ubound`=@ubound",
                                p =>
                                {
                                    p.Add("@ratio", MySqlDbType.Double).Value = commissionRatio.Ratio;
                                    p.Add("@lbound", MySqlDbType.Int32).Value = commissionRatio.LowerBound;
                                    p.Add("@ubound", MySqlDbType.Int32).Value = commissionRatio.UpperBound;
                                });
                            return this.Ok(new WebApiResult<CommissionRatio>(commissionRatio));
                        }
                        else
                        {
                            return this.BadRequest(this.CreateErrorResult<CommissionRatio>(Constants.WebApiErrors.InvalidData, "range overlapped"));
                        }
                    }
                }

                // if there is no overlapped nor duplication, we do add
                await this._dataAccessor.Execute(
                    "insert into `commission_ratio`(`lbound`, `ubound`, `ratio`) values(@lbound, @ubound, @ratio)",
                    p =>
                    {
                        p.Add("@lbound", MySqlDbType.Int32).Value = commissionRatio.LowerBound;
                        p.Add("@ubound", MySqlDbType.Int32).Value = commissionRatio.UpperBound;
                        p.Add("@ratio", MySqlDbType.Double).Value = commissionRatio.Ratio;
                    });
                return this.Ok(new WebApiResult<CommissionRatio>(commissionRatio));
            }
        }

        [HttpGet]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<CommissionRatio>>>> Get()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "ListCommissionRatios", this.TraceActivity()))
            {
                List<CommissionRatio> ratios = await this._dataAccessor.GetAll<CommissionRatio>("select * from `commission_ratio` order by `lbound`");
                return this.Ok(new WebApiResult<List<CommissionRatio>>(ratios));
            }
        }

        [HttpDelete("{lbound}/{ubound}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult> Delete(int lbound, int ubound)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            using (OperationMonitor operationMonitor = new OperationMonitor(this._logger, Constants.Events.IncomingRequest, "DeleteCommissionRatio", this.TraceActivity()))
            {
                await this._dataAccessor.Execute(
                    "delete from `commission_ratio` where `lbound`=@lbound and `ubound`=@ubound",
                    p =>
                    {
                        p.Add("@lbound", MySqlDbType.Int32).Value = lbound;
                        p.Add("@ubound", MySqlDbType.Int32).Value = ubound;
                    });
                return this.NoContent();
            }
        }
    }
}
