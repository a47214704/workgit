using Core.Data;
using System;
namespace GroupPay.Models
{
    public class AwardConfig
    {
        [Column("id")]
        public long Id { set; get; }

        [Column("condition")]
        public int AwardCondition { set; get; }

        [Column("bouns")]
        public int Bouns { set; get; }

        [Column("modify_time")]
        public long ModifyTime { set; get; }
    }
}
