/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    class Program
    {
        static string _resolution = "minute";
        static string _sourceDirectory = @"C:\Users\Alexandre\Desktop\AlgoSeek";
        static string _destinationDirectory = @"C:\Users\Alexandre\Desktop\AlgoSeek\Data\options\usa\";
        static TimeSpan _span = new TimeSpan(0, 1, 0);

        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        static void Main(string[] args)
        {
            //Document the process:
            Console.WriteLine("QuantConnect.ToolBox: AlgoSeek Options Processor ");
            Console.WriteLine("=================================================");
            Console.WriteLine("The AlgoSeek Options Processor transforms AlgoSeek options ticks into the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Three parameters are required: ");
            Console.WriteLine("   1> Output resolution of the LEAN data. (either minute, second or hour)");
            Console.WriteLine("   2> Source Directory of Zipped AlgoSeek Options Data.");
            Console.WriteLine("   3> Destination Directory of LEAN Data Folder. (Typically located under Lean/Data)");
            Console.WriteLine(" ");
            Console.WriteLine("NOTE: THIS WILL OVERWRITE ANY EXISTING FILES.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            if (args.Length > 0) _resolution = args[0];
            if (args.Length > 1) _sourceDirectory = args[1];
            if (args.Length > 2) _destinationDirectory = args[2];

            //Validate the user input:
            Validate(_sourceDirectory, _destinationDirectory, _resolution);

            //Remove the final slash to make the path building easier:
            _sourceDirectory = StripFinalSlash(_sourceDirectory);
            _destinationDirectory = StripFinalSlash(_destinationDirectory);

            //Get all the zip files to process
            Console.WriteLine("Counting Files...");
            var zipFiles = new DirectoryInfo(_sourceDirectory).GetFiles("*.zip");
            Console.WriteLine("Processing {0} Files ...", zipFiles.Count());

            // Dummy zip file
            var zipOut = new Ionic.Zip.ZipFile();

            foreach (var zipFile in zipFiles)
            {
                var starttime = DateTime.Now;
                Console.WriteLine("Reading " + zipFile.Name);

                var listTick = new List<TmpTick>();

                using (var reader = Compression.Unzip(zipFile.FullName, out zipOut))
                    while (!reader.EndOfStream)
                    {
                        var tick = new TmpTick(zipFile.Name.Split('.')[1], reader.ReadLine());
                        if (tick.IsInvalid) continue;

                        if (listTick.Count == 0)
                        {
                            listTick.Add(tick);
                            continue;
                        }

                        var lastRoundTime = listTick.Last().Time.RoundDown(_span);

                        if (lastRoundTime == tick.Time.RoundDown(_span))
                        {
                            listTick.Add(tick);
                            continue;
                        }

                        Console.Write("\r" + lastRoundTime.ToString(@"HH\:mm\:ss") + "\t" + listTick.Count + "\t" + (DateTime.Now - starttime).ToString(@"hh\:mm\:ss\.fff"));

                        WriteLeanCsvFiles(listTick);

                        listTick.Add(tick);
                    }

                WriteLeanCsvFiles(listTick);

                CompressLeanCSV(zipFile);

                Console.WriteLine("... done!");
            }

            Console.WriteLine("All done! Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Application error: display error and then stop conversion
        /// </summary>
        /// <param name="error">Error string</param>
        private static void Error(string error)
        {
            Console.WriteLine(error);
            Console.ReadKey();
            Environment.Exit(0);
        }

        /// <summary>
        /// Validate the users input and throw error if not valid
        /// </summary>
        private static void Validate(string sourceDirectory, string destinationDirectory, string resolution)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                Error("Error: Please enter a valid source directory.");
            }
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Error("Error: Please enter a valid destination directory.");
            }
            if (!Directory.Exists(sourceDirectory))
            {
                Error("Error: Source directory does not exist.");
            }
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            if (resolution != "minute" && resolution != "second" && resolution != "hour" && resolution != "daily")
            {
                Error("Error: Resolution specified is not supported. Please enter daily, hour, second or minute");
            }

            _span = new Dictionary<string, TimeSpan>()
                {
                    { "daily",  new TimeSpan(1, 0, 0, 0)},
                    { "hour",   new TimeSpan(0, 1, 0, 0)},
                    { "minute", new TimeSpan(0, 0, 1, 0)},
                    { "second", new TimeSpan(0, 0, 0, 1)}
                }[resolution];
        }

        /// <summary>
        /// Remove the final slash to make path building easier
        /// </summary>
        private static string StripFinalSlash(string directory)
        {
            return directory.Trim('/', '\\');
        }

        /// <summary>
        /// Group list of ticks, create bar and write it in a csv file
        /// <param name="listTick">List of ticks to porcess</param>
        /// </summary>
        private static void WriteLeanCsvFiles(List<TmpTick> listTick)
        {
            listTick.GroupBy(t => t.SetFilename(_resolution)).ToList().ForEach(g =>
            {
                var filename = _destinationDirectory + @"\" + g.Key;
                if (!new FileInfo(filename).Directory.Exists) new FileInfo(filename).Directory.Create();

                if (g.Key.Contains("trade"))
                {
                    var tradeBar = new TmpTradeBar
                    {
                        Time = g.First().Time.RoundDown(_span),
                        Underlying = g.First().Underlying,
                        OptionType = g.First().OptionType,
                        Strike = g.First().Strike,
                        Expiration = g.First().Expiration,

                        TradeOpen = g.First().Price,
                        TradeHigh = g.Max(x => x.Price),
                        TradeLow = g.Min(x => x.Price),
                        TradeClose = g.Last().Price,

                        TradeVolume = g.Sum(x => x.Quantity)
                    };

                    File.AppendAllText(filename, tradeBar.CsvFileOutput() + "\r\n");
                }
                else
                {
                    var quoteBar = new TmpQuoteBar
                    {
                        Time = g.First().Time.RoundDown(_span),
                        Underlying = g.First().Underlying,
                        OptionType = g.First().OptionType,
                        Strike = g.First().Strike,
                        Expiration = g.First().Expiration
                    };

                    var a = g.Where(x => x.TickType == "ASK").Select(x => x.Price);
                    var b = g.Where(x => x.TickType == "BID").Select(x => x.Price);

                    if (a.Count() > 0)
                    {
                        quoteBar.AskOpen = a.First();
                        quoteBar.AskHigh = a.Max();
                        quoteBar.AskLow = a.Min();
                        quoteBar.AskClose = a.Last();
                        quoteBar.AvgAskSize = (long)g.Where(x => x.TickType == "ASK").Average(x => x.Quantity);
                    }

                    if (b.Count() > 0)
                    {
                        quoteBar.BidOpen = b.First();
                        quoteBar.BidHigh = b.Max();
                        quoteBar.BidLow = b.Min();
                        quoteBar.BidClose = b.Last();
                        quoteBar.AvgBidSize = (long)g.Where(x => x.TickType == "BID").Average(x => x.Quantity);
                    }

                    File.AppendAllText(filename, quoteBar.CsvFileOutput() + "\r\n");
                }
            });

            listTick.Clear();
        }

        /// <summary>
        /// Compress csv files transformed from current AlgoSeek file
        /// <param name="sourceFile">AlgoSeek file which we extract data from</param>
        /// </summary>
        private static void CompressLeanCSV(FileInfo sourceFile)
        {
            var fileinfo = sourceFile.Name.Split('.');

            var csvFilesFolders = new List<DirectoryInfo>();

            new DirectoryInfo(_destinationDirectory + @"\" + _resolution + @"\" + fileinfo[0].ToLower()).GetDirectories()
                .ToList().ForEach(optionType => csvFilesFolders.AddRange(optionType.GetDirectories().ToList()));

            foreach (var csvFilesFolder in csvFilesFolders)
            {
                foreach (var ticktype in new string[] { "quote", "trade" })
                {
                    var zipfile = csvFilesFolder.FullName + @"\" + fileinfo[1] + "_" + ticktype + ".zip";

                    using (var z = new FileStream(zipfile, FileMode.Create))
                    using (var a = new ZipArchive(z, ZipArchiveMode.Create, true))
                    {
                        var csvFiles = csvFilesFolder.GetFiles(fileinfo[1] + "*_" + ticktype + ".csv");

                        if (csvFiles.Length == 0) Console.WriteLine("No " + ticktype + " csv file to zip.");

                        foreach (var csvFile in csvFiles)
                        {
                            a.CreateEntryFromFile(csvFile.FullName, csvFile.Name, CompressionLevel.Optimal);
                            csvFile.Delete();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tick class
    /// </summary>
    class TmpTick
    {
        public bool IsInvalid { get; set; }
        public DateTime Time { get; set; }
        public string Underlying { get; set; }
        public string OptionType { get; set; }
        public long Strike { get; set; }
        public string Expiration { get; set; }

        public string TickType { get; set; }
        public long Price { get; set; }
        public long Quantity { get; set; }

        public TmpTick() { }

        public TmpTick(string datestr, string str)
        {
            if (this.IsInvalid = !(str.Contains("TRADE") || str.Contains("FIRM_QUOTE ASK NB") || str.Contains("FIRM_QUOTE BID NB"))) return;

            var strarr = str.Split(',').Select(x => x.Trim()).ToArray();

            if (this.IsInvalid = strarr[7].Length > 0 && "ABCDEFG".Contains(strarr[7])) return;

            this.Price = ConvertStringToLong(strarr[4]);
            if (this.IsInvalid = this.Price <= 0) return;

            this.Time = DateTime.ParseExact(datestr, "yyyyMMdd", CultureInfo.InvariantCulture).Add(TimeSpan.Parse(strarr[0]));
            this.TickType = strarr[1].Contains("ASK") ? "ASK" : strarr[1].Contains("BID") ? "BID" : "TRADE";
            this.Underlying = strarr[2];
            this.Quantity = long.Parse(strarr[5]);

            var detail = strarr[3].Trim().Split(' ');
            this.OptionType = detail[0].Trim();
            this.Strike = ConvertStringToLong(detail[2]);
            this.Expiration = detail[4].Replace("-", "").Trim();
        }

        private long ConvertStringToLong(string str)
        {
            return (long)(10000 * decimal.Parse(str.Trim(), NumberStyles.Any, new CultureInfo("en-US")));
        }

        public string SetFilename(string resolution)
        {
            return this.IsInvalid ? "Invalid tick" :
                string.Format(@"{0}\{1}\{2}\{3}\{4:yyyyMMdd}_{1}_{0}_{2}_{3}_{5}_{6}.csv", resolution,
                this.Underlying, this.OptionType, this.Expiration, this.Time, this.Strike,
                this.TickType == "TRADE" ? "trade" : "quote").ToLower();
        }

        public string CsvFileOutput()
        {
            return this.Time.TimeOfDay.TotalMilliseconds.ToString("F0") + "," + this.Price + "," + this.Quantity;
        }

        public override string ToString()
        {
            return this.IsInvalid ? "Invalid tick" :
                (this.Underlying + " " + this.OptionType + " " + this.Expiration + " " + this.Strike + " " +
                this.Time.ToString(@"yyyyMMdd hhmmss\.fff") + " " + this.Price + " " + this.Quantity + " " + this.TickType).ToLower();
        }
    }
    /// <summary>
    /// QuoteBar class
    /// </summary>
    class TmpQuoteBar
    {
        public DateTime Time { get; set; }
        public string Underlying { get; set; }
        public string OptionType { get; set; }
        public long Strike { get; set; }
        public string Expiration { get; set; }

        public long BidOpen { get; set; }
        public long BidHigh { get; set; }
        public long BidLow { get; set; }
        public long BidClose { get; set; }
        public long AskOpen { get; set; }
        public long AskHigh { get; set; }
        public long AskLow { get; set; }
        public long AskClose { get; set; }
        public long AvgBidSize { get; set; }
        public long AvgAskSize { get; set; }

        public string CsvFileOutput()
        {
            var bidstr = this.AvgBidSize == 0 ? ",,,," : string.Format("{0},{1},{2},{3},{4}",
                this.BidOpen, this.BidHigh, this.BidLow, this.BidClose, this.AvgBidSize);

            var askstr = this.AvgAskSize == 0 ? ",,,," : string.Format("{0},{1},{2},{3},{4}",
                this.AskOpen, this.AskHigh, this.AskLow, this.AskClose, this.AvgAskSize);

            return this.Time.TimeOfDay.TotalMilliseconds.ToString("F0") + "," + bidstr + "," + askstr;
        }

        public override string ToString()
        {
            return this.Underlying + " " + this.OptionType + " " + this.Expiration + " " + this.Strike + " " + this.Time.ToString("yyyyMMdd") + " " + this.CsvFileOutput();
        }
    }
    /// <summary>
    /// TradeBar class
    /// </summary>
    class TmpTradeBar
    {
        public DateTime Time { get; set; }
        public string Underlying { get; set; }
        public string OptionType { get; set; }
        public long Strike { get; set; }
        public string Expiration { get; set; }

        public long TradeOpen { get; set; }
        public long TradeHigh { get; set; }
        public long TradeLow { get; set; }
        public long TradeClose { get; set; }
        public long TradeVolume { get; set; }

        public string CsvFileOutput()
        {
            return this.Time.TimeOfDay.TotalMilliseconds.ToString("F0") +
                (this.TradeVolume == 0 ? ",,,,," : string.Format(",{0},{1},{2},{3},{4}",
                this.TradeOpen, this.TradeHigh, this.TradeLow, this.TradeClose, this.TradeVolume));
        }

        public override string ToString()
        {
            return this.Underlying + " " + this.OptionType + " " + this.Expiration + " " + this.Strike + " " + this.Time.ToString("yyyyMMdd") + " " + this.CsvFileOutput();
        }
    }
}
