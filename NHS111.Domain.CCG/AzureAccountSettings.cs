namespace NHS111.Domain.CCG
{
    public class AzureAccountSettings : IAzureAccountSettings
    {
        public AzureAccountSettings(string connectionString, string ccgTableReference, string stpTableReference, string nationalWhitelistBlobName)
        {
            ConnectionString = connectionString;
            CCGTableReference = ccgTableReference;
            STPTableReference = stpTableReference;
            NationalWhitelistBlobName = nationalWhitelistBlobName;
        }
        public string ConnectionString { get; set; }

        public string CCGTableReference { get; set; }

        public string STPTableReference { get; set; }

        public string NationalWhitelistBlobName { get; set; }
    }
}