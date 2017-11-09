
namespace NHS111.Domain.CCG {
    public class AzureAccountSettings {
        public AzureAccountSettings(string connectionString, string tableReference) {
            ConnectionString = connectionString;
            TableReference = tableReference;
        }
        public string ConnectionString { get; set; }
        public string TableReference { get; set; }
    }
}