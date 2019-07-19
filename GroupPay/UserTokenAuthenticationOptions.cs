using System;
using Microsoft.AspNetCore.Authentication;

namespace GroupPay
{
    public class UserTokenAuthenticationOptions : AuthenticationSchemeOptions
    {
        public UserTokenAuthenticationOptions()
            : base()
        {
            this.TokenExpiry = 24 * 3600; // defaut to one day
        }

        public string SecretKey { get; set; }

        public int TokenExpiry { get; set; }
    }
}
