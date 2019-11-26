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
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace QuantConnect.ToolBox.Benzinga
{
    /// <summary>
    /// Downloads Benzinga news data from Benzinga's API
    /// </summary>
    public class BenzingaNewsDataDownloader
    {
        private const string _baseUrl = "https://api.benzinga.com/api/v2";
        private const int _retryMaxCount = 5;

        private readonly DirectoryInfo _destinationDirectory;
        private readonly RateGate _rateGate;
        private readonly string _apiKey;

        /// <summary>
        /// Creates an instance of the class
        /// </summary>
        /// <param name="destinationDirectory">
        /// Directory to write files to. This will be a top-level folder that will have folders
        /// following the pattern: {destinationDirectory}/YYYY/MM/dd/entry.json
        ///
        /// This should normally be `/raw_data_folder/alternative/benzinga`
        /// </param>
        /// <param name="apiKey">Key to access Benzinga's API</param>
        public BenzingaNewsDataDownloader(DirectoryInfo destinationDirectory, string apiKey)
        {
            _destinationDirectory = destinationDirectory;
            _apiKey = apiKey;

            // Limit ourselves to 2 requests per second to Benzinga's API.
            // There are 237391 articles starting from 2017-09-12 and ending on 2019-09-13.
            // This means that at 200 articles/second, it would take us approximately
            // 1186.955 seconds (19 minutes 45 seconds) + some latency to download.
            _rateGate = new RateGate(1, TimeSpan.FromSeconds(0.5));
        }

        /// <summary>
        /// Downloads news data from Benzinga's API
        /// </summary>
        /// <param name="startDate">Starting date</param>
        /// <param name="endDate">Ending date</param>
        public void Download(DateTime startDate, DateTime endDate, bool forceOverwrite = true)
        {
            foreach (var date in Time.EachDay(startDate, endDate))
            {
                var finished = false;
                var page = 0;

                var finalDateDirectory = new DirectoryInfo(
                    Path.Combine(
                        _destinationDirectory.FullName,
                        date.ToStringInvariant("yyyyMMdd")
                    )
                );

                finalDateDirectory.Create();

                while (!finished)
                {
                    for (var retries = 0; retries < _retryMaxCount; retries++)
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                var tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json"));
                                var finalFileBackup = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bak.json"));

                                _rateGate.WaitToProceed();

                                client.Headers.Add(HttpRequestHeader.Accept, "application/json");

                                Log.Trace($"BenzingaNewsDataDownloader.Download(): Getting data for {date:yyyy-MM-dd} on page: {page}");
                                var rawContents = client.DownloadString($"{_baseUrl}/news?token={_apiKey}&pageSize=100&displayOutput=full&date={date:yyyy-MM-dd}&page={page}");

                                // We can't trust the output of `CreateBenzingaNewsFromJSON` because we do some additional filtering steps, potentially
                                // causing us to emit empty output, though more data may exist on the next page.
                                // By checking for empty output, we can ensure that no more data exists after the current page.
                                if (rawContents == "[]")
                                {
                                    Log.Trace($"BenzingaNewsDataDownloader.Download(): Exhausted available data for {date:yyyy-MM-dd} - Exiting download loop");
                                    finished = true;
                                    break;
                                }

                                // Convert the raw contents to a JArray and then deserialize each entry
                                var finalEntry = JsonConvert.DeserializeObject<JArray>(rawContents)
                                    .Select(rawArticle => BenzingaNewsJsonConverter.DeserializeNews(rawArticle, enableLogging: true))
                                    .OrderBy(rawArticle => rawArticle.Id)
                                    .ToList();

                                if (finalEntry.Count == 0)
                                {
                                    Log.Trace($"BenzingaNewsDataDownloader.Download(): No parsable data found on {date} for page {page}");
                                    continue;
                                }

                                var firstId = finalEntry.First().Id;
                                var lastId = finalEntry.Last().Id;

                                var finalFile = new FileInfo(Path.Combine(finalDateDirectory.FullName, $"benzinga_api_{firstId}_{lastId}.json"));

                                // If for some reason we failed to write to a file, let's try again retries permitting
                                if (!TryWriteToFile(rawContents, tempFile, finalFileBackup, finalFile, forceOverwrite))
                                {
                                    continue;
                                }
                            }

                            break;
                        }
                        catch (Exception error)
                        {
                            Log.Error(error, $"Download failed. {_retryMaxCount - retries - 1} retries remaining");
                        }
                    }

                    page++;
                }
            }
        }

        /// <summary>
        /// Attempt to write the raw contents of the article to a file
        /// </summary>
        /// <param name="rawContents">Raw contents of the API response</param>
        /// <param name="tempFile">Temporary file to write to</param>
        /// <param name="finalFileBackup">Backup location for the existing raw file</param>
        /// <param name="finalFile">Final destination for the raw contents</param>
        /// <param name="forceOverwrite">Flag to determine whether we should overwrite an existing final file</param>
        public bool TryWriteToFile(string rawContents, FileInfo tempFile, FileInfo finalFileBackup, FileInfo finalFile, bool forceOverwrite)
        {
            // Recover original file if we fail to overwrite an existing file
            try
            {
                if (finalFile.Exists && !forceOverwrite)
                {
                    Log.Trace("BenzingaNewsDataDownloader.WriteToFile(): API response has already been saved. Skipping");
                    return false;
                }

                Log.Trace($"BenzingaNewsDataDownloader.WriteToFile(): Writing contents to temp file: {tempFile.FullName}");
                File.WriteAllText(tempFile.FullName, rawContents);

                // Refresh because we've actually written a file now and it should exist
                tempFile.Refresh();

                if (finalFile.Exists && forceOverwrite)
                {
                    Log.Trace($"BenzingaNewsDataDownloader.WriteToFile(): Moving existing raw API response to backup location: {finalFileBackup.FullName}");
                    File.Move(finalFile.FullName, finalFileBackup.FullName);

                    finalFile.Refresh();
                    finalFileBackup.Refresh();
                }

                Log.Trace($"BenzingaNewsDataDownloader.WriteToFile(): Moving temp file to final location: {finalFile.FullName}");
                File.Move(tempFile.FullName, finalFile.FullName);

                if (finalFileBackup.Exists)
                {
                    Log.Trace($"BenzingaNewsDataDownloader.WriteToFile(): Deleting backup file: {finalFileBackup.FullName}");
                    finalFileBackup.Delete();
                }

                return true;
            }
            catch (Exception error)
            {
                tempFile.Refresh();
                finalFile.Refresh();
                finalFileBackup.Refresh();

                if (tempFile.Exists)
                {
                    Log.Trace($"Deleting existing temp file");
                    tempFile.Delete();
                }
                if (finalFileBackup.Exists && !finalFile.Exists)
                {
                    Log.Error(error, $"Failed to write to final file after backup. Restoring from {finalFileBackup.FullName}");
                    File.Move(finalFileBackup.FullName, finalFile.FullName);
                    return false;
                }
                if (!tempFile.Exists)
                {
                    Log.Error(error, $"Failed to write to temp file: {tempFile.FullName}");
                    return false;
                }
                if (finalFileBackup.Exists && finalFile.Exists)
                {
                    Log.Error(error, $"Failed to delete final file backup at {finalFileBackup.FullName}");
                    return false;
                }

                return false;
            }
        }
    }
}
