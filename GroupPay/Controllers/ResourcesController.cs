using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResourcesController : ControllerBase
    {
        public ResourcesController(ILoggerFactory loggerFactory, SiteConfig siteConfig)
        {
            this.Logger = loggerFactory?.CreateLogger<ResourcesController>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.Config = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
        }

        private ILogger Logger { get; set; }

        private SiteConfig Config { get; set; }


        [HttpPost("UploadImage")]
        public async Task<ActionResult<UploadResult>> UploadImage()
        {
            using (var reader = new StreamReader(this.Request.Body))
            {
                try
                {
                    string fileName = Guid.NewGuid().ToString() + ".jpg";
                    await System.IO.File.WriteAllBytesAsync(Path.Combine(this.Config.UploadFilesPath, fileName), Convert.FromBase64String(await reader.ReadToEndAsync()));
                    return this.Ok(new UploadResult
                    {
                        Error = 0,
                        Url = $"{this.Config.BaseUrl}{this.Config.UploadFilesRelativeUrl}{fileName}"
                    });
                }
                catch (Exception exception)
                {
                    this.Logger.LogError(exception, "failed to save image data");
                    return this.Ok(new UploadResult
                    {
                        Error = -1,
                        Msg = exception.Message
                    });
                }
            }
        }

        public class UploadResult
        {
            public int Error { get; set; }

            public string Url { get; set; }

            public string Msg { get; set; }
        }
    }
}
