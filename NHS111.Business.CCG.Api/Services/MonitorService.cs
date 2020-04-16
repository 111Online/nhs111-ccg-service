using System.Reflection;

namespace NHS111.Business.CCG.Api.Services
{
    public class MonitorService : IMonitorService
    {
        public string Ping()
        {
            return "pong";
        }

        public string Metrics()
        {
            return "Metrics";
        }

        public bool Health()
        {
            return true;
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
        bool Health();
        string Version();
    }
}
