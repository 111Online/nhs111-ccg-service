using NHS111.Business.CCG.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NHS111.Business.CCG.Api.Services
{
    public class MonitorService : IMonitorService
    {
        private readonly ICCGService _ccgService;

        public MonitorService(ICCGService service)
        {
            _ccgService = service;
        }

        public string Ping()
        {
            return "pong";
        }

        public string Metrics()
        {
            return "Metrics";
        }

        private string[] postCodes = new[] { "B151NQ", "AL108XU", "BA228RZ", "UB81PG" }; // just a random list of post codes to choose from
        private Random r = new Random();

        public async Task<bool> Health()
        {
            try
            {
                // Try to fetch a random postcode from CCG service to test if Table storage works fine
                var result = await _ccgService.GetCCGDetails(postCodes[r.Next(postCodes.Length)]);
                return result != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Version()
        {
            return Assembly.GetCallingAssembly().GetName().Version.ToString();
        }
    }

    public interface IMonitorService
    {
        string Ping();
        string Metrics();
        Task<bool> Health();
        string Version();
    }
}
