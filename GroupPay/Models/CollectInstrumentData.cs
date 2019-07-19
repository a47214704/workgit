using System;
using GroupPay.Models;
using Microsoft.AspNetCore.Http;

namespace GroupPay.Models
{
    public class CollectInstrumentData
    {
        public string Name { get; set; }

        public CollectChannelType ChannelId { get; set; }

        public IFormFile QrCodeFile { get; set; }

        public string BankName { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; }
    }
}
