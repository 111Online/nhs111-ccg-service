using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using NHS111.Domain.CCG;
using CsvHelper;
using CsvHelper.TypeConversion;
using Microsoft.WindowsAzure.Storage.Table;
using NHS111.Domain.CCG.Models;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;

namespace NHS111.DataImport.CCG
{
    class Program
    {
        private static string _accountname;
        private static string _tableReference;
        private static string _ccgtableReference;
        private static string _accountKey;
        private static string _postcodeCsvFilePath;
        private static string _ccgCsvFilePath;
        private static int _counter;
        private static int _recordCount;
        private static int _terminatedPostcodesCount;
        private static Dictionary<string, PostcodeRecord> _ccgLookup = new Dictionary<string, PostcodeRecord>();
        static void Main(string[] args)
        {

            Console.WriteLine("Beginning Data import");
            LoadDefaultgSettings();
            LoadSettings(args);
            //LoadDebugSettings();
            LoadCCGLookupdata(_ccgCsvFilePath).Wait();
            var clock = new Stopwatch();
            clock.Start();
            RunImport().Wait();
            clock.Stop();

            Console.WriteLine("finished importing " + _recordCount + " in " + (clock.ElapsedMilliseconds /1000.00) + " seconds");
            Console.ReadLine();

        }

        public static async Task LoadCCGLookupdata(string csvFilePath)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(_accountname, _accountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            var stptable = tableClient.GetTableReference(_ccgtableReference);
            var csvlookup = new CsvReader(new StreamReader(csvFilePath));
            csvlookup.Read();
            csvlookup.ReadHeader();
            var ccgs = new List<STPEntity>();
            var batch = new TableBatchOperation();

            while (csvlookup.Read())
            {
                batch.Add(TableOperation.InsertOrReplace(new STPEntity()
                {
                    PartitionKey = "CCGs",
                    RowKey = csvlookup.GetField<string>("FID"),
                    CCGId = csvlookup.GetField<string>("CCG16CD"),
                    STPId = csvlookup.GetField<string>("STP17CD"),
                    STPName = csvlookup.GetField<string>("STP17NM"),
                    CCGName = csvlookup.GetField<string>("CCG16NM"),
                    ProductName = csvlookup.GetField<string>("Product"),
                    LiveDate = csvlookup.GetField<DateTime?>("LiveDate"),
                    ServiceIdWhitelist = csvlookup.GetField<string>("ServiceIdWhitelist"),
                    ITKServiceIdWhitelist = csvlookup.GetField<string>("ITKServiceIdWhitelist")
                }));

                if (!_ccgLookup.ContainsKey(csvlookup.GetField<string>("CCG16CD")))
                {
                    _ccgLookup.Add(csvlookup.GetField<string>("CCG16CD"), new PostcodeRecord()
                    {
                        AppName = csvlookup.GetField<string>("Product"),
                        StpName = csvlookup.GetField<string>("STP17NM"),
                        CcgName = csvlookup.GetField<string>("CCG16NM"),
                        CCGId = csvlookup.GetField<string>("CCG16CD")
                    });
                }
                if (batch.Count % 100 == 0)
                {
                    await stptable.ExecuteBatchAsync(batch);
                    batch = new TableBatchOperation();
                }
            }
            if (batch.Count > 0) await stptable.ExecuteBatchAsync(batch);

        }

        public static async Task  RunImport()
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(_accountname, _accountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_tableReference);
            var csv = new CsvReader(new StreamReader(_postcodeCsvFilePath));
            var batchsizeMax = 100;
            var batch = new TableBatchOperation();
            _terminatedPostcodesCount = 0;
            var i = 0;
            csv.Read();
            csv.ReadHeader();
            _recordCount = File.ReadLines(_postcodeCsvFilePath).Count() -1;
            var tasks = new List<Task>();
            while (csv.Read())
            {
                var termenatedDate = "";
                csv.TryGetField<string>("doterm", out termenatedDate);

                if (String.IsNullOrWhiteSpace(termenatedDate))
                {
                    var ccgName = "";
                    var ccgId = "";
                    var postcode = "";
                    var app = "";
                    csv.TryGetField<string>("ccg", out ccgId);
                    csv.TryGetField<string>("pcd", out postcode);

                    batch.Add(TableOperation.InsertOrReplace(new CCGEntity()
                    {
                        CCG = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].CcgName : "",
                        CCGId = ccgId,
                        Postcode = postcode,
                        App = _ccgLookup.ContainsKey(ccgId) ? _ccgLookup[ccgId].AppName : "",
                        PartitionKey = "Postcodes",
                        RowKey = postcode
                    }));
                    i++;

                    if ((i % batchsizeMax) == 0)
                    {

                        var task = ImportBatch(table, batch, i);
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
            tasks.Add(ImportBatch(table, batch, i));
            await Task.WhenAll(tasks);
        }

        public static async Task ImportBatch(CloudTable table, TableBatchOperation batch, int number)
        {
            var iportedCount = await table.ExecuteBatchAsync(batch);
            var newcount = _counter + iportedCount.Count;
            _counter = newcount;
            Console.WriteLine("Imported " + _counter + " records ("+_terminatedPostcodesCount +" terminated) of " + _recordCount + " (" + CalcuatePercentDone() + "%)");
        }

        public static string CalcuatePercentDone()
        {
            return ((((decimal) _counter + (decimal) _terminatedPostcodesCount)
                     / (decimal) _recordCount) * 100m).ToString("0.00");
        }

       public static void LoadDefaultgSettings()
        {
            _tableReference= "ccgTest";
            _accountname = "111storestd";
            _ccgtableReference = "stpTest";
        }
        public static void LoadDebugSettings()
        {
            _accountname = "111storestd";
            _tableReference = "ccgTest";
            _accountKey = @"TXXoIUj4ySXovV0G42CCPsLzLwcbztDvGqOZpq5Vj/+oxB7sNMgcU+uuPPZ65xzwHu66KxG5XDfKQLO7YeER+A==";
            _postcodeCsvFilePath = @"C:\Users\jtiffen\Downloads\ccgstagingtest.csv";
        }
        public static void LoadSettings(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-AccountName="))
                {
                    _accountname = args[i].Replace("-AccountName=", "");
                }

                if (args[i].StartsWith("-TableRef="))
                {
                    _tableReference = args[i].Replace("-TableRef=","");
                }

                if (args[i].StartsWith("-ccgTableRef="))
                {
                    _ccgtableReference = args[i].Replace("-ccgTableRef=", "");
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
            }


        }


    }

    public class PostcodeRecord
    {
        public string CCGId { get; set; }
        public string StpName { get; set; }
        public string CcgName { get; set; }
        public string AppName { get; set; }
    }
}
