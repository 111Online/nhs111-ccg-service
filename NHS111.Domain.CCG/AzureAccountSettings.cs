namespace NHS111.Domain.CCG
{
    public class AzureAccountSettings : IAzureAccountSettings
    {
        public AzureAccountSettings(string connectionString, string ccgTableReference, string stpTableReference, string nationalWhitelistBlobName, string enablePostcodePartitionKey)
        {
            ConnectionString = connectionString;
            CCGTableReference = ccgTableReference;
            STPTableReference = stpTableReference;
            NationalWhitelistBlobName = nationalWhitelistBlobName;
            EnablePostcodePartitionKey = bool.TryParse(enablePostcodePartitionKey, out var _enablePostcodePartitionKey) ? _enablePostcodePartitionKey : false;
        }
        public string ConnectionString { get; set; }

        public string CCGTableReference { get; set; }

        public string STPTableReference { get; set; }

        public string NationalWhitelistBlobName { get; set; }

        public bool EnablePostcodePartitionKey { get; set; }
    }
}