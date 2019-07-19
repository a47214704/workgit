using Core.Data;
namespace GroupPay.Models
{
    public class Recharge
    {
        [Column("id")]
        public string Id { get; set; }
        [Column("channel")]
        public int Channel { get; set; }
        [Column("amount")]
        public int Amount { get; set; }
        [Column("user_id")]
        public long UserId { get; set; }
        [Column("create_time")]
        public long CreateTime { get; set; }
        [Column("settle_time")]
        public long SettleTime { get; set; }
    }
}
