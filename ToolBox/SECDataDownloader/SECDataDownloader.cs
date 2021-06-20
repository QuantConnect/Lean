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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Logging;
using QuantConnect.Util;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace QuantConnect.ToolBox.SECDataDownloader
{
    public class SECDataDownloader
    {
        // SEC imposes rate limits of 10 requests per second. Set to 5 req / 1.1 sec just to be safe.
        private readonly RateGate _indexGate = new RateGate(1, TimeSpan.FromMilliseconds(220));
        private readonly HashSet<string> _downloadedIndexFiles = new HashSet<string>();
        private readonly Dictionary<string, SECReportIndexFile> _archiveIndexFileCache = new Dictionary<string, SECReportIndexFile>();

        /// <summary>
        /// Base URL to query the SEC website for reports
        /// </summary>
        public string BaseUrl = "https://www.sec.gov/Archives/edgar";

        /// <summary>
        /// Maximum retries to request for SEC edgar filings
        /// </summary>
        public int MaxRetries = 5;

        /// <summary>
        /// SEC data downloader constructor
        /// </summary>
        public SECDataDownloader()
        {

        }

        /// <summary>
        /// Downloads the raw data from the data vendor and stores it on disk
        /// </summary>
        /// <param name="rawDestination">Destination we will write raw data to</param>
        /// <param name="start">Starting date</param>
        /// <param name="end">Ending date</param>
        public void Download(string rawDestination, DateTime start, DateTime end)
        {
            // We will be rate limited from the SEC website if we don't identify ourselves via User-Agent.
            // Also we use a global HttpClient instance to enable HTTP keep-alive, which will improve performance
            // and also reduce the chances of being rate-limited (plus, this is recommended practice).

            using (var client = new HttpClient())
            {
                var userAgent = new ProductInfoHeaderValue("User-Agent", $"QC-SEC-{Guid.NewGuid()}");
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
                
                Directory.CreateDirectory(Path.Combine(rawDestination, "indexes"));

                for (var currentDate = start; currentDate <= end; currentDate = currentDate.AddDays(1))
                {
                    // SEC does not publish documents on US federal holidays or weekends
                    if (!currentDate.IsCommonBusinessDay() || USHoliday.Dates.Contains(currentDate))
                    {
                        Log.Trace(
                            $"SECDataDownloader.Download(): Skipping date {currentDate:yyyy-MM-dd} because it was during the weekend or was a holiday");
                        continue;
                    }

                    // SEC files are stored by quarters on EDGAR
                    var quarter = currentDate < new DateTime(currentDate.Year, 4, 1) ? "QTR1" :
                        currentDate < new DateTime(currentDate.Year, 7, 1) ? "QTR2" :
                        currentDate < new DateTime(currentDate.Year, 10, 1) ? "QTR3" :
                        "QTR4";

                    var rawFile = Path.Combine(rawDestination, $"{currentDate:yyyyMMdd}.nc.tar.gz");
                    var tmpFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nc.tar.gz.tmp");

                    // We can access the index files for any given date and filter by form type
                    var dailyIndexTmp = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.idx"));
                    var dailyIndexRaw =
                        new FileInfo(Path.Combine(rawDestination, "indexes", $"{currentDate:yyyyMMdd}.idx"));

                    var cacheKey = $"{currentDate.Year}/{quarter}";
                    SECReportIndexFile indexFile;
                    if (!_archiveIndexFileCache.TryGetValue(cacheKey, out indexFile))
                    {
                        indexFile = GetArchiveIndexFile(client, currentDate.Year, quarter);
                        _archiveIndexFileCache[cacheKey] = indexFile;
                    }

                    // Attempt to parse the archive index file. Skip downloading the data for
                    // the day if we can't determine its size pre-emptively
                    string rawSize;
                    try
                    {
                        rawSize = indexFile.Directory.Items
                            .Find(item => item.Name == $"{currentDate:yyyyMMdd}.nc.tar.gz").Size;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e,
                            $"SECDataDownloader.TryGetFileSizeFromIndex(): Failed to find {currentDate:yyyyMMdd}.nc.tar.gz in the index file. Skipping...");
                        continue;
                    }

                    // Strip out kilobyte unit. All SEC data is reported in kilobytes
                    rawSize = rawSize.Replace("KB", "");

                    decimal fileSizeInKB;
                    if (!decimal.TryParse(rawSize, NumberStyles.Number, CultureInfo.InvariantCulture, out fileSizeInKB))
                    {
                        Log.Error(
                            $"SECDataDownloader.TryGetFileSizeFromIndex(): Failed to convert {rawSize} to decimal");
                        continue;
                    }

                    // Sometimes, requests to the SEC can fail for no apparent reason.
                    // We implement retry logic here to mitigate that potential issue
                    for (var retries = 0; retries < MaxRetries; retries++)
                    {
                        try
                        {
                            if (File.Exists(rawFile))
                            {
                                Log.Trace(
                                    $"SECDataDownloader.Download(): Skipping download of archive: {currentDate:yyyyMMdd}.nc.tar.gz");
                                break;
                            }

                            _indexGate.WaitToProceed();

                            Log.Trace($"SECDataDownloader.Download(): Downloading temp filing archive to: {tmpFile}");
                            // *.nc.tar.gz files are massive, potentially causing integer overflow when trying to get
                            // a byte array back from the HttpClient. Use stream instead to prevent that from happening.
                            // (Example case: 2021-05-17)
                            using (var tempFilingArchiveBytes = client.GetStreamAsync($"{BaseUrl}/Feed/{currentDate.Year}/{quarter}/{currentDate:yyyyMMdd}.nc.tar.gz")
                                .SynchronouslyAwaitTaskResult())
                            {
                                using (var tmpFileStream = File.OpenWrite(tmpFile))
                                {
                                    tempFilingArchiveBytes.CopyTo(tmpFileStream);
                                }
                            }

                            var tmpFileStat = new FileInfo(tmpFile);
                            var tmpFileSizeInKB = tmpFileStat.Length / 1024;

                            // Have max and low be +-1% of the stated file size
                            if (tmpFileSizeInKB > fileSizeInKB + (fileSizeInKB * 0.01m) ||
                                tmpFileSizeInKB < fileSizeInKB - (fileSizeInKB * 0.01m))
                            {
                                Log.Error(
                                    $"Temporary file is {tmpFileSizeInKB}KB, but is supposed to be {fileSizeInKB}KB. Deleting temp file and retrying...");
                                tmpFileStat.Delete();
                                continue;
                            }

                            Log.Trace($"SECDataDownloader.Download(): Moving temp archive to: {rawFile}");
                            File.Move(tmpFile, rawFile);

                            Log.Trace(
                                $"SECDataDownloader.Download(): Successfully downloaded {currentDate:yyyyMMdd}.nc.tar.gz");

                            break;
                        }
                        catch (HttpRequestException err)
                        {
                            Log.Error(
                                $"SECDataDownloader.Download(): Received status code {(int) err.StatusCode} - Retrying...");
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }

                    // Sometimes, requests to the SEC can fail for no apparent reason.
                    // We implement retry logic here to mitigate that potential issue
                    for (var retries = 0; retries < MaxRetries; retries++)
                    {
                        try
                        {
                            _indexGate.WaitToProceed();

                            Log.Trace(
                                $"SECDataDownloader.Download(): Downloading temp index manifest to: {dailyIndexTmp.FullName}");
                            var indexBytes = client
                                .GetByteArrayAsync(
                                    $"{BaseUrl}/daily-index/{currentDate.Year}/{quarter}/master.{currentDate:yyyyMMdd}.idx")
                                .SynchronouslyAwaitTaskResult();

                            File.WriteAllBytes(dailyIndexTmp.FullName, indexBytes);

                            if (dailyIndexRaw.Exists)
                            {
                                Log.Trace(
                                    $"SECDataDownloader.Download(): Deleting existing index file manifest: {dailyIndexRaw.FullName}");
                                dailyIndexRaw.Delete();
                            }

                            Log.Trace(
                                $"SECDataDownloader.Download(): Moving temp index manifest to: {dailyIndexRaw.FullName}");
                            dailyIndexTmp.MoveTo(dailyIndexRaw.FullName);

                            Log.Trace(
                                $"SECDataDownloader.Download(): Successfully downloaded master.{currentDate:yyyyMMdd}.idx");

                            break;
                        }
                        catch (HttpRequestException err)
                        {
                            Log.Error(
                                $"SECDataDownloader.Download(): Got error code {(int) err.StatusCode} attempting to download index manifest for date {currentDate:yyyy-MM-dd} - retrying");
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }

                    // Skip miscellaneous header rows because it is unstructured data
                    var dailyIndexes = File.ReadAllLines(dailyIndexRaw.FullName).Skip(7);

                    // Increase max simultaneous HTTP connection count
                    ServicePointManager.DefaultConnectionLimit = 1000;

                    // Tasks of index file downloads
                    var previousCik = string.Empty;
                    var i = 0;

                    // Parse CIK from text database and download the file asynchronously if we don't already have it
                    foreach (var line in dailyIndexes)
                    {
                        i++;

                        // CIK[0] | Company Name[1] | Form Type[2] | Date Filed[3] | File Name[4]
                        var csv = line.Split('|');

                        if (csv.Length < 5)
                        {
                            Log.Error(
                                $"SECDataDownloader.Download(): Length of daily index file line is less than five");
                            continue;
                        }

                        // CIK is 10 digits long, which we use to get the index file
                        var cik = csv[0].PadLeft(10, '0');
                        var formType = csv[2];

                        switch (formType)
                        {
                            case "8-K":
                            case "10-K":
                            case "10-Q":
                                break;
                            default:
                                // To prevent duplicate log spam
                                if (!string.IsNullOrEmpty(previousCik) && cik != previousCik)
                                {
                                    Log.Error(
                                        $"SECDataDownloader.Download(): Skipping form type {formType} with CIK: {cik} - line {i}");
                                }

                                previousCik = cik;
                                continue;
                        }

                        if (_downloadedIndexFiles.Contains(cik))
                        {
                            Log.Trace(
                                $"SECDataDownloader.Download(): Skipping index file since we already downloaded it during this session: {cik}.json");
                            previousCik = cik;
                            continue;
                        }

                        DownloadIndexFile(client, cik, rawDestination).SynchronouslyAwaitTask();
                        _downloadedIndexFiles.Add(cik);
                        previousCik = cik;
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

                for (var i = 0; i < MaxRetries; i++)
                {
                    try
                    {
                        if (!File.Exists(cikTickerListPath))
                        {
                            _indexGate.WaitToProceed();

                            Log.Trace("SECDataDownloader.Download(): Downloading ticker-CIK mappings list");
                            var tickerCikMappingsBytes = client
                                .GetByteArrayAsync("https://www.sec.gov/include/ticker.txt")
                                .SynchronouslyAwaitTaskResult();

                            File.WriteAllBytes(cikTickerListTempPath, tickerCikMappingsBytes);
                            File.Move(cikTickerListTempPath, cikTickerListPath);
                            File.Delete(cikTickerListTempPath);
                        }

                        if (!File.Exists(cikRankAndFileTickerListPath))
                        {
                            _indexGate.WaitToProceed();

                            Log.Trace(
                                "SECDataDownloader.Download(): Downloading ticker-CIK mappings list from rankandfile");
                            var tickerCikMappingsBytes = client
                                .GetByteArrayAsync("http://rankandfiled.com/static/export/cik_ticker.csv")
                                .SynchronouslyAwaitTaskResult();

                            File.WriteAllBytes(cikRankAndFileTickerListTempPath, tickerCikMappingsBytes);
                            File.Move(cikRankAndFileTickerListTempPath, cikRankAndFileTickerListPath);
                            File.Delete(cikRankAndFileTickerListTempPath);
                        }

                        if (!File.Exists(cikLookupPath))
                        {
                            _indexGate.WaitToProceed();

                            Log.Trace("SECDataDownloader.Download(): Downloading CIK lookup data");
                            var cikLookupBytes = client.GetByteArrayAsync($"{BaseUrl}/cik-lookup-data.txt")
                                .SynchronouslyAwaitTaskResult();

                            File.WriteAllBytes(cikLookupTempPath, cikLookupBytes);
                            File.Move(cikLookupTempPath, cikLookupPath);
                            File.Delete(cikLookupTempPath);
                        }
                    }
                    catch (HttpRequestException err)
                    {
                        if (err.StatusCode == HttpStatusCode.Forbidden ||
                            err.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Log.Trace(
                                $"SECDataDownloader.Download(): Rate limited downloading CIK-ticker mappings - retrying in 10s");
                            Thread.Sleep(10000);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to download SEC index files for a given CIK, with added
        /// fault tolerance
        /// </summary>
        /// <param name="cik">CIK of the equity</param>
        /// <param name="rawDestination">Destination where we will write to</param>
        /// <exception cref="Exception">We were unable to download the index file</exception>
        private async Task DownloadIndexFile(HttpClient client, string cik, string rawDestination)
        {
            for (var i = 0; i < MaxRetries; i++)
            {
                try
                {
                    _indexGate.WaitToProceed();
                    
                    var indexFileBytes = await client.GetByteArrayAsync($"{BaseUrl}/data/{cik}/index.json");
                    var indexPathTmp = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json"));
                    var indexPath = new FileInfo(Path.Combine(rawDestination, "indexes", $"{cik}.json"));

                    await File.WriteAllBytesAsync(indexPathTmp.FullName, indexFileBytes);
                    OnIndexFileDownloaded(indexPathTmp, indexPath);

                    return;
                }
                catch (HttpRequestException err)
                {
                    if (err.StatusCode == HttpStatusCode.Forbidden || err.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Log.Trace($"SECDataDownloader.DownloadIndexFile(): Rate limited downloading index file for {cik} ({(int)err.StatusCode}) - retrying in 10s");
                        // We've been rate limited, sleep for 10 seconds then try again
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            }

            throw new Exception($"Failed to download index file \"{cik}.json\" after {MaxRetries} attempts");
        }

        /// <summary>
        /// Moves temporary file to permanent path
        /// </summary>
        /// <param name="source">Path we will move index file from</param>
        /// <param name="destination">Path we will move index file to</param>
        private void OnIndexFileDownloaded(FileInfo source, FileInfo destination)
        {
            try
            {
                source.MoveTo(destination.FullName, true);
                Log.Trace($"SECDataDownloader.OnIndexFileDownloaded(): Successfully downloaded {destination.FullName}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// Downloads the archive index file
        /// </summary>
        /// <param name="year">Year to download index file for</param>
        /// <param name="quarter">Quarter to download index file for</param>
        /// <returns>SEC index directory</returns>
        private SECReportIndexFile GetArchiveIndexFile(HttpClient client, int year, string quarter)
        {
            for (var retries = 0; retries < MaxRetries; retries++)
            {
                // Download the index file for the quarter archive files before we download the archive so we know
                // its size and make sure it's within +-1% of the original file on the server
                try
                {
                    _indexGate.WaitToProceed();
                    
                    Log.Trace($"SECDataDownloader.GetFileSize(): Downloading archive index file for file size verification");
                    var contents = client.GetStringAsync($"{BaseUrl}/Feed/{year}/{quarter}/index.json").SynchronouslyAwaitTaskResult();

                    var indexFile = JsonConvert.DeserializeObject<SECReportIndexFile>(contents);
                    Log.Trace($"SECDataDownloader.GetFileSize(): Successfully downloaded {BaseUrl}/Feed/{year}/{quarter}/index.json");

                    return indexFile;
                }
                catch (HttpRequestException err)
                {
                    Log.Error($"SECDataDownloader.GetFileSize(): Received status code {(int)err.StatusCode} - Retrying...");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Retrying...");
                }
            }

            throw new Exception("Failed to download SEC archive index file. No more retries remaining");
        }
    }
}
