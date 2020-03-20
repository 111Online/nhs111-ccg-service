namespace NHS111.DataImport.CCG
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using CsvHelper;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Table;

    using NHS111.Domain.CCG.Models;

    using OfficeOpenXml;

    public class DataImport
    {
        public DataImport()
        {
            _arguments = new Dictionary<string, string>();

            _ccgLookup = new Dictionary<string, PostcodeRecord>();
            
            _dosSearchDistanceLookup = new Dictionary<string, int>();

            _dosSearchDistancePartialLookup = new Dictionary<string, int>();
        }

        public void PerformImport(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning Data import");

                LoadSettings(args);

                var clock = new Stopwatch();

                clock.Start();

                Task.Run(UploadNationalWhitelist).GetAwaiter().GetResult();

                Task.Run(LoadCCGLookupData).GetAwaiter().GetResult();
                
                LoadDOSSearchDistanceLookupData();

                clock.Stop();

                var elapsed = TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).ToString(@"hh\:mm\:ss");

                if (!string.IsNullOrWhiteSpace(GetSetting("STPDataOnly")))
                {
                    Console.WriteLine("finished importing stp data only in {0}", elapsed);
                }
                else
                {
                    RunImport();

                    Console.WriteLine("finished importing {0} in {1}", _recordCount, elapsed);
                }

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Import failed: {0}", e.Message);

                throw new Exception("", e);
            }
        }
        
        private async Task UploadNationalWhitelist()
        {
            try
            {
                var filePath = GetSetting("whitelistFilePath");
                
                var content = File.ReadAllText(filePath);
                
                var blobName = filePath.Substring(filePath.LastIndexOf(@"\", StringComparison.Ordinal) +1).Replace(".csv", "");
                
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    var blob = GetBlob(blobName);

                    await blob.Result.UploadTextAsync(content);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to load the national whitelist file: {0}", e.Message);

                throw new Exception("", e);
            }
        }

        public async Task LoadCCGLookupData()
        {
            try
            {
                var tableReference = GetSetting("ccgTableRef");

                var stpTable = GetTable(tableReference);

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
                            batch.Add(
                                TableOperation.InsertOrReplace(
                                    new STPEntity
                                    {
                                        PartitionKey = "CCGs",
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
                Console.WriteLine("Unable to Load the CCG Lookup Data: {0}", e.Message);

                throw new Exception("", e);
            }
        }

        public void LoadDOSSearchDistanceLookupData()
        {
            try
            {
                var filePath = GetSetting("DosSaerchDistanceFilePath");

                var package = new ExcelPackage(new FileInfo(filePath));

                Console.WriteLine("Loading DOS search distance Data");

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
                Console.WriteLine("Unable to load the DOS Search Distance Lookup Data: {0}", e.Message);

                throw new Exception("", e);
            }
        }

        public async void RunImport()
        {
            try
            {
                const int BatchSizeMax = 100;

                var tableReference = GetSetting("TableRef");
                var table = GetTable(tableReference);

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

                                batch.Add(TableOperation.InsertOrReplace(new CCGEntity
                                {
                                    CCG = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].CcgName : "",
                                    CCGId = ccgId,
                                    Postcode = postcode,
                                    App = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].AppName : "",
                                    PartitionKey = "Postcodes",
                                    RowKey = RemoveWhitespace(postcode),
                                    DOSSearchDistance = dosSearchDistance
                                }));

                                elementCount++;

                                if (elementCount % BatchSizeMax == 0)
                                {
                                    var task = ImportBatch(table, batch);
                                    tasks.Add(task);
                                    batch = new TableBatchOperation();
                                }

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
                
                //run remaining records
                tasks.Add(ImportBatch(table, batch));
                await Task.WhenAll(tasks);

                Console.WriteLine("DOS Search distance not mapped count: " + noDosSearchDistanceCount);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to complete the import: {0}", e.Message);

                throw new Exception("", e);
            }
        }

        public async Task ImportBatch(CloudTable table, TableBatchOperation batch)
        {
            try
            {
                var importedCount = await table.ExecuteBatchAsync(batch);

                var newCount = _counter + importedCount.Count;

                _counter = newCount;

                var percentDone = CalculatePercentDone();
                
                Console.WriteLine("Imported {0} records ({1} terminated) of {2} ({3}%)", _counter, _terminatedPostcodesCount, _recordCount, percentDone);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to import batch: {0}", e.Message);

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
                Console.WriteLine("Load settings error: {0}", e.Message);

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
                Console.WriteLine("Unable to retrieve setting {0}", key);

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
                Console.WriteLine("Unable to normalise postcode: {0}", e.Message);

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
                Console.WriteLine("Unable to calculate percent done: {0}", e.Message);

                throw new Exception("", e);
            }
        }
        
        private CloudTable GetTable(string name)
        {
            try
            {
                var storageAccount = GetStorageAccount();

                var tableClient = storageAccount.CreateCloudTableClient();

                return tableClient.GetTableReference(name);
            }
            catch (Exception e)
            {
                Console.WriteLine("GetTable({0}) failed: {1}", name, e.Message);

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
                Console.WriteLine("GetBlob({0}) failed: {1}", name, e.Message);

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
                Console.WriteLine("GetStorageAccount failed: {0}", e.Message);

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
