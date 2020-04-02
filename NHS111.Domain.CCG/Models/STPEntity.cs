using Microsoft.WindowsAzure.Storage.Table;
using System;

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

        public string PharmacyServiceIdWhitelist { get; set; }

        public string ReferralServiceIdWhitelist { get; set; }
    }
}
