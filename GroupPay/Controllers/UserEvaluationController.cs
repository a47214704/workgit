using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; 

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserEvaluationController : Controller
    {
        private readonly SiteConfig _config;
        private readonly DataAccessor _dataAccessor;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UserEvaluationController> _logger;
        

        public UserEvaluationController(IDistributedCache cache, DataAccessor dataAccessor, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            this._config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this._logger = loggerFactory.CreateLogger<UserEvaluationController>();
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Post([FromBody]UserEvaluation evaluation)
        {
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(Sql.Insert, parameters =>
                {
                    parameters.Add("@type", MySqlDbType.Int32).Value = (int)evaluation.Type;
                    parameters.Add("@condition", MySqlDbType.Int32).Value = evaluation.Condition;
                    parameters.Add("@count", MySqlDbType.Int32).Value = evaluation.Count;
                    parameters.Add("@value", MySqlDbType.Int32).Value = evaluation.Value;
                    parameters.Add("@group", MySqlDbType.Int32).Value = evaluation.Group;
                    parameters.Add("@repeat", MySqlDbType.Int32).Value = evaluation.Repeat;
                });
                evaluation.Id = await transaction.GetLastInsertId32();
                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<UserEvaluation>(evaluation));
        }

        [HttpGet]
        [Authorize(Roles = "ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Get()
        {
            return this.Ok(new WebApiResult<UserEvaluationForm>(new UserEvaluationForm
            {
                UserEvaluations = await this._dataAccessor.GetAll<UserEvaluation>(Sql.List)
            }));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserEvaluation>>> Put([FromRoute]int id, [FromBody]UserEvaluation evaluation)
        {
            UserEvaluation existingOne = await this._dataAccessor.GetOne<UserEvaluation>(Sql.GetOne, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int64).Value = id;
            });

            if (existingOne == null)
            {
                return this.NotFound(this.CreateErrorResult<UserEvaluation>(Constants.WebApiErrors.ObjectNotFound, "evaluation not found"));
            }

            evaluation.Id = id;
            await this._dataAccessor.Execute(Sql.Update, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int64).Value = id;
                parameters.Add("@condition", MySqlDbType.Int32).Value = evaluation.Condition;
                parameters.Add("@count", MySqlDbType.Int32).Value = evaluation.Count;
                parameters.Add("@value", MySqlDbType.Int32).Value = evaluation.Value;
                parameters.Add("@group", MySqlDbType.Int32).Value = evaluation.Group;
                parameters.Add("@repeat", MySqlDbType.Int32).Value = evaluation.Repeat;
            });
            return this.Ok(new WebApiResult<UserEvaluation>(evaluation));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserEvaluation>>> Delete(int id)
        {
            UserEvaluation award = await this._dataAccessor.GetOne<UserEvaluation>(Sql.GetOne, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int64).Value = id;
            });
            if (award == null)
            {
                return this.NotFound(this.CreateErrorResult<UserEvaluation>(Constants.WebApiErrors.ObjectNotFound, "evaluation not found"));
            }

            await this._dataAccessor.Execute(Sql.Delete, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int64).Value = id;
            });
            return this.NoContent();
        }

        private static class Sql
        {
            internal const string Insert = "INSERT INTO `user_evaluation`(`type`,`condition`,`count`,`value`,`group`,`repeat`)VALUES(@type,@condition,@count,@value,@group,@repeat);";
            internal const string List = "select * from `user_evaluation`";
            internal const string GetOne = "select * from `user_evaluation` where id=@id";
            internal const string Update = "update `user_evaluation` set `condition`=@condition,`count`=@count,`value`=@value,`group`=@group,`repeat`=@repeat where `id`=@id";
            internal const string Delete = "delete from `user_evaluation` where `id`=@id";
        }
    }

    class UserEvaluationForm
    {
        public List<UserEvaluation> UserEvaluations { get; set; }
        public List<UserEvaluation> PayAllowLimits { get { return UserEvaluations.Where(p => p.Type == UserEvaluationType.PayAllowLimits).ToList(); } }
        public List<UserEvaluation> OverTimePunishs { get { return UserEvaluations.Where(p => p.Type == UserEvaluationType.OverTimePunish).ToList(); } }
        public List<UserEvaluation> SpeedPayCommends { get { return UserEvaluations.Where(p => p.Type == UserEvaluationType.SpeedPayCommend).ToList(); } }
    }
}