using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace GroupPay
{
    public static class Constants
    {
        public static class WebApiErrors
        {
            public const string Success = "success";
            public const string InvalidData = "invalid_data";
            public const string ObjectNotFound = "object_not_found";
            public const string DependencyFailed = "dependency_failed";
            public const string ObjectConflict = "object_conflict";
            public const string RequiredMoreData = "more_data";
            public const string ServiceNotReady = "service_not_ready";
            public const string InvalidCredentials = "invalid_credentials";
            public const string PasswordChangeRequired = "password_change_required";
            public const string Pending = "pending";
            public const string NotSupported = "not_supported";
            public const string Unknown = "unknown";
        }

        public static class Web
        {
            public const string CaptchaCode = "CaptchaCode";
            public const string UserTokenAuthScheme = "UserToken";
            public const string CombinedAuthSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + UserTokenAuthScheme;
            public const int DefaultRequestTimeout = 60 * 1000;
            public const int DefaultPageSize = 20;
        }

        public static class Operations
        {
            public const string Unknown = "unknown";
            public const string PaymentNotification = "payment_notification";
            public const string ListPayment = "list_payment";
            public const string AcceptPayment = "accept_payment";
            public const string NotifyToSettle = "notify_to_settle";
            public const string RemovePayment = "remove_payment";
        }

        public static class Events
        {
            public static readonly EventId IncomingRequest = new EventId(5001);
        }

        public static readonly Dictionary<string, string> BankMark = new Dictionary<string, string>
        {
            { "中国工商银行", "ICBC" },
            { "中国农业银行", "ABC" },
            { "中国银行", "BOC" },
            { "中国建设银行", "CCB" },
            { "中国交通银行", "BOCM" },
            { "中国招商银行", "CMB" },
            { "中信银行", "CITIC" },
            { "中国民生银行", "CMBC" },
            { "中国光大银行", "CEB" },
            { "中国平安银行", "PABC" },
            { "上海浦东发展银行", "SPDB" },
            { "中国邮政储蓄银行", "PSBC" },
            { "华夏银行", "HXB" },
            { "兴业银行", "CIB" },
            { "北京银行", "BOB" },
            { "上海银行", "BOS" },
            { "广东发展银行", "CGB" },
            { "厦门银行", "XMBANK" }
        };

        public static class ContextKeys
        {
            public const string PaymentStatus = "PaymentStatus";
        }
    }
}
