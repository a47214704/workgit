namespace GroupPay.Models
{
    public enum TransactionType : int
    {
        /// <summary>
        /// 手动修改
        /// </summary>
        Modify,
        /// <summary>
        /// 抢单
        /// </summary>
        Redeem,
        /// <summary>
        /// 运输中返回
        /// </summary>
        Refund,
        /// <summary>
        /// 充值
        /// </summary>
        Refill,
        /// <summary>
        /// 转入
        /// </summary>
        WireIn,
        /// <summary>
        /// 转出
        /// </summary>
        WireOut,
        /// <summary>
        /// 奖励
        /// </summary>
        Rewards,
        /// <summary>
        /// 利润
        /// </summary>
        Commission,
        /// <summary>
        /// 补单
        /// </summary>
        ManualRedeem,
        /// <summary>
        /// 卖点
        /// </summary>
        Sell,
        /// <summary>
        /// 买点
        /// </summary>
        Buy
    }
}
