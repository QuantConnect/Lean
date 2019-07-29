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
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.Estimize;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.EstimizeDataDownloader
{
    public class EstimizeEstimateDataDownloader : EstimizeDataDownloader
    {
        private readonly string _destinationFolder;
        private readonly MapFileResolver _mapFileResolver;

        /// <summary>
        /// Creates a new instance of <see cref="EstimizeEstimateDataDownloader"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        public EstimizeEstimateDataDownloader(string destinationFolder)
        {
            _destinationFolder = Path.Combine(destinationFolder, "estimate");
            _mapFileResolver = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"))
                .Get(Market.USA);

            Directory.CreateDirectory(_destinationFolder);
        }

        /// <summary>
        /// Runs the instance of the object.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public override bool Run()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var companies = GetCompanies().Result.DistinctBy(x => x.Ticker).ToList();
                var count = companies.Count;
                var currentPercent = 0.05;
                var percent = 0.05;
                var i = 0;

                Log.Trace($"EstimizeEstimateDataDownloader.Run(): Start processing {count.ToStringInvariant()} companies");

                var tasks = new List<Task>();

                foreach (var company in companies)
                {
                    var ticker = company.Ticker;

                    // Include tickers that are "defunct".
                    // Remove the tag because it cannot be part of the API endpoint
                    if (ticker.IndexOf("defunct", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        var length = ticker.IndexOf('-');
                        ticker = ticker.Substring(0, length).Trim();
                    }

                    Log.Trace($"EstimizeEstimateDataDownloader.Run(): Processing {ticker}");

                    try
                    {
                        // Makes sure we don't overrun Estimize rate limits accidentally
                        IndexGate.WaitToProceed();
                    }
                    // This is super super rare, but it failures in RateGate (RG) can still happen nonetheless. Let's not
                    // rely on RG operating successfully all the time so that if RG fails, our download process doesn't fail
                    catch (ArgumentOutOfRangeException e)
                    {
                        Log.Error(e, $"EstimizeEstimateDataDownloader.Run(): RateGate failed. Sleeping for 110 milliseconds with Thread.Sleep()");
                        Thread.Sleep(110);
                    }

                    tasks.Add(
                        HttpRequester($"/companies/{ticker}/estimates")
                            .ContinueWith(
                                y =>
                                {
                                    i++;

                                    if (y.IsFaulted)
                                    {
                                        Log.Error($"EstimizeEstimateDataDownloader.Run(): Failed to get data for {company}");
                                        return;
                                    }

                                    var result = y.Result;
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        // We've already logged inside HttpRequester
                                        return;
                                    }

                                    var estimates = JsonConvert.DeserializeObject<List<EstimizeEstimate>>(result)
                                        .GroupBy(estimate =>
                                        {
                                            var oldTicker = ticker;
                                            var newTicker = ticker;
                                            var createdAt = estimate.CreatedAt;

                                            try
                                            {
                                                var mapFile = _mapFileResolver.ResolveMapFile(ticker, createdAt);

                                                // Ensure we're writing to the correct historical ticker
                                                if (!mapFile.Any())
                                                {
                                                    Log.Trace($"EstimizeEstimateDataDownloader.Run(): Failed to find map file for: {newTicker} - on: {createdAt}");
                                                    return string.Empty;
                                                }

                                                newTicker = mapFile.GetMappedSymbol(createdAt);
                                                if (string.IsNullOrWhiteSpace(newTicker))
                                                {
                                                    Log.Trace($"EstimizeEstimateDataDownloader.Run(): New ticker is null. Old ticker: {oldTicker} - on: {createdAt.ToStringInvariant()}");
                                                    return string.Empty;
                                                }

                                                if (oldTicker != newTicker)
                                                {
                                                    Log.Trace($"EstimizeEstimateDataDonwloader.Run(): Remapping {oldTicker} to {newTicker}");
                                                }
                                            }
                                            // We get a failure inside the map file constructor rarely. It tries
                                            // to access the last element of an empty list. Maybe this is a bug?
                                            catch (InvalidOperationException e)
                                            {
                                                Log.Error(e, $"EstimizeEstimateDataDownloader.Run(): Failed to load map file for: {oldTicker} - on {createdAt}");
                                                return string.Empty;
                                            }

                                            return newTicker;
                                        })
                                        .Where(kvp => !string.IsNullOrEmpty(kvp.Key));

                                    foreach (var kvp in estimates)
                                    {
                                        var csvContents = kvp.Select(x =>
                                            $"{x.CreatedAt.ToStringInvariant("yyyyMMdd HH:mm:ss")}," +
                                            $"{x.Id}," +
                                            $"{x.AnalystId}," +
                                            $"{x.UserName}," +
                                            $"{x.FiscalYear.ToStringInvariant()}," +
                                            $"{x.FiscalQuarter.ToStringInvariant()}," +
                                            $"{x.Eps.ToStringInvariant()}," +
                                            $"{x.Revenue.ToStringInvariant()}," +
                                            $"{x.Flagged.ToStringInvariant().ToLowerInvariant()}"
                                        );
                                        SaveContentToFile(_destinationFolder, kvp.Key, csvContents);
                                    }

                                    var percentageDone = i / count;
                                    if (percentageDone >= currentPercent)
                                    {
                                        Log.Trace($"EstimizeEstimateDataDownloader.Run(): {percentageDone.ToStringInvariant("P2")} complete");
                                        currentPercent += percent;
                                    }
                                }
                            )
                    );
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }

            Log.Trace($"EstimizeEstimateDataDownloader.Run(): Finished in {stopwatch.Elapsed.ToStringInvariant(null)}");
            return true;
        }
    }
}