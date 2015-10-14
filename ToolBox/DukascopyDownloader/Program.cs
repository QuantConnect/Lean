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
using System.Linq;
using System.Net;
using System.Text;
using Ionic.Zip;
using Newtonsoft.Json;
using SevenZip;

namespace QuantConnect.ToolBox.DukascopyDownloader
{
    class Program
    {
        private const string ConfigFileName = "config.json";
        private const string InstrumentsFileName = "instruments.txt";
        private const string EndOfLine = "\r\n";
        private const int DukascopyTickLength = 20;

        private static ConfigSettings _settings;
        private static Dictionary<string, LeanInstrument> _instruments;

        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        static void Main(string[] args)
        {
            // Startup doc screen
            Console.WriteLine("QuantConnect.ToolBox: Dukascopy Downloader: ");
            Console.WriteLine("==============================================");
            Console.WriteLine("The Dukascopy downloader retrieves historical data from the Dukascopy servers");
            Console.WriteLine("and saves files in the LEAN Algorithmic Trading Engine Data Format.");
            Console.WriteLine("Settings are loaded from the config.json file.");
            Console.WriteLine();
            Console.WriteLine("Note: Timestamps are stored in UTC");
            Console.WriteLine();

            Console.WriteLine("Press any key to Continue or Escape to quit.");
            Console.WriteLine();
            if (Console.ReadKey().Key == ConsoleKey.Escape) Environment.Exit(0);

            // Load instrument list
            if (!LoadInstruments()) Environment.Exit(0);

            // Read configuration 
            if (!LoadConfiguration()) Environment.Exit(0);

            try
            {
                foreach (var symbol in _settings.InstrumentList)
                {
                    RunProcess(symbol, _settings.StartDate, _settings.EndDate, _settings.OutputFormat);
                }

                Console.WriteLine("Process completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                Console.WriteLine("Press any key to quit.");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Loads the instrument list from the instruments.txt file
        /// </summary>
        /// <returns></returns>
        private static bool LoadInstruments()
        {
            if (!File.Exists(InstrumentsFileName))
            {
                Console.WriteLine(InstrumentsFileName + " file not found.");
                return false;
            }

            _instruments = new Dictionary<string, LeanInstrument>();

            var lines = File.ReadAllLines(InstrumentsFileName);
            foreach (var line in lines)
            {
                var tokens = line.Split(',');
                if (tokens.Length >= 4)
                {
                    _instruments.Add(tokens[0], new LeanInstrument
                    {
                        Symbol = tokens[0],
                        Name = tokens[1],
                        Type = (InstrumentType)Enum.Parse(typeof(InstrumentType), tokens[2]),
                        PointValue = Convert.ToInt32(tokens[3])
                    });
                }
            }

            return true;
        }

        /// <summary>
        /// Loads configuration settings from the config.json file
        /// </summary>
        /// <returns></returns>
        private static bool LoadConfiguration()
        {
            if (!File.Exists(ConfigFileName))
            {
                Console.WriteLine(ConfigFileName + " file not found.");
                return false;
            }

            _settings = JsonConvert.DeserializeObject<ConfigSettings>(File.ReadAllText(ConfigFileName));

            return ValidateConfiguration();
        }

        /// <summary>
        /// Validates the loaded configuration settings
        /// </summary>
        /// <returns></returns>
        private static bool ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_settings.OutputFolder))
            {
                Console.WriteLine("The Lean data folder is required.");
                return false;
            }
            if (!Directory.Exists(_settings.OutputFolder))
                Directory.CreateDirectory(_settings.OutputFolder);

            foreach (var symbol in _settings.InstrumentList)
            {
                if (!_instruments.ContainsKey(symbol))
                {
                    Console.WriteLine("Invalid symbol requested: {0}", symbol);
                    return false;
                }
            }

            if (_settings.EndDate < _settings.StartDate)
            {
                Console.WriteLine("The end date must be greater or equal to the start date.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Downloads historical bars from Dukascopy servers and saves them in the requested output format
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="barsPerRequest"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        public static void RunProcess(string symbol, DateTime fromDate, DateTime toDate, string outputFormat)
        {
            Console.WriteLine("Symbol: {0}, from {1} to {2}", symbol, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));

            var hourlyBars = new List<LeanBar>();

            // set the starting date
            DateTime date = fromDate;

            // loop until last date
            while (date <= toDate)
            {
                // request all ticks for a specific date
                try
                {
                    var ticks = DownloadTicks(symbol, date);
                    if (ticks.Count > 0)
                    {
                        SaveBars(ticks, symbol, date, outputFormat);

                        // Aggregate ticks to hourly bars
                        hourlyBars.AddRange(AggregateTicks(ticks, new TimeSpan(1, 0, 0)));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                date = date.AddDays(1);
            }

            if (hourlyBars.Count > 0)
            {
                SaveHourlyBars(hourlyBars, symbol);

                // Aggregate second bars to one single daily bar
                var dailyBars = AggregateBars(hourlyBars, new TimeSpan(1, 0, 0, 0));
                SaveDailyBars(dailyBars, symbol);
            }
        }

        /// <summary>
        /// Saves a list of bars in hourly resolution in Lean format (zipped CSV)
        /// </summary>
        /// <param name="hourlyBars"></param>
        /// <param name="symbol"></param>
        private static void SaveHourlyBars(List<LeanBar> hourlyBars, string symbol)
        {
            var sb = new StringBuilder();
            foreach (var row in hourlyBars)
            {
                var timestamp = row.Time.ToString("yyyyMMdd HH:mm");

                sb.AppendLine(string.Join(",", timestamp,
                    row.Open.ToString(CultureInfo.InvariantCulture),
                    row.High.ToString(CultureInfo.InvariantCulture),
                    row.Low.ToString(CultureInfo.InvariantCulture),
                    row.Close.ToString(CultureInfo.InvariantCulture),
                    row.TickVolume));
            }

            // File path: /Lean/Data/forex/dukascopy/daily/eurusd.zip -> eurusd.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "dukascopy",
                "hour");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}.zip", symbol.Replace("_", "").ToLower()));
            var entryName = string.Format("{0}.csv", symbol.Replace("_", "").ToLower());

            if (File.Exists(zipFileName))
                UpdateZipFile(zipFileName, entryName, sb);
            else
                WriteZipFile(zipFileName, entryName, sb);
        }

        /// <summary>
        /// Saves a list of bars in daily resolution in Lean format (zipped CSV)
        /// </summary>
        /// <param name="dailyBars"></param>
        /// <param name="symbol"></param>
        private static void SaveDailyBars(List<LeanBar> dailyBars, string symbol)
        {
            var sb = new StringBuilder();
            foreach (var row in dailyBars)
            {
                var timestamp = row.Time.ToString("yyyyMMdd 00:00");

                sb.AppendLine(string.Join(",", timestamp,
                    row.Open.ToString(CultureInfo.InvariantCulture),
                    row.High.ToString(CultureInfo.InvariantCulture),
                    row.Low.ToString(CultureInfo.InvariantCulture),
                    row.Close.ToString(CultureInfo.InvariantCulture),
                    row.TickVolume));
            }

            // File path: /Lean/Data/forex/dukascopy/daily/eurusd.zip -> eurusd.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "dukascopy",
                "daily");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}.zip", symbol.Replace("_", "").ToLower()));
            var entryName = string.Format("{0}.csv", symbol.Replace("_", "").ToLower());

            if (File.Exists(zipFileName))
                UpdateZipFile(zipFileName, entryName, sb);
            else
                WriteZipFile(zipFileName, entryName, sb);
        }

        /// <summary>
        /// Aggregates a list of bars at the requested resolution
        /// </summary>
        /// <param name="bars"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private static List<LeanBar> AggregateBars(List<LeanBar> bars, TimeSpan resolution)
        {
            return
                (from b in bars
                 group b by b.Time.RoundDown(resolution)
                     into g
                     select new LeanBar
                     {
                         Time = g.Key,
                         Open = g.First().Open,
                         High = g.Max(b => b.High),
                         Low = g.Min(b => b.Low),
                         Close = g.Last().Close,
                         TickVolume = g.Sum(b => b.TickVolume)
                     }).ToList();
        }

        /// <summary>
        /// Saves a list of downloaded ticks in the requested output format
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        /// <param name="outputFormat"></param>
        private static void SaveBars(List<DukascopyTick> ticks, string symbol, DateTime date, string outputFormat)
        {
            switch (outputFormat)
            {
                // zipped CSV format
                case "lean":
                    {
                        // Write ticks to zip file
                        SaveTicks(ticks, symbol, date);

                        // Write 1-second resolution bars to zip file
                        var barsSecond = AggregateTicks(ticks, new TimeSpan(0, 0, 1));
                        SaveSecondBars(barsSecond, symbol, date);

                        // Aggregate to 1-minute resolution + write zip file
                        var barsMinute = AggregateTicks(ticks, new TimeSpan(0, 1, 0));
                        SaveMinuteBars(barsMinute, symbol, date);
                    }
                    break;

                default:
                    throw new NotSupportedException("Unsupported output format.");
            }
        }

        /// <summary>
        /// Saves a list of bars in 1-minute resolution in Lean format (zipped CSV)
        /// </summary>
        /// <param name="minuteBars"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        private static void SaveMinuteBars(List<LeanBar> minuteBars, string symbol, DateTime date)
        {
            var sb = new StringBuilder();
            foreach (var row in minuteBars)
            {
                // convert datetime to millis
                var timestamp = (int)row.Time.TimeOfDay.TotalMilliseconds;

                sb.AppendLine(string.Join(",", timestamp,
                    row.Open.ToString(CultureInfo.InvariantCulture),
                    row.High.ToString(CultureInfo.InvariantCulture),
                    row.Low.ToString(CultureInfo.InvariantCulture),
                    row.Close.ToString(CultureInfo.InvariantCulture),
                    row.TickVolume));
            }

            // File path: /Lean/Data/forex/dukascopy/minute/eurusd/yyyymmdd_quote.zip -> yyyymmdd_eurusd_minute_quote.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "dukascopy",
                "minute",
                symbol.Replace("_", "").ToLower());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}_quote.zip", date.ToString("yyyyMMdd")));
            var entryName = string.Format("{0}_{1}_minute_quote.csv", date.ToString("yyyyMMdd"), symbol.Replace("_", "").ToLower());

            WriteZipFile(zipFileName, entryName, sb);
        }

        /// <summary>
        /// Saves a list of bars in 1-second resolution in Lean format (zipped CSV)
        /// </summary>
        /// <param name="secondBars"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        private static void SaveSecondBars(List<LeanBar> secondBars, string symbol, DateTime date)
        {
            var sb = new StringBuilder();
            foreach (var row in secondBars)
            {
                // convert datetime to millis
                var timestamp = (int)row.Time.TimeOfDay.TotalMilliseconds;

                sb.AppendLine(string.Join(",", timestamp,
                    row.Open.ToString(CultureInfo.InvariantCulture),
                    row.High.ToString(CultureInfo.InvariantCulture),
                    row.Low.ToString(CultureInfo.InvariantCulture),
                    row.Close.ToString(CultureInfo.InvariantCulture),
                    row.TickVolume));
            }

            // File path: /Lean/Data/forex/dukascopy/second/eurusd/yyyymmdd_quote.zip -> yyyymmdd_eurusd_second_quote.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "dukascopy",
                "second",
                symbol.Replace("_", "").ToLower());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}_quote.zip", date.ToString("yyyyMMdd")));
            var entryName = string.Format("{0}_{1}_second_quote.csv", date.ToString("yyyyMMdd"), symbol.Replace("_", "").ToLower());

            WriteZipFile(zipFileName, entryName, sb);
        }

        /// <summary>
        /// Aggregates a list of ticks at the requested resolution
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private static List<LeanBar> AggregateTicks(List<DukascopyTick> ticks, TimeSpan resolution)
        {
            return
                (from t in ticks
                 group t by t.Time.RoundDown(resolution)
                     into g
                     select new LeanBar
                     {
                         Time = g.Key,
                         Open = g.First().MidPrice,
                         High = g.Max(t => t.MidPrice),
                         Low = g.Min(t => t.MidPrice),
                         Close = g.Last().MidPrice,
                         TickVolume = g.Count()
                     }).ToList();
        }

        /// <summary>
        /// Saves a list of downloaded ticks in Lean format (zipped CSV)
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        private static void SaveTicks(List<DukascopyTick> ticks, string symbol, DateTime date)
        {
            var sb = new StringBuilder();
            foreach (var row in ticks)
            {
                // convert datetime to millis
                var timestamp = (int)row.Time.TimeOfDay.TotalMilliseconds;

                sb.AppendLine(string.Join(",", timestamp, 
                    row.BidPrice.ToString(CultureInfo.InvariantCulture),
                    row.AskPrice.ToString(CultureInfo.InvariantCulture)));
            }

            // File path: /Lean/Data/forex/dukascopy/tick/eurusd/yyyymmdd_quote.zip -> yyyymmdd_eurusd_tick_quote.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "dukascopy",
                "tick",
                symbol.Replace("_", "").ToLower());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}_quote.zip", date.ToString("yyyyMMdd")));
            var entryName = string.Format("{0}_{1}_tick_quote.csv", date.ToString("yyyyMMdd"), symbol.Replace("_", "").ToLower());

            WriteZipFile(zipFileName, entryName, sb);
        }

        /// <summary>
        /// Writes bars to an entry in a zip file 
        /// The zip file is always overwritten.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="entryName"></param>
        /// <param name="sb"></param>
        private static void WriteZipFile(string zipFileName, string entryName, StringBuilder sb)
        {
            if (File.Exists(zipFileName)) File.Delete(zipFileName);

            using (var zip = new ZipFile(zipFileName))
            {
                zip.AddEntry(entryName, sb.ToString());
                zip.Save();
            }

            if (_settings.EnableTrace)
            {
                Console.WriteLine("Created: " + zipFileName);
            }
        }

        /// <summary>
        /// Inserts/Updates/appends bars to an entry in a zip file
        /// The existing bars are updated with the newer ones.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="entryName"></param>
        /// <param name="sb"></param>
        private static void UpdateZipFile(string zipFileName, string entryName, StringBuilder sb)
        {
            using (var zip = ZipFile.Read(zipFileName))
            {
                var entry = zip[entryName];

                using (var stream = new MemoryStream())
                {
                    entry.Extract(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(stream))
                    {
                        // old bars
                        var rowsExisting = reader.ReadToEnd().Split(new[] { EndOfLine }, StringSplitOptions.RemoveEmptyEntries);
                        // new bars
                        var rowsNew = sb.ToString().Split(new[] { EndOfLine }, StringSplitOptions.RemoveEmptyEntries);

                        // merge bars
                        var rowsFinal = new SortedDictionary<string, string>();
                        foreach (var row in rowsExisting)
                        {
                            rowsFinal.Add(row.Substring(0, 14), row);
                        }
                        // new bars always overwrite existing ones
                        foreach (var row in rowsNew)
                        {
                            rowsFinal[row.Substring(0, 14)] = row;
                        }

                        // new entry content
                        string content = string.Join(EndOfLine, rowsFinal.Values.ToList()) + EndOfLine;

                        // update zip entry and save
                        zip.UpdateEntry(entryName, content);
                        zip.Save();
                    }
                }
            }

            if (_settings.EnableTrace)
            {
                Console.WriteLine("Updated: " + zipFileName);
            }
        }

        /// <summary>
        /// Downloads all ticks for the specified date
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private static List<DukascopyTick> DownloadTicks(string symbol, DateTime date)
        {
            var ticks = new List<DukascopyTick>();

            var pointValue = _instruments[symbol].PointValue;

            for (int hour = 0; hour < 24; hour++)
            {
                var timeOffset = hour * 3600000;

                var url = string.Format(@"http://www.dukascopy.com/datafeed/{0}/{1:D4}/{2:D2}/{3:D2}/{4:D2}h_ticks.bi5", 
                    symbol, date.Year, date.Month - 1, date.Day, hour);

                if (_settings.EnableTrace)
                {
                    Console.WriteLine(url);
                }

                using (var client = new WebClient())
                {
                    var bytes = client.DownloadData(url);
                    if (bytes.Length > 0)
                    {
                        AppendTicksToList(ticks, bytes, date, timeOffset, pointValue);
                    }
                }
            }

            return ticks;
        }

        /// <summary>
        /// Reads ticks from a Dukascopy binary buffer into a list
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="bytesBi5"></param>
        /// <param name="date"></param>
        /// <param name="timeOffset"></param>
        /// <param name="pointValue"></param>
        private static unsafe void AppendTicksToList(List<DukascopyTick> ticks, byte[] bytesBi5, DateTime date, int timeOffset, double pointValue)
        {
            using (var inStream = new MemoryStream(bytesBi5))
            {
                using (var outStream = new MemoryStream())
                {
                    SevenZipExtractor.DecompressStream(inStream, outStream, (int) inStream.Length, null);

                    byte[] bytes = outStream.GetBuffer();
                    int count = bytes.Length / DukascopyTickLength;

                    // Numbers are big-endian
                    // ii1 = milliseconds within the hour
                    // ii2 = AskPrice * point value
                    // ii3 = BidPrice * point value
                    // ff1 = AskVolume (not used)
                    // ff2 = BidVolume (not used)

                    fixed (byte* pBuffer = &bytes[0])
                    {
                        uint* p = (uint*)pBuffer;

                        for (int i = 0; i < count; i++)
                        {
                            ReverseBytes(p); uint time = *p++;
                            ReverseBytes(p); uint ask = *p++;
                            ReverseBytes(p); uint bid = *p++;
                            p++; p++;

                            if (bid > 0 && ask > 0)
                            {
                                ticks.Add(new DukascopyTick
                                {
                                    Time = date.AddMilliseconds(timeOffset + time),
                                    BidPrice = bid / pointValue,
                                    AskPrice = ask / pointValue,
                                    MidPrice = (bid + ask) / (2 * pointValue)
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts a 32-bit unsigned integer from big-endian to little-endian (and vice-versa)
        /// </summary>
        /// <param name="p"></param>
        private static unsafe void ReverseBytes(uint* p)
        {
            *p = (*p & 0x000000FF) << 24 | (*p & 0x0000FF00) << 8 | (*p & 0x00FF0000) >> 8 | (*p & 0xFF000000) >> 24;
        }


    }
}
