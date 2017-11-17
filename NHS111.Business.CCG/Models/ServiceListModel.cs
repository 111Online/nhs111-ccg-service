using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHS111.Business.CCG.Models
{

    public class ServiceListModel : List<string>
    {
        public ServiceListModel()
        {
        }
        public ServiceListModel(string serviceidList) : base()
        {
            this.AddRange(serviceidList.Split('|').ToList());
        }
        public override string ToString()
        {
            return String.Join('|', this);
        }
    }
}
