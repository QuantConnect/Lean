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
    public class EstimizeReleaseDataDownloader : EstimizeDataDownloader
    {
        private readonly string _destinationFolder;
        private readonly MapFileResolver _mapFileResolver;

        /// <summary>
        /// Creates a new instance of <see cref="EstimizeReleaseDataDownloader"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        public EstimizeReleaseDataDownloader(string destinationFolder)
        {
            _destinationFolder = Path.Combine(destinationFolder, "release");
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

                Log.Trace($"EstimizeReleaseDataDownloader.Run(): Start processing {count} companies");

                var tasks = new List<Task>();

                foreach (var company in companies)
                {

                    try
                    {
                        // Makes sure we don't overrun Estimize rate limits accidentally
                        IndexGate.WaitToProceed();
                    }
                    // This is super super rare, but it failures in RateGate (RG) can still happen nonetheless. Let's not
                    // rely on RG operating successfully all the time so that if RG fails, our download process doesn't fail
                    catch (ArgumentOutOfRangeException e)
                    {
                        Log.Error(e, $"EstimizeReleaseDataDownloader.Run(): RateGate failed. Sleeping for 110 milliseconds with Thread.Sleep()");
                        Thread.Sleep(110);
                    }

                    var ticker = company.Ticker;
                    if (ticker.IndexOf("defunct", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        var length = ticker.IndexOf('-');
                        ticker = ticker.Substring(0, length).Trim();
                    }

                    Log.Trace($"EstimizeReleaseDataDownloader.Run(): Processing {ticker}");

                    tasks.Add(
                        HttpRequester($"/companies/{ticker}/releases")
                            .ContinueWith(
                                y =>
                                {
                                    i++;

                                    if (y.IsFaulted)
                                    {
                                        Log.Error($"EstimizeReleaseDataDownloader.Run(): Failed to get data for {company}");
                                        return;
                                    }

                                    var result = y.Result;
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        // We've already logged inside HttpRequester
                                        return;
                                    }

                                    // Just like TradingEconomics, we only want the events that already occured
                                    // instead of having "forecasts" that will change in the future taint our
                                    // data and make backtests non-deterministic. We want to have
                                    // consistency with our data in live trading historical requests as well
                                    var releases = JsonConvert.DeserializeObject<List<EstimizeRelease>>(result)
                                        .Where(x => x.Eps != null)
                                        .GroupBy(x =>
                                        {
                                            var releaseDate = x.ReleaseDate;

                                            try
                                            {
                                                var mapFile = _mapFileResolver.ResolveMapFile(ticker, releaseDate);
                                                var oldTicker = ticker;
                                                var newTicker = ticker;

                                                // Ensure we're writing to the correct historical ticker
                                                if (!mapFile.Any())
                                                {
                                                    Log.Trace($"EstimizeReleaseDataDownloader.Run(): Failed to find map file for: {newTicker} - on: {releaseDate}");
                                                    return string.Empty;
                                                }

                                                newTicker = mapFile.GetMappedSymbol(releaseDate);
                                                if (string.IsNullOrWhiteSpace(newTicker))
                                                {
                                                    Log.Trace($"EstimizeReleaseDataDownloader.Run(): Failed to find mapping for null new ticker. Old ticker: {oldTicker} - on: {releaseDate}");
                                                    return string.Empty;
                                                }

                                                if (oldTicker != newTicker)
                                                {
                                                    Log.Trace($"EstimizeReleaseDataDownloader.Run(): Remapped from {oldTicker} to {newTicker} for {releaseDate}");
                                                }

                                                return newTicker;
                                            }
                                            // We get a failure inside the map file constructor rarely. It tries
                                            // to access the last element of an empty list. Maybe this is a bug?
                                            catch (InvalidOperationException e)
                                            {
                                                Log.Error(e, $"EstimizeReleaseDataDownloader.Run(): Failed to load map file for: {ticker} - on: {releaseDate}");
                                                return string.Empty;
                                            }
                                        })
                                        .Where(x => !string.IsNullOrEmpty(x.Key));

                                    foreach (var kvp in releases)
                                    {
                                        var csvContents = kvp.Select(x => $"{x.ReleaseDate.ToUniversalTime():yyyyMMdd HH:mm:ss},{x.Id},{x.FiscalYear},{x.FiscalQuarter},{x.Eps},{x.Revenue},{x.ConsensusEpsEstimate},{x.ConsensusRevenueEstimate},{x.WallStreetEpsEstimate},{x.WallStreetRevenueEstimate},{x.ConsensusWeightedEpsEstimate},{x.ConsensusWeightedRevenueEstimate}");
                                        SaveContentToFile(_destinationFolder, kvp.Key, csvContents);
                                    }

                                    var percentDone = i / count;
                                    if (percentDone >= currentPercent)
                                    {
                                        Log.Trace($"EstimizeEstimateDataDownloader.Run(): {percentDone:P2} complete");
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

            Log.Trace($"EstimizeReleaseDataDownloader.Run(): Finished in {stopwatch.Elapsed}");
            return true;
        }
    }
}