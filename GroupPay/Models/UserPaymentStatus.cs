namespace GroupPay.Models
{
    public class UserPaymentStatus
    {
        public long Timestamp { get; set; }

        public int Payments { get; set; }

        public int GetValidPayments(long timestamp)
        {
            lock (this)
            {
                if (this.Timestamp != timestamp)
                {
                    return 0;
                }
            }

            return this.Payments;
        }

        public void IncreasePayments(long timestamp)
        {
            lock (this)
            {
                if (this.Timestamp != timestamp)
                {
                    this.Timestamp = timestamp;
                    this.Payments = 0;
                }

                this.Payments += 1;
            }
        }
    }
}
