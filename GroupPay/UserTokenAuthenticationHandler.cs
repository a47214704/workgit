using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Core;
using Core.Crypto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroupPay
{
    public class UserTokenAuthenticationHandler : AuthenticationHandler<UserTokenAuthenticationOptions>
    {
        public UserTokenAuthenticationHandler(
            IOptionsMonitor<UserTokenAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
         : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authentication = this.Context.Request.Headers["Authentication"];
            string[] schemeAndToken = null;
            if (string.IsNullOrEmpty(authentication))
            {
                this.Logger.LogDebug("noAuthenticationHeader:{0}", this.Context.TraceIdentifier);

                authentication = this.Context.Request.Query["_token"];
                if (string.IsNullOrEmpty(authentication))
                {
                    return Task.FromResult(AuthenticateResult.Fail("User token not found"));
                }

                schemeAndToken = new string[]
                {
                    Constants.Web.UserTokenAuthScheme,
                    authentication
                };
            }
            else
            {
                schemeAndToken = authentication.Split(' ');
            }

            if (schemeAndToken.Length != 2)
            {
                this.Logger.LogDebug("badAuthenticationHeaderFormat:{0},{1}", this.Context.TraceIdentifier, authentication);
                return Task.FromResult(AuthenticateResult.Fail("Bad Authentication header format"));
            }

            if (schemeAndToken[0] != Constants.Web.UserTokenAuthScheme)
            {
                this.Logger.LogDebug("unsupportedAuthenticationScheme:{0},{1}", this.Context.TraceIdentifier, schemeAndToken[0]);
                return Task.FromResult(AuthenticateResult.Fail("Bad authentication scheme"));
            }

            string[] tokens = schemeAndToken[1].Split('.');
            if (tokens.Length != 2)
            {
                this.Logger.LogDebug("badTokenFormat:{0},{1}", this.Context.TraceIdentifier, schemeAndToken[1]);
                return Task.FromResult(AuthenticateResult.Fail("Bad token format"));
            }

            string iv = tokens[0];
            string token = tokens[1];
            try
            {
                string data = Encoding.UTF8.GetString(token.DecodeWithBase64().DecryptWithAes256(this.Options.SecretKey, iv));
                string[] values = data.Split(',');
                if (values.Length < 4)
                {
                    this.Logger.LogDebug("badTokenContent:{0},{1}", this.Context.TraceIdentifier, data);
                    return Task.FromResult(AuthenticateResult.Fail("unexpected user token data"));
                }

                if (!long.TryParse(values[3], out long timestamp))
                {
                    this.Logger.LogInformation("invalidTimestampInToken:{0},{1}", this.Context.TraceIdentifier, data);
                    return Task.FromResult(AuthenticateResult.Fail("invalid user token data"));
                }

                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp >= this.Options.TokenExpiry * 1000)
                {
                    this.Logger.LogInformation("tokenExpired:{0},{1}", this.Context.TraceIdentifier, timestamp);
                    return Task.FromResult(AuthenticateResult.Fail("user token expired"));
                }

                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, values[0]),
                    new Claim(ClaimTypes.Name, values[1]),
                    new Claim(ClaimTypes.Sid, values[2])
                };
                for (int i = 4; i < values.Length; ++i)
                {
                    claims.Add(new Claim(ClaimTypes.Role, values[i]));
                }

                ClaimsIdentity identity = new ClaimsIdentity(claims, Constants.Web.UserTokenAuthScheme);
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Constants.Web.UserTokenAuthScheme)));
            }
            catch (Exception exception)
            {
                this.Logger.LogWarning("failedToParseToken:{0},{1}", this.Context.TraceIdentifier, exception);
                return Task.FromResult(AuthenticateResult.Fail("Failed to parse token"));
            }
        }
    }
}
