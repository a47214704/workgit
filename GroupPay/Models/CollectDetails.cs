using System;
namespace GroupPay.Models
{
    public class CollectDetails
    {
        public string RefNumber { get; set; }

        public string QrCodeUrl { get; set; }

        public string BankName { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public int Amount { get; set; }
    }
}
