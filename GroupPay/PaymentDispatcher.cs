using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.Data;
using GroupPay.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace GroupPay
{
    public class PaymentDispatcher
    {
        private readonly ConcurrentDictionary<long, WebSocketSession> _users = new ConcurrentDictionary<long, WebSocketSession>();
        private readonly ConcurrentDictionary<long, DispatchItem> _items = new ConcurrentDictionary<long, DispatchItem>();
        private readonly ILogger<PaymentDispatcher> _logger;
        private readonly DataAccessor _dataAccessor;
        private readonly SiteConfig _config;
        private readonly Random _random = new Random();

        public PaymentDispatcher(ILoggerFactory loggerFactory, DataAccessor dataAccessor, SiteConfig config)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this._logger = loggerFactory.CreateLogger<PaymentDispatcher>();
            this._dataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<bool> AddUserSession(WebSocketSession session)
        {
            if (session.User == null)
            {
                this._logger.LogError("noUserPrincipalInSession:{0},{1}", session.TraceIdentifier, session.RemoteAddr);
                return false;
            }

            long userId = session.User.GetId();
            if (userId == 0)
            {
                this._logger.LogWarning("badUserId:{0},{1}", session.TraceIdentifier, session.RemoteAddr);
                return false;
            }

            UserPaymentStatus status = new UserPaymentStatus
            {
                Timestamp = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone).ToUnixTimeMilliseconds()
            };
            status.Payments = await this._dataAccessor.GetOne(
                "select count(p.`id`) as payments from `payment` as p " +
                "join `collect_instrument` as ci on ci.`id`=p.`ciid` " +
                "where p.`accept_time`>=@ts and ci.`user_id`=@uid",
                new SimpleRowMapper<int>(reader => Task.FromResult(reader.GetInt32(0))),
                p =>
                {
                    p.Add("@ts", MySqlDbType.Int64).Value = status.Timestamp;
                    p.Add("@uid", MySqlDbType.Int64).Value = userId;
                });
            session[Constants.ContextKeys.PaymentStatus] = status;
            return this._users.TryAdd(userId, session);
        }

        public bool RemoveUserSession(WebSocketSession session)
        {
            if (session.User == null)
            {
                this._logger.LogError("cannotRemoveSessionNoUser:{0},{1}", session.TraceIdentifier, session.RemoteAddr);
                return false;
            }

            long userId = session.User.GetId();
            if (userId == 0)
            {
                this._logger.LogWarning("cannotRemoveSessionNoUserId:{0},{1}", session.TraceIdentifier, session.RemoteAddr);
                return false;
            }

            return this._users.TryRemove(userId, out WebSocketSession ignored);
        }

        public async Task<CollectInstrument> Dispatch(Payment payment, CancellationToken cancellationToken)
        {
            if (payment == null)
            {
                throw new ArgumentNullException(nameof(payment));
            }

            bool duplication = true;
            DispatchItem item = this._items.GetOrAdd(payment.Id, key =>
            {
                duplication = false;
                return new DispatchItem
                {
                    Payment = payment,
                    TaskSource = new TaskCompletionSource<CollectInstrument>()
                };
            });
            if (duplication)
            {
                throw new PaymentDispatchException(DispatchError.PaymentDuplication);
            }

            CollectChannel channel = await this._dataAccessor.GetOne<CollectChannel>(
                "select * from `collect_channel` where `id`=@id",
                p => p.Add("@id", MySqlDbType.Int32).Value = payment.Channel);
            if (channel == null)
            {
                throw new PaymentDispatchException(DispatchError.InvalidPaymentData);
            }

            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long todayTs = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone).ToUnixTimeMilliseconds();
            List<long> eligibleUsers = await this.ListEligibleUsers(payment, todayTs);

            List<List<WebSocketSession>> dispatchBatches = new List<List<WebSocketSession>>();
            List<WebSocketSession> currentBatch = null;
            NotificationService.PaymentItem paymentItem = new NotificationService.PaymentItem
            {
                Id = payment.Id,
                Channel = channel.Name,
                Amount = payment.Amount,
                MerchantReferenceNumber = payment.MerchantReferenceNumber
            };
            foreach (long userId in eligibleUsers)
            {
                if (this._users.TryGetValue(userId, out WebSocketSession session))
                {
                    if (currentBatch == null || (this._config.MaxDispatchBatchSize > 0 && currentBatch.Count >= this._config.MaxDispatchBatchSize))
                    {
                        currentBatch = new List<WebSocketSession>();
                        dispatchBatches.Add(currentBatch);
                    }
                    currentBatch.Add(session);
                }
                else
                {
                    this._logger.LogWarning("userOnlineButNoSessionFound:{0}", userId);
                }
            }

            if (dispatchBatches.Sum(batch => batch.Count) == 0)
            {
                throw new PaymentDispatchException(DispatchError.NoEligibleUser);
            }

            // register item cleanup before save the item
            if (cancellationToken.CanBeCanceled)
            {
                item.CancellationRegistration = cancellationToken.Register(
                    state =>
                    {
                        DispatchItem i = (DispatchItem)state;
                        if (i.TaskSource.TrySetCanceled() && this._items.TryRemove(i.Payment.Id, out DispatchItem ignored))
                        {
                            this._logger.LogInformation("dispatchItemRemovedByTimeout:{0}", i.Payment.Id);
                        }

                        // broadcast remove_payment to all sessions
                        NotificationService.Message<NotificationService.PaymentItem> message = new NotificationService.Message<NotificationService.PaymentItem>
                        {
                            Operation = Constants.Operations.RemovePayment,
                            Content = paymentItem
                        };

                        foreach (WebSocketSession session in this._users.Values)
                        {
                            try
                            {
                                session.SendText(JsonConvert.SerializeObject(message)).Forget();
                            }
                            catch (Exception e)
                            {
                                this._logger.LogInformation("failedToSendMessageToUser:{0},{1}", session.User.GetId(), e);
                            }
                        }
                    }, item);
            }

            // now we can save the dispatch item into _items
            this._items.AddOrUpdate(payment.Id, item, (k, v) => item);
            for (int i = 0; i < dispatchBatches.Count; ++i)
            {
                foreach (WebSocketSession session in dispatchBatches[i])
                {
                    try
                    {
                        this._logger.LogInformation("SendPayMessageToUser:{0}", session.User.GetId());
                        session.SendText(JsonConvert.SerializeObject(new NotificationService.Message<NotificationService.PaymentItem>
                        {
                            Operation = Constants.Operations.PaymentNotification,
                            Content = paymentItem
                        })).Forget();
                    }
                    catch (Exception e)
                    {
                        this._logger.LogInformation("failedToSendMessageToUser:{0},{1}", session.User.GetId(), e);
                    }
                }

                // No timeout for last batch
                if (i != dispatchBatches.Count - 1)
                {
                    Task timer = Task.Delay(this._config.DispatchTimeout);
                    Task completed = await Task.WhenAny(timer, item.TaskSource.Task);
                    if (ReferenceEquals(item.TaskSource.Task, completed)) // the item has been expired or accepted
                    {
                        // let the exception propagate as it is
                        return await item.TaskSource.Task;
                    }
                }
            }

            // wait for the last batch
            return await item.TaskSource.Task;
        }

        public async Task<Payment> AcceptPayment(long userId, long paymentId)
        {
            // step 1. lock payment object
            // step 2. ensure that the payment is not yet accepted
            // step 3. ensure that the user has sufficient balance to accept this payment
            // step 4. ensure that the user has collect instrument and instrument limit is not met
            // step 5. Update the payment with user's collect instrument
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                Payment payment = await transaction.GetOne<Payment>(
                    "select * from `payment` where `id`=@id and `status`=1 for update",
                    p => p.Add("@id", MySqlDbType.Int64).Value = paymentId);
                if (payment == null)
                {
                    // payment does not exist or not in pending state
                    return null;
                }

                if (!this._items.TryGetValue(paymentId, out DispatchItem dispatchItem))
                {
                    this._logger.LogError("dispatchItemNotFound:{payment}", paymentId);
                }

                UserAccount user = await transaction.GetOne<UserAccount>(
                    "select * from `user_account` where `id`=@userId",
                    p => p.Add("@userId", MySqlDbType.Int64).Value = userId);

                if (user == null)
                {
                    this._logger.LogInformation("userNotFound:{user}", userId);
                    return null;
                }

                if (user.Balance * 100 < payment.Amount)
                {
                    this._logger.LogInformation("insufficientBalance:{user},{balance},{pending},{payment},{amount}", user.Id, user.Balance, user.PendingBalance, payment.Id, payment.Amount);
                    return null;
                }

                if (this._config.PendingPaymentsLimit > 0 && user.PendingPayments >= this._config.PendingPaymentsLimit)
                {
                    this._logger.LogInformation("pendingPaymentsLimitMet:{user},{pending},{limit}", user.Id, user.PendingPayments, this._config.PendingPaymentsLimit);
                    return null;
                }

                long todayTs = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone).ToUnixTimeMilliseconds();
                List<CollectInstrument> collectInstruments = await transaction.GetAll<CollectInstrument>(
                    "select * from `collect_instrument` as ci " +
                    "where ci.`user_id`=@userId " +
                    "and ci.`status`=2 " +
                    "and ci.`channel_id`=@channel " +
                    "and ci.`daily_limit`*100-@amount>=coalesce((select sum(`amount`) from `payment` where `ciid`=ci.`id` and `accept_time`>=@todayTs and `status` in (2,3)), 0) " +
                    "order by ci.`id` ASC",
                    p =>
                    {
                        p.Add("@amount", MySqlDbType.Int64).Value = payment.Amount;
                        p.Add("@userId", MySqlDbType.Int64).Value = userId;
                        p.Add("@channel", MySqlDbType.Int32).Value = payment.Channel;
                        p.Add("@todayTs", MySqlDbType.Int64).Value = todayTs;
                    }
                );

                if (collectInstruments.Count == 0)
                {
                    this._logger.LogError("noEligibleCollectInstrumentFound:{0},{1}", userId, payment.Channel);
                    return null;
                }

                CollectInstrument instrument = collectInstruments[this._config.RandomInstrumentMode ? this._random.Next(collectInstruments.Count) : 0];

                this._logger.LogInformation($"user:{userId} accept payment amount:{payment.Amount} id:{payment.Id} before_pendingBalance:{user.PendingBalance}, after_pendingBalance:{user.PendingBalance+payment.Amount}!");

                await transaction.Execute(
                    @"update `payment` set `ciid`=@ciid, `status`=2, `accept_time`=@acceptTime where `id`=@id;",
                    p =>
                    {
                        p.Add("@ciid", MySqlDbType.Int64).Value = instrument.Id;
                        p.Add("@acceptTime", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        p.Add("@id", MySqlDbType.Int64).Value = paymentId;
                    });

                await transaction.Execute(
                    @"update `user_account` set balance = balance - @amount/100, pending_balance = pending_balance + @amount, pending_payments=pending_payments + 1 where `id` = @userId;",
                    p =>
                    {
                        p.Add("@amount", MySqlDbType.Int64).Value = payment.Amount;
                        p.Add("@userId", MySqlDbType.Int64).Value = userId;
                    });
                
                // Add a transaction log
                await transaction.Execute(
                    "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                    p =>
                    {
                        p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Redeem;
                        p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                        p.Add("@userId", MySqlDbType.Int64).Value = userId;
                        p.Add("@amount", MySqlDbType.Int64).Value = -payment.Amount;
                        p.Add("@balBefore", MySqlDbType.Double).Value = user.Balance;
                        p.Add("@balAfter", MySqlDbType.Double).Value = (user.Balance - payment.Amount / 100.0);
                        p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    });

                // update payment status
                if (this._users.TryGetValue(userId, out WebSocketSession userSession))
                {
                    UserPaymentStatus userPaymentStatus = (UserPaymentStatus)userSession[Constants.ContextKeys.PaymentStatus];
                    if (userPaymentStatus != null)
                    {
                        userPaymentStatus.IncreasePayments(todayTs);
                    }
                }

                await transaction.Commit();
                dispatchItem?.TaskSource?.TrySetResult(instrument);
                if (this._items.TryRemove(paymentId, out DispatchItem ignored))
                {
                    // Cancellation is nolonger required
                    dispatchItem?.CancellationRegistration.Dispose();
                    this._logger.LogInformation("dispatchItemRemovedByAccepted:{payment}", paymentId);
                }
                else
                {
                    this._logger.LogWarning("paymentNotFoundInDispatchItems:{payment}", paymentId);
                }

                // broadcast remove_payment to all sessions
                NotificationService.Message<NotificationService.PaymentItem> message = new NotificationService.Message<NotificationService.PaymentItem>
                {
                    Operation = Constants.Operations.RemovePayment,
                    Content = new NotificationService.PaymentItem
                    {
                        Id = payment.Id,
                        Amount = payment.Amount,
                        MerchantReferenceNumber = payment.MerchantReferenceNumber
                    }
                };
                foreach (WebSocketSession session in this._users.Values)
                {
                    try
                    {
                        session.SendText(JsonConvert.SerializeObject(message)).Forget();
                    }
                    catch (Exception e)
                    {
                        this._logger.LogInformation("failedToSendMessageToUser:{0},{1}", userId, e);
                    }
                }

                return payment;
            }
        }

        public void NotifyUserToSettle(Payment payment)
        {
            if (this._users.TryGetValue(payment.Instrument.UserId, out WebSocketSession session))
            {
                session.SendText(JsonConvert.SerializeObject(new NotificationService.Message<NotificationService.PaymentItem>
                {
                    Operation = Constants.Operations.NotifyToSettle,
                    Content = new NotificationService.PaymentItem
                    {
                        Id = payment.Id,
                        MerchantReferenceNumber = payment.MerchantReferenceNumber,
                        Amount = payment.Amount
                    }
                })).Forget();
            }
        }

        private async Task<List<long>> ListEligibleUsers(Payment payment, long todayTs)
        {
            List<long> results = new List<long>();
            List<long> lastBatch = new List<long>();

            async Task<List<long>> FilterEligibleUsers(List<long> userIds)
            {
                string ids = string.Join(',', userIds);
                
                List<UserPoint> users = await this._dataAccessor.GetAll<UserPoint>("select u.`id` as `id`,u.`evaluation_point` as `evaluation_point` from `user_account` as u " +
                    "join `collect_instrument` as ci on ci.`user_id`=u.`id` " +
                    "where ci.`status`=2 " + 
                    (_config.PendingPaymentsLimit > 0 ? $"and u.`pending_payments`<{_config.PendingPaymentsLimit} " : "") +
                    "and ci.`channel_id`=@channel " +
                    "and u.`id` in (" + ids + ") and u.`balance`*100>@amount " +
                    "and (u.`merchant_id`=@merchant_id or u.`merchant_id`=0) " +
                    "and ci.`daily_limit`*100-@amount>=coalesce((select sum(`amount`) from `payment` where `ciid`=ci.`id` and `accept_time`>=@todayTs and `status` in (2,3)), 0) " +
                    "group by u.`id`, u.`evaluation_point`",
                p =>
                {
                    p.Add("@merchant_id", MySqlDbType.Int32).Value = payment.Merchant.Id;
                    p.Add("@channel", MySqlDbType.Int32).Value = payment.Channel;
                    p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount;
                    p.Add("@todayTs", MySqlDbType.Int64).Value = todayTs;
                });

                List<UserEvaluation> payLimitConfigs = await this._dataAccessor.GetAll<UserEvaluation>
                                                    (
                                                        "select * from `user_evaluation` where `type`=@type order by `condition`",
                                                        p => p.Add("@type", MySqlDbType.Int32).Value = UserEvaluationType.PayAllowLimits
                                                    );

                List<long> allowUsers = new List<long>();
                foreach (UserPoint user in users)
                {
                    foreach (UserEvaluation limit in payLimitConfigs)
                    {
                        if (limit.Condition > user.Point)
                        {
                            break;
                        }

                        if ( limit.Value >= payment.Amount / 100)
                        {
                            allowUsers.Add(user.Id);
                            break;
                        }
                    }
                }
                this._logger.LogInformation($"get user list for payment:{payment.Id}-{string.Join(',', allowUsers)}");
                return allowUsers;
            }
            this._logger.LogInformation($"user connect list{JsonConvert.SerializeObject(this._users.Keys)}");
            foreach (long uid in this._users.Keys)
            {
                if (this._config.EligibleUserQueryBatchSize <= 0 || lastBatch.Count < this._config.EligibleUserQueryBatchSize)
                {
                    lastBatch.Add(uid);
                }
                else
                {
                    results.AddRange(await FilterEligibleUsers(lastBatch));
                    lastBatch.Clear();
                }
            }

            if (lastBatch.Count > 0)
            {
                results.AddRange(await FilterEligibleUsers(lastBatch));
            }

            int GetUserValidPayments(long uid)
            {
                if (this._users.TryGetValue(uid, out WebSocketSession s))
                {
                    UserPaymentStatus paymentStatus = (UserPaymentStatus)s[Constants.ContextKeys.PaymentStatus];
                    return paymentStatus?.GetValidPayments(todayTs) ?? 0;
                }

                return 0;
            }

            results.Sort((u1, u2) => GetUserValidPayments(u1) - GetUserValidPayments(u2));
            return results;
        }

        public async Task GcTimeoutPayments()
        {
            while (true)
            {
                long epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long lastAcceptEpoch = epoch - this._config.PaymentSettleTimeout * 1000;
                using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
                {
                    List<Payment> timeoutPayments = await transaction.GetAll<Payment>("select *, `ciid` as `ci_id` from payment where `status`=@currentStatus and `accept_time`<@acceptTime",
                        p =>
                        {
                            p.Add("@currentStatus", MySqlDbType.Int32).Value = (int)PaymentStatus.Accepted;
                            p.Add("@acceptTime", MySqlDbType.Int64).Value = lastAcceptEpoch;
                        });
                    foreach (Payment payment in timeoutPayments)
                    {
                        UserAccount acceptor = await transaction.GetOne<UserAccount>("select ua.* from user_account as ua"
                            + " join `collect_instrument` as ci on ua.`id` = ci.`user_id`"
                            + " join `payment` as p on p.`ciid` = ci.`id`"
                            + " where p.id=@pid",
                        p =>
                        {
                            p.Add("@pid", MySqlDbType.Int64).Value = payment.Id;
                        });

                        if (acceptor != null)
                        {
                            this._logger.LogInformation($"gcTimeOut paymentId:{payment.Id}, userId:{acceptor.Id} before_pending:{acceptor.PendingBalance}, after_pending:{acceptor.PendingBalance-payment.Amount}");
                            //return PendingBalance to balance
                            await transaction.Execute("update `user_account` set `pending_balance`=`pending_balance`-@amount," +
                                " `balance`=`balance`+@amount/100 where id=@uid",
                                p =>
                                {
                                    p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount;
                                    p.Add("@uid", MySqlDbType.Int64).Value = acceptor.Id;
                                });

                            // Add a transaction log
                            await transaction.Execute(
                                "insert into `transaction_log`(`type`, `payment_id`, `user_id`, `amount`, `balance_before`, `balance_after`, `time`) values(@type, @paymentId, @userId, @amount, @balBefore, @balAfter, @time)",
                                p =>
                                {
                                    p.Add("@type", MySqlDbType.Int32).Value = (int)TransactionType.Refund;
                                    p.Add("@paymentId", MySqlDbType.Int64).Value = payment.Id;
                                    p.Add("@userId", MySqlDbType.Int64).Value = acceptor.Id;
                                    p.Add("@amount", MySqlDbType.Int32).Value = payment.Amount;
                                    p.Add("@balBefore", MySqlDbType.Double).Value = acceptor.Balance;
                                    p.Add("@balAfter", MySqlDbType.Double).Value = (acceptor.Balance + payment.Amount / 100.0);
                                    p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                });
                        }
                        else
                        {
                            this._logger.LogError("AcceptorNotFound:{payment},{ciid}", payment.Id, payment.Instrument.Id);
                        }

                        //update payment status
                        await transaction.Execute("update `payment` set `status`=@status where id=@id",
                            p =>
                            {
                                p.Add("@status", MySqlDbType.Int32).Value = PaymentStatus.Aborted;
                                p.Add("@id", MySqlDbType.Int64).Value = payment.Id;
                            });
                    }
                    await transaction.Commit();

                }
                await Task.Delay(60 * 1000); // activate every minute
            }
        }

        public async Task EvaluationTask(long start=0)
        {
            DateTimeOffset today = DateTimeOffset.UtcNow.RoundDay(this._config.TimeZone);
            long endTime = today.ToUnixTimeMilliseconds();
            long startTime = today.AddDays(-1).ToUnixTimeMilliseconds();
            long settleDate = today.AddDays(-1).ToDateValue(this._config.TimeZone);
            using (DataTransaction transaction = await this._dataAccessor.CreateTransaction())
            {
                this._logger.LogInformation($"start settle {settleDate} point");
                List<Payment> payments = await transaction.GetAll<Payment>("select p.*,ci.`id` as ci_id, ci.`user_id` as ci_user_id from `payment` as p join `collect_instrument` as ci on p.`ciid`=ci.`id` where p.`accept_time`>=@startTime and p.`accept_time`<@endTime",
                    p =>
                    {
                        p.Add("@startTime", MySqlDbType.Int64).Value = startTime;
                        p.Add("@endTime", MySqlDbType.Int64).Value = endTime;
                    });

                List<UserEvaluation> userEvaluations = await transaction.GetAll<UserEvaluation>("select * from `user_evaluation` order by `value`");
                List<UserEvaluation> OverTimePunish = userEvaluations.Where(e => e.Type == UserEvaluationType.OverTimePunish).ToList();
                List<UserEvaluation> SpeedPayCommend = userEvaluations.Where(e => e.Type == UserEvaluationType.SpeedPayCommend).ToList();
                Dictionary<long, List<Payment>> paymentLists = payments.GroupBy(x => x.Instrument.UserId).ToDictionary(x => x.Key, x => x.ToList());
                foreach (long userId in paymentLists.Keys)
                {
                    this._logger.LogInformation($"start settle user{userId}");
                    UserAccount user = await transaction.GetOne<UserAccount>("select * from user_account where id=@uid",
                        p => p.Add("@uid", MySqlDbType.Int64).Value = userId);

                    if (user == null)
                    {
                        this._logger.LogInformation($"user:{userId} is not found");
                        continue;
                    }

                    List<Payment> userPayments = paymentLists.GetValueOrDefault(userId);
                    //punish overTime
                    int overTimeCount = userPayments.Where(x => x.Status == PaymentStatus.Aborted).Count();
                    int overTimePunish = 0;
                    foreach (UserEvaluation punish in OverTimePunish)
                    {
                        if (punish.Condition > overTimeCount)
                        {
                            break;
                        }
                        overTimePunish = punish.Value;
                    }
                        
                    if (overTimePunish > 0)
                    {
                        this._logger.LogInformation($"user{userId} {settleDate} overTimePunish {overTimePunish}");
                        //punish point
                        await transaction.Execute("update `user_account` set `evaluation_point`=`evaluation_point`-@point where id=@uid",
                            p =>
                            {
                                p.Add("@point", MySqlDbType.Int32).Value = overTimePunish;
                                p.Add("@uid", MySqlDbType.Int64).Value = userId;
                            });

                        // Add a transaction log
                        await transaction.Execute(
                            "insert into `evaluation_log`(`type`, `user_id`, `point`, `point_before`, `point_after`, `time`) values(@type, @userId, @point, @pointBefore, @pointAfter, @time)",
                            p =>
                            {
                                p.Add("@type", MySqlDbType.Int32).Value = (int)UserEvaluationType.OverTimePunish;
                                p.Add("@userId", MySqlDbType.Int64).Value = userId;
                                p.Add("@point", MySqlDbType.Int32).Value = -overTimePunish;
                                p.Add("@pointBefore", MySqlDbType.Int32).Value = user.Point;
                                p.Add("@pointAfter", MySqlDbType.Int32).Value = user.Point - overTimePunish;
                                p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            });
                        user.Point = user.Point - overTimePunish;
                    }

                    //SpeedPayCommend
                    int maxCommand = 0;
                    foreach (UserEvaluation evaluation in SpeedPayCommend)
                    {
                        int speedPayCount = userPayments.Where(x=>x.SettleTimestamp != 0).Where(x => (x.SettleTimestamp - x.AcceptTimestamp) <= evaluation.Condition * 60 * 1000).Count();
                        if (evaluation.Count > speedPayCount)
                        {
                            break;
                        }
                        maxCommand = evaluation.Value;
                    }

                    if (maxCommand > 0)
                    {
                        this._logger.LogInformation($"user{userId} {settleDate} speedCommand {maxCommand}");

                        //punish point
                        await transaction.Execute("update `user_account` set `evaluation_point`=`evaluation_point`+@point where id=@uid",
                            p =>
                            {
                                p.Add("@point", MySqlDbType.Int32).Value = maxCommand;
                                p.Add("@uid", MySqlDbType.Int64).Value = userId;
                            });

                        // Add a transaction log
                        await transaction.Execute(
                            "insert into `evaluation_log`(`type`, `user_id`, `point`, `point_before`, `point_after`, `time`) values(@type, @userId, @point, @pointBefore, @pointAfter, @time)",
                            p =>
                            {
                                p.Add("@type", MySqlDbType.Int32).Value = (int)UserEvaluationType.SpeedPayCommend;
                                p.Add("@userId", MySqlDbType.Int64).Value = userId;
                                p.Add("@point", MySqlDbType.Int32).Value = maxCommand;
                                p.Add("@pointBefore", MySqlDbType.Int32).Value = user.Point;
                                p.Add("@pointAfter", MySqlDbType.Int32).Value = user.Point + maxCommand;
                                p.Add("@time", MySqlDbType.Int64).Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            });
                    }
                }
                await transaction.Commit();
            }
        }

        public class UserPoint
        {
            [Column("id")]
            public long Id { get; set; }
            [Column("evaluation_point")]
            public int Point { get; set; }
        }

        public class DispatchItem
        {
            public Payment Payment { get; set; }

            public TaskCompletionSource<CollectInstrument> TaskSource { get; set; }

            public CancellationTokenRegistration CancellationRegistration { get; set; }
        }
    }
}
