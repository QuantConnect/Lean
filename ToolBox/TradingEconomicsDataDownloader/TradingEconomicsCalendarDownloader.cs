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

using QuantConnect.Configuration;
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
            _fromDate = new DateTime(2000, 1, 1);
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
            var data = new List<TradingEconomicsCalendar>();
            var startUtc = _fromDate;

            while (startUtc < _toDate)
            {
                var endUtc = startUtc.AddMonths(1).AddDays(-1);

                Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Collecting calendar data from {startUtc:yyyy-MM-dd} to {endUtc:yyyy-MM-dd}");

                try
                {
                    _requestGate.WaitToProceed(TimeSpan.FromSeconds(1));

                    if (_supportedCountries.IsNullOrEmpty())
                    {
                        var content = Get(startUtc, endUtc).Result;
                        data.AddRange(TradingEconomicsCalendar.ProcessAPIResponse(content));
                    }
                    else
                    {
                        foreach (var supportedCountry in _supportedCountries)
                        {
                            Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Collecting calendar data for {supportedCountry}...");
                            var content = Get(supportedCountry.ToLowerInvariant(), startUtc, endUtc).Result;
                            data.AddRange(TradingEconomicsCalendar.ProcessAPIResponse(content));
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

            var status = ProcessData(data);

            Log.Trace($"TradingEconomicsCalendarDownloader.Run(): Finished in {stopwatch.Elapsed}");
            return status;
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
                data.GroupBy(x => x.Country.ToLowerInvariant()),
                (kvp, state) =>
                {
                    var tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv"));
                    var finalTempBackup = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv"));
                    var finalFile = new FileInfo(Path.Combine(Globals.DataFolder, "alternative", "trading-economics", "calendar", $"{kvp.Key.Replace(" ", "")}.csv"));

                    try
                    {
                        Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): Writing new contents to temporary file: {tempFile.FullName}");
                        File.WriteAllLines(tempFile.FullName, kvp.OrderBy(x => x.EndTime).Select(x => x.ToCsv()));
                        tempFile.Refresh();

                        // TODO: Maybe move this into another method? We need the same logic for the TE code file
                        if (!finalFile.Exists)
                        {
                            Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): Creating new file - Moving temp file: {tempFile.FullName} - to: {finalFile.FullName}");
                            File.Move(tempFile.FullName, finalFile.FullName);
                            return;
                        }

                        Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): Moving existing file: {finalFile.FullName} - to backup path: {finalTempBackup.FullName}");
                        File.Move(finalFile.FullName, finalTempBackup.FullName);
                        Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): Moving temp file: {tempFile.FullName} - to final path: {finalFile.FullName}");
                        File.Move(tempFile.FullName, finalFile.FullName);

                        finalFile.Refresh();
                        finalTempBackup.Refresh();

                        // Stop immediately if something beyond our control has occurred.
                        if (!finalFile.Exists)
                        {
                            throw new Exception($"{finalFile.FullName} does not exist.");
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, $"TradingEconomicsCalendarDownloader.ProcessData(): Error creating data file for {kvp.Key}, attempting to recover backup: {finalTempBackup.FullName}");

                        finalTempBackup.Refresh();
                        if (finalTempBackup.Exists)
                        {
                            try
                            {
                                finalTempBackup.MoveTo(finalFile.FullName);
                                Log.Trace($"TradingEconomicsCalendarDownloader.ProcessData(): Successfully recovered backup to: {finalFile.FullName}");
                            }
                            catch (Exception internalErr)
                            {
                                Log.Error(internalErr, $"TradingEconomicsCalendarDownloader.ProcessData(): Could not recover backup from: {finalTempBackup.FullName} to {finalFile.FullName}");
                            }
                        }

                        status = false;
                        state.Stop();
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
    }
}