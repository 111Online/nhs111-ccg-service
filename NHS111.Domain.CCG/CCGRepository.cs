﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;
using Polly.Retry;
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

        private readonly AsyncRetryPolicy retryIfException = PolicyFactory.IfException();

        public CCGRepository(IAzureAccountSettings settings)
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
            _table = tableClient.GetTableReference(settings.CCGTableReference);
        }

        public async Task<CCGEntity> Get(string postcode)
        {
            var partitionKey = postcode?.Length > 1 ? postcode.Substring(0, 2).Trim() : "emptypostcode";

            var retrieveOperation = TableOperation.Retrieve<CCGEntity>(partitionKey, postcode);

            // Use Polly to retry to catch transient Storage errors
            var res = await retryIfException.ExecuteAndCaptureAsync(() => _table.ExecuteAsync(retrieveOperation));
            var retrievedResult = res.Result;

            return (CCGEntity)retrievedResult.Result;
        }
    }
}
