using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;

namespace NHS111.Domain.CCG
{
    public class AzureAccountSettings : IAzureAccountSettings
    {
        public AzureAccountSettings(string connectionString, string ccgTableReference, string stpTableReference, string nationalWhitelistBlobName, string locationMode = "PrimaryThenSecondary")
        {
            ConnectionString = connectionString;
            LocationMode = Enum.TryParse(locationMode, out LocationMode _locationMode) ? _locationMode : LocationMode.PrimaryThenSecondary;
            CCGTableReference = ccgTableReference;
            STPTableReference = stpTableReference;
            NationalWhitelistBlobName = nationalWhitelistBlobName;
        }
        public string ConnectionString { get; set; }
        public LocationMode LocationMode { get; set; }

        public string CCGTableReference { get; set; }

        public string STPTableReference { get; set; }

        public string NationalWhitelistBlobName { get; set; }
    }
}