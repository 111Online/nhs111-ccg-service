using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public ServiceIdWhitelist ServiceIdWhitelist { get; set; }
    }

    public class ServiceIdWhitelist : List<string>
    {
        public ServiceIdWhitelist()
        {
        }
        public ServiceIdWhitelist(string serviceidList) : base()
        {
            this.AddRange(serviceidList.Split('|').ToList());
        }
        public override string ToString()
        {
            return String.Join('|', this);
        }
    }
}
