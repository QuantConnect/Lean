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
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.QuiverDataDownloader
{
    /// <summary>
    /// Base QuiverDataDownloader implementation. https://www.quiverquant.com/
    /// </summary>
    public abstract class QuiverDataDownloader : IDataDownloader
    {
        private readonly string _clientKey;
        private readonly int _maxRetries = 5;
        private static readonly List<char> _defunctDelimiters = new List<char>
        {
            '-',
            '_'
        };

        /// <summary>
        /// Control the rate of download per unit of time.
        /// </summary>
        public RateGate IndexGate { get; }

        protected readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };



        protected QuiverDataDownloader()
        {
            _clientKey = Config.Get("quiver-auth-token");

            // Represents rate limits of 10 requests per 1.1 second
            IndexGate = new RateGate(10, TimeSpan.FromSeconds(1.1));
        }

        /// <summary>
        /// Runs the instance of the object.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public abstract bool Run();

        /// <summary>
        /// Get Trading Economics data for a given this start and end times(in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <returns>Enumerable of string representing data for this date range</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Company>> GetCompanies()
        {
            try
            {
                var content = await HttpRequester("companies");
                return JsonConvert.DeserializeObject<List<Company>>(content);
            }
            catch (Exception e)
            {
                throw new Exception("QuiverDownloader.GetSymbols(): Error parsing companies list", e);
            }
        }

        public async Task<string> HttpRequester(string url)
        {
            for (var retries = 1; retries <= _maxRetries; retries++)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri("https://api.quiverquant.com/beta/");
                        client.DefaultRequestHeaders.Clear();

                        // You must supply your API key in the HTTP header,
                        // otherwise you will receive a 403 Forbidden response
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _clientKey);

                        // Responses are in JSON: you need to specify the HTTP header Accept: application/json
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var response = await client.GetAsync(Uri.EscapeUriString(url));
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            Log.Error($"QuiverDataDownloader.HttpRequester(): Files not found at url: {Uri.EscapeUriString(url)}");
                            response.DisposeSafely();
                            return string.Empty;
                        }

                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            var finalRequestUri = response.RequestMessage.RequestUri; // contains the final location after following the redirect.
                            response = client.GetAsync(finalRequestUri).Result; // Reissue the request. The DefaultRequestHeaders configured on the client will be used, so we don't have to set them again.

                        }

                        response.EnsureSuccessStatusCode();

                        var result =  await response.Content.ReadAsStringAsync();
                        response.DisposeSafely();

                        return result;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"QuiverDataDownloader.HttpRequester(): Error at HttpRequester. (retry {retries}/{_maxRetries})");
                    Thread.Sleep(1000);
                }
            }

            throw new Exception($"Request failed with no more retries remaining (retry {_maxRetries}/{_maxRetries})");
        }

        /// <summary>
        /// Saves contents to disk, deleting existing zip files
        /// </summary>
        /// <param name="destinationFolder">Final destination of the data</param>
        /// <param name="ticker">Stock ticker</param>
        /// <param name="contents">Contents to write</param>
        protected void SaveContentToFile(string destinationFolder, string ticker, IEnumerable<string> contents)
        {
            ticker = ticker.ToLowerInvariant();
            var bkPath = Path.Combine(destinationFolder, $"{ticker}-bk.csv");
            var finalPath = Path.Combine(destinationFolder, $"{ticker}.csv");
            var finalFileExists = File.Exists(finalPath);

            var lines = new HashSet<string>(contents);
            if (finalFileExists)
            {
                Log.Trace($"QuiverDataDownloader.SaveContentToZipFile(): Adding to existing file: {finalPath}");
                foreach (var line in File.ReadAllLines(finalPath))
                {
                    lines.Add(line);
                }
            }
            else
            {
                Log.Trace($"QuiverDataDownloader.SaveContentToZipFile(): Writing to file: {finalPath}");
            }

            var finalLines = lines.OrderBy(x => DateTime.ParseExact(x.Split(',').First(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                .ToList();

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
            File.WriteAllLines(tempPath, finalLines);
            var tempFilePath = new FileInfo(tempPath);
            if (finalFileExists)
            {
                tempFilePath.Replace(finalPath,bkPath);
                var bkFilePath = new FileInfo(bkPath);
                bkFilePath.Delete();
            }
            else
            {
                tempFilePath.MoveTo(finalPath);
            }

        }

        /// <summary>
        /// Tries to normalize a potentially defunct ticker into a normal ticker.
        /// </summary>
        /// <param name="ticker">Ticker as received from Estimize</param>
        /// <param name="nonDefunctTicker">Set as the non-defunct ticker</param>
        /// <returns>true for success, false for failure</returns>
        public static bool TryNormalizeDefunctTicker(string ticker, out string nonDefunctTicker)
        {
            // The "defunct" indicator can be in any capitalization/case
            if (ticker.IndexOf("defunct", StringComparison.OrdinalIgnoreCase) > 0)
            {
                foreach (var delimChar in _defunctDelimiters)
                {
                    var length = ticker.IndexOf(delimChar);

                    // Continue until we exhaust all delimiters
                    if (length == -1)
                    {
                        continue;
                    }

                    nonDefunctTicker = ticker.Substring(0, length).Trim();
                    return true;
                }

                nonDefunctTicker = string.Empty;
                return false;
            }

            nonDefunctTicker = ticker;
            return true;
        }


        /// <summary>
        /// Normalizes Estimize tickers to a format usable by the <see cref="Data.Auxiliary.MapFileResolver"/>
        /// </summary>
        /// <param name="ticker">Ticker to normalize</param>
        /// <returns>Normalized ticker</returns>
        public static string NormalizeTicker(string ticker)
        {
            return ticker.ToLowerInvariant()
                .Replace("- defunct", string.Empty)
                .Replace("-defunct", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("|", string.Empty)
                .Replace("-", ".");
        }

        public class Company
        {
            /// <summary>
            /// The name of the company
            /// </summary>
            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            /// <summary>
            /// The ticker/symbol for the company
            /// </summary>
            [JsonProperty(PropertyName = "Ticker")]
            public string Ticker { get; set; }
        }
    }
}