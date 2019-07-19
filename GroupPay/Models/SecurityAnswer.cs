using Core.Data;

namespace GroupPay.Models
{
    public class SecurityAnswer
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Prefix("q_")]
        public SecurityQuestion Question { get; set; }

        [Column("answer")]
        public string Answer { get; set; }
    }
}
