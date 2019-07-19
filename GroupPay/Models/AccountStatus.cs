using System;
namespace GroupPay.Models
{
    public enum AccountStatus : int
    {
        None = 0,
        Active = 1,
        Lockout = 2,
        Disabled = 3,
        Removed = 4,
        NotYetVerified = -1
    }
}
