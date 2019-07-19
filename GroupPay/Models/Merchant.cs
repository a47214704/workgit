using Core;
using Core.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GroupPay.Models
{
    public class Merchant
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("app_key")]
        public string AppKey { get; set; }

        [Column("app_pwd")]
        public string AppSecret { get; set; }

        [Column("wechat_ratio")]
        public double WechatRatio { get; set; }

        [Column("ali_ratio")]
        public double AliRatio { get; set; }

        [Column("bank_ratio")]
        public double BankRatio { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("channel_enabled")]
        public int ChannelEnabled
        {
            get
            {
                return ChannelEnabledList.ToBitInt();
            }

            set
            {
                ChannelEnabledList = value.ToBitBool(Enum.GetNames(typeof(CollectChannelType)).Length);
            }
        }

        public bool[] ChannelEnabledList { get; set; }

        [Column("channel_limit")]
        public string ChannelLimitJson
        {
            get
            {
                return JsonConvert.SerializeObject(ChannelLimit);
            }

            set
            {
                ChannelLimit = string.IsNullOrEmpty(value) ? new Dictionary<string, Tuple<int, int>>
                    {
                        { "Ali",Tuple.Create(300,10000)},
                        { "Wechat",Tuple.Create(500,5000)},
                        { "AliToCard",Tuple.Create(100,20000)},
                        { "AliWap",Tuple.Create(300,10000)},
                        { "WechatWap",Tuple.Create(500,5000)},
                        { "Card",Tuple.Create(100,20000)},
                        { "AliH5",Tuple.Create(300,10000)},
                        { "WechatH5",Tuple.Create(500,5000)},
                        { "Ubank",Tuple.Create(100,20000)}
                    } : JsonConvert.DeserializeObject<Dictionary<string, Tuple<int, int>>>(value);
            }
        }

        public Dictionary<string, Tuple<int, int>> ChannelLimit { get; set; }
    }
    public enum WireOutOrderStatus
    {
        all = -1,
        pending,
        paying,
        settle,
        cancel,
        refuse
    }
    public class MerchantWireOutOrder
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("status")]
        public WireOutOrderStatus Status { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("operator")]
        public long OperatorId { get; set; }

        [Column("operator_name")]
        public string OperatorName { get; set; }

        /// <summary>银行名称，支付宝用户ID</summary>
        [Column("account_provider")]
        public string AccountProvider { get; set; }

        /// <summary>持卡人名称，支付宝昵称</summary>
        [Column("account_holder")]
        public string AccountHolder { get; set; }

        /// <summary>银行卡号，支付宝账号</summary>
        [Column("account_name")]
        public string AccountName { get; set; }

        [Column("amount")]
        public int Amount { get; set; }

        [Column("create_time")]
        public long CreateTimestamp { get; set; }

        public DateTime CreateTime => this.CreateTimestamp.ToDateTime();
        
        [Column("settle_time")]
        public long SettleTimestamp { get; set; }

        public DateTime SettleTime => this.SettleTimestamp.ToDateTime();

    }
}
