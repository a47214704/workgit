namespace GroupPay.Models
{
    public enum ChannelType : int
    {
        None = 0,
        QrCode = 1,
        Account = 2,
        AliRedEnvelope = 3,
        uBank = 4
    }
}
