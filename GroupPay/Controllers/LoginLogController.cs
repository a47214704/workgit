using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginLogController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;

        public LoginLogController(DataAccessor dataAccessor)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        [HttpGet]
        [Authorize(Roles = "LogReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<LoginLog>>>> Get([FromQuery]long userId)
        {
            List<LoginLog> logs = await this._dataAccessor.GetAll<LoginLog>(
                "select l.*, u.account_name as user_account_name from login_log as l join user_account as u on l.user_id = u.id where l.user_id=@userId",
                parameters => parameters.Add("@userId", MySqlDbType.Int64).Value = userId);
            return this.Ok(new WebApiResult<List<LoginLog>>(logs));
        }

        [HttpDelete]
        [Authorize(Roles = "LogWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult> Delete([FromQuery]long userId)
        {
            await this._dataAccessor.Execute("delete from login_log where user_id=@userId", p => p.Add("@userId", MySqlDbType.Int64).Value = userId);
            return this.NoContent();
        }
    }
}
