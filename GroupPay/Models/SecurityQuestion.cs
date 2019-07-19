using Core.Data;

namespace GroupPay.Models
{
    public class SecurityQuestion
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("question")]
        public string Question { get; set; }
    }
}
