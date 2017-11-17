using System;
using System.Collections.Generic;
using System.Text;

namespace NHS111.Business.CCG.Models
{
    public class CCGDetailsModel :CCGModel
    {
        public string STPName { get; set; }

        public ServiceListModel ServiceIdWhitelist { get; set; }
    }
}
