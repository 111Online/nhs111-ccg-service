﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NHS111.Domain.CCG
{
    public class STPRepository : ISTPRepository
    {
        private readonly CloudTable _table;
        private const string _partitionKey = "CCG";
        private List<STPEntity> allEntities = null;

        private readonly AsyncRetryPolicy retryIfException = PolicyFactory.IfException();

        public STPRepository(IAzureAccountSettings settings)
        {
            var storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            tableClient.DefaultRequestOptions = new TableRequestOptions()
            {
                ServerTimeout = TimeSpan.FromMilliseconds(200),
                MaximumExecutionTime = TimeSpan.FromSeconds(3),
                RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(500), 3),
                LocationMode = settings.LocationMode
            };
            _table = tableClient.GetTableReference(settings.STPTableReference);
        }

        private async Task<List<STPEntity>> LoadEntitiesIntoMemory()
        {
            TableContinuationToken token = null;

            var entities = new List<STPEntity>();
            do
            {
                // Use Polly to retry to catch transient Storage errors
                TableQuery<STPEntity> partitionQuery = new TableQuery<STPEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _partitionKey));
                var res = await retryIfException.ExecuteAndCaptureAsync(() => _table.ExecuteQuerySegmentedAsync(partitionQuery, token));
                var queryResult = res.Result;
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entities;
        }

        public async Task<STPEntity> Get(string ccgId)
        {
            if (allEntities == null)
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
