using System;
namespace GroupPay
{
    public enum DispatchError
    {
        InvalidPaymentData,
        PaymentDuplication,
        NoEligibleUser,
        PaymentNotFound
    }

    public class PaymentDispatchException : ApplicationException
    {
        public PaymentDispatchException(DispatchError dispatchError)
            : base(string.Format("Not able to dispatch payment due to error {0}", dispatchError))
        {
            this.Error = dispatchError;
        }

        public DispatchError Error { get; private set; }
    }
}
