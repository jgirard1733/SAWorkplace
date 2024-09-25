using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SAWorkplace.Data;
using SAWorkplace.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Okta.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using NLog;
using LoggerService;
using System.IO;
using Microsoft.IdentityModel.Logging;

namespace SAWorkplace
{
    public class Startup
    {
        protected Decrypter decrypter;
        private static ILoggerManager logger;
        public Startup(IConfiguration configuration)
        {
            LogManager.LoadConfiguration(String.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));
            Configuration = configuration;
            decrypter = new Decrypter(logger);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IdentityModelEventSource.ShowPII = true;
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.SameAsRequest;
            });
            services.AddControllersWithViews();
            services.AddDbContext<ApplicationDBContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromHours(24);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddApplicationInsightsTelemetry();
            services.AddSignalR();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<ILoggerManager, LoggerManager>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OktaDefaults.MvcAuthenticationScheme;
            })
            .AddOktaMvc(new OktaMvcOptions
            {
                // Replace these values with your Okta configuration
                OktaDomain = Configuration["Okta:Domain"],
                ClientId = decrypter.Decrypt("*bJ:}Y9W[.G3R{g>", Configuration["Okta:ClientId"]),
                ClientSecret = decrypter.Decrypt("r=[vv54H`qnp\\q5/", Configuration["Okta:ClientSecret"]),
                AuthorizationServerId = "",
                Scope = new List<string> { "openid", "profile", "email", "groups" },
            })
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
            services.AddMvc(o =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                o.Filters.Add(new AuthorizeFilter(policy));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerManager logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Request/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<Services.SignalR_Notifications>("/signalServer");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Request}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "SME",
                    pattern: "{controller=SME}/{action=Index}/{id?}");
            });



            //app.UseSignalR(route =>
            //{
            //    route.MapHub<Services.SignalR_Notifications>("/signalServer");
            //});

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Request}/{action=Index}/{id?}");
            //});
        }
    }
}