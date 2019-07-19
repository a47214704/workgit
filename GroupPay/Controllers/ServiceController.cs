using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroupPay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceAccountStore _store;

        public ServiceController(IServiceAccountStore serviceAcccountStore)
        {
            this._store = serviceAcccountStore ?? throw new ArgumentNullException(nameof(serviceAcccountStore));
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ServiceAccount>>> Post([FromBody]ServiceAccount service)
        {
            if (!service.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<ServiceAccount>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            await this._store.AddAccount(service);
            return this.Ok(service);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ServiceAccount>>> Put([FromRoute]int id, [FromBody]ServiceAccount service)
        {
            if (!service.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<ServiceAccount>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            ServiceAccount account = await this._store.FindAccountById(id);
            if (account == null)
            {
                return this.NotFound(this.CreateErrorResult<ServiceAccount>(Constants.WebApiErrors.ObjectNotFound, "service account not found"));
            }

            service.Id = account.Id;
            await this._store.UpdateAccount(service);
            return this.Ok(new WebApiResult<ServiceAccount>(service));
        }

        [HttpGet]
        [Authorize(Roles = "ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<ServiceAccount>>>> Get()
        {
            List<ServiceAccount> accounts = await this._store.ListAccounts();
            return Ok(new WebApiResult<List<ServiceAccount>>(accounts));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WebApiResult<ServiceAccount>>> Get(int id)
        {
            ServiceAccount account = await this._store.FindAccountById(id);
            if (account == null)
            {
                return this.NotFound(this.CreateErrorResult<ServiceAccount>(Constants.WebApiErrors.ObjectNotFound, "service account not found"));
            }

            return this.Ok(new WebApiResult<ServiceAccount>(account));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<ServiceAccount>>> Delete(int id)
        {
            ServiceAccount account = await this._store.FindAccountById(id);
            if (account == null)
            {
                return this.NotFound(this.CreateErrorResult<ServiceAccount>(Constants.WebApiErrors.ObjectNotFound, "service account not found"));
            }

            await this._store.DeleteAccount(id);
            return this.NoContent();
        }
    }
}
