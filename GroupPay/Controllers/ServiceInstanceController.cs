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
    public class ServiceInstanceController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;

        public ServiceInstanceController(DataAccessor dataAccessor)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ServiceInstance>>> Post([FromBody]ServiceInstance instance)
        {
            if (!instance.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<ServiceInstance>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(Sql.Insert, parameters =>
                {
                    parameters.Add("@service_id", MySqlDbType.Int32).Value = instance.ServiceId;
                    parameters.Add("@cluster", MySqlDbType.VarChar).Value = instance.Cluster;
                    parameters.Add("@server", MySqlDbType.VarChar).Value = instance.Server;
                    parameters.Add("@endpoint", MySqlDbType.VarChar).Value = instance.Endpoint;
                });
                instance.Id = await transaction.GetLastInsertId32();
                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<ServiceInstance>(instance));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ServiceInstance>>> Put([FromRoute]int id, [FromBody]ServiceInstance instance)
        {
            if (!instance.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<ServiceInstance>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            ServiceInstance existingOne = await this._dataAccessor.GetOne<ServiceInstance>("select * from `service_instance` where id=@id", parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            if (existingOne == null)
            {
                return this.NotFound(this.CreateErrorResult<ServiceInstance>(Constants.WebApiErrors.ObjectNotFound, "service instance not found"));
            }

            instance.Id = id;
            await this._dataAccessor.Execute(Sql.Update, parameters =>
            {
                parameters.Add("@service_id", MySqlDbType.Int32).Value = instance.ServiceId;
                parameters.Add("@cluster", MySqlDbType.VarChar).Value = instance.Cluster;
                parameters.Add("@server", MySqlDbType.VarChar).Value = instance.Server;
                parameters.Add("@endpoint", MySqlDbType.VarChar).Value = instance.Endpoint;
                parameters.Add("@id", MySqlDbType.Int32).Value = instance.Id;
            });
            return this.Ok(new WebApiResult<ServiceInstance>(instance));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ServiceInstance>>> Delete(int id)
        {
            ServiceInstance instance = await this._dataAccessor.GetOne<ServiceInstance>("select * from `service_instance` where id=@id", parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            if (instance == null)
            {
                return this.NotFound(this.CreateErrorResult<ServiceInstance>(Constants.WebApiErrors.ObjectNotFound, "service instance not found"));
            }

            await this._dataAccessor.Execute(Sql.Delete, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            return this.NoContent();
        }

        [HttpGet]
        [Authorize(Roles = "ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<ServiceInstance>>>> Get([FromQuery]int serviceId)
        {
            List<ServiceInstance> instances = await this._dataAccessor.GetAll<ServiceInstance>(Sql.ListByService, parameters =>
            {
                parameters.Add("@service_id", MySqlDbType.Int32).Value = serviceId;
            });
            return this.Ok(new WebApiResult<List<ServiceInstance>>(instances));
        }

        private static class Sql
        {
            internal const string Insert = "insert into `service_instance`(`service_id`, `cluster`, `server`, `endpoint`) values(@service_id, @cluster, @server, @endpoint)";
            internal const string Update = "update `service_instance` set `service_id`=@service_id, `cluster`=@cluster, `server`=@server, `endpoint`=@endpoint where `id`=@id";
            internal const string ListByService = "select * from `service_instance` where `service_id`=@service_id order by `id` asc";
            internal const string Delete = "delete from `service_instance` where `id`=@id";
        }
    }
}
