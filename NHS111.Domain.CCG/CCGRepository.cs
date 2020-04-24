using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;
using System;
using System.Threading.Tasks;

namespace NHS111.Domain.CCG
{
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
        }

        private TableRequestOptions requestOptions = new TableRequestOptions()
        {
            ServerTimeout = TimeSpan.FromSeconds(5),
            MaximumExecutionTime = TimeSpan.FromSeconds(5),
            RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(500), 3)
        };

        public async Task<CCGEntity> Get(string postcode)
        {
            var partitionKey = postcode?.Length > 1 ? postcode.Substring(0, 2).Trim() : "emptypostcode";
            var retrieveOperation = TableOperation.Retrieve<CCGEntity>(partitionKey, postcode);

            var retrievedResult = await _table.ExecuteAsync(retrieveOperation, requestOptions, null);

            return (CCGEntity)retrievedResult.Result;
        }
    }
}
