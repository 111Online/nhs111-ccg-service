using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NHS111.Domain.CCG.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NHS111.Domain.CCG
{
    public class STPRepository : ISTPRepository
    {
        private readonly CloudTable _table;
        private List<STPEntity> allEntities = null;


        public STPRepository(IAzureAccountSettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(settings.STPTableReference);
        }

        private async Task<List<STPEntity>> LoadEntitiesIntoMemory()
        {
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

        public async Task<STPEntity> Get(string ccgId)
        {
            if(allEntities == null)
            {
                // Load all STP entities into memory on first call
                allEntities = await LoadEntitiesIntoMemory();
            }

            var res = allEntities.FirstOrDefault(e => e.CCGId == ccgId);
            if (res != null)
            {
                // Returning a clone to prevent increasing memory consumption.
                return new STPEntity()
                {
                    CCGId = res.CCGId,
                    CCGName = res.CCGName,
                    LiveDate = res.LiveDate,
                    PharmacyServiceIdWhitelist = res.PharmacyServiceIdWhitelist,
                    ProductName = res.ProductName,
                    ReferralServiceIdWhitelist = res.ReferralServiceIdWhitelist,
                    STPId = res.STPId,
                    STPName = res.STPName,
                    Timestamp = res.Timestamp
                };
            }

            return null;
        }

        public async Task<List<STPEntity>> List()
        {
            if (allEntities == null)
            {
                // Load all STP entities into memory on first call
                allEntities = await LoadEntitiesIntoMemory();
            }
            return allEntities;
        }
    }
}
