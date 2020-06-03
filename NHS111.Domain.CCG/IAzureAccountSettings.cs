namespace NHS111.Domain.CCG
{
    public interface IAzureAccountSettings
    {
        string ConnectionString { get; set; }
        bool PreferSecondaryStorageEndpoint { get; set; }

        string CCGTableReference { get; set; }

        string STPTableReference { get; set; }

        string NationalWhitelistBlobName { get; set; }
    }
}