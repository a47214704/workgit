using System;
using Core;
using Core.Crypto;
using Core.Data;

namespace GroupPay.Models
{
    public class UserAccount
    {
        [Column("id")]
        public long Id { get; set; }

        [Prefix("role_")]
        public UserRole Role { get; set; }

        [Column("account_name")]
        public string AccountName { get; set; }

        [Column("avatar")]
        public string Avatar { get; set; }

        [Column("`never_fill_this`")]
        public string Password { get; set; }

        [Column("nick_name")]
        public string NickName { get; set; }

        [Column("balance")]
        public double Balance { get; set; }

        [Column("pending_balance")]
        public int PendingBalance { get; set; }

        [Column("pending_payments")]
        public int PendingPayments { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("merchant_id")]
        public int MerchantId { get; set; }

        [Column("wechat_account")]
        public string WechatAccount { get; set; }

        [Prefix("m_")]
        public Merchant Merchant { get; set; }

        public string PromotionCode => BitConverter.GetBytes(this.Id).ToUrlSafeBase64();

        [Column("evaluation_point")]
        public int Point { get; set; }

        [Column("commission")]
        public double Commission { get; set; }

        [Column("award")]
        public double Award { get; set; }

        [Column("status")]
        public AccountStatus Status { get; set; }

        [Column("has_sub_account")]
        public bool HasSubAccounts { get; set; }

        [Column("create_time")]
        public long CreateTimestamp { get; set; }

        [Column("password_last_set")]
        public long PasswordLastSet { get; set; }

        [Column("last_login_timestamp")]
        public long LastLoginTimestamp { get; set; }
        
        public string UpperAccountName { get; set; }

        public string PromotionUrl { get; set; }

        public DateTime CreateTime => this.CreateTimestamp.ToDateTime();

        public DateTime PasswordLastSetTime => this.PasswordLastSet.ToDateTime();

        public DateTime LastLoginTime => this.LastLoginTimestamp.ToDateTime();
    }
}
