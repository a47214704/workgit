using Core.Data;
using System;
namespace GroupPay.Models
{
    public enum AwardError
    {
        UserNotFound,
        AlreadyWithdraw,
        Pending,
        UnReadySettle
    }

    public class AwardException : ApplicationException
    {
        public AwardException(AwardError awardError)
            : base(string.Format("Not able to withdraw award due to error {0}", awardError))
        {
            this.Error = awardError;
        }

        public AwardError Error { get; private set; }
    }
}
