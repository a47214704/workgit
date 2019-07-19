using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace GroupPay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectInstrumentController : ControllerBase
    {
        private readonly DataAccessor _dataAccessor;
        private readonly ILogger<CollectInstrumentController> _logger;
        private readonly SiteConfig _siteConfig;
        private readonly HttpClient _httpClient;

        public CollectInstrumentController(DataAccessor dataAccessor, SiteConfig siteConfig, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._siteConfig = siteConfig ?? throw new ArgumentNullException(nameof(siteConfig));
            this._logger = loggerFactory.CreateLogger<CollectInstrumentController>();
            this._httpClient = new HttpClient();
        }

        [HttpPost("Create")]
        [Authorize(Roles = "InstrumentOwner", AuthenticationSchemes = Constants.Web.CombinedAuthSchemes)]
        public Task<ActionResult<WebApiResult<CollectInstrument>>> Create([FromForm]CollectInstrumentData instrumentData)
        {
            return this.CreateCollectInstrument(
                new CollectInstrument
                {
                    Name = instrumentData.Name,
                    Channel = new CollectChannel
                    {
                        Id = instrumentData.ChannelId
                    },
                    AccountName = instrumentData.AccountNumber,
                    AccountHolder = instrumentData.AccountName,
                    AccountProvider = instrumentData.BankName
                },
                async (instrument) =>
                {
                    if (instrumentData.QrCodeFile == null)
                    {
                        return null;
                    }

                    string fileName = string.Format("{0}{1}", Guid.NewGuid(), Path.GetExtension(instrumentData.QrCodeFile.FileName));
                    try
                    {
                        using (FileStream fileStream = new FileStream(Path.Combine(this._siteConfig.UploadFilesPath, fileName), FileMode.Create))
                        {
                            await instrumentData.QrCodeFile.CopyToAsync(fileStream);
                        }

                        return fileName;
                    }
                    catch (Exception exception)
                    {
                        this._logger.LogWarning("failedToSaveQrCodeFile:{0},{1}", exception.Message, this.TraceActivity());
                        return null;
                    }
                });
        }

        [HttpPost]
        [Authorize(Roles = "InstrumentOwner", AuthenticationSchemes = Constants.Web.UserTokenAuthScheme)]
        public Task<ActionResult<WebApiResult<CollectInstrument>>> Post([FromBody]CollectInstrument collectInstrument)
        {
            return this.CreateCollectInstrument(
                collectInstrument,
                async (instrument) =>
                {
                    if (string.IsNullOrEmpty(collectInstrument.OriginalQrCode))
                    {
                        return null;
                    }

                    string[] values = collectInstrument.OriginalQrCode.Split('.');
                    if (values.Length != 2 || string.IsNullOrEmpty(values[0]) || string.IsNullOrEmpty(values[1]))
                    {
                        this._logger.LogWarning("badOrigianlQrCodeFormat:{0}", this.TraceActivity());
                        return null;
                    }

                    try
                    {
                        byte[] qrCode = Convert.FromBase64String(values[1]);
                        string fileName = string.Format("{0}.{1}", Guid.NewGuid(), values[0]);
                        using (FileStream fileStream = new FileStream(Path.Combine(this._siteConfig.UploadFilesPath, fileName), FileMode.Create))
                        {
                            await fileStream.WriteAsync(qrCode, 0, qrCode.Length);
                        }
                        return fileName;
                    }
                    catch (Exception exception)
                    {
                        this._logger.LogWarning("badQrCodeContent:{0},{1}", exception.Message, this.TraceActivity());
                        return null;
                    }
                });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "InstrumentOwner,InstrumentWriter", AuthenticationSchemes = Constants.Web.UserTokenAuthScheme)]
        public async Task<ActionResult<WebApiResult<CollectInstrument>>> Delete([FromRoute]long id)
        {
            CollectInstrument instrument = await this._dataAccessor.GetOne<CollectInstrument>(
                "select * from collect_instrument where `user_id`=@userId and `id`=@id",
                p =>
                {
                    p.Add("@userId", MySqlDbType.Int64).Value = this.HttpContext.User.GetId();
                    p.Add("@id", MySqlDbType.Int64).Value = id;
                });
            if (instrument == null)
            {
                return this.NotFound(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.ObjectNotFound, "instrument not found"));
            }

            await this._dataAccessor.Execute(
                "update collect_instrument set `status`=@status where `id`=@id",
                p =>
                {
                    p.Add("@status", MySqlDbType.Int32).Value = (int)CollectInstrumentStatus.Removed;
                    p.Add("@id", MySqlDbType.Int64).Value = id;
                });
            return this.NoContent();
        }

        [HttpGet]
        [ResponseCache(VaryByHeader = "User-Agent", NoStore = true, Location = ResponseCacheLocation.None)]
        [Authorize(Roles = "InstrumentOwner,InstrumentReader", AuthenticationSchemes = Constants.Web.UserTokenAuthScheme)]
        public async Task<ActionResult<WebApiResult<List<CollectInstrument>>>> Get()
        {
            long userId = this.HttpContext.User.GetId();
            long todayTs = DateTimeOffset.UtcNow.RoundDay(this._siteConfig.TimeZone).ToUnixTimeMilliseconds();
            List<CollectInstrument> instruments = await this._dataAccessor.GetAll<CollectInstrument>(
                "select ci.*, c.`name` as channel_name from collect_instrument as ci" +
                " join collect_channel as c on ci.`channel_id`=c.`id`" +
                " where ci.`user_id`=@userId and ci.`status` in (1, 2)",
                p => p.Add("@userId", MySqlDbType.Int64).Value = userId);
            foreach (CollectInstrument instrument in instruments)
            {
                instrument.DailyTotal = await this._dataAccessor.GetOne("select coalesce(sum(`amount`), 0) / 100 +0E0 from `payment` where `ciid`=@cid and `settle_time`>=@todayTs",
                    new SimpleRowMapper<double>(reader => Task.FromResult(reader.GetDouble(0))),
                    p =>
                    {
                        p.Add("@cid", MySqlDbType.Int64).Value = instrument.Id;
                        p.Add("@todayTs", MySqlDbType.Int64).Value = todayTs;
                    });
            }

            return this.Ok(new WebApiResult<List<CollectInstrument>>(instruments));
        }

        private async Task<ActionResult<WebApiResult<CollectInstrument>>> CreateCollectInstrument(CollectInstrument collectInstrument, Func<CollectInstrument, Task<string>> validateAndSaveQrCode)
        {
            if (!collectInstrument.IsValid())
            {
                return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "bad request data"));
            }

            // check channel id
            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>(
                "select * from collect_channel where id=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = (int)collectInstrument.Channel.Id);
            if (channel == null)
            {
                this._logger.LogWarning("channelDoesNotExist:{0},{1}", (int)collectInstrument.Channel.Id, this.TraceActivity());
                return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "channel not found"));
            }

            // check limit of collectInstrument count
            long numberOfInstruments = await this._dataAccessor.GetOne(
                "select count(`id`) from `collect_instrument` where `user_id`=@uid and `channel_id`=@cid and `status` = 2",
                new SimpleRowMapper<long>(reader => Task.FromResult(reader.GetInt64(0))),
                p =>
                {
                    p.Add("@uid", MySqlDbType.Int64).Value = this.HttpContext.User.GetId();
                    p.Add("@cid", MySqlDbType.Int32).Value = (int)channel.Id;
                });

            if (numberOfInstruments >= channel.InstrumentsLimit)
            {
                this._logger.LogWarning("InstrumentOfChannelAlreadyFull:{0},{1}", (int)collectInstrument.Channel.Id, this.TraceActivity());
                return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "Instrument of channel already  full"));
            }

            collectInstrument.Channel = channel;
            collectInstrument.UserId = this.HttpContext.User.GetId();
            collectInstrument.Status = CollectInstrumentStatus.Pending;

            // check qrcode
            string qrCodeFileName = null;
            if (channel.ChannelType == ChannelType.QrCode || channel.ChannelType == ChannelType.uBank)
            {
                qrCodeFileName = await validateAndSaveQrCode(collectInstrument);
                if (string.IsNullOrEmpty(qrCodeFileName))
                {
                    return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "qr code is required"));
                }
            }
            else if (channel.ChannelType == ChannelType.Account)
            {
                if (string.IsNullOrEmpty(collectInstrument.AccountProvider) ||
                    string.IsNullOrEmpty(collectInstrument.AccountHolder) ||
                    string.IsNullOrEmpty(collectInstrument.AccountName))
                {
                    return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "account info is required"));
                }
            }

            // save the stub first
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                await transaction.Execute("insert into collect_instrument(`name`, `status`, `user_id`, `channel_id`, `daily_limit`) values(@name, @status, @userId, @channelId, @dailyLimit)",
                    p =>
                    {
                        p.Add("@name", MySqlDbType.VarChar).Value = collectInstrument.Name;
                        p.Add("@status", MySqlDbType.Int32).Value = (int)collectInstrument.Status;
                        p.Add("@userId", MySqlDbType.Int64).Value = collectInstrument.UserId;
                        p.Add("@channelId", MySqlDbType.Int32).Value = (int)collectInstrument.Channel.Id;
                        p.Add("@dailyLimit", MySqlDbType.Int32).Value = collectInstrument.DailyLimit;
                    });
                collectInstrument.Id = await transaction.GetLastInsertId();
                await transaction.Commit();
            }

            if (channel.ChannelType == ChannelType.Account || channel.ChannelType == ChannelType.AliRedEnvelope)
            {
                collectInstrument.Status = CollectInstrumentStatus.Active;
                await this._dataAccessor.Execute(
                        "update collect_instrument set `account_provider`=@accountProvider, `account_name`=@accountName, `account_holder`=@accountHolder, `status`=@status where `id`=@id",
                        p =>
                        {
                            p.Add("@accountProvider", MySqlDbType.VarChar).Value = collectInstrument.AccountProvider;
                            p.Add("@accountName", MySqlDbType.VarChar).Value = collectInstrument.AccountName;
                            p.Add("@accountHolder", MySqlDbType.VarChar).Value = collectInstrument.AccountHolder;
                            p.Add("@status", MySqlDbType.Int32).Value = (int)collectInstrument.Status;
                            p.Add("@id", MySqlDbType.Int64).Value = collectInstrument.Id;
                        });
                return this.Ok(new WebApiResult<CollectInstrument>(collectInstrument));
            }
            else if (channel.ChannelType == ChannelType.QrCode || channel.ChannelType == ChannelType.uBank)
            {
                // Double check the qr code info
                if (string.IsNullOrEmpty(qrCodeFileName))
                {
                    // This should not happen
                    this._logger.LogWarning("noQrCodeInRequest:{0}", this.TraceActivity());
                    return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "qr code not found"));
                }

                QrCodeReEncodeRequest encodeRequest = new QrCodeReEncodeRequest
                {
                    Id = string.Format("{0}_{1}", collectInstrument.UserId, collectInstrument.Id),
                    ImageUrl = this._siteConfig.BaseUrl + this._siteConfig.UploadFilesRelativeUrl + qrCodeFileName
                };
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, this._siteConfig.QrCodeService)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(encodeRequest), Encoding.UTF8, "application/json")
                };
                HttpResponseMessage responseMessage = await this._httpClient.SendAsync(requestMessage);
                if (responseMessage.IsSuccessStatusCode)
                {
                    QrCodeReEncodeResponse encodeResponse = JsonConvert.DeserializeObject<QrCodeReEncodeResponse>(await responseMessage.Content.ReadAsStringAsync());
                    collectInstrument.Status = CollectInstrumentStatus.Active;
                    collectInstrument.Token = encodeResponse.Token;
                    collectInstrument.QrCode = encodeResponse.ImageUrl;
                    collectInstrument.OriginalQrCode = encodeRequest.ImageUrl;
                    await this._dataAccessor.Execute(
                        "update collect_instrument set `token`=@token, `qr_code`=@qrCode, `status`=@status, `original_qr_code`=@originalQrCode, `account_provider`=@accountProvider, `account_name`=@accountName, `account_holder`=@accountHolder where `id`=@id",
                        p =>
                        {
                            p.Add("@token", MySqlDbType.VarChar).Value = collectInstrument.Token;
                            p.Add("@qrCode", MySqlDbType.VarChar).Value = collectInstrument.QrCode;
                            p.Add("@status", MySqlDbType.Int32).Value = (int)collectInstrument.Status;
                            p.Add("@originalQrCode", MySqlDbType.VarChar).Value = collectInstrument.OriginalQrCode;
                            p.Add("@accountProvider", MySqlDbType.VarChar).Value = collectInstrument.AccountProvider ?? string.Empty;
                            p.Add("@accountName", MySqlDbType.VarChar).Value = collectInstrument.AccountName ?? string.Empty;
                            p.Add("@accountHolder", MySqlDbType.VarChar).Value = collectInstrument.AccountHolder ?? string.Empty;
                            p.Add("@id", MySqlDbType.Int64).Value = collectInstrument.Id;
                        });
                    return this.Ok(new WebApiResult<CollectInstrument>(collectInstrument));
                }
                else
                {
                    this._logger.LogError("callQrCodeServiceFailed:{0},{1},{2}", this._siteConfig.QrCodeService, responseMessage.StatusCode, this.TraceActivity());
                    collectInstrument.Status = CollectInstrumentStatus.Invalid;
                    await this._dataAccessor.Execute(
                        "update collect_instrument set `status`=@status where `id`=@id",
                        p =>
                        {
                            p.Add("@status", MySqlDbType.Int32).Value = (int)collectInstrument.Status;
                            p.Add("@id", MySqlDbType.Int64).Value = collectInstrument.Id;
                        });
                    return this.StatusCode(500, this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.DependencyFailed, "qr code service failed"));
                }
            }
            else
            {
                this._logger.LogWarning("unsupportedChannelType:{0}", this.TraceActivity());
                return this.BadRequest(this.CreateErrorResult<CollectInstrument>(Constants.WebApiErrors.InvalidData, "channel type not supported"));
            }
        }
    }
}
