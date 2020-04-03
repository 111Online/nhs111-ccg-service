namespace NHS111.Domain.CCG
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Models;
    using System.Threading.Tasks;

    public interface ICCGRepository
    {
        Task<CCGEntity> Get(string postcode);
    }
     
    public class CCGRepository : ICCGRepository
    {
        private readonly CloudTable _table;

        public CCGRepository(IAzureAccountSettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();

            _table = tableClient.GetTableReference(settings.CCGTableReference);

            _table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public async Task<CCGEntity> Get(string postcode)
        {
            var partitionKey = postcode?.Length > 1 ? postcode.Substring(0, 2).Trim() : "emptypostcode";
            var retrieveOperation = TableOperation.Retrieve<CCGEntity>(partitionKey, postcode);

            var retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            return (CCGEntity)retrievedResult.Result;
        }
    }
}