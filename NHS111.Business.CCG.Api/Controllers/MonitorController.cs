using Microsoft.AspNetCore.Mvc;
using NHS111.Business.CCG.Api.Services;
using System.Threading.Tasks;

namespace NHS111.Business.CCG.Api.Controllers
{

    public class MonitorController : ControllerBase

    {
        private readonly IMonitorService _monitor;

        public MonitorController(IMonitorService monitor)
        {
            _monitor = monitor;
        }

        [HttpGet]
        [Route("api/Monitor/{service}")]
        public async Task<string> MonitorPing(string service)
        {
            switch (service.ToLower())
            {
                case "ping":
                    return _monitor.Ping();

                case "metrics":
                    return _monitor.Metrics();

                case "health":
                    return (await _monitor.Health()).ToString();

                case "version":
                    return _monitor.Version();
            }

            return null;
        }
    }
}


