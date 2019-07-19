using System;
using Core;
using Core.Data;

namespace GroupPay.Models
{
    public class TransactionLog
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("type")]
        public TransactionType TransactionType { get; set; }

        [Prefix("operator_")]
        public UserAccount Operator { get; set; }

        [Prefix("user_")]
        public UserAccount User { get; set; }

        [Prefix("payment_id")]
        public long PaymentId { get; set; }

        [Column("amount")]
        public int Amount { get; set; }

        [Column("balance_before")]
        public double BalanceBefore { get; set; }

        [Column("balance_after")]
        public double BalanceAfter { get; set; }

        [Column("time")]
        public long Timestamp { get; set; }

        public DateTime Time => this.Timestamp.ToDateTime();
    }
}
