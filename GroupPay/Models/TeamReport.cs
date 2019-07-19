using Core.Data;

namespace GroupPay.Models
{
    public class TeamReport
    {
        public int Total { get; set; }

        [Prefix("m_")]
        public DetailReport DirectMembers { get; set; }

        [Prefix("a_")]
        public DetailReport DirectAgents { get; set; }

        public class DetailReport
        {
            [Column("total")]
            public long Total { get; set; }

            [Column("this_week")]
            public long ThisWeekIncrement { get; set; }

            [Column("this_month")]
            public long ThisMonthIncrement { get; set; }
        }
    }
}
