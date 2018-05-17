
using Microsoft.Extensions.Configuration;

namespace NHS111.Business.CCG.Api {
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    public class Program {
        public static void Main(string[] args) {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseStartup<Startup>().ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json")
                        .AddJsonFile("C:\\Configurations\\nhs111-shared-resources\\appsettings.debug.json", optional: true);
                }).Build();

    }
}