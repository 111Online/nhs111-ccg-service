using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

using CsvHelper;

using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;

using OfficeOpenXml;

namespace NHS111.DataImport.CCG
{
    public class Program
    {
        private static string _accountName;
        private static string _tableReference;
        private static string _ccgTableReference;
        private static string _accountKey;
        private static string _postcodeCsvFilePath;
        private static string _ccgCsvFilePath;
        private static string _nationalWhitelistFilePath;
        private static string _dosSearchDistanceFilePath;
        private static int _counter;
        private static int _recordCount;
        private static readonly Regex RegexRemoveWhitespace = new Regex(@"\s+");
        private static int _terminatedPostcodesCount;
        private static Dictionary<string, int> _dosSearchDistanceLookup = new Dictionary<string, int>();
        private static Dictionary<string, int> _dosSearchDistancePartialLookup = new Dictionary<string, int>();
        private static bool _onlyImportSTPData = false;
        private static Dictionary<string, PostcodeRecord> _ccgLookup = new Dictionary<string, PostcodeRecord>();
        
        public static void Main(string[] args)
        {
            Console.WriteLine("Beginning Data import");
            
            LoadSettings(args);

            var clock = new Stopwatch();
            
            clock.Start();

            var nationalWhiteList = new List<string>();

            if (!string.IsNullOrWhiteSpace(_nationalWhitelistFilePath))
            {
                nationalWhiteList = LoadNationalWhitelist(_nationalWhitelistFilePath);
            }

            LoadCCGLookupData(_ccgCsvFilePath, nationalWhiteList).Wait();

            LoadDOSSearchDistanceLookupdata(_dosSearchDistanceFilePath).Wait();

            if (!_onlyImportSTPData)
            {
                RunImport().Wait();
                clock.Stop();
                Console.WriteLine("finished importing " + _recordCount + " in " + TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).ToString(@"hh\:mm\:ss"));
            }

            clock.Stop();

            Console.WriteLine("finished importing stp data only in " + TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).ToString(@"hh\:mm\:ss"));
            Console.ReadLine();
        }

        public static List<string> LoadNationalWhitelist(string path)
        {
            var whitelist = new List<string>();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    var content = sr.ReadToEnd();

                    whitelist.AddRange(content.Split('|'));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Unable to load the national whitelist file at path: {0} Error {1}", path, e.Message));
            }

            return whitelist;
        }

        public static async Task LoadCCGLookupData(string csvFilePath, List<string> nationalWhitelist)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(_accountName, _accountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            var stpTable = tableClient.GetTableReference(_ccgTableReference);
            var csvLookup = new CsvReader(new StreamReader(csvFilePath));
            csvLookup.Read();
            csvLookup.ReadHeader();
            var batch = new TableBatchOperation();

            while (csvLookup.Read())
            {
                var pharmacyServiceIdWhitelist = csvLookup.GetField<string>("PharmacyReferralServiceIdWhitelist");

                if (!string.IsNullOrWhiteSpace(pharmacyServiceIdWhitelist))
                {
                    var whitelistCollection = pharmacyServiceIdWhitelist
                        .Split('|')
                        .ToList();

                    whitelistCollection.AddRange(nationalWhitelist);
                    pharmacyServiceIdWhitelist = string.Join('|', whitelistCollection);
                }

                batch.Add(
                    TableOperation.InsertOrReplace(
                        new STPEntity
                            {
                                PartitionKey = "CCGs",
                                RowKey = csvLookup.GetField<string>("CCG16CD"),
                                CCGId = csvLookup.GetField<string>("CCG16CD"),
                                STPId = csvLookup.GetField<string>("STP17CD"),
                                STPName = csvLookup.GetField<string>("STP17NM"),
                                CCGName = csvLookup.GetField<string>("CCG16NM"),
                                ProductName = csvLookup.GetField<string>("Product"),
                                LiveDate = csvLookup.GetField<DateTime?>("LiveDate", new DateTimeLocalConverter()),
                                PharmacyServiceIdWhitelist = pharmacyServiceIdWhitelist,
                                ReferralServiceIdWhitelist = csvLookup.GetField<string>("ReferralServiceIdWhitelist")
                            }));

                if (!_ccgLookup.ContainsKey(csvLookup.GetField<string>("CCG16CD")))
                {
                    _ccgLookup.Add(
                        csvLookup.GetField<string>("CCG16CD"),
                        new PostcodeRecord
                            {
                                AppName = csvLookup.GetField<string>("Product"),
                                StpName = csvLookup.GetField<string>("STP17NM"),
                                CcgName = csvLookup.GetField<string>("CCG16NM"),
                                CCGId = csvLookup.GetField<string>("CCG16CD")
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
        
        public static async Task LoadDOSSearchDistanceLookupdata(string xlsFilePath)
        {
            var package = new ExcelPackage(new FileInfo(xlsFilePath));
            
            Console.WriteLine("Loading DOS search distance Data");
            
            var fullPostcodeSheet = package.Workbook.Worksheets[2];
            
            for (var i = 1; i <= fullPostcodeSheet.Dimension.End.Row; i++)
            {
                _dosSearchDistanceLookup.Add(NormalisePostcode(fullPostcodeSheet.Cells[i, 1].Value.ToString()), Convert.ToInt32(fullPostcodeSheet.Cells[i, 2].Value));
            }

            var partialPostcodeSheet = package.Workbook.Worksheets[1];
            
            for (var i = 2; i <= partialPostcodeSheet.Dimension.End.Row; i++)
            {
                _dosSearchDistancePartialLookup.Add(RegexRemoveWhitespace.Replace(partialPostcodeSheet.Cells[i, 1].Value.ToString(), string.Empty), Convert.ToInt32(partialPostcodeSheet.Cells[i, 2].Value));
            }

            Console.WriteLine("Finished loading DOS search distance Data");
        }

        public static async Task  RunImport()
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(_accountName, _accountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_tableReference);
            var csv = new CsvReader(new StreamReader(_postcodeCsvFilePath));
            var batchSizeMax = 100;
            var batch = new TableBatchOperation();
            _terminatedPostcodesCount = 0;
            var i = 0;
            var noDosSearchDistanceCount = 0;
            csv.Read();
            csv.ReadHeader();
            _recordCount = File.ReadLines(_postcodeCsvFilePath).Count() -1;
            var tasks = new List<Task>();
            
            while (csv.Read())
            {
                csv.TryGetField<string>("doterm", out var terminatedDate);

                if (string.IsNullOrWhiteSpace(terminatedDate))
                {
                    var dosSearchDistance = string.Empty;

                    csv.TryGetField<string>("ccg", out var ccgId);
                    csv.TryGetField<string>("pcd", out var postcode);
                    csv.TryGetField<string>("pcds", out var formattedPostcode);

                    var partialPostcode = formattedPostcode
                        .Split(' ')
                        .First()
                        .Trim();

                    if (_dosSearchDistanceLookup.ContainsKey(NormalisePostcode(postcode)))
                    {
                        dosSearchDistance = _dosSearchDistanceLookup[NormalisePostcode(postcode)].ToString();
                    }
                    else if (_dosSearchDistancePartialLookup.ContainsKey(partialPostcode))
                    {
                        dosSearchDistance = _dosSearchDistancePartialLookup[partialPostcode].ToString();
                    }
                    else
                    {
                        noDosSearchDistanceCount++;
                    }
                  
                    batch.Add(TableOperation.InsertOrReplace(new CCGEntity()
                    {
                        CCG = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].CcgName : "",
                        CCGId = ccgId,
                        Postcode = postcode,
                        App = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].AppName : "",
                        PartitionKey = "Postcodes",
                        RowKey = NormalisePostcode(postcode),
                        DOSSearchDistance = dosSearchDistance
                    }));

                    i++;

                    if (i % batchSizeMax == 0)
                    {
                        var task = ImportBatch(table, batch);
                        tasks.Add(task);
                        batch = new TableBatchOperation();
                    }
                    
                    if (tasks.Count == 25) // limit async tasks to 10
                    {
                        await Task.WhenAll(tasks);
                        tasks = new List<Task>();
                    }
                }
                else _terminatedPostcodesCount++;
            }
            //run remaining records
            tasks.Add(ImportBatch(table, batch));
            await Task.WhenAll(tasks);
            Console.WriteLine("DOS Search distance not mapped count: " + noDosSearchDistanceCount);
        }

        public static async Task ImportBatch(CloudTable table, TableBatchOperation batch)
        {
            var importedCount = await table.ExecuteBatchAsync(batch);
            var newCount = _counter + importedCount.Count;
            _counter = newCount;
            Console.WriteLine("Imported " + _counter + " records ("+_terminatedPostcodesCount +" terminated) of " + _recordCount +  " (" + CalculatePercentDone() + "%)");
        }

        public static string CalculatePercentDone()
        {
            return ((_counter + (decimal) _terminatedPostcodesCount) / _recordCount * 100m).ToString("0.00");
        }

        private static string NormalisePostcode(string postcode)
        {
            return RegexRemoveWhitespace.Replace(postcode, string.Empty).ToUpper();
        }

        public static void LoadSettings(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-AccountName="))
                {
                    _accountName = args[i].Replace("-AccountName=", "");
                }

                if (args[i].StartsWith("-TableRef="))
                {
                    _tableReference = args[i].Replace("-TableRef=","");
                }

                if (args[i].StartsWith("-ccgTableRef="))
                {
                    _ccgTableReference = args[i].Replace("-ccgTableRef=", "");
                }

                if (args[i].StartsWith("-ccgCsvFilePath="))
                {
                    _ccgCsvFilePath = args[i].Replace("-ccgCsvFilePath=", "");
                }

                if (args[i].StartsWith("-AccountKey="))
                {
                    _accountKey = args[i].Replace("-AccountKey=","");
                }
                
                if (args[i].StartsWith("-CSVFilePath="))
                {
                    _postcodeCsvFilePath = args[i].Replace("-CSVFilePath=","");
                }

                if (args[i].StartsWith("-whitelistFilePath="))
                {
                    _nationalWhitelistFilePath = args[i].Replace("-whitelistFilePath=","");
                }

                if (args[i].StartsWith("-DosSaerchDistanceFilePath"))
                {
                    _dosSearchDistanceFilePath = args[i].Replace("-DosSaerchDistanceFilePath=", "");
                }

                if (args[i].StartsWith("-STPDataOnly"))
                {
                    _onlyImportSTPData = true;
                }
            }
        }
    }
}
