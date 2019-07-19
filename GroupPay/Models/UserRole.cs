using System.Collections.Generic;
using Core.Data;

namespace GroupPay.Models
{
    public enum UserRoleType
    {
        /// <summary>
        /// 卡商
        /// </summary>
        Member = 100,

        /// <summary>
        /// 管理员
        /// </summary>
        SA,

        /// <summary>
        /// 运维
        /// </summary>
        Operations,

        /// <summary>
        /// 客服
        /// </summary>
        CS,

        /// <summary>
        /// 查单
        /// </summary>
        Checker,

        /// <summary>
        /// 查账
        /// </summary>
        CA,

        /// <summary>
        /// 商户
        /// </summary>
        Agent,

        /// <summary>
        /// 商戶總代理
        /// </summary>
        AgentMaster
    }

    public class UserRole
    {
        public UserRole()
        {
            this.Permissions = new List<string>();
        }

        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("memo")]
        public string Memo { get; set; }

        public List<string> Permissions { get; private set; }
    }
}
