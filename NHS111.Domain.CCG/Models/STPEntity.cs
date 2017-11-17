using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Table;

namespace NHS111.Domain.CCG.Models
{
    public class STPEntity : TableEntity
    {
        public string STPId { get; set; }
        public string STPName { get; set; }
        public string CCGName { get; set; }
        public string CCGId { get; set; }
        public string ProductName { get; set; }
        public DateTime? LiveDate { get; set; }
        public string ServiceIdWhitelist { get; set; }
        public string ITKServiceIdWhitelist { get; set; }
    }

}
