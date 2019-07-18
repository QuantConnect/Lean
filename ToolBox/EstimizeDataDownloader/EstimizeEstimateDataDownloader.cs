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

using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.EstimizeDataDownloader
{
    public class EstimizeEstimateDataDownloader : EstimizeDataDownloader
    {
        private readonly string _destinationFolder;

        /// <summary>
        /// Creates a new instance of <see cref="EstimizeEstimateDataDownloader"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        public EstimizeEstimateDataDownloader(string destinationFolder)
        {
            _destinationFolder = Path.Combine(destinationFolder, "estimate");
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
                var companies = GetCompanies().Result;
                var count = companies.Count;
                var currentPercent = 0.05;
                var percent = 0.05;
                var i = 0;

                Log.Trace($"EstimizeEstimateDataDownloader.Run(): Start processing {count} companies");

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

                    // Makes sure we don't overrun Estimize rate limits accidentally
                    IndexGate.WaitToProceed();

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

                                    SaveContentToZipFile(_destinationFolder, ticker, y.Result);

                                    var percentageDone = i / count;
                                    if (percentageDone >= currentPercent)
                                    {
                                        Log.Trace($"EstimizeEstimateDataDownloader.Run(): {percentageDone:P2} complete");
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

            Log.Trace($"EstimizeEstimateDataDownloader.Run(): Finished in {stopwatch.Elapsed}");
            return true;
        }
    }
}