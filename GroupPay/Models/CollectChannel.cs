using Core.Data;

namespace GroupPay.Models
{ 
    public enum CollectChannelType : int
    {
        /// <summary>
        /// 微信
        /// </summary>
        Wechat = 1,
        /// <summary>
        /// 支付宝
        /// </summary>
        Ali,
        /// <summary>
        /// 银行卡
        /// </summary>
        Card,
        /// <summary>
        /// 支付宝红包
        /// </summary>
        AliRed,
        /// <summary>
        /// 云闪付
        /// </summary>
        UBank,
        /// <summary>
        /// 支付宝转银行卡
        /// </summary>
        AliToCard,
        /// <summary>
        /// 支付宝wap
        /// </summary>
        AliWap,
        /// <summary>
        /// 微信wap
        /// </summary>
        WechatWap,
        /// <summary>
        /// 支付宝跳转
        /// </summary>
        AliH5,
        /// <summary>
        /// 微信跳转
        /// </summary>
        WechatH5
    }

    public class CollectChannel
    {
        [Column("id")]
        public CollectChannelType Id { get; set; }

        [Column("name")]
        public string Name { get; set; }
        
        [Column("type")]
        public ChannelType ChannelType { get; set; }

        [Column("provider")]
        public ChannelProvider ChannelProvider { get; set; }

        [Column("ratio")]
        public int Ratio { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("instruments_limit")]
        public int InstrumentsLimit { get; set; }

        [Column("default_daliy_limit")]
        public int DefaultDaliyLimit { get; set; }

        [Column("valid_time")]
        public int ValidTime { get; set; }
    }
}
