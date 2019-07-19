using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrlController : ControllerBase
    {
        [HttpGet("Shorten")]
        public async Task<ActionResult<WebApiResult<string>>> Shorten([FromQuery]string url)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Constants.Web.DefaultRequestTimeout))
            {
                HttpClient httpClient = new HttpClient();
                using (HttpResponseMessage response = await httpClient.GetAsync("http://api.t.sina.com.cn/short_url/shorten.json?source=3271760578&url_long=" + url, cts.Token))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return this.StatusCode((int)HttpStatusCode.ServiceUnavailable, this.CreateErrorResult<string>(Constants.WebApiErrors.DependencyFailed, "url shorten service is not available"));
                    }

                    List<ShortenResult> results = JsonConvert.DeserializeObject<List<ShortenResult>>(await response.Content.ReadAsStringAsync());
                    if (results == null || results.Count == 0)
                    {
                        return this.BadRequest(this.CreateErrorResult<string>(Constants.WebApiErrors.InvalidData, "not able to create shorten url based on the input"));
                    }

                    return this.Ok(new WebApiResult<string>(results[0].UrlShort));
                }
            }
        }

        public class ShortenResult
        {
            [JsonProperty("url_short")]
            public string UrlShort { get; set; }

            [JsonProperty("url_long")]
            public string UrlLong { get; set; }
        }
    }
}
