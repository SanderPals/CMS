using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rotativa.AspNetCore;
using Site.Data;
using Site.Models;
using Site.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace Site
{
    public class Startup
    {
        public static IConfigurationRoot Configuration { get; set; }

        private CultureInfo[] supportedCultures = new CultureInfo[]
        {
                new CultureInfo("nl"),
                new CultureInfo("en"),
        };

        public class MyConfig
        {
            //public string ApplicationName { get; set; }
            //public int WebsiteId { get; set; }
        }

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();

                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        //public void OnConfiguring(IServiceCollection services)
        //{
        //    services.AddMvc();

        //    var cs = Configuration.GetConnectionString("DatabaseCon");
        //    services.AddDbContext<ets1Context>(options => options.UseSqlServer(cs));
        //}

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });

            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            //var cs = Configuration.GetConnectionString("DatabaseCon");
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(cs));


            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(s =>
            {
                s.SupportedCultures = supportedCultures;
                s.SupportedUICultures = supportedCultures;
                s.DefaultRequestCulture = new RequestCulture(culture: "nl", uiCulture: "nl");
            });


            var cs = Configuration.GetConnectionString("DatabaseCon");

            var connectionStringBuilder = new SqlConnectionStringBuilder(cs)
            {
                TrustServerCertificate = true,
                Encrypt = true,
            };

            services.AddIdentity<ApplicationUser, IdentityRole>(config =>
            {
                config.Password.RequireDigit = true;
                config.Password.RequireLowercase = true;
                config.Password.RequireUppercase = true;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequiredLength = 8;
                config.SignIn.RequireConfirmedEmail = true;
                config.Lockout.MaxFailedAccessAttempts = 5;
                config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
                config.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();


            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(cs));
            services.AddDbContext<SiteContext>(options => options.UseSqlServer(cs));

            services.AddAuthorization();

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            }).AddJsonOptions(
                options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            ).AddViewLocalization(
                LanguageViewLocationExpanderFormat.SubFolder,
                opts => { opts.ResourcesPath = "Resources"; })
            .AddDataAnnotationsLocalization();

            services.AddMemoryCache();
            services.AddSession();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = new PathString("/");
                options.AccessDeniedPath = new PathString("/access-denied");
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.SlidingExpiration = true;
            });

            services.AddSingleton(_ => Configuration);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //services.Configure<MyConfig>(options => Configuration.GetSection("WebsiteSettings").Bind(options));

            services.Configure<AuthMessageSenderOptions>(options => Configuration.GetSection("SendGridSettings").Bind(options));

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            // DISABLES REASON: GCM not supported on MacOS. Only supported on Windows.
            // Add data protection service
            /*DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), @"Keys"));
            services.AddDataProtection()
                    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
                    {
                        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                        ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                    })
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(730))
                    .DisableAutomaticKeyGeneration()
                    .PersistKeysToFileSystem(directoryInfo)
                    .SetApplicationName("Spine");*/
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseAuthentication();

            app.UseResponseCompression();
            //app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }


            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("nl"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new QueryStringRequestCultureProvider
                    {
                        QueryStringKey = "culture",
                        UIQueryStringKey = "ui-culture"
                    }
                }
            });

            //app.UseApplicationInsightsExceptionTelemetry();

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @".well-known")),
                RequestPath = new PathString("/.well-known"),
                ServeUnknownFileTypes = true // serve extensionless file
            });                      // Must be AFTER UseDefaultFiles

            app.UseSession();

            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Error",
                    template: "error",
                    defaults: new { controller = "Shared", action = "Error" });

                routes.MapRoute(
                    name: "VerifyCode",
                    template: "verify-code",
                    defaults: new { controller = "Account", action = "VerifyCode" });

                routes.MapRoute(
                    name: "SendCode",
                    template: "send-code",
                    defaults: new { controller = "Account", action = "SendCode" });

                routes.MapRoute(
                    name: "Lockout",
                    template: "lockout",
                    defaults: new { controller = "Account", action = "Lockout" });

                routes.MapRoute(
                    name: "ForgotPassword",
                    template: "forgot-password",
                    defaults: new { controller = "Account", action = "ForgotPassword" });

                routes.MapRoute(
                    name: "Register",
                    template: "register",
                    defaults: new { controller = "Account", action = "Register" });

                routes.MapRoute(
                    name: "ResetPassword",
                    template: "reset-password",
                    defaults: new { controller = "Account", action = "ResetPassword" });

                routes.MapRoute(
                    name: "ConfirmEmail",
                    template: "confirm-email",
                    defaults: new { controller = "Account", action = "ConfirmEmail" });

                routes.MapRoute(
                    name: "Login",
                    template: "",
                    defaults: new { controller = "Account", action = "Login" });

                routes.MapRoute(
                    name: "Dashboard",
                    template: "dashboard",
                    defaults: new { controller = "Account", action = "Dashboard" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Account}/{action=Login}/{id?}");
            });

            // call rotativa conf passing env to get web root path
            RotativaConfiguration.Setup(env, "exe/Rotativa");
        }
    }
}
