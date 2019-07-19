using Core.Data;

namespace GroupPay.Models
{
    public class CommissionRatio
    {
        [Column("lbound")]
        public int LowerBound { get; set; }

        [Column("ubound")]
        public int UpperBound { get; set; }

        [Column("ratio")]
        public double Ratio { get; set; }
    }
}
