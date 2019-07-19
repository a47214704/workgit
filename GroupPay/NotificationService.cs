using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Core;
using Core.Data;
using GroupPay.Models;
using GroupPay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace GroupPay
{
    [Route("/ws/paysrv")]
    [Authorize(AuthenticationSchemes = Constants.Web.CombinedAuthSchemes, Roles = "InstrumentOwner")]
    public class NotificationService : IWebSocketListener
    {
        private readonly PaymentDispatcher _paymentDispatcher;
        private readonly DataAccessor _dataAccessor;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILoggerFactory loggerFactory, PaymentDispatcher paymentDispatcher, DataAccessor dataAccessor)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this._logger = loggerFactory.CreateLogger<NotificationService>();
            this._paymentDispatcher = paymentDispatcher ?? throw new ArgumentNullException(nameof(paymentDispatcher));
            this._paymentDispatcher.GcTimeoutPayments().Forget();
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        public Task OnBinaryMessage(WebSocketSession session, byte[] message)
        {
            this._logger.LogWarning("notSupportedBinaryMessage:{0},{1}", session.RemoteAddr, session.TraceIdentifier);
            return Task.CompletedTask;
        }

        public async Task OnClosed(WebSocketSession session, WebSocketCloseStatus status, string reason)
        {
            if (!this._paymentDispatcher.RemoveUserSession(session))
            {
                this._logger.LogWarning("failedToUnRegisterUserSession:{0},{1}", session.RemoteAddr, session.TraceIdentifier);
                return;
            }

            await this._dataAccessor.Execute(
                "update `user_account` set `online`=0 where `id`=@userId",
                p => p.Add("@userId", MySqlDbType.Int64).Value = session.User.GetId());
        }

        public async Task OnConnected(WebSocketSession session)
        {
            if (!await this._paymentDispatcher.AddUserSession(session))
            {
                this._logger.LogWarning("failedToRegisterUserSession:{0},{1}", session.RemoteAddr, session.TraceIdentifier);
                return;
            }

            await this._dataAccessor.Execute(
                "update `user_account` set `online`=1 where `id`=@userId",
                p => p.Add("@userId", MySqlDbType.Int64).Value = session.User.GetId());
        }

        public Task OnError(WebSocketSession session, Exception exception)
        {
            this._logger.LogWarning("userWebSocketFaulted:{0},{1},{2}", session.RemoteAddr, session.TraceIdentifier, exception);
            return Task.CompletedTask;
        }

        public async Task OnTextMessage(WebSocketSession session, string message)
        {
            try
            {
                // decode the message using JSON
                // deal with listing, accepting and settlement
                Message<PaymentItem> msg = JsonConvert.DeserializeObject<Message<PaymentItem>>(message);
                long userId = session.User?.GetId() ?? 0;
                if (msg == null || string.IsNullOrEmpty(msg.Operation) || userId <= 0)
                {
                    await this.ResponseWithError(session, Constants.Operations.Unknown, Constants.WebApiErrors.InvalidData);
                    return;
                }

                if (Constants.Operations.AcceptPayment.Equals(msg.Operation))
                {
                    if (msg.Content == null || msg.Content.Id <= 0)
                    {
                        await this.ResponseWithError(session, Constants.Operations.AcceptPayment, Constants.WebApiErrors.InvalidData);
                        return;
                    }

                    Payment result = await this._paymentDispatcher.AcceptPayment(userId, msg.Content.Id);
                    if (result == null)
                    {
                        await this.ResponseWithError(session, Constants.Operations.AcceptPayment, Constants.WebApiErrors.ObjectNotFound, msg.Content.Id);
                    }
                    else
                    {
                        await session.SendText(JsonConvert.SerializeObject(new Message<PaymentItem>()
                        {
                            Operation = Constants.Operations.AcceptPayment,
                            Content = new PaymentItem
                            {
                                Id = result.Id,
                                Amount = result.Amount,
                                MerchantReferenceNumber = result.MerchantReferenceNumber,
                                Channel = (await this.GetChannel(result.Channel))?.Name
                            }
                        }));
                    }
                }
                else
                {
                    await this.ResponseWithError(session, msg.Operation, Constants.WebApiErrors.NotSupported);
                }
            }
            catch (Exception exception)
            {
                this._logger.LogError("failedToProcessMessage:{0},{1},{2}", session.TraceIdentifier, session.RemoteAddr, exception);
            }
        }

        private Task ResponseWithError(WebSocketSession session, string operation, string errorCode, long id = 0)
        {
            return session.SendText(JsonConvert.SerializeObject(new Message<PaymentItem>
            {
                Operation = operation,
                Error = errorCode,
                Content = id == 0 ? null : new PaymentItem
                {
                    Id = id
                }
            }));
        }

        private Task<CollectChannel> GetChannel(int channelId)
        {
            return this._dataAccessor.GetOne<CollectChannel>(
                "select * from collect_channel where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = channelId);
        }

        public class Message<T>
        {
            public Message()
            {
                this.Error = Constants.WebApiErrors.Success;
            }

            [JsonProperty("operation")]
            public string Operation { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("content")]
            public T Content { get; set; }
        }

        public class PaymentItem
        {
            [JsonProperty("id")]
            [Column("id")]
            public long Id { get; set; }

            [JsonProperty("channel")]
            [Column("channel")]
            public string Channel { get; set; }

            [JsonProperty("mrn")]
            [Column("mrn")]
            public string MerchantReferenceNumber { get; set; }

            [JsonProperty("amount")]
            [Column("amount")]
            public double Amount { get; set; }

            [JsonProperty("expiration")]
            [Column("expiration")]
            public long Expiration { get; set; }
        }
    }
}
