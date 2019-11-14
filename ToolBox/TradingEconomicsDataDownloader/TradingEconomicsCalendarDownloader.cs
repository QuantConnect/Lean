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
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QuantConnect.Configuration;

namespace QuantConnect.ToolBox.TradingEconomicsDataDownloader
{
    /// <summary>
    /// Trading Economics Calendar Downloader class
    /// </summary>
    public class TradingEconomicsCalendarDownloader : TradingEconomicsDataDownloader
    {
        private readonly string _destinationFolder;
        private readonly DateTime _fromDate;
        private readonly DateTime _toDate;
        private readonly RateGate _requestGate;
        private readonly IEnumerable<string> _supportedCountries;

        public TradingEconomicsCalendarDownloader(string destinationFolder)
        {
            _fromDate = new DateTime(2000, 10, 1);
            _toDate = DateTime.Now;
            _destinationFolder = Path.Combine(destinationFolder, "calendar");
            // Rate limits on Trading Economics is one request per second
            _requestGate = new RateGate(1, TimeSpan.FromSeconds(1));

            _supportedCountries = Config.Get("trading-economics-supported-countries").Split(',');

            // Create the destination directory so that we don't error out in case there's no data
            Directory.CreateDirectory(_destinationFolder);
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public override bool Run()
        {
            Log.Trace("TradingEconomicsCalendarDownloader.Run(): Begin downloading calendar data");

            var stopwatch = Stopwatch.StartNew();

            var availableFiles = Directory.GetFiles(_destinationFolder, "*.zip", SearchOption.AllDirectories)
                .Select(
                    x =>
                    {
                        try
                        {
                            return Parse.DateTimeExact(Path.GetFileName(x).Substring(0, 8), "yyyyMMdd");
                        }
                        catch
                        {
                            return DateTime.MinValue;
                        }
                    }
                )
                .Where(x => x != DateTime.MinValue)
                .ToHashSet();

            var data = new List<TradingEconomicsCalendar>();
            var startUtc = _fromDate;
            while (startUtc < _toDate)
            {
                var endUtc = startUtc.AddMonths(1).AddDays(-1);

                Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Collecting calendar data from {startUtc:yyyy-MM-dd} to {endUtc:yyyy-MM-dd}");

                if (availableFiles.Contains(endUtc))
                {
                    Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Skipping data because it already exists for month: {startUtc:MMMM}");
                    startUtc = startUtc.AddMonths(1);
                    continue;
                }

                try
                {
                    _requestGate.WaitToProceed(TimeSpan.FromSeconds(1));

                    if (_supportedCountries.IsNullOrEmpty())
                    {
                        var content = Get(startUtc, endUtc).Result;
                        data.AddRange(ProcessRawContent(content));
                    }
                    else
                    {
                        foreach (var supportedCountry in _supportedCountries)
                        {
                            Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Collecting calendar data for {supportedCountry}...");
                            var content = Get(supportedCountry.ToLowerInvariant(), startUtc, endUtc).Result;
                            data.AddRange(ProcessRawContent(content));
                            _requestGate.WaitToProceed(TimeSpan.FromSeconds(1));
                        }
                    }

                    startUtc = startUtc.AddMonths(1);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"TradingEconomicsCalendarDownloader.Run(): Error parsing data for date {startUtc.ToStringInvariant("yyyyMMdd")}");
                    return false;
                }
            }

            Log.Trace($"TradingEconomicsCalendarDownloader.Run(): {data.Count} calendar entries read in {stopwatch.Elapsed}");

            // Return status code. We default to `true` so that we can identify if an error occured during the loop
            var status = ProcessData(data);

            Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Finished in {stopwatch.Elapsed}");
            return status;
        }

        private static List<TradingEconomicsCalendar> ProcessRawContent(string content)
        {
            var rawCollection = JsonConvert.DeserializeObject<JArray>(content);

            foreach (var rawData in rawCollection)
            {
                var inPercentage = rawData["Actual"].Value<string>().Contains("%");

                rawData["IsPercentage"] = inPercentage;
                rawData["Actual"] = ParseDecimal(rawData["Actual"].Value<string>(), inPercentage);
                rawData["Previous"] = ParseDecimal(rawData["Previous"].Value<string>(), inPercentage);
                rawData["Forecast"] = ParseDecimal(rawData["Forecast"].Value<string>(), inPercentage);
                rawData["TEForecast"] = ParseDecimal(rawData["TEForecast"].Value<string>(), inPercentage);
                rawData["Revised"] = ParseDecimal(rawData["Revised"].Value<string>(), inPercentage);
            }

            var collection = rawCollection.ToObject<List<TradingEconomicsCalendar>>();

            // Only write data that contains the "actual" field so that we get the final
            // piece of unchanging data in order to maintain backwards consistency with
            // the given data since we can't get historical snapshots of the data
            var onlyActual = collection
                .Where(x => x.Actual.HasValue)
                .ToList();

            var totalFiltered = collection.Count - onlyActual.Count;

            if (totalFiltered != 0)
            {
                Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Filtering {totalFiltered}/{collection.Count} entries because they contain no 'actual' field");
            }

            return onlyActual;
        }

        /// <summary>
        /// Processes the downloaded data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private bool ProcessData(List<TradingEconomicsCalendar> data)
        {
            var status = true;

            Parallel.ForEach(
                data.GroupBy(x => GetTicker(x.Ticker, x.Category, x.Country)),
                (kvp, state) =>
                {
                    // Create the destination directory, otherwise we risk having it fail when we move
                    // the temp file to its final destination
                    Directory.CreateDirectory(Path.Combine(_destinationFolder, kvp.Key));

                    foreach (var calendarDataByDate in kvp.GroupBy(x => x.LastUpdate.Date))
                    {
                        var date = calendarDataByDate.Key.ToStringInvariant("yyyyMMdd");
                        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToStringInvariant(null)}.json");
                        var tempZipPath = tempPath.Replace(".json", ".zip");
                        var finalZipPath = Path.Combine(_destinationFolder, kvp.Key, $"{date}.zip");
                        var dataFolderZipPath = Path.Combine(Globals.DataFolder, "alternative", "trading-economics", "calendar", kvp.Key, $"{date}.zip");

                        if (File.Exists(finalZipPath))
                        {
                            Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): {date} - Skipping file because it already exists: {finalZipPath}");
                            continue;
                        }

                        if (File.Exists(dataFolderZipPath))
                        {
                            Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): {date} - Skipping file because it already exists: {dataFolderZipPath}");
                            continue;
                        }

                        try
                        {
                            var contents = JsonConvert.SerializeObject(calendarDataByDate.ToList());
                            Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): {date} - Writing file before compression: {tempPath}");
                            File.WriteAllText(tempPath, contents);

                            Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): {date} - Compressing to: {tempZipPath}");
                            // Write out this data string to a zip file
                            Compression.Zip(tempPath, tempZipPath, $"{date}.json", true);

                            Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): {date} - Moving temp file: {tempZipPath} to {finalZipPath}");
                            File.Move(tempZipPath, finalZipPath);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, $"TradingEconomicsCalendarDownloader.ProcessData(): {date} - Error creating zip file for ticker: {kvp.Key}");
                            status = false;
                            state.Stop();
                        }
                    }
                }
            );
            return status;
        }

        /// <summary>
        /// Get Trading Economics Calendar data for a given this start and end times(in UTC).
        /// </summary>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>String representing data for this date range</returns>
        public override Task<string> Get(DateTime startUtc, DateTime endUtc)
        {
            var url = $"/calendar/country/all/{startUtc.ToStringInvariant("yyyy-MM-dd")}/{endUtc.ToStringInvariant("yyyy-MM-dd")}";
            return HttpRequester(url);
        }

        /// <summary>
        /// Get Trading Economics Calendar data for a given this start and end times(in UTC).
        /// </summary>
        /// <param name="country"></param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>String representing data for this date range</returns>
        public Task<string> Get(string country, DateTime startUtc, DateTime endUtc)
        {
            var url = $"/calendar/country/{country}/{startUtc.ToStringInvariant("yyyy-MM-dd")}/{endUtc.ToStringInvariant("yyyy-MM-dd")}";
            return HttpRequester(url);
        }

        /// <summary>
        /// Parse decimal from calendar data
        /// </summary>
        /// <param name="value">Value to parse</param>
        /// <returns>Nullable decimal</returns>
        /// <remarks>Will be null when we can't parse the data reliably</remarks>
        public static decimal? ParseDecimal(string value, bool inPercent)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // Remove dollar signs from values
            // Remove (P) and (R) from values
            // Edge cases: values are reported as XYZ.5.1B, -4-XYZ
            var newFigure = value.Replace("$", "")
                .Replace("(P)", "")
                .Replace("(R)", "")
                .Replace("--", "-")
                .Replace(".5.1", ".5")
                .Replace("-1-", "-")
                .Replace("-2-", "-")
                .Replace("-3-", "-")
                .Replace("-4-", "-")
                .Replace("-5-", "-")
                .Replace("-6-", "-")
                .Replace("-7-", "-")
                .Replace("-8-", "-")
                .Replace("-9-", "-");

            if (newFigure.EndsWith("."))
            {
                newFigure = newFigure.Substring(0, newFigure.Length - 1);
            }

            var inTrillions = newFigure.EndsWith("T");
            var inBillions = newFigure.EndsWith("B");
            var inMillions = newFigure.EndsWith("M");
            var inThousands = newFigure.EndsWith("K");

            // Finally, remove any alphabetical characters from the string before we parse
            newFigure = Regex.Replace(newFigure, "[^0-9.+-]", "");

            while (Regex.IsMatch(newFigure, @"(\.[0-9]+\.)"))
            {
                newFigure = newFigure.Substring(0, newFigure.Length - 1);
            }

            if (string.IsNullOrWhiteSpace(newFigure))
            {
                // U.S. Presidential election is unparsable as decimal.
                // Other events similar to it might exist as well.
                return null;
            }

            // Return null If we can't parse the result as is
            decimal finalFigure;
            if (!decimal.TryParse(newFigure, NumberStyles.Any, CultureInfo.InvariantCulture, out finalFigure))
            {
                Log.Error($"TradingEconomicsCalendarDownloader.ParseDecimal(): Failed to parse the figure {value}. Final form before parsing: {newFigure}");
                return null;
            }

            if (inPercent)
            {
                return finalFigure / 100m;
            }
            if (inTrillions)
            {
                return finalFigure * 1000000000000m;
            }
            if (inBillions)
            {
                return finalFigure * 1000000000m;
            }
            if (inMillions)
            {
                return finalFigure * 1000000m;
            }
            if (inThousands)
            {
                return finalFigure * 1000m;
            }

            return finalFigure;
        }
    }
}