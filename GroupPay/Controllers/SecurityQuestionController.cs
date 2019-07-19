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
    public class SecurityQuestionController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;

        public SecurityQuestionController(DataAccessor dataAccessor)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<SecurityQuestion>>> Post([FromBody]SecurityQuestion securityQuestion)
        {
            if (!securityQuestion.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<SecurityQuestion>(Constants.WebApiErrors.InvalidData, "invalid question data"));
            }

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(
                    "insert into `security_question`(`question`) values(@question)", 
                    p => p.Add("@question", MySqlDbType.VarChar).Value = securityQuestion.Question);
                securityQuestion.Id = await transaction.GetLastInsertId32();
                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<SecurityQuestion>(securityQuestion));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<SecurityQuestion>>> Put([FromRoute]int id, [FromBody]SecurityQuestion securityQuestion)
        {
            if (!securityQuestion.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<SecurityQuestion>(Constants.WebApiErrors.InvalidData, "invalid question data"));
            }

            SecurityQuestion item = await this._dataAccessor.GetOne<SecurityQuestion>(
                "select * from `security_question` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = id);
            if (item == null)
            {
                return this.NotFound(this.CreateErrorResult<SecurityQuestion>(Constants.WebApiErrors.ObjectNotFound, "question not found"));
            }

            await this._dataAccessor.Execute(
                "update `security_question` set `question`=@question where `id`=@id",
                p =>
                {
                    p.Add("@question", MySqlDbType.VarChar).Value = securityQuestion.Question;
                    p.Add("@id", MySqlDbType.Int32).Value = id;
                });
            securityQuestion.Id = id;
            return this.Ok(new WebApiResult<SecurityQuestion>(securityQuestion));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<SecurityQuestion>>> Delete([FromRoute]int id)
        {
            SecurityQuestion item = await this._dataAccessor.GetOne<SecurityQuestion>(
                "select * from `security_question` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = id);
            if (item == null)
            {
                return this.NotFound(this.CreateErrorResult<SecurityQuestion>(Constants.WebApiErrors.ObjectNotFound, "questio not found"));
            }

            await this._dataAccessor.Execute(
                "delete from `security_question` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = id);
            return this.NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<List<SecurityQuestion>>> Get()
        {
            List<SecurityQuestion> questions = await this._dataAccessor.GetAll<SecurityQuestion>("select * from `security_question` order by `id`");
            return this.Ok(new WebApiResult<List<SecurityQuestion>>(questions));
        }

    }
}
