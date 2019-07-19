using System;
using Core;
using GroupPay.Models;

namespace GroupPay
{
    public static class ValidationExtensions
    {
        public static bool IsValid(this ServiceInstance instance)
        {
            return instance != null &&
                instance.ServiceId > 0 &&
                !string.IsNullOrEmpty(instance.Cluster) &&
                !string.IsNullOrEmpty(instance.Endpoint) &&
                !string.IsNullOrEmpty(instance.Server);
        }

        public static bool IsValid(this ServiceAccount service)
        {
            return service != null &&
                service.Id > 0 &&
                !string.IsNullOrEmpty(service.Name) &&
                !string.IsNullOrEmpty(service.Category) &&
                !string.IsNullOrEmpty(service.ServiceEndpoint) &&
                !string.IsNullOrEmpty(service.PrimaryPassword) &&
                !string.IsNullOrEmpty(service.SecondaryPassword);
        }

        public static bool IsValid(this CollectChannel channel)
        {
            return channel != null &&
                !string.IsNullOrEmpty(channel.Name) &&
                channel.ChannelType != ChannelType.None;
        }

        public static bool IsValid(this RolePermission rolePermission)
        {
            return rolePermission != null &&
                rolePermission.RoleId > 0 &&
                !string.IsNullOrEmpty(rolePermission.Permission);
        }

        public static bool IsValid(this UserRole role)
        {
            return role != null && !string.IsNullOrEmpty(role.Name);
        }

        public static bool IsValid(this CollectInstrument instrument)
        {
            return instrument != null &&
                !string.IsNullOrEmpty(instrument.Name) &&
                instrument.Channel != null &&
                Enum.IsDefined(typeof(CollectChannelType), instrument.Channel.Id);
        }

        public static bool IsValid(this Merchant merchant)
        {
            return !string.IsNullOrEmpty(merchant.Name) &&
                !string.IsNullOrEmpty(merchant.AppKey) &&
                !string.IsNullOrEmpty(merchant.AppSecret);
        }

        public static bool IsValid(this SecurityQuestion question)
        {
            return question != null && !string.IsNullOrEmpty(question.Question);
        }

        public static bool IsValid(this UserAccount userAccount)
        {
            return userAccount != null &&
                !string.IsNullOrEmpty(userAccount.AccountName) &&
                !string.IsNullOrEmpty(userAccount.Password) &&
                userAccount.AccountName.IsPhoneFormat();
        }

        public static bool IsValid(this SecurityAnswer answer)
        {
            return answer != null && !string.IsNullOrEmpty(answer.Answer) && answer.Question != null;
        }

        public static bool IsValid(this Payment payment)
        {
            return payment != null &&
                payment.Channel > 0 &&
                payment.Amount > 0 &&
                !string.IsNullOrEmpty(payment.MerchantReferenceNumber) &&
                !string.IsNullOrEmpty(payment.NotifyUrl);
        }

        public static bool IsValid(this CommissionRatio commissionRatio)
        {
            return commissionRatio != null &&
                commissionRatio.LowerBound >= 0 &&
                (commissionRatio.UpperBound > commissionRatio.LowerBound || commissionRatio.UpperBound == -1) &&
                commissionRatio.Ratio > 0d;
        }

        public static bool IsValid(this ConfigItem config)
        {
            return config != null &&
                !string.IsNullOrEmpty(config.Name) &&
                !string.IsNullOrEmpty(config.DisplayName) &&
                !string.IsNullOrEmpty(config.Value);
        }

        public static bool IsValid(this AwardConfig config)
        {
            return config != null &&
                config.Bouns >= 0 &&
                config.AwardCondition >= 0;
        }
    }
}
