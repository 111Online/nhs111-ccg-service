using Microsoft.Extensions.Configuration;

namespace NHS111.Business.CCG.Functional.Tests.Tools
{
    public class ConfigurationHelper
    {
        public static IConfigurationRoot GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("C:\\Configurations\\nhs111-shared-resources\\appsettings.debug.json", optional: true)
                .Build();
        }
    }
}
