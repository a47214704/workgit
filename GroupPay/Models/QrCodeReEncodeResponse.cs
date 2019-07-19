using Newtonsoft.Json;

namespace GroupPay.Models
{
    public class QrCodeReEncodeResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
