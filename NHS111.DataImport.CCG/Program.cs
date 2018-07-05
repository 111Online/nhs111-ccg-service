using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
using CsvHelper.Configuration;

using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing;

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
        private static string _dosSaerchDistanceFilePath;
        private static int _counter;
        private static int _recordCount;
        private static readonly Regex _regexRemoveWhitespace = new Regex(@"\s+");
        private static int _terminatedPostcodesCount;
        private static Dictionary<string, int> _dosSearchDistanceLookup = new Dictionary<string, int>();
        private static Dictionary<string, int> _dosSearchDistancePartialLookup = new Dictionary<string, int>();
        private static bool _onlyImportstpData = false;
        private static Dictionary<string, PostcodeRecord> _ccgLookup = new Dictionary<string, PostcodeRecord>();
        static void Main(string[] args)
        {

            Console.WriteLine("Beginning Data import");
            LoadSettings(args);

            var clock = new Stopwatch();
            clock.Start();
            LoadCCGLookupdata(_ccgCsvFilePath).Wait();
            LoadDOSSearchDistanceLookupdata("").Wait();

            if (!_onlyImportstpData)
            {
                RunImport().Wait();
                clock.Stop();
                Console.WriteLine("finished importing " + _recordCount + " in " +
                                  TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).ToString(@"hh\:mm\:ss"));
            }
            clock.Stop();
            Console.WriteLine("finished importing stp data only in " +
                              TimeSpan.FromMilliseconds(clock.ElapsedMilliseconds).ToString(@"hh\:mm\:ss"));
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
                    RowKey = csvlookup.GetField<string>("CCG16CD"),
                    CCGId = csvlookup.GetField<string>("CCG16CD"),
                    STPId = csvlookup.GetField<string>("STP17CD"),
                    STPName = csvlookup.GetField<string>("STP17NM"),
                    CCGName = csvlookup.GetField<string>("CCG16NM"),
                    ProductName = csvlookup.GetField<string>("Product"),
                    LiveDate = csvlookup.GetField<DateTime?>("LiveDate", new DateTimeLocalConverter()),
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


        public static async Task LoadDOSSearchDistanceLookupdata(string xlsFilePath)
        {
            var package = new ExcelPackage(new FileInfo(_dosSaerchDistanceFilePath));
            Console.WriteLine("Loading DOS search distance Data");
            var fullPostcodeSheet = package.Workbook.Worksheets[2];
            for (int i = 1; i <= fullPostcodeSheet.Dimension.End.Row; i++)
            {
                _dosSearchDistanceLookup.Add(NormalisePostcode(fullPostcodeSheet.Cells[i, 1].Value.ToString()), Convert.ToInt32(fullPostcodeSheet.Cells[i, 2].Value));
            }

            var partialPostcodeSheet = package.Workbook.Worksheets[1];
            for (int i = 2; i <= partialPostcodeSheet.Dimension.End.Row; i++)
            {
                _dosSearchDistancePartialLookup.Add(partialPostcodeSheet.Cells[i, 1].Value.ToString(), Convert.ToInt32(partialPostcodeSheet.Cells[i, 2].Value));
            }

            Console.WriteLine("Finished loading DOS search distance Data");
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
            var noDosSearchDistanceCount = 0;
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
                    var formattedPostcode = "";
                    var partialPostcode = "";
                    var app = "";
                    var dosSearchDistance = "";
                    csv.TryGetField<string>("ccg", out ccgId);
                    csv.TryGetField<string>("pcd", out postcode);
                    csv.TryGetField<string>("pcds", out formattedPostcode);
                    partialPostcode = formattedPostcode.Split(' ').First().Trim();
                    if (_dosSearchDistanceLookup.ContainsKey(NormalisePostcode(postcode)))
                        dosSearchDistance = _dosSearchDistanceLookup[NormalisePostcode(postcode)].ToString();
                    else if (_dosSearchDistancePartialLookup.ContainsKey(partialPostcode))
                        dosSearchDistance = _dosSearchDistancePartialLookup[partialPostcode].ToString();
                    else noDosSearchDistanceCount++;
                  
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
            Console.WriteLine("DOS Search distance not mapped count: " + noDosSearchDistanceCount);
            await Task.WhenAll(tasks);
        }

        public static async Task ImportBatch(CloudTable table, TableBatchOperation batch, int number)
        {
            var importedCount = await table.ExecuteBatchAsync(batch);
            var newcount = _counter + importedCount.Count;
            _counter = newcount;
            Console.WriteLine("Imported " + _counter + " records ("+_terminatedPostcodesCount +" terminated) of " + _recordCount +  " (" + CalcuatePercentDone() + "%)");
        }

        public static string CalcuatePercentDone()
        {
            return ((((decimal) _counter + (decimal) _terminatedPostcodesCount)
                     / (decimal) _recordCount) * 100m).ToString("0.00");
        }

        private static string NormalisePostcode(string postcode)
        {
            return _regexRemoveWhitespace.Replace(postcode, string.Empty).ToUpper();
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

                if (args[i].StartsWith("-DosSaerchDistanceFilePath"))
                {
                    _dosSaerchDistanceFilePath = args[i].Replace("-DosSaerchDistanceFilePath=", "");
                }

                if (args[i].StartsWith("-STPDataOnly"))
                {
                    _onlyImportstpData = true;
                }
            }


        }


    }

    public class DateTimeLocalConverter : DateTimeConverter, ITypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (String.IsNullOrWhiteSpace(text))
                return null;
            return DateTime.ParseExact(text, "dd/MM/yyyy", null);
        }
    }

    public class PostcodeRecord
    {
        public string CCGId { get; set; }
        public string StpName { get; set; }
        public string CcgName { get; set; }
        public string AppName { get; set; }
    }

    public class DosSearchRecord
    {
        public string Postcode { get; set; }
        public int searchDistance { get; set; }
    }
}
