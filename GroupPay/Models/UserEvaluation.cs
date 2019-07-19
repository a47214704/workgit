using Core.Data;
using System;
namespace GroupPay.Models
{
    public enum UserEvaluationType
    {
        Other = -1,
        OverTimePunish = 0,
        SpeedPayCommend,
        PayAllowLimits
    }

    public class UserEvaluation
    {
        [Column("id")]
        public long Id { set; get; }

        [Column("type")]
        public UserEvaluationType Type { set; get; }

        [Column("condition")]
        public int Condition { set; get; }

        [Column("count")]
        public int Count { set; get; }

        [Column("value")]
        public int Value { set; get; }

        [Column("group")]
        public int Group { set; get; }

        [Column("repeat")]
        public int Repeat { set; get; }
    }
}
