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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.SECDataDownloader
{
    public class SECDataDownloader
    {
        // SEC imposes rate limits of 10 requests per second. Set to 10 req / 1.1 sec just to be safe.
        private readonly RateGate _indexGate = new RateGate(10, TimeSpan.FromSeconds(1.1));

        /// <summary>
        /// Base URL to query the SEC website for reports
        /// </summary>
        public string BaseUrl = "https://www.sec.gov/Archives/edgar";

        /// <summary>
        /// Maximum retries to request for SEC edgar filings
        /// </summary>
        public int MaxRetries = 5;

        /// <summary>
        /// Downloads the raw data from the data vendor and stores it on disk
        /// </summary>
        /// <param name="rawDestination">Destination we will write raw data to</param>
        /// <param name="start">Starting date</param>
        /// <param name="end">Ending date</param>
        public void Download(string rawDestination, DateTime start, DateTime end)
        {
            Directory.CreateDirectory(rawDestination);

            for (var currentDate = start; currentDate <= end; currentDate = currentDate.AddDays(1))
            {
                // SEC does not publish documents on US federal holidays or weekends
                if (!currentDate.IsCommonBusinessDay() || USHoliday.Dates.Contains(currentDate))
                {
                    continue;
                }

                // SEC files are stored by quarters on EDGAR
                var quarter = currentDate < new DateTime(currentDate.Year, 4, 1) ? "QTR1" :
                    currentDate < new DateTime(currentDate.Year, 7, 1) ? "QTR2" :
                    currentDate < new DateTime(currentDate.Year, 10, 1) ? "QTR3" :
                    "QTR4";

                var rawFile = Path.Combine(rawDestination, $"{currentDate:yyyyMMdd}.nc.tar.gz");
                var tmpFile = Path.Combine(rawDestination, $"{currentDate:yyyyMMdd}.nc.tar.gz.tmp");

                if (File.Exists(rawFile))
                {
                    continue;
                }

                // Sometimes, requests to the SEC can fail for no apparent reason.
                // We implement retry logic here to mitigate that potential issue
                for (var retries = 0; retries < MaxRetries; retries++)
                {
                    try
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile($"{BaseUrl}/Feed/{currentDate.Year}/{quarter}/{currentDate:yyyyMMdd}.nc.tar.gz", tmpFile);
                            File.Move(tmpFile, rawFile);
                            Log.Trace($"SECDataDownloader.Download(): Successfully downloaded {currentDate:yyyyMMdd}.nc.tar.gz");
                            break;
                        }
                    }
                    catch (WebException e)
                    {
                        var response = (HttpWebResponse) e.Response;

                        // SEC website uses s3, which returns a 403 if the given file does not exist
                        if (response?.StatusCode != null && response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            Log.Error($"SECDataDownloader.Download(): Report files not found on date {currentDate:yyyy-MM-dd}");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }

            // Download list of Ticker to CIK mappings from SEC website. Note that this list
            // is not complete and does not contain all historical tickers.
            var cikTickerListPath = Path.Combine(rawDestination, "cik-ticker-mappings.txt");
            var cikTickerListTempPath = $"{cikTickerListPath}.tmp";

            var cikRankAndFileTickerListPath = Path.Combine(rawDestination, "cik-ticker-mappings-rankandfile.txt");
            var cikRankAndFileTickerListTempPath = $"{cikRankAndFileTickerListPath}.tmp";

            // Download master list of CIKs from SEC website and store on disk
            var cikLookupPath = Path.Combine(rawDestination, "cik-lookup-data.txt");
            var cikLookupTempPath = $"{cikLookupPath}.tmp";

            using (var client = new WebClient())
            {
                if (!File.Exists(cikTickerListPath))
                {
                    Log.Trace("SECDataDownloader.Download(): Downloading ticker-CIK mappings list");
                    client.DownloadFile("https://www.sec.gov/include/ticker.txt", cikTickerListTempPath);
                    File.Move(cikTickerListTempPath, cikTickerListPath);
                    File.Delete(cikTickerListTempPath);
                }
                if (!File.Exists(cikRankAndFileTickerListPath))
                {
                    Log.Trace("SECDataDownloader.Download(): Downloading ticker-CIK mappings list from rankandfile");
                    client.DownloadFile("http://rankandfiled.com/static/export/cik_ticker.csv", cikRankAndFileTickerListTempPath);
                    File.Move(cikRankAndFileTickerListTempPath, cikRankAndFileTickerListPath);
                    File.Delete(cikRankAndFileTickerListTempPath);
                }
                if (!File.Exists(cikLookupPath))
                {
                    Log.Trace("SECDataDownloader.Download(): Downloading CIK lookup data");
                    client.DownloadFile($"{BaseUrl}/cik-lookup-data.txt", cikLookupTempPath);
                    File.Move(cikLookupTempPath, cikLookupPath);
                    File.Delete(cikLookupTempPath);
                }
            }

            // WebClient does not create the directory for us, so let's make sure it exists
            Directory.CreateDirectory(Path.Combine(rawDestination, "indexes"));

            // Increase max simultaneous HTTP connection count
            ServicePointManager.DefaultConnectionLimit = 1000;

            // Tasks of index file downloads
            var downloadTasks = new List<Task>();

            // Parse CIK from text database and download the file asynchronously if we don't already have it
            foreach (var line in File.ReadLines(cikLookupPath))
            {
                // CIK is 10 digits long, which allows us to get the CIK effortlessly
                var cik = line.Substring(line.Length - 11).Trim(':');
                var cikPath = Path.Combine(rawDestination, "indexes", $"{cik}.json");

                // Only download files we don't have
                if (File.Exists(cikPath))
                {
                    continue;
                }
                
                // If we re-use the webclient from above, we get a concurrent I/O operations error
                using (var client = new WebClient())
                {
                    // Makes sure we don't overrun SEC rate limits accidentally
                    _indexGate.WaitToProceed();

                    downloadTasks.Add(client.DownloadFileTaskAsync($"{BaseUrl}/data/{cik}/index.json", $"{cikPath}.tmp").ContinueWith(_ => OnIndexFileDownloaded(cikPath)));
                }
            }

            Task.WaitAll(downloadTasks.ToArray());
        }

        /// <summary>
        /// Moves temporary file to permanent path
        /// </summary>
        /// <param name="cikPath">Path we will move temporary index file to</param>
        private void OnIndexFileDownloaded(string cikPath)
        {
            try
            {
                File.Move($"{cikPath}.tmp", cikPath);
                Log.Trace($"SECDataDownloader.Download(): Successfully downloaded {cikPath}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
