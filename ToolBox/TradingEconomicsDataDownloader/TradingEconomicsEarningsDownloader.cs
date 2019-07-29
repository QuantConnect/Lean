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

using Newtonsoft.Json;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.TradingEconomicsDataDownloader
{
    /// <summary>
    /// Trading Economics Earnings Downloader class (only earnings for United States)
    /// </summary>
    public class TradingEconomicsEarningsDownloader : TradingEconomicsDataDownloader
    {
        private readonly MapFileResolver _mapFileResolver;
        private readonly string _destinationFolder;
        private readonly RateGate _requestGate;
        private readonly DateTime _fromDate;
        private readonly DateTime _toDate;

        public TradingEconomicsEarningsDownloader(string destinationFolder)
        {
            _fromDate = new DateTime(1998, 1, 1);
            _toDate = DateTime.Now;
            _destinationFolder = Path.Combine(destinationFolder, "earnings");
            _requestGate = new RateGate(1, TimeSpan.FromSeconds(1));
            _mapFileResolver = MapFileResolver.Create(Globals.DataFolder, Market.USA);

            Directory.CreateDirectory(_destinationFolder);
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public override bool Run()
        {
            Log.Trace("TradingEconoimcsEarningsDownloader.Run(): Begin downloading earnings data");

            var stopwatch = Stopwatch.StartNew();
            var data = new List<TradingEconomicsEarnings>();

            var startUtc = _fromDate;
            while (startUtc < _toDate)
            {
                try
                {
                    var endUtc = startUtc.AddMonths(1).AddDays(-1);

                    Log.Trace($"TradingEconomicsEarningsDownloader.Run(): Collecting earnings data from {startUtc:yyyy-MM-dd} to {endUtc:yyyy-MM-dd}");

                    _requestGate.WaitToProceed(TimeSpan.FromSeconds(1));

                    var content = Get(startUtc, endUtc).Result;
                    var collection = JsonConvert.DeserializeObject<List<TradingEconomicsEarnings>>(content);

                    data.AddRange(collection);

                    startUtc = startUtc.AddMonths(1);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"TradingEconomicsEarningsDownloader.Run(): Error parsing data for date {startUtc:yyyyMMdd}");
                    return false;
                }
            }

            Log.Trace($"TradingEconomicsEarningsDownloader.Run(): {data.Count} earnings entries read in {stopwatch.Elapsed}");

            foreach (var kvp in data.GroupBy(GetMappedSymbol))
            {
                var ticker = kvp.Key;

                // We return string.Empty if we fail to resolve a map file or a symbol inside a map file
                if (string.IsNullOrEmpty(ticker))
                {
                    continue;
                }

                // Create the destination directory, otherwise we risk having it fail when we move
                // the temp file to its final destination
                Directory.CreateDirectory(Path.Combine(_destinationFolder, kvp.Key));

                foreach (var earningsDataByDate in kvp.GroupBy(x => x.LastUpdate.Date))
                {
                    var date = earningsDataByDate.Key.ToStringInvariant("yyyyMMdd");
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
                    var tempZipPath = tempPath.Replace(".json", ".zip");
                    var finalZipPath = Path.Combine(_destinationFolder, kvp.Key, $"{date}.zip");

                    try
                    {
                        var contents = JsonConvert.SerializeObject(earningsDataByDate.ToList());
                        Log.Trace($"TradingEconomicsEarningsDownloader.Run(): {date} - Writing file before compression: {tempPath}");
                        File.WriteAllText(tempPath, contents);

                        Log.Trace($"TradingEconomicsEarningsDownloader.Run(): {date} - Compressing to: {tempZipPath}");
                        // Write out this data string to a zip file
                        Compression.Zip(tempPath, tempZipPath, $"{date}.json", true);

                        if (File.Exists(finalZipPath))
                        {
                            Log.Trace($"TradingEconomicsEarningsDownloader.Run(): {date} - Deleting existing file: {finalZipPath}");
                            File.Delete(finalZipPath);
                        }

                        Log.Trace($"TradingEconomicsEarningsDownloader.Run(): {date} - Moving temp file: {tempZipPath} to {finalZipPath}");
                        File.Move(tempZipPath, finalZipPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"TradingEconomicsEarningsDownloader.Run(): {date} - Error creating zip file for ticker: {ticker}");
                        return false;
                    }
                }
            }

            Log.Trace($"TradingEconomicsEarningsDownloader.Run(): Finished in {stopwatch.Elapsed}");
            return true;
        }

        /// <summary>
        /// Get Trading Economics Earnings data for a given this start and end times(in UTC).
        /// </summary>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>String representing data for this date range</returns>
        public override Task<string> Get(DateTime startUtc, DateTime endUtc)
        {
            var url = $"/earnings/country/united states/?type=earnings&d1={startUtc:yyyy-MM-dd}&d2={endUtc:yyyy-MM-dd}";
            return HttpRequester(url);
        }

        /// <summary>
        /// Gets the ticker using map files. If the ticker is empty, we can't resolve a map file, or we can't
        /// resolve a ticker within a map file, we return null
        /// </summary>
        /// <param name="tradingEconomicsEarnings">TE Earnings data</param>
        /// <returns>Mapped ticker or null</returns>
        private string GetMappedSymbol(TradingEconomicsEarnings tradingEconomicsEarnings)
        {
            var ticker = tradingEconomicsEarnings.Symbol;
            var mapFile = _mapFileResolver.ResolveMapFile(ticker, tradingEconomicsEarnings.LastUpdate);

            if (!mapFile.Any())
            {
                Log.Error($"TradingEconomicsEarningsDownloader.GetMappedSymbol(): No mapfile found for ticker {ticker}");
                return string.Empty;
            }

            var symbol = mapFile.GetMappedSymbol(tradingEconomicsEarnings.LastUpdate);

            if (string.IsNullOrEmpty(symbol))
            {
                Log.Error($"TradingEconomicsEarningsDownloader.GetMappedSymbol(): No mapped symbol found for ticker {ticker}");
                return string.Empty;
            }

            return symbol.ToLowerInvariant();
        }
    }
}