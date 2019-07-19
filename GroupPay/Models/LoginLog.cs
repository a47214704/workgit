using System;
using Core;
using Core.Data;

namespace GroupPay.Models
{
    public class LoginLog
    {
        [Column("id")]
        public long Id { get; set; }

        [Prefix("user_")]
        public UserAccount User { get; set; }

        [Column("browser")]
        public string Browser { get; set; }

        [Column("ip")]
        public string IP { get; set; }

        [Column("timestamp")]
        public long Timestamp { get; set; }

        public DateTime Time => this.Timestamp.ToDateTime();

    }
}
