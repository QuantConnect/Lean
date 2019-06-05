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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using QuantConnect.Configuration;

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
        /// <returns>Enumerable of string representing data for this date range</returns>
        public abstract IEnumerable<string> Get(DateTime startUtc, DateTime endUtc);

        public async Task<string> HttpRequester(string url)
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
            catch (Exception e)
            {
                throw new Exception($"Error at HttpRequester with msg: {e}", e);
            }
        }
    }
}