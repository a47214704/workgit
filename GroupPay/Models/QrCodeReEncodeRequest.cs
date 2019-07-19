using Newtonsoft.Json;

namespace GroupPay.Models
{
    public class QrCodeReEncodeRequest
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }
    }
}
