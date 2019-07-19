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
    public class CollectChannelController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;

        public CollectChannelController(DataAccessor dataAccessor)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = "ChannelWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<CollectChannel>>> Put([FromRoute]int id, [FromBody]CollectChannel channel)
        {
            if (!channel.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<CollectChannel>(Constants.WebApiErrors.InvalidData, "incomplete channel object"));
            }

            CollectChannel existingOne = await this._dataAccessor.GetOne<CollectChannel>("select * from `collect_channel` where `id`=@id", parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });

            if (existingOne == null)
            {
                return this.NotFound(this.CreateErrorResult<CollectChannel>(Constants.WebApiErrors.ObjectNotFound, "channel not found"));
            }

            channel.Id = (CollectChannelType)id;
            await this._dataAccessor.Execute(Sql.Update, parameters =>
            {
                parameters.Add("@name", MySqlDbType.VarChar).Value = channel.Name;
                parameters.Add("@type", MySqlDbType.Int32).Value = (int)channel.ChannelType;
                parameters.Add("@provider", MySqlDbType.Int32).Value = (int)channel.ChannelProvider;
                parameters.Add("@id", MySqlDbType.Int32).Value = (int)channel.Id;
                parameters.Add("@enabled", MySqlDbType.Bit).Value = channel.Enabled;
                parameters.Add("@instrumentsLimit", MySqlDbType.Int32).Value = channel.InstrumentsLimit;
                parameters.Add("@validTime", MySqlDbType.Int32).Value = channel.ValidTime;
            });
            return this.Ok(new WebApiResult<CollectChannel>(channel));
        }

        [HttpGet]
        [Authorize(Roles = "InstrumentOwner,ChannelReader,AgentMgmt,Agent,PaymentWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<CollectChannel>>>> Get()
        {
            List<CollectChannel> channels = await this._dataAccessor.GetAll<CollectChannel>(Sql.List);
            return this.Ok(new WebApiResult<List<CollectChannel>>(channels));
        }

        [HttpPut("Ratio")]
        [Authorize(Roles = "ChannelWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ChannelProvider>>> UpdateRatio([FromBody]CollectChannel channel)
        {
            if (!Enum.IsDefined(typeof(ChannelProvider), channel.ChannelProvider)) {
                return this.NotFound(this.CreateErrorResult<CollectChannel>(Constants.WebApiErrors.ObjectNotFound, "provider not found"));
            }

            await this._dataAccessor.Execute(Sql.UpdateRatio, parameters =>
            {
                parameters.Add("@ratio", MySqlDbType.Int32).Value = channel.Ratio;
                parameters.Add("@defaultDaliyLimit", MySqlDbType.Int32).Value = channel.DefaultDaliyLimit;
                parameters.Add("@provider", MySqlDbType.Int32).Value = (int)channel.ChannelProvider;
                
            });
            return this.Ok(new WebApiResult<ChannelProvider>());
        }

        [HttpGet("Ratio")]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<CollectChannel>>>> GetRatio()
        {
            List<CollectChannel> channels = await this._dataAccessor.GetAll<CollectChannel>(Sql.Ratio);
            return this.Ok(new WebApiResult<List<CollectChannel>>(channels));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ChannelWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<CollectChannel>>> Delete(int id)
        {
            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>("select * from `collect_channel` where `id`=@id", parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            if (channel == null)
            {
                return this.NotFound(this.CreateErrorResult<CollectChannel>(Constants.WebApiErrors.ObjectNotFound, "channel not found"));
            }

            await this._dataAccessor.Execute(Sql.Delete, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            return this.NoContent();
        }

        private static class Sql
        {
            internal const string Insert = "insert into `collect_channel`(`name`, `type`, `ratio`, `provider`, `enabled`, `instruments_limit`, `valid_time`) values(@name, @type, @ratio, @provider, @enabled, @instrumentsLimit, @validTime)";
            internal const string List = "select * from `collect_channel` order by `id` asc";
            internal const string Update = "update `collect_channel` set `name`=@name, `type`=@type, `enabled`=@enabled, `instruments_limit`=@instrumentsLimit, `valid_time`=@validTime where `id`=@id";
            internal const string Delete = "delete `collect_channel` where `id`=@id";
            internal const string Ratio = "select `ratio`,`provider`,`default_daliy_limit` from `collect_channel` group by `ratio`,`provider`,`default_daliy_limit`;";
            internal const string UpdateRatio = "update `collect_channel` set `ratio`=@ratio,`default_daliy_limit`=@defaultDaliyLimit where `provider`=@provider";
        }
    }
}
