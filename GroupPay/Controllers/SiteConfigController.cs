using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GroupPay.Models;
using Core;
using Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiteConfigController : ControllerBase
    {
        public SiteConfigController(SiteConfig siteConfig, DataAccessor dataAccessor)
        {
            this.Config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this.DataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        private SiteConfig Config { get; set; }

        private DataAccessor DataAccessor { get; set; }

        [HttpGet]
        [Authorize(Roles = "ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<List<ConfigItem>>>> Get()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                List<ConfigItem> configs = await this.DataAccessor.GetAll<ConfigItem>(context, "select * from `site_config` order by `id`");
                return this.Ok(new WebApiResult<List<ConfigItem>>(configs));
            }
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ConfigItem>>> Post([FromBody]ConfigItem siteConfig)
        {
            if (!siteConfig.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<ConfigItem>(Constants.WebApiErrors.InvalidData, "invalid data"));
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                using (DataTransaction transaction = await this.DataAccessor.CreateTransaction(context))
                {
                    await transaction.Execute("insert into `site_config`(`name`, `display_name`, `value`) values(@name, @displayName, @value)",
                        p =>
                        {
                            p.Add("@name", MySqlDbType.VarChar).Value = siteConfig.Name;
                            p.Add("@displayName", MySqlDbType.VarChar).Value = siteConfig.DisplayName;
                            p.Add("@value", MySqlDbType.VarChar).Value = siteConfig.Value;
                        });
                    siteConfig.Id = await transaction.GetLastInsertId32();
                    await transaction.Commit();
                    this.Config.Apply(siteConfig);
                }

                return this.Ok(new WebApiResult<ConfigItem>(siteConfig));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ConfigItem>>> Put([FromRoute]int id, [FromBody]ConfigItem siteConfig)
        {
            if (!siteConfig.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<ConfigItem>(Constants.WebApiErrors.InvalidData, "invalid data"));
            }

            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                using (DataTransaction transaction = await this.DataAccessor.CreateTransaction(context))
                {
                    ConfigItem existingOne = await transaction.GetOne<ConfigItem>("select * from `site_config` where `id`=@id", p => p.Add("@id", MySqlDbType.Int32).Value = id);
                    if (existingOne == null)
                    {
                        return this.NotFound(this.CreateErrorResult<ConfigItem>(Constants.WebApiErrors.ObjectNotFound, "config not found"));
                    }

                    await transaction.Execute("update `site_config` set `name`=@name, `display_name`=@displayName, `value`=@value where `id`=@id",
                        p =>
                        {
                            p.Add("@name", MySqlDbType.VarChar).Value = siteConfig.Name;
                            p.Add("@displayName", MySqlDbType.VarChar).Value = siteConfig.DisplayName;
                            p.Add("@value", MySqlDbType.VarChar).Value = siteConfig.Value;
                            p.Add("@id", MySqlDbType.Int32).Value = id;
                        });
                    await transaction.Commit();
                    this.Config.Apply(siteConfig);
                }

                return this.Ok(new WebApiResult<ConfigItem>(siteConfig));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ConfigItem>>> Delete([FromRoute]int id)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            {
                CallContext context = new CallContext(cts.Token, this.TraceActivity());
                using (DataTransaction transaction = await this.DataAccessor.CreateTransaction(context))
                {
                    ConfigItem existingOne = await transaction.GetOne<ConfigItem>("select * from `site_config` where `id`=@id", p => p.Add("@id", MySqlDbType.Int32).Value = id);
                    if (existingOne == null)
                    {
                        return this.NotFound(this.CreateErrorResult<ConfigItem>(Constants.WebApiErrors.ObjectNotFound, "config not found"));
                    }

                    await transaction.Execute("delete from `site_config` where `id`=@id",
                        p =>
                        {
                            p.Add("@id", MySqlDbType.Int32).Value = id;
                        });
                    await transaction.Commit();
                }

                return this.NoContent();
            }
        }

        [HttpGet("{key}")]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<WebApiResult<ConfigItem>>> GetOne([FromRoute]string key)
        {
            switch (key)
            {
                case "marquee":
                case "wxkf":
                    using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
                    {
                        CallContext context = new CallContext(cts.Token, this.TraceActivity());
                        ConfigItem configs = await this.DataAccessor.GetOne<ConfigItem>(context, "select * from `site_config` where `name` = @key", p => p.Add("@key", MySqlDbType.VarChar).Value = key);
                        return this.Ok(new WebApiResult<ConfigItem>(configs));
                    }   
                default:
                    return this.BadRequest(this.CreateErrorResult<ConfigItem>(Constants.WebApiErrors.InvalidCredentials, "Invalid Credentials"));
            }
            
        }
    }
}
