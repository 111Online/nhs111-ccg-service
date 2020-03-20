using System.Collections.Generic;

namespace NHS111.Business.CCG.Models
{
    public class ServiceListModel : List<string>
    {
        public ServiceListModel(string serviceIdList)
        {
            if (!string.IsNullOrWhiteSpace(serviceIdList))
            {
                AppendServices(serviceIdList);
            }
        }

        public void AppendServices(string services)
        {
            if (!string.IsNullOrWhiteSpace(services))
            {
                AddRange(services.Split('|'));
            }
        }

        public override string ToString()
        {
            return string.Join('|', this);
        }
    }
}
