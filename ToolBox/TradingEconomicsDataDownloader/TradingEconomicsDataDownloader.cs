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

using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.TradingEconomicsDataDownloader
{
    public abstract class TradingEconomicsDataDownloader : ITradingEconomicsDataDownloader
    {
        private readonly string _clientKey;

        protected TradingEconomicsDataDownloader()
        {
            var authId = Config.Get("trading-economics-auth-id", "guest");
            var authToken = Config.Get("trading-economics-auth-token", "guest");
            _clientKey = $"{authId}:{authToken}";
        }

        /// <summary>
        /// Runs the instance of the object.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public abstract bool Run();

        /// <summary>
        /// Get Trading Economics data for a given this start and end times(in UTC).
        /// </summary>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>String representing data for this date range</returns>
        public abstract Task<string> Get(DateTime startUtc, DateTime endUtc);

        public async Task<string> HttpRequester(string url)
        {
            for (var retries = 1; retries <= 5; retries++)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri("https://api.tradingeconomics.com");
                        client.DefaultRequestHeaders.Clear();

                        //ADD Accept Header to tell the server what data type you want
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                        //ADD Authorization
                        var auth = new AuthenticationHeaderValue("Client", _clientKey);
                        client.DefaultRequestHeaders.Authorization = auth;

                        var response = await client.GetAsync(Uri.EscapeUriString(url));

                        response.EnsureSuccessStatusCode();

                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch (HttpRequestException e)
                {
                    Log.Trace($"TradingEconomicsDataDownloader.HttpRequester(): HTTP Request failed with message: {e.Message} - (attempt {retries} / 5). Retrying...");

                    // Sleep for half a second in order to respect rate limits. This event is rare enough
                    // that sleeping for half a second won't have much impact on performance
                    Thread.Sleep(500);
                }
            }

            throw new Exception("HTTP request retries exceeded maximum (5/5)");
        }

        /// <summary>
        /// Gets the ticker. If the ticker is empty, we return the category and country of the calendar
        /// </summary>
        /// <param name="tradingEconomicsIndicator">Indicator data</param>
        /// <returns>Ticker or category + country data</returns>
        public string GetTicker(string ticker, string category, string country)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return (category + country).ToLowerInvariant().Replace(" ", "-");
            }

            return ticker.ToLowerInvariant().Replace(" ", "-");
        }
    }
}