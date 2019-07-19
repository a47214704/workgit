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
    public class UserRoleController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;

        public UserRoleController(DataAccessor dataAccessor)
        {
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        [HttpPost]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserRole>>> Post([FromBody]UserRole role)
        {
            if (!role.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute(Sql.Insert, parameters =>
                {
                    parameters.Add("@name", MySqlDbType.VarChar).Value = role.Name;
                    parameters.Add("@memo", MySqlDbType.VarChar).Value = role.Memo ?? string.Empty;
                });
                role.Id = await transaction.GetLastInsertId32();
                await transaction.Commit();
            }

            return this.Ok(new WebApiResult<UserRole>(role));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserRole>>> Put([FromRoute]int id, [FromBody]UserRole role)
        {
            if (!role.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            UserRole existingOne = await this._dataAccessor.GetOne<UserRole>(
                "select * from user_role where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = id);
            if (existingOne == null)
            {
                return this.NotFound(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectNotFound, "role not found"));
            }

            role.Id = id;
            await this._dataAccessor.Execute(Sql.Update, parameters =>
            {
                parameters.Add("@name", MySqlDbType.VarChar).Value = role.Name;
                parameters.Add("@memo", MySqlDbType.VarChar).Value = role.Memo ?? string.Empty;
                parameters.Add("@id", MySqlDbType.Int32).Value = role.Id;
            });
            return this.Ok(new WebApiResult<UserRole>(role));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserRole>>> Delete(int id)
        {
            UserRole role = await this._dataAccessor.GetOne<UserRole>(Sql.GetOne, parameters =>
            {
                parameters.Add("@id", MySqlDbType.Int32).Value = id;
            });
            if (role == null)
            {
                return this.NotFound(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectNotFound, "role not found"));
            }

            await this._dataAccessor.Execute(Sql.Delete, p => p.Add("@id", MySqlDbType.Int32).Value = id);
            return this.NoContent();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "ConfigReader", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserRole>>> Get(int id)
        {
            UserRole role = await this._dataAccessor.GetOne<UserRole>(Sql.GetOne, p => p.Add("@id", MySqlDbType.Int32).Value = id);
            if (role == null)
            {
                return this.NotFound(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectNotFound, "role not found"));
            }

            role.Permissions.AddRange(await this.GetPermissionsForRole(id));
            return this.Ok(new WebApiResult<UserRole>(role));
        }

        [HttpGet]
        [Authorize(Roles = "SelfMgmt", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<List<UserRole>>>> Get()
        {
            List<UserRole> roles = await this._dataAccessor.GetAll<UserRole>(Sql.List);
            foreach (UserRole role in roles)
            {
                role.Permissions.AddRange(await this.GetPermissionsForRole(role.Id));
            }

            return this.Ok(new WebApiResult<List<UserRole>>(roles));
        }

        [HttpPost("{roleId}/perms")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserRole>>> AddPerm(int roleId, [FromBody]string perm)
        {
            if (string.IsNullOrEmpty(perm))
            {
                return this.BadRequest(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            UserRole role = await this._dataAccessor.GetOne<UserRole>(Sql.GetOne, p => p.Add("@id", MySqlDbType.Int32).Value = roleId);
            if (role == null)
            {
                return this.NotFound(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectNotFound, "role not found"));
            }

            role.Permissions.AddRange(await this.GetPermissionsForRole(roleId));
            if (role.Permissions.Contains(perm))
            {
                return this.Conflict(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectConflict, "permission already assigned"));
            }

            role.Permissions.Add(perm);
            await this._dataAccessor.Execute(Sql.AddPerm, p =>
            {
                p.Add("@roleId", MySqlDbType.Int32).Value = roleId;
                p.Add("@perm", MySqlDbType.VarChar).Value = perm;
            });
            return this.Ok(new WebApiResult<UserRole>(role));
        }

        [HttpDelete("{roleId}/perms/{perm}")]
        [Authorize(Roles = "ConfigWriter", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public async Task<ActionResult<WebApiResult<UserRole>>> DeletePerm(int roleId, string perm)
        {
            UserRole role = await this._dataAccessor.GetOne<UserRole>(Sql.GetOne, p => p.Add("@id", MySqlDbType.Int32).Value = roleId);
            if (role == null)
            {
                return this.NotFound(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectNotFound, "role not found"));
            }

            role.Permissions.AddRange(await this.GetPermissionsForRole(roleId));
            if (!role.Permissions.Contains(perm))
            {
                return this.NotFound(this.CreateErrorResult<UserRole>(Constants.WebApiErrors.ObjectNotFound, "permission not found"));
            }

            await this._dataAccessor.Execute(Sql.DeletePerm, p =>
            {
                p.Add("@roleId", MySqlDbType.Int32).Value = roleId;
                p.Add("@perm", MySqlDbType.VarChar).Value = perm;
            });
            return this.NoContent();
        }

        private Task<List<string>> GetPermissionsForRole(int roleId)
        {
            return this._dataAccessor.GetAll<string>(
                Sql.ListPerms,
                new SimpleRowMapper<string>(reader => Task.FromResult(reader.GetString(0))),
                p => p.Add("@roleId", MySqlDbType.Int32).Value = roleId);
        }

        private static class Sql
        {
            internal const string Insert = "insert into `user_role`(`name`, `memo`) values(@name, @memo)";
            internal const string Update = "update `user_role` set `name`=@name, `memo`=@memo where `id`=@id";
            internal const string Delete = "delete `user_role` where `id`=@id";
            internal const string GetOne = "select * from `user_role` where `id`=@id";
            internal const string ListPerms = "select `perm` from `role_perm` where `role_id`=@roleId";
            internal const string List = "select * from `user_role` order by `id` asc";
            internal const string AddPerm = "insert into `role_perm`(`role_id`, `perm`) values(@roleId, @perm)";
            internal const string DeletePerm = "delete `role_perm` where `role_id`=@roleId and `perm`=@perm";
        }
    }
}
