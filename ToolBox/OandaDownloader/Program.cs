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
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Newtonsoft.Json;
using OANDARestLibrary;
using OANDARestLibrary.TradeLibrary.DataTypes;
using OANDARestLibrary.TradeLibrary.DataTypes.Communications.Requests;

namespace QuantConnect.ToolBox.OandaDownloader
{
    class Program
    {
        private const string ConfigFileName = "config.json";
        private const string InstrumentsFileName = "instruments.txt";
        private const string EndOfLine = "\r\n";

        private static ConfigSettings _settings;
        private static Dictionary<string, LeanInstrument>  _instruments;

        /// <summary>
        /// Primary entry point to the program
        /// </summary>
        static void Main(string[] args)
        {
            // Startup doc screen
            Console.WriteLine("QuantConnect.ToolBox: Oanda Downloader: ");
            Console.WriteLine("==============================================");
            Console.WriteLine("The Oanda downloader retrieves historical data from the Oanda servers");
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

            // Set Oanda account credentials
            Credentials.SetCredentials(EEnvironment.Practice, _settings.AccessToken, _settings.AccountId);

            try
            {
                foreach (var symbol in _settings.InstrumentList)
                {
                    RunProcess(symbol, _settings.StartDate, _settings.EndDate, _settings.BarsPerRequest, _settings.OutputFormat).Wait();
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
                if (tokens.Length >= 3)
                {
                    _instruments.Add(tokens[0], new LeanInstrument
                    {
                        Symbol = tokens[0],
                        Name = tokens[1],
                        Type = (InstrumentType)Enum.Parse(typeof(InstrumentType), tokens[2])
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
            if (string.IsNullOrWhiteSpace(_settings.AccessToken))
            {
                Console.WriteLine("The access token is required.");
                return false;
            }

            if (_settings.AccountId == 0)
            {
                Console.WriteLine("The account id is required.");
                return false;
            }

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
        /// Downloads historical bars from Oanda servers and saves them in the requested output format
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="barsPerRequest"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        public static async Task RunProcess(string symbol, DateTime fromDate, DateTime toDate, int barsPerRequest, string outputFormat)
        {
            Console.WriteLine("Symbol: {0}, from {1} to {2}", symbol, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));

            var barsTotalInPeriod = new List<Candle>();
            var barsToSave = new List<Candle>();

            // set the starting date/time
            DateTime date = fromDate;
            DateTime startDateTime = date;
            string dateString = date.ToString("yyyy-MM-dd");

            // loop until last date
            while (startDateTime <= toDate.AddDays(1))
            {
                string start = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // request blocks of N bars with a starting date/time
                var bars = await DownloadBars(symbol, start, barsPerRequest);
                if (bars.Count == 0)
                    break;

                var groupedBars = GroupBarsByDate(bars);

                if (groupedBars.Count > 1)
                {
                    // we received more than one day, so we save the completed days and continue
                    while (groupedBars.Count > 1)
                    {
                        var currentDate = groupedBars.Keys.First();
                        if (currentDate > toDate)
                            break;

                        // update the current date
                        date = currentDate;
                        dateString = date.ToString("yyyy-MM-dd");

                        barsToSave.AddRange(groupedBars[currentDate]);

                        SaveBars(barsToSave, symbol, dateString, outputFormat);
                        barsTotalInPeriod.AddRange(barsToSave);

                        barsToSave.Clear();

                        // remove the completed date 
                        groupedBars.Remove(currentDate);
                    }

                    // update the current date
                    date = groupedBars.Keys.First();
                    dateString = date.ToString("yyyy-MM-dd");

                    if (date <= toDate)
                    {
                        barsToSave.AddRange(groupedBars[date]);
                    }
                }
                else
                {
                    var currentDate = groupedBars.Keys.First();
                    if (currentDate > toDate)
                        break;

                    // update the current date
                    date = currentDate;
                    dateString = date.ToString("yyyy-MM-dd");

                    barsToSave.AddRange(groupedBars[date]);
                }

                // calculate the next request datetime (next 5-sec bar time)
                startDateTime = GetDateTimeFromString(bars[bars.Count - 1].time).AddSeconds(5);
            }

            if (barsToSave.Count > 0)
            {
                SaveBars(barsToSave, symbol, dateString, outputFormat);
                barsTotalInPeriod.AddRange(barsToSave);
            }

            // Aggregate bars to hourly bars
            var hourlyBars = AggregateBars(barsTotalInPeriod, new TimeSpan(1, 0, 0));
            SaveHourlyBars(hourlyBars, symbol);

            // Aggregate second bars to one single daily bar
            var dailyBars = AggregateBars(barsTotalInPeriod, new TimeSpan(1, 0, 0, 0));
            SaveDailyBars(dailyBars, symbol);
        }

        /// <summary>
        /// Groups a list of bars into a dictionary keyed by date
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        private static SortedDictionary<DateTime, List<Candle>> GroupBarsByDate(List<Candle> bars)
        {
            var groupedBars = new SortedDictionary<DateTime, List<Candle>>();

            foreach (var bar in bars)
            {
                var date = GetDateTimeFromString(bar.time).Date;

                if (!groupedBars.ContainsKey(date))
                    groupedBars[date] = new List<Candle>();

                groupedBars[date].Add(bar);
            }

            return groupedBars;
        }

        /// <summary>
        /// Returns a DateTime from an RFC3339 string (with microsecond resolution)
        /// </summary>
        /// <param name="time"></param>
        private static DateTime GetDateTimeFromString(string time)
        {
            return DateTime.ParseExact(time, "yyyy-MM-dd'T'HH:mm:ss.000000'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Saves a list of downloaded bars in the requested output format
        /// </summary>
        /// <param name="bars"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        /// <param name="outputFormat"></param>
        private static void SaveBars(List<Candle> bars, string symbol, string date, string outputFormat)
        {
            switch (outputFormat)
            {
                // zipped CSV format
                case "lean":
                    {
                        // Write 5-second resolution bars to zip file
                        SaveSecondBars(bars, symbol, date);

                        // Aggregate to 1-minute resolution + write zip file
                        var barsMinute = AggregateBars(bars, new TimeSpan(0, 1, 0));
                        SaveMinuteBars(barsMinute, symbol, date);
                    }
                    break;

                default:
                    throw new NotSupportedException("Unsupported output format.");
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

            // File path: /Lean/Data/forex/oanda/daily/eurusd.zip -> eurusd.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "oanda",
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

            // File path: /Lean/Data/forex/oanda/daily/eurusd.zip -> eurusd.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "oanda",
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
        /// Saves a list of bars in 1-minute resolution in Lean format (zipped CSV)
        /// </summary>
        /// <param name="minuteBars"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        private static void SaveMinuteBars(List<LeanBar> minuteBars, string symbol, string date)
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

            // File path: /Lean/Data/forex/oanda/minute/eurusd/yyyymmdd_quote.zip -> yyyymmdd_eurusd_minute_quote.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "oanda",
                "minute",
                symbol.Replace("_", "").ToLower());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}_quote.zip", date.Replace("-", "")));
            var entryName = string.Format("{0}_{1}_minute_quote.csv", date.Replace("-", ""), symbol.Replace("_", "").ToLower());

            WriteZipFile(zipFileName, entryName, sb);
        }

        /// <summary>
        /// Saves a list of bars in 5-second resolution in Lean format (zipped CSV)
        /// </summary>
        /// <param name="secondBars"></param>
        /// <param name="symbol"></param>
        /// <param name="date"></param>
        private static void SaveSecondBars(List<Candle> secondBars, string symbol, string date)
        {
            var sb = new StringBuilder();
            foreach (var row in secondBars)
            {
                // convert datetime to millis
                var timestamp = (int)GetDateTimeFromString(row.time).TimeOfDay.TotalMilliseconds;

                sb.AppendLine(string.Join(",", timestamp, 
                    row.openMid.ToString(CultureInfo.InvariantCulture),
                    row.highMid.ToString(CultureInfo.InvariantCulture),
                    row.lowMid.ToString(CultureInfo.InvariantCulture),
                    row.closeMid.ToString(CultureInfo.InvariantCulture), 
                    row.volume));
            }

            // File path: /Lean/Data/forex/oanda/second/eurusd/yyyymmdd_quote.zip -> yyyymmdd_eurusd_second_quote.csv
            var path = Path.Combine(
                _settings.OutputFolder,
                _instruments[symbol].Type.ToString().ToLower(),
                "oanda",
                "second",
                symbol.Replace("_", "").ToLower());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var zipFileName = Path.Combine(path, string.Format("{0}_quote.zip", date.Replace("-", "")));
            var entryName = string.Format("{0}_{1}_second_quote.csv", date.Replace("-", ""), symbol.Replace("_", "").ToLower());

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
        /// Aggregates a list of 5-second bars at the requested resolution
        /// </summary>
        /// <param name="bars"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private static List<LeanBar> AggregateBars(List<Candle> bars, TimeSpan resolution)
        {
            return
                (from b in bars
                group b by GetDateTimeFromString(b.time).RoundDown(resolution)
                into g
                select new LeanBar
                {
                    Time = g.Key,
                    Open = g.First().openMid,
                    High = g.Max(b => b.highMid),
                    Low = g.Min(b => b.lowMid),
                    Close = g.Last().closeMid,
                    TickVolume = g.Sum(b => b.volume)
                }).ToList();
        }

        /// <summary>
        /// Downloads a block of 5-second bars from a starting datetime
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="start"></param>
        /// <param name="barsPerRequest"></param>
        /// <returns></returns>
        private static async Task<List<Candle>> DownloadBars(string symbol, string start, int barsPerRequest)
        {
            if (_settings.EnableTrace)
            {
                Console.WriteLine("Requesting {0} bars for {1} from {2}", barsPerRequest, symbol, start);
            }

            var request = new CandlesRequest
            {
                instrument = symbol,
                granularity = EGranularity.S5,
                candleFormat = ECandleFormat.midpoint,
                count = barsPerRequest,
                start = Uri.EscapeDataString(start)
            };
            var bars = await Rest.GetCandlesAsync(request);

            if (_settings.EnableTrace)
            {
                if (bars.Count > 0)
                    Console.WriteLine("Received {0} bars: {1} to {2}", bars.Count, bars[0].time, bars[bars.Count - 1].time);
                else
                    Console.WriteLine("Received 0 bars");
            }

            return bars;
        }

    }
}
