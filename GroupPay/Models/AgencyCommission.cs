using Core.Data;

namespace GroupPay.Models
{
    public class AgencyCommission
    {
        [Column("user_id")]
        public long UserId { get; set; }

        [Column("account_name")]
        public string AccountName { get; set; }

        [Column("week")]
        public int Week { get; set; }

        [Column("revenue")]
        public double Revenue { get; set; }

        [Column("commission")]
        public double Commission { get; set; }

        [Column("cashed")]
        public bool Cashed { get; set; }

        [Column("cash_time")]
        public long CashTime { get; set; }
    }
}
