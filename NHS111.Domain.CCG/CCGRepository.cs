
namespace NHS111.Domain.CCG {
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Models;

    public interface ICCGRepository {
        Task<CCGEntity> Get(string postcode);
    }

    public class CCGRepository
        : ICCGRepository {

        private readonly CloudTable _table;

        public CCGRepository(AzureAccountSettings settings) {

            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(settings.CCGTableReference);
        }

        public async Task<CCGEntity> Get(string postcode) {
            await _table.CreateIfNotExistsAsync();

            var retrieveOperation = TableOperation.Retrieve<CCGEntity>("Postcodes", postcode);

            var retrievedResult = await _table.ExecuteAsync(retrieveOperation);
            return (CCGEntity)retrievedResult.Result;
        }
    }
}