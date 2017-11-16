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
        private static string _accountKey;
        private static string _csvFilePath;
        private static int _counter;
        private static int _recordCount;
        private static Dictionary<string, PostcodeRecord> _ccgLookup = new Dictionary<string, PostcodeRecord>();

        static void Main(string[] args)
        {
            Console.WriteLine("Beginning Data import");
            LoadSettings(args);
            //LoadDebugSettings();
            LoadCCGLookupdata(@"C:\Users\jtiffen\Downloads\ons ccg area2.csv");
            var clock = new Stopwatch();
            clock.Start();
            RunImport().Wait();
            clock.Stop();

            Console.WriteLine("finished importing " + _recordCount + " in " + (clock.ElapsedMilliseconds /1000.00) + " seconds");
            Console.ReadLine();

        }

        public static void LoadCCGLookupdata(string csvFilePath)
        {
            var csvlookup = new CsvReader(new StreamReader(csvFilePath));
            csvlookup.Read();
            csvlookup.ReadHeader();
            while (csvlookup.Read())
            {
                _ccgLookup.Add(csvlookup.GetField<string>("ONSCODE"), new PostcodeRecord()
                {
                    AppName = csvlookup.GetField<string>("Product"),
                    StpName = csvlookup.GetField<string>("STp"),
                    CcgName = csvlookup.GetField<string>("CCG"),
                    CCGId = csvlookup.GetField<string>("ONSCODE")
                });
            }
        }

        public static async Task  RunImport()
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(_accountname, _accountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_tableReference);
            var csv = new CsvReader(new StreamReader(_csvFilePath));
            var batchsizeMax = 100;
            var batch = new TableBatchOperation();
            var i = 0;
            csv.Read();
            csv.ReadHeader();
            _recordCount = File.ReadLines(_csvFilePath).Count() -1;
            var tasks = new List<Task>();
            while (csv.Read())
            {
                var termenatedDate = "";
                csv.TryGetField<string>("doTerm", out termenatedDate);

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
           // await table.ExecuteBatchAsync(batch);
           // return number;
            Console.WriteLine("Imported " + _counter + " records of " + _recordCount);
        }


        public static void LoadDebugSettings()
        {
            _accountname = "111storestd";
            _tableReference = "ccgTest";
            _accountKey = @"REDACTED";
            _csvFilePath = @"c:\path\test.csv";
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

                if (args[i].StartsWith("-AccountKey="))
                {
                    _accountKey = args[i].Replace("-AccountKey=","");
                }
                if (args[i].StartsWith("-CSVFilePath="))
                {
                    _csvFilePath = args[i].Replace("-CSVFilePath=","");
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
