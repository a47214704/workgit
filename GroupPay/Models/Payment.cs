using System;
using Core;
using Core.Data;

namespace GroupPay.Models
{
    public class Payment
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("channel")]
        public int Channel { get; set; }

        [Prefix("ci_")]
        public CollectInstrument Instrument { get; set; }

        [Prefix("merchant_")]
        public Merchant Merchant { get; set; }

        [Column("mrn")]
        public string MerchantReferenceNumber { get; set; }

        [Column("amount")]
        public int Amount { get; set; }

        [Column("origin_amount")]
        public int OriginAmount { get; set; }

        [Column("notify_url")]
        public string NotifyUrl { get; set; }

        [Column("callback_url")]
        public string CallBackUrl { get; set; }

        [Column("status")]
        public PaymentStatus Status { get; set; }

        [Column("create_time")]
        public long CreateTimestamp { get; set; }

        public DateTime CreateTime => this.CreateTimestamp.ToDateTime();

        [Column("accept_time")]
        public long AcceptTimestamp { get; set; }

        public DateTime AcceptTime => this.AcceptTimestamp.ToDateTime();

        [Column("settle_time")]
        public long SettleTimestamp { get; set; }

        public DateTime SettleTime => this.SettleTimestamp.ToDateTime();

        [Column("account_name")]
        public string AccountName { get; set; }
    }
}
