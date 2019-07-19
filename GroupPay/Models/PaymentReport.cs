using System;
using Core;
using Core.Data;

namespace GroupPay.Models
{
    public class PaymentReport
    {
        [Column("account_name")]
        public string AccountName { get; set; }

        [Column("wechat_amount")]
        public decimal WechatAmount { get; set; }

        [Column("alipay_amount")]
        public decimal AlipayAmount { get; set; }

        [Column("unionpay_amount")]
        public decimal UnionpayAmount { get; set; }

        [Column("aliRedEnvelope_amount")]
        public decimal AliRedEnvelopeAmount { get; set; }

        [Column("uBank_amount")]
        public decimal UBankAmount { get; set; }

        [Column("aliToCard_amount")]
        public decimal AliToCardAmount { get; set; }

        [Column("aliWap_amount")]
        public decimal AliWapAmount { get; set; }

        [Column("wechatWap_amount")]
        public decimal WechatWapAmount { get; set; }

        [Column("aliH5_amount")]
        public decimal AliH5Amount { get; set; }

        [Column("wechatH5_amount")]
        public decimal WechatH5Amount { get; set; }

    }
}