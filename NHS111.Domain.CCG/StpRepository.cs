using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;

namespace NHS111.Domain.CCG
{

    public interface ISTPRepository
    {
        Task<STPEntity> Get(string ccgId);
        Task<List<STPEntity>> List();
    }

    public class STPRepository : ISTPRepository
    {
        private readonly CloudTable _table;

        public STPRepository(AzureAccountSettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(settings.STPTableReference);
        }

        public async Task<STPEntity> Get(string ccgId)
        {
            await _table.CreateIfNotExistsAsync();

            TableQuery<STPEntity> query = new TableQuery<STPEntity>().Where("CCGId eq '" + ccgId +"'");

            var retrievedResult = await _table.ExecuteQuerySegmentedAsync(query, null);
            return (STPEntity)retrievedResult.Results.First();
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
