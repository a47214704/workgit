using Core.Data;

namespace GroupPay.Models
{
    public class RevenueReport
    {
        const double StandRatio = 0.005;

        public double TotalRevenue => AgentRevenue + SelfRevenue;

        public double SelfRevenue => SelfAliRevenue + SelfWechatRevenue + SelfBankRevenue;

        [Column("selfAliRevenue")]
        public double SelfAliRevenue { get; set; }

        [Column("selfWechatRevenue")]
        public double SelfWechatRevenue { get; set; }

        [Column("selfBankRevenue")]
        public double SelfBankRevenue { get; set; }

        [Column("agentRevenue")]
        public double AgentRevenue { get; set; }

        public double AgencyCommission { get; set; }

        public double SelfCommission => SelfWechatCommission + SelfAliCommission + SelfBankCommission;

        public double SelfAliCommission => (AliRatio + RankRatio - StandRatio) * SelfAliRevenue;

        public double SelfWechatCommission => (WechatRatio + RankRatio - StandRatio) * SelfWechatRevenue;

        public double SelfBankCommission => (BankRatio + RankRatio - StandRatio) * SelfBankRevenue;

        public double AliRatio { get; set; }

        public double WechatRatio { get; set; }

        public double BankRatio { get; set; }

        public double RankRatio { get; set; }

        public double TotalCommission => (TotalRevenue - SelfRevenue) * RankRatio + SelfCommission - AgencyCommission;

    }
}
