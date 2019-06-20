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
using QuantConnect.Data.Custom.Estimize;
using Type = QuantConnect.Data.Custom.Estimize.Type;

namespace QuantConnect.ToolBox.EstimizeDataDownloader
{
    public class EstimizeConsensusDataDownloader : EstimizeDataDownloader
    {
        private readonly string[] _zipFiles;
        private readonly string _destinationFolder;

        /// <summary>
        /// Creates a new instance of <see cref="EstimizeEstimateDataDownloader"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        public EstimizeConsensusDataDownloader(string destinationFolder)
        {
            var path = Path.Combine(destinationFolder, "release");
            if (Directory.Exists(path))
            {
                _zipFiles = Directory.GetFiles(path);
            }

            _destinationFolder = Path.Combine(destinationFolder, "consensus");
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
                if (_zipFiles.Length == 0)
                {
                    Log.Trace($"EstimizeConsensusDataDownloader.Run(): No files found. Please run EstimizeEstimateDataDownloader first");
                    return false;
                }

                foreach (var zipFile in _zipFiles)
                {
                    var tasks = new List<Task<List<EstimizeConsensus>>>();

                    foreach (var kvp in Compression.Unzip(zipFile))
                    {
                        var content = kvp.Value.SingleOrDefault();

                        var estimates = JsonConvert.DeserializeObject<List<EstimizeEstimate>>(content);

                        foreach (var estimate in estimates)
                        {
                            // Makes sure we don't overrun Estimize rate limits accidentally
                            IndexGate.WaitToProceed();

                            tasks.Add(
                                HttpRequester($"/releases/{estimate.Id}/consensus")
                                    .ContinueWith(
                                        x =>
                                        {
                                            var jObject = JObject.Parse(x.Result);

                                            var list = new List<EstimizeConsensus>();

                                            list.AddRange(Unpack(estimate, Source.WallStreet, Type.Eps, jObject));
                                            list.AddRange(Unpack(estimate, Source.WallStreet, Type.Revenue, jObject));
                                            list.AddRange(Unpack(estimate, Source.Estimize, Type.Eps, jObject));
                                            list.AddRange(Unpack(estimate, Source.Estimize, Type.Revenue, jObject));

                                            return list;
                                        }
                                    )
                            );
                        }
                    }

                    Task.WaitAll(tasks.ToArray());

                    var ticker = Path.GetFileNameWithoutExtension(zipFile) ?? string.Empty;
                    Directory.CreateDirectory(Path.Combine(_destinationFolder, ticker));

                    var consensuses = tasks.SelectMany(x => x.Result);

                    foreach (var kvp in consensuses.GroupBy(x => x.UpdatedAt.Date))
                    {
                        var contents = JsonConvert.SerializeObject(kvp.OrderBy(x => x.UpdatedAt));
                        SaveContentToZipFile(ticker, $"{kvp.Key:yyyyMMdd}", contents);
                    }

                    Log.Trace($"EstimizeConsensusDataDownloader.Run(): EstimizeConsensus files for {ticker} created : {stopwatch.Elapsed}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }

            Log.Trace($"EstimizeConsensusDataDownloader.Run(): Finished in {stopwatch.Elapsed}");
            return true;
        }

        private IEnumerable<EstimizeConsensus> Unpack(EstimizeEstimate estimizeEstimate, Source source, Type type, JObject jObject)
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

        private void SaveContentToZipFile(string ticker, string filename, string contents)
        {
            var zipEntryName = $"{filename}.json";
            var path = Path.Combine(_destinationFolder, ticker, zipEntryName);
            File.WriteAllText(path, contents);

            // Write out this data string to a zip file
            var zipPath = Path.Combine(_destinationFolder, ticker, $"{filename}.zip");
            Compression.Zip(path, zipPath, zipEntryName, true);
        }
    }
}