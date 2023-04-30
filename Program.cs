using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace Site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    EnsureDataStorageIsReady(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return new WebHostBuilder()
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .ConfigureAppConfiguration((hostingContext, config) =>
                        {
                            var env = hostingContext.HostingEnvironment;

                            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                                  .SetBasePath(env.ContentRootPath);

                            if (env.IsDevelopment())
                            {
                                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                                //builder.AddUserSecrets<Startup>();

                                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.

                            }

                            if (env.IsDevelopment())
                            {
                                //config.AddApplicationInsightsSettings(developerMode: true);

                                var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                                if (appAssembly != null)
                                {
                                    config.AddUserSecrets(appAssembly, optional: true);
                                }
                            }

                            config.AddEnvironmentVariables();

                            if (args != null)
                            {
                                //config.AddCommandLine(args);
                            }
                        })
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            //logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                            //logging.AddConsole();
                            //logging.AddDebug();
                        })
                        .UseIISIntegration()
                        .UseDefaultServiceProvider((context, options) =>
                        {
                            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                        })
                        .UseStartup<Startup>()
                        .Build();
        }

        private static void EnsureDataStorageIsReady(IServiceProvider services)
        {
            //CoreEFStartup.InitializeDatabaseAsync(services).Wait();
            //SimpleContentEFStartup.InitializeDatabaseAsync(services).Wait();
            //LoggingEFStartup.InitializeDatabaseAsync(services).Wait();
        }
    }
}
