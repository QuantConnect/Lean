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
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.Estimize;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using Type = QuantConnect.Data.Custom.Estimize.Type;
using System.Threading;

namespace QuantConnect.ToolBox.EstimizeDataDownloader
{
    public class EstimizeConsensusDataDownloader : EstimizeDataDownloader
    {
        private readonly string[] _releaseFiles;
        private readonly string _destinationFolder;
        private readonly MapFileResolver _mapFileResolver;

        /// <summary>
        /// Creates a new instance of <see cref="EstimizeEstimateDataDownloader"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        public EstimizeConsensusDataDownloader(string destinationFolder)
        {
            _mapFileResolver = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"))
                .Get(Market.USA);

            var path = Path.Combine(destinationFolder, "release");
            Directory.CreateDirectory(path);
            _releaseFiles = Directory.GetFiles(path);

            _destinationFolder = Path.Combine(destinationFolder, "consensus");
            Directory.CreateDirectory(_destinationFolder);
        }

        /// <summary>
        /// Runs the instance of the object.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public override bool Run()
        {
            try
            {
                if (_releaseFiles.Length == 0)
                {
                    Log.Trace($"EstimizeConsensusDataDownloader.Run(): No files found. Please run EstimizeEstimateDataDownloader first");
                    return false;
                }

                var utcNow = DateTime.UtcNow;

                foreach (var releaseFile in _releaseFiles)
                {
                    Log.Trace($"EstimizeConsensusDataDownloader.Run(): Processing release file: {releaseFile}");
                    var stopwatch = Stopwatch.StartNew();
                    var tasks = new List<Task<List<EstimizeConsensus>>>();

                    // We don't need to apply any sort of mapfile transformations to the ticker
                    // since we've already applied mapping to the release file ticker
                    var ticker = Path.GetFileNameWithoutExtension(releaseFile) ?? string.Empty;
                    var releases = File.ReadAllLines(releaseFile).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => new EstimizeRelease(x));
                    var finalPath = Path.Combine(_destinationFolder, $"{ticker}.csv");
                    var existingConsensus = (File.Exists(finalPath) ? File.ReadAllLines(finalPath) : new string[] { })
                        .Select(x => new EstimizeConsensus(x));

                    foreach (var release in releases)
                    {
                        // We detect duplicates by checking for release IDs that match consensus IDs
                        // in consensus files and ensuring that no more updates will be published to
                        // consensus data by making sure the release has been made public
                        if ((utcNow - release.ReleaseDate).TotalDays > 1 && existingConsensus.Where(x => x.Id == release.Id).Any())
                        {
                            Log.Trace($"EstimizeConsensusDataDownloader.Run(): Duplicate entry found for ID {release.Id} in {finalPath} on: {release.ReleaseDate}");
                            continue;
                        }

                        Log.Trace($"EstimizeConsensusDataDownloader.Run(): Earnings release: {release.ReleaseDate:yyyy-MM-dd} - Parsing Estimate {release.Id} for: {ticker}");

                        try
                        {
                            // Makes sure we don't overrun Estimize rate limits accidentally
                            IndexGate.WaitToProceed();
                        }
                        // This is super super rare, but it failures in RateGate (RG) can still happen nonetheless. Let's not
                        // rely on RG operating successfully all the time so that if RG fails, our download process doesn't fail
                        catch (ArgumentOutOfRangeException e)
                        {
                            Log.Error(e, $"EstimizeConsensusDataDownloader.Run(): RateGate failed. Sleeping for 110 milliseconds with Thread.Sleep()");
                            Thread.Sleep(110);
                        }

                        tasks.Add(
                            HttpRequester($"/releases/{release.Id}/consensus")
                                .ContinueWith(
                                    x =>
                                    {
                                        var result = x.Result;
                                        if (string.IsNullOrEmpty(result))
                                        {
                                            return new List<EstimizeConsensus>();
                                        }

                                        var jObject = JObject.Parse(result);

                                        var list = new List<EstimizeConsensus>();

                                        list.AddRange(Unpack(release, Source.WallStreet, Type.Eps, jObject));
                                        list.AddRange(Unpack(release, Source.WallStreet, Type.Revenue, jObject));
                                        list.AddRange(Unpack(release, Source.Estimize, Type.Eps, jObject));
                                        list.AddRange(Unpack(release, Source.Estimize, Type.Revenue, jObject));

                                        return list;
                                    }
                                )
                        );
                    }

                    Task.WaitAll(tasks.ToArray());

                    var csvContents = tasks.SelectMany(x => x.Result)
                        .OrderBy(x => x.UpdatedAt)
                        .Select(x => $"{x.UpdatedAt.ToUniversalTime():yyyyMMdd HH:mm:ss},{x.Id},{x.Source},{x.Type},{x.Mean},{x.High},{x.Low},{x.StandardDeviation},{x.FiscalYear},{x.FiscalQuarter},{x.Count}");

                    SaveContentToFile(_destinationFolder, ticker, csvContents);

                    Log.Trace($"EstimizeConsensusDataDownloader.Run(): EstimizeConsensus files for {ticker} created : {stopwatch.Elapsed}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "EstimizeConsensusDataDownloader.Run(): Failure in consensus download");
                return false;
            }

            return true;
        }

        private IEnumerable<EstimizeConsensus> Unpack(EstimizeRelease estimizeEstimate, Source source, Type type, JObject jObject)
        {
            var jToken = jObject[source.ToLower()][type.ToLower()];
            var revisionsJToken = jToken["revisions"];

            var consensuses = revisionsJToken == null
                ? new List<EstimizeConsensus>()
                : JsonConvert.DeserializeObject<List<EstimizeConsensus>>(revisionsJToken.ToString());

            consensuses.Add(JsonConvert.DeserializeObject<EstimizeConsensus>(jToken.ToString()));

            foreach (var consensus in consensuses)
            {
                consensus.Id = estimizeEstimate.Id;
                consensus.FiscalYear = estimizeEstimate.FiscalYear;
                consensus.FiscalQuarter = estimizeEstimate.FiscalQuarter;
                consensus.Source = source;
                consensus.Type = type;
            }

            return consensuses.Where(x => x.UpdatedAt > DateTime.MinValue);
        }
    }
}