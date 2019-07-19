using Core.Data;

namespace GroupPay.Models
{
    public class RolePermission
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("perm")]
        public string Permission { get; set; }
    }
}
