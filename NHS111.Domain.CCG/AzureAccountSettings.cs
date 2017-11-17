
namespace NHS111.Domain.CCG {
    public class AzureAccountSettings {
        public AzureAccountSettings(string connectionString, string ccgTableReference, string stpTableReference) {
            ConnectionString = connectionString;
            CCGTableReference = ccgTableReference;
            STPTableReference = stpTableReference;
        }
        public string ConnectionString { get; set; }
        public string CCGTableReference { get; set; }
        public string STPTableReference { get; set; }
    }
}