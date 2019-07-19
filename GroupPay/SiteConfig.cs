using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Core;
using GroupPay.Models;
using Newtonsoft.Json;

namespace GroupPay
{
    public class SiteConfig
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo> configurableProperties = new ConcurrentDictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly object _timeZoneLock = new object();
        private readonly object _semaphoreLock = new object();
        private TimeZoneInfo _timeZoneInfo = null;
        private SemaphoreSlim _commissionClearSemaphore = null;

        [JsonProperty("timeZoneOffset")]
        public int TimeZoneOffset { get; set; }

        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("uploadFilesPath")]
        public string UploadFilesPath { get; set; }

        [JsonProperty("uploadFilesRelativeUrl")]
        public string UploadFilesRelativeUrl { get; set; }

        [JsonProperty("qrCodeService")]
        public string QrCodeService { get; set; }

        [JsonProperty("userRole")]
        public string UserRole { get; set; }

        [JsonProperty("secretKey")]
        public string SecretKey { get; set; }

        [JsonProperty("forcePwdChangeBefore")]
        public long ForcePwdChangeBefore { get; set; }

        [JsonProperty("paymentSettleTimeout")]
        public int PaymentSettleTimeout { get; set; } = 300;

        [JsonProperty("paymentPendingTimeout")]
        public int PaymentPendingTimeout { get; set; }

        [JsonProperty("tokenExpiry")]
        public int TokenExpiry { get; set; }

        [JsonProperty("commissionClearThrottling")]
        public int CommissionClearThrottling { get; set; } = 100;

        [JsonProperty("userLogsWindow")]
        public int UserLogsWindow { get; set; } = 3;

        [JsonProperty("appDownloadUrl")]
        public string AppDownloadUrl { get; set; }

        [JsonProperty("urlShortenService")]
        public string UrlShortenService { get; set; } = "http://localhost:5000/api/Url/Shorten";

        [JsonProperty("registerUrl")]
        public string RegisterUrl { get; set; } = "http://localhost:5000/Home/Register";

        [JsonProperty("maxDispatchBatchSize")]
        public int MaxDispatchBatchSize { get; set; } = -1;

        [JsonProperty("dispatchTimeout")]
        public int DispatchTimeout { get; set; } = 10000; // 10 seconds

        [JsonProperty("pendingPaymentsLimit")]
        public int PendingPaymentsLimit { get; set; } = -1; // no limit

        [JsonProperty("randomInstrumentMode")]
        public bool RandomInstrumentMode { get; set; } = false;

        [JsonProperty("eligibleUserQueryBatchSize")]
        public int EligibleUserQueryBatchSize { get; set; } = 50;

        [JsonProperty("chatSvcEndpoint")]
        public string ChatServiceEndpoint { get; set; } = "wss://chat.jdbishang.com/chat/";

        [JsonProperty("chatSvcKey")]
        public string ChatServiceKey { get; set; } = "nKncRiWnvkZ7JuwM";

        public TimeZoneInfo TimeZone
        {
            get
            {
                if (this._timeZoneInfo == null)
                {
                    lock (this._timeZoneLock)
                    {
                        if (this._timeZoneInfo == null)
                        {
                            string standardName = "Platform Time";
                            string displayName = string.Format("(GMT{0}{1}:00) Platform Time", this.TimeZoneOffset >= 0 ? '+' : '-', Math.Abs(this.TimeZoneOffset));
                            this._timeZoneInfo = TimeZoneInfo.CreateCustomTimeZone(standardName, TimeSpan.FromHours(this.TimeZoneOffset), displayName, standardName);
                        }
                    }
                }

                return this._timeZoneInfo;
            }
        }

        public SemaphoreSlim CommissionClearingSemaphore
        {
            get
            {
                if (this._commissionClearSemaphore == null)
                {
                    lock (this._semaphoreLock)
                    {
                        if (this._commissionClearSemaphore == null)
                        {
                            this._commissionClearSemaphore = new SemaphoreSlim(this.CommissionClearThrottling, this.CommissionClearThrottling);
                        }
                    }
                }

                return this._commissionClearSemaphore;
            }
        }

        public void Apply(ConfigItem configItem)
        {
            if (!configurableProperties.TryGetValue(configItem.Name, out PropertyInfo property))
            {
                Type type = this.GetType();
                foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    JsonPropertyAttribute attribute = prop.GetCustomAttribute<JsonPropertyAttribute>();
                    if (attribute != null && (configItem.Name.EqualsIgnoreCase(attribute.PropertyName) || prop.Name.EqualsIgnoreCase(configItem.Name)))
                    {
                        property = prop;
                        configurableProperties.TryAdd(configItem.Name, prop);
                        break;
                    }
                }
            }

            if (property == null)
            {
                return;
            }

            if (property.PropertyType == typeof(string))
            {
                if (!string.IsNullOrEmpty(configItem.Value))
                {
                    property.SetValue(this, configItem.Value);
                }
            }
            else if (property.PropertyType == typeof(int))
            {
                if (int.TryParse(configItem.Value, out int intValue))
                {
                    property.SetValue(this, intValue);
                }
            }
            else if (property.PropertyType == typeof(long))
            {
                if (long.TryParse(configItem.Value, out long longValue))
                {
                    property.SetValue(this, longValue);
                }
            }
            else if (property.PropertyType == typeof(bool))
            {
                if (bool.TryParse(configItem.Value, out bool boolValue))
                {
                    property.SetValue(this, boolValue);
                }
            }
            else if (property.PropertyType == typeof(double))
            {
                if (double.TryParse(configItem.Value, out double doubleValue))
                {
                    property.SetValue(this, doubleValue);
                }
            }
        }
    }
}
