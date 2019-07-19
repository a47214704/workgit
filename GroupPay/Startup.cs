using System;
using Core;
using Core.Data;
using GroupPay.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroupPay
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            SiteConfig siteConfig = this.Configuration.GetSection("SiteConfig").Get<SiteConfig>();
            services.AddSingleton(siteConfig);
            services.AddDataAccessor(settings =>
            {
                ConnectionSettings config = this.Configuration.GetSection("Database").Get<ConnectionSettings>();
                settings.Server = config.Server;
                settings.Database = config.Database;
                settings.User = config.User;
                settings.Password = config.Password;
            });
            services.AddDistributedMemoryCache();
            services.AddServiceAccountStore();
            services.AddSingleton<PaymentDispatcher>();
            services.AddSingleton<AgencyCommissionService>();
            services.AddSession();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(opt =>
                    {
                        opt.LoginPath = "/Home/Login";
                        opt.LogoutPath = "/Home/Logout";
                        opt.AccessDeniedPath = "/Home/AccessDeined";
                        opt.DataProtectionProvider = new DataProtectionProvider(siteConfig.SecretKey);
                    })
                    .AddServiceTicket(ServiceTicketAuthenticationDefaults.AuthenticationScheme, options =>
                    {
                        options.TicketLifeTime = TimeSpan.FromHours(1);
                    })
                    .AddUserToken(Constants.Web.UserTokenAuthScheme, options =>
                    {
                        options.SecretKey = siteConfig.SecretKey;
                        options.TokenExpiry = siteConfig.TokenExpiry;
                    });
            services.AddAuthorization();
            services.AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "text/plain"
            });
            app.UseProxyHeaders();
            app.UseSession();
            app.UseAuthentication();
            app.UseRequestLogging();
            app.UseWebApiExceptionHandling("/api");
            app.UseWebSocketsAndRouter(configWsRouter: opts =>
            {
                opts.IdleTimeout = TimeSpan.FromMinutes(5);
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
