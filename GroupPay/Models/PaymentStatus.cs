namespace GroupPay.Models
{
    public enum PaymentStatus : int
    {
        Pending = 1,
        Accepted = 2,
        Settled = 3,
        Reconciled = 4,
        Aborted = 5
    }
}
