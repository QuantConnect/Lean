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

namespace QuantConnect.ToolBox.EstimizeDataDownloader
{
    public abstract class EstimizeDataDownloader : IDataDownloader
    {
        private readonly string _clientKey;
        private readonly int _maxRetries = 5;

        /// <summary>
        /// Control the rate of download per unit of time.
        /// </summary>
        public RateGate IndexGate { get; }

        protected EstimizeDataDownloader()
        {
            _clientKey = Config.Get("estimize-economics-auth-token");

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
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of string representing data for this date range</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Company>> GetCompanies()
        {
            try
            {
                var content = await HttpRequester("/companies");
                return JsonConvert.DeserializeObject<List<Company>>(content);
            }
            catch (Exception e)
            {
                throw new Exception("EstimizeDownloader.GetSymbols(): Error parsing companies list", e);
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
                        client.BaseAddress = new Uri("https://api.estimize.com/");
                        client.DefaultRequestHeaders.Clear();

                        // You must supply your API key in the HTTP header X-Estimize-Key,
                        // otherwise you will receive a 403 Forbidden response
                        client.DefaultRequestHeaders.Add("X-Estimize-Key", _clientKey);

                        // Responses are in JSON: you need to specify the HTTP header Accept: application/json
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await client.GetAsync(Uri.EscapeUriString(url));

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            Log.Error($"EstimizeDataDownloader.HttpRequester(): File not found at url: {url}");
                            return string.Empty;
                        }

                        response.EnsureSuccessStatusCode();

                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"EstimizeDataDownloader.HttpRequester(): Error at HttpRequester. (retry {retries}/{_maxRetries})");
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
            var finalPath = Path.Combine(destinationFolder, $"{ticker}.csv");
            var finalFileExists = File.Exists(finalPath);

            var lines = new HashSet<string>(contents);
            if (finalFileExists)
            {
                Log.Trace($"EstimizeDataDownloader.SaveContentToZipFile(): Adding to existing file: {finalPath}");
                foreach (var line in File.ReadAllLines(finalPath))
                {
                    lines.Add(line);
                }
            }
            else
            {
                Log.Trace($"EstimizeDataDownloader.SaveContentToZipFile(): Writing to file: {finalPath}");
            }

            var finalLines = lines.OrderBy(x => DateTime.ParseExact(x.Split(',').First(), "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal))
                .ToList();

            File.WriteAllLines(finalPath, finalLines);
        }

        public class Company
        {
            /// <summary>
            /// The name of the company
            /// </summary>
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            /// <summary>
            /// The ticker/symbol for the company
            /// </summary>
            [JsonProperty(PropertyName = "ticker")]
            public string Ticker { get; set; }

            /// <summary>
            /// The CUSIP used to identify the security
            /// </summary>
            [JsonProperty(PropertyName = "cusip")]
            public string Cusip { get; set; }

            /// <summary>
            /// Returns a string that represents the Company object
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"{Cusip} - {Ticker} - {Name}";
        }
    }
}