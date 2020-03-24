using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;

namespace NHS111.Domain.CCG
{
    public class STPRepository : ISTPRepository
    {
        private readonly CloudTable _table;

        public STPRepository(IAzureAccountSettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(settings.STPTableReference);
        }

        public async Task<STPEntity> Get(string ccgId)
        {
            await _table.CreateIfNotExistsAsync();

            var operation = TableOperation.Retrieve<STPEntity>("CCGs", ccgId);

            var result = await  _table.ExecuteAsync(operation);

            return result.Result as STPEntity;
        }

        public async Task<List<STPEntity>> List()
        {
            await _table.CreateIfNotExistsAsync();

            TableContinuationToken token = null;

            var entities = new List<STPEntity>();
            do
            {
                var queryResult = await _table.ExecuteQuerySegmentedAsync(new TableQuery<STPEntity>(), token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entities;
        }
    }
}
