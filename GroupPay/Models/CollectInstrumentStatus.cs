using System;
namespace GroupPay.Models
{
    public enum CollectInstrumentStatus : int
    {
        None = 0,
        Pending = 1,
        Active = 2,
        Invalid = 3,
        Expired = 4,
        Removed = 5
    }
}
