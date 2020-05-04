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
        private readonly bool _enablePostcodePartitionKey;

        public CCGRepository(IAzureAccountSettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();

            tableClient.DefaultRequestOptions = new TableRequestOptions()
            {
                ServerTimeout = TimeSpan.FromSeconds(3),
                MaximumExecutionTime = TimeSpan.FromSeconds(3),
                RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(500), 3),
                LocationMode = settings.PreferSecondaryStorageEndpoint ? LocationMode.SecondaryThenPrimary : LocationMode.PrimaryThenSecondary // when this flag is set to true, the geo-replicated endpoint will be used for reads (only applies to RA-GRS storage accounts)
            };
            _table = tableClient.GetTableReference(settings.CCGTableReference);

            _enablePostcodePartitionKey = settings.EnablePostcodePartitionKey;
        }

        public async Task<CCGEntity> Get(string postcode)
        {
            var partitionKey = "Postcodes";
            if (_enablePostcodePartitionKey)
                partitionKey = postcode?.Length > 1 ? postcode.Substring(0, 2).Trim() : "emptypostcode";

            var retrieveOperation = TableOperation.Retrieve<CCGEntity>(partitionKey, postcode);

            var retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            return (CCGEntity)retrievedResult.Result;
        }
    }
}
