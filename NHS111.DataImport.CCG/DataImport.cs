namespace NHS111.DataImport.CCG
{
    using CsvHelper;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Table;
    using NHS111.Domain.CCG.Models;
    using OfficeOpenXml;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class DataImport
    {
        public DataImport()
        {
            _arguments = new Dictionary<string, string>();

            _ccgLookup = new Dictionary<string, PostcodeRecord>();

            _dosSearchDistanceLookup = new Dictionary<string, int>();

            _dosSearchDistancePartialLookup = new Dictionary<string, int>();
        }

        public async Task PerformImportAsync(string[] args)
        {
            try
            {
                LoadSettings(args);

                Console.WriteLine($"Beginning Data import to storage account {GetSetting("AccountName")}");

                Console.WriteLine("Uploading National Whitelist");
                await UploadNationalWhitelist();
                Console.WriteLine("Finished uploading National Whitelist");

                Console.WriteLine("Beginning CCG Lookup Data loading");
                var clock = new Stopwatch();
                clock.Start();

                await LoadCCGLookupData();

                clock.Stop();
                Console.WriteLine("Finished importing CCG Lookup Data in " + clock.Elapsed.ToString(@"hh\:mm\:ss"));

                LoadDOSSearchDistanceLookupData();

                if (string.IsNullOrWhiteSpace(GetSetting("STPDataOnly")))
                {
                    Console.WriteLine("Beginning CCG loading");
                    clock.Restart();
                    await RunImportAsync();
                    clock.Stop();
                    Console.WriteLine("Finished importing CCG in " + clock.Elapsed.ToString(@"hh\:mm\:ss"));
                }
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(PerformImportAsync)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        private async Task UploadNationalWhitelist()
        {
            try
            {
                var filePath = GetSetting("whitelistFilePath");
                var blobName = Path.GetFileName(filePath);
                Console.WriteLine($"Using National Whitelist file name={blobName}");

                var blob = await GetBlob(blobName);
                await blob.UploadFromFileAsync(filePath);
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(UploadNationalWhitelist)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        public async Task LoadCCGLookupData()
        {
            try
            {
                var tableReference = GetSetting("stpTableRef");

                var stpTable = await GetTable(tableReference);

                var filePath = GetSetting("ccgCsvFilePath");

                using (var sr = new StreamReader(filePath))
                {
                    using (var reader = new CsvReader(sr))
                    {
                        reader.Read();
                        reader.ReadHeader();

                        var batch = new TableBatchOperation();

                        while (reader.Read())
                        {
                            batch.Add(TableOperation.InsertOrReplace(
                                    new STPEntity
                                    {
                                        PartitionKey = "true".Equals(GetSetting("EnablePostcodePartitionKey")) ? "CCG" : "CCGs",
                                        RowKey = reader.GetField<string>("CCG16CD"),
                                        CCGId = reader.GetField<string>("CCG16CD"),
                                        STPId = reader.GetField<string>("STP17CD"),
                                        STPName = reader.GetField<string>("STP17NM"),
                                        CCGName = reader.GetField<string>("CCG16NM"),
                                        ProductName = reader.GetField<string>("Product"),
                                        LiveDate = reader.GetField<DateTime?>("LiveDate", new DateTimeLocalConverter()),
                                        PharmacyServiceIdWhitelist = reader.GetField<string>("PharmacyReferralServiceIdWhitelist"),
                                        ReferralServiceIdWhitelist = reader.GetField<string>("ReferralServiceIdWhitelist")
                                    }));

                            if (!_ccgLookup.ContainsKey(reader.GetField<string>("CCG16CD")))
                            {
                                _ccgLookup.Add(
                                    reader.GetField<string>("CCG16CD"),
                                    new PostcodeRecord
                                    {
                                        AppName = reader.GetField<string>("Product"),
                                        StpName = reader.GetField<string>("STP17NM"),
                                        CcgName = reader.GetField<string>("CCG16NM"),
                                        CCGId = reader.GetField<string>("CCG16CD")
                                    });
                            }

                            if (batch.Count % 100 == 0)
                            {
                                await stpTable.ExecuteBatchAsync(batch);
                                batch = new TableBatchOperation();
                            }
                        }

                        if (batch.Count > 0)
                        {
                            await stpTable.ExecuteBatchAsync(batch);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(LoadCCGLookupData)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        public void LoadDOSSearchDistanceLookupData()
        {
            try
            {
                var filePath = GetSetting("DosSearchDistanceFilePath");

                var package = new ExcelPackage(new FileInfo(filePath));
                Console.WriteLine($"Loading DOS search distance Data from file {filePath}");

                var fullPostcodeSheet = package.Workbook.Worksheets[2];

                for (var i = 1; i <= fullPostcodeSheet.Dimension.End.Row; i++)
                {
                    _dosSearchDistanceLookup.Add(RemoveWhitespace(fullPostcodeSheet.Cells[i, 1].Value.ToString()), Convert.ToInt32(fullPostcodeSheet.Cells[i, 2].Value));
                }

                var partialPostcodeSheet = package.Workbook.Worksheets[1];

                for (var i = 2; i <= partialPostcodeSheet.Dimension.End.Row; i++)
                {
                    _dosSearchDistancePartialLookup.Add(RemoveWhitespace(partialPostcodeSheet.Cells[i, 1].Value.ToString()), Convert.ToInt32(partialPostcodeSheet.Cells[i, 2].Value));
                }

                Console.WriteLine("Finished loading DOS search distance Data");
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(LoadDOSSearchDistanceLookupData)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        public async Task RunImportAsync()
        {
            try
            {
                const int BatchSizeMax = 100;

                var tableReference = GetSetting("ccgTableRef");
                var ccgTable = await GetTable(tableReference);

                var filePath = GetSetting("CSVFilePath");
                _recordCount = File.ReadLines(filePath).Count() - 1;

                var batch = new TableBatchOperation();
                var tasks = new List<Task>();

                _terminatedPostcodesCount = 0;
                var noDosSearchDistanceCount = 0;

                using (var sr = new StreamReader(filePath))
                {
                    using (var reader = new CsvReader(sr))
                    {
                        reader.Read();
                        reader.ReadHeader();

                        var elementCount = 0;

                        string lastPartitionKey = "";

                        while (reader.Read())
                        {
                            reader.TryGetField<string>("doterm", out var terminatedDate);

                            if (string.IsNullOrWhiteSpace(terminatedDate))
                            {
                                var dosSearchDistance = string.Empty;

                                reader.TryGetField<string>("ccg", out var ccgId);
                                reader.TryGetField<string>("pcd", out var postcode);
                                reader.TryGetField<string>("pcds", out var formattedPostcode);

                                var partialPostcode = formattedPostcode
                                    .Split(' ')
                                    .First()
                                    .Trim();

                                if (_dosSearchDistanceLookup.ContainsKey(RemoveWhitespace(postcode)))
                                {
                                    dosSearchDistance = _dosSearchDistanceLookup[RemoveWhitespace(postcode)].ToString();
                                }
                                else if (_dosSearchDistancePartialLookup.ContainsKey(partialPostcode))
                                {
                                    dosSearchDistance = _dosSearchDistancePartialLookup[partialPostcode].ToString();
                                }
                                else
                                {
                                    noDosSearchDistanceCount++;
                                }

                                var partitionKey = "Postcodes";
                                if ("true".Equals(GetSetting("EnablePostcodePartitionKey")))
                                    partitionKey = postcode?.Length > 1 ? postcode.Substring(0, 2).Trim() : "emptypostcode";

                                // In each batch we can only have one partition key. Hence, when we go to the next key, we need so send and empty the batch first
                                if (elementCount > 0 && (partitionKey != lastPartitionKey || elementCount % BatchSizeMax == 0))
                                {
                                    var task = ImportBatch(ccgTable, batch);
                                    tasks.Add(task);
                                    batch = new TableBatchOperation();
                                }

                                lastPartitionKey = partitionKey;

                                batch.Add(TableOperation.InsertOrReplace(new CCGEntity
                                {
                                    CCG = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].CcgName : "",
                                    CCGId = ccgId,
                                    Postcode = postcode,
                                    App = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].AppName : "",
                                    PartitionKey = partitionKey, //"Postcodes",
                                    RowKey = RemoveWhitespace(postcode),
                                    DOSSearchDistance = dosSearchDistance
                                }));

                                elementCount++;

                                if (tasks.Count == 25)
                                {
                                    await Task.WhenAll(tasks);
                                    tasks = new List<Task>();
                                }
                            }
                            else
                            {
                                _terminatedPostcodesCount++;
                            }
                        }
                    }
                }

                tasks.Add(ImportBatch(ccgTable, batch));
                await Task.WhenAll(tasks);
                Console.WriteLine("DOS Search distance not mapped count: " + noDosSearchDistanceCount);
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(RunImportAsync)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        public async Task ImportBatch(CloudTable table, TableBatchOperation batch)
        {
            try
            {
                var requestOptions = new TableRequestOptions();
                requestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(500), 3);
                requestOptions.MaximumExecutionTime = TimeSpan.FromMinutes(1);
                requestOptions.ServerTimeout = TimeSpan.FromMinutes(1);

                var context = new OperationContext();
                context.ClientRequestID = "ccg-data-importer";
                context.Retrying += (sender, args) =>
                {
                    Console.WriteLine($"WARN: Retrying batchimport. Error code={args?.RequestInformation?.ErrorCode}");
                };

                var importedResult = await table.ExecuteBatchAsync(batch, requestOptions, context);

                var newCount = _counter + importedResult.Count;

                _counter = newCount;
                Console.WriteLine("Imported " + _counter + " records (" + _terminatedPostcodesCount + " terminated) of " + _recordCount + " (" + CalculatePercentDone() + "%)");
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(ImportBatch)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        private void LoadSettings(string[] args)
        {
            try
            {
                foreach (var t in args)
                {
                    var key = t
                        .Substring(0, t.IndexOf('='))
                        .Replace("-", string.Empty);

                    var value = t.Substring(t.IndexOf('=') + 1);

                    _arguments.Add(key, value);
                }
            }
            catch (Exception e)
            {
                // TODO: Add application logging
                Console.WriteLine($"Exception in {nameof(LoadSettings)}: {e.Message}");
                throw new Exception("", e);
            }
        }

        private string GetSetting(string key)
        {
            try
            {
                var argument = _arguments.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.InvariantCultureIgnoreCase));

                return argument.Value ?? string.Empty;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        private string RemoveWhitespace(string value)
        {
            try
            {
                var regex = new Regex(WhitespacePattern);

                var noWhitespace = regex
                    .Replace(value, string.Empty)
                    .ToUpper();

                return noWhitespace;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        public string CalculatePercentDone()
        {
            try
            {
                return ((_counter + (decimal)_terminatedPostcodesCount) / _recordCount * 100m).ToString("0.00");
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        private async Task<CloudTable> GetTable(string name)
        {
            try
            {
                var storageAccount = GetStorageAccount();

                var tableClient = storageAccount.CreateCloudTableClient();

                var tableRef = tableClient.GetTableReference(name);
                await tableRef.CreateIfNotExistsAsync();
                return tableRef;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        private async Task<CloudBlockBlob> GetBlob(string name)
        {
            try
            {
                var storageAccount = GetStorageAccount();

                var client = storageAccount.CreateCloudBlobClient();

                var container = client.GetContainerReference(BlobContainerName);

                await container.CreateIfNotExistsAsync();

                var blob = container.GetBlockBlobReference(name);

                return blob;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        private CloudStorageAccount GetStorageAccount()
        {
            try
            {
                if (_storageAccount == null)
                {
                    var accountName = GetSetting("AccountName");

                    var accountKey = GetSetting("AccountKey");

                    var credentials = new StorageCredentials(accountName, accountKey);

                    _storageAccount = new CloudStorageAccount(credentials, true);
                }

                return _storageAccount;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        private const string WhitespacePattern = @"\s+";

        private const string BlobContainerName = "epwhitelist";

        private int _recordCount;

        private int _counter;

        private int _terminatedPostcodesCount;

        private Dictionary<string, string> _arguments;

        private static Dictionary<string, PostcodeRecord> _ccgLookup;

        private CloudStorageAccount _storageAccount;

        private Dictionary<string, int> _dosSearchDistanceLookup;

        private Dictionary<string, int> _dosSearchDistancePartialLookup;
    }
}
