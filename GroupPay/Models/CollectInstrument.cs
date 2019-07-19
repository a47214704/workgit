using System;
using Core.Data;

namespace GroupPay.Models
{
    public class CollectInstrument
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("status")]
        public CollectInstrumentStatus Status { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Prefix("channel_")]
        public CollectChannel Channel { get; set; }

        [Column("token")]
        public string Token { get; set; }

        [Column("qr_code")]
        public string QrCode { get; set; }

        [Column("original_qr_code")]
        public string OriginalQrCode { get; set; }

        /// <summary>银行名称，支付宝用户ID</summary>
        [Column("account_provider")]
        public string AccountProvider { get; set; }

        /// <summary>持卡人名称，支付宝昵称</summary>
        [Column("account_holder")]
        public string AccountHolder { get; set; }

        /// <summary>银行卡号，支付宝账号</summary>
        [Column("account_name")]
        public string AccountName { get; set; }

        [Column("daily_limit")]
        public int DailyLimit { get; set; }

        [Column("daily_total")]
        public double DailyTotal { get; set; }
    }
}
