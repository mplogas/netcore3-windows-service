using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Samples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var pathToAppSettings = @"c:\tmp\appsettings.json";

            var builder = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile(pathToAppSettings, true);
                })
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddHostedService<Worker>();
                })
                .UseServiceBaseLifetime()
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddSerilog(new LoggerConfiguration()
                                  .ReadFrom.Configuration(hostContext.Configuration)
                                  .CreateLogger());
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                });
            Log.Information("Dependencies built.");

            await builder.Build().RunAsync();
        }
    }
}
