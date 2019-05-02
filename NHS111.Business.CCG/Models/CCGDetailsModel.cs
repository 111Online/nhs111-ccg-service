namespace NHS111.Business.CCG.Models
{
    public class CCGDetailsModel :CCGModel
    {
        public string STPName { get; set; }

        public ServiceListModel ReferralServiceIdWhitelist { get; set; }

        public ServiceListModel PharmacyReferralServiceIdWhitelist { get; set; }

        public bool PharmacyServicesAvailable
        {
            get
            {
                return PharmacyReferralServiceIdWhitelist?.Count > 0;
            }
        }
    }
}
