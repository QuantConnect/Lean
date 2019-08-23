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
using System.IO;
using System.Net;
using System.Threading;
using NodaTime;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.PsychSignalDataConverter
{
    public class PsychSignalDataDownloader
    {
        private readonly string _apiKey;
        private readonly string _dataSource;

        /// <summary>
        /// Psychsignal imposes strict API rate limits set to about one request per ten seconds
        /// </summary>
        private readonly RateGate _apiRateGate = new RateGate(occurrences: 1, timeUnit: TimeSpan.FromSeconds(10));

        /// <summary>
        /// Base URL for the psychsignal API
        /// </summary>
        private readonly string _baseUrl = "https://api.psychsignal.com/v2";

        /// <summary>
        /// Destination we will write raw data to
        /// </summary>
        private readonly string _rawDataDestination;

        /// <summary>
        /// Maximum amount of retries per data hour
        /// </summary>
        public int MaxRetries = 5;

        /// <summary>
        /// Downlods data from psychsignal
        /// </summary>
        /// <param name="rawDataDestination">Directory we write raw data to</param>
        /// <param name="apiKey">PsychSignal API key</param>
        /// <param name="dataSource">Data source (e.g. stocktwits,twitter_withretweets)</param>
        public PsychSignalDataDownloader(string rawDataDestination, string apiKey, string dataSource)
        {
            _rawDataDestination = rawDataDestination;

            _dataSource = dataSource;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Download the data from the given starting date to the ending date.
        /// Note that if the ending date is in the same hour as the current time,
        /// we will lower the <paramref name="endDateUtc" /> by one hour in order
        /// to make sure that we only download complete, non-changing dataa
        /// </summary>
        /// <param name="startDateUtc">Starting date</param>
        /// <param name="endDateUtc">Ending date</param>
        public void Download(DateTime startDateUtc, DateTime endDateUtc)
        {
            var now = DateTime.UtcNow;
            var nowHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

            Directory.CreateDirectory(_rawDataDestination);

            if (startDateUtc < now.AddDays(-15))
            {
                throw new ArgumentException("The starting date can only be at most 15 days from now");
            }

            // Makes sure we only get final, non-changing data by checking if the end date is greater than
            // or equal to the current time and setting it to an hour before the current time if the condition is met
            if (nowHour <= new DateTime(endDateUtc.Year, endDateUtc.Month, endDateUtc.Day, endDateUtc.Hour, 0, 0))
            {
                Log.Trace($"PsychSignalDataDownloader.Download(): Adjusting end time from {endDateUtc:yyyy-MM-dd HH:mm:ss} to {nowHour.AddHours(-1):yyyy-MM-dd HH:mm:ss}");
                endDateUtc = nowHour.AddHours(-1);
            }

            // Get the total amount of hours in order to keep track of progress
            var totalHours = endDateUtc.Subtract(startDateUtc).TotalHours;
            var percentage = 1 / totalHours;
            Log.Trace("PsychSignalDataDownloader.Download(): Begin downloading raw data");

            // PsychSignal paginates data by hour. Note that it is possible to retrieve non-complete data if the requested hour
            // is the same as the current hour or greater than the current hour.
            for (var i = 1; startDateUtc < endDateUtc; startDateUtc = startDateUtc.AddHours(1), i++)
            {
                var rawDataPath = Path.Combine(_rawDataDestination, $"{startDateUtc:yyyyMMdd_HH}.csv");
                var rawDataPathTemp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString()}.csv.tmp");

                // Don't download files we already have
                if (File.Exists(rawDataPath))
                {
                    Log.Trace($"PsychSignalDataDownloader.Download(): File already exists: {rawDataPath}");
                    continue;
                }

                // Retry in case a download failed
                for (var retries = 0; retries < MaxRetries; retries++)
                {
                    // Set a max timeout of ten seconds
                    _apiRateGate.WaitToProceed(10000);

                    try
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile($"{_baseUrl}/replay?apikey={_apiKey}&update=1m&sources={_dataSource}&from={startDateUtc:yyyyMMddHH}&format=csv", rawDataPathTemp);
                            File.Move(rawDataPathTemp, rawDataPath);
                            Log.Trace($"PsychSignalDataDownloader.Download(): Successfully downloaded file: {rawDataPath}");
                            break;
                        }
                    }
                    catch (WebException e)
                    {
                        var response = (HttpWebResponse) e.Response;

                        if (retries == MaxRetries - 1)
                        {
                            Log.Error($"PsychSignalDataDownloader.Download(): We've reached the maximum number of retries for date {startDateUtc:yyyy-MM-dd HH:00:00}");
                            continue;
                        }
                        if (response == null)
                        {
                            Log.Error("PsychSignalDataDownloader.Download(): Response was null. Retrying...");
                            continue;
                        }
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            Log.Error("PsychSignalDataDownloader.Download(): Server received a bad request. Continuing...");
                            break;
                        }
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            Log.Error("PsychSignalDataDownloader.Download(): Received an HTTP 404. Continuing...");
                            break;
                        }
                        if (response.StatusCode == (HttpStatusCode) 429)
                        {
                            Log.Trace("PsychSignalDataDownloader.Download(): We are being rate limited. Retrying...");
                        }
                        else
                        {
                            Log.Error($"PsychSignalDataDownloader.Download(): Received unknown HTTP status code {(int) response.StatusCode}. Retrying...");
                        }
                    }
                }

                var complete = i / totalHours;
                var eta = TimeSpan.FromSeconds((totalHours - i) * 10);

                Log.Trace($"PsychSignalDataDownloader.Download(): Downloading {complete:P2} complete. ETA is {eta.TotalMinutes:N2} minutes");
            }
        }
    }
}
