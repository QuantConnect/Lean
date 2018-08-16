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
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// An implementation of <see cref="IPriceProvider"/> which uses QC API to fetch price data
    /// </summary>
    public class ApiPriceProvider : IPriceProvider
    {
        private readonly IApi _api;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiPriceProvider"/> class
        /// </summary>
        /// <param name="api">The <see cref="IApi"/> instance</param>
        /// <remarks>The caller is responsible for calling <see cref="IApi.Initialize"/> before</remarks>
        public ApiPriceProvider(IApi api)
        {
            _api = api;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiPriceProvider"/> class
        /// </summary>
        /// <param name="userId">The QC user id</param>
        /// <param name="userToken">The QC user token</param>
        public ApiPriceProvider(int userId, string userToken)
        {
            _api = new Api.Api();
            _api.Initialize(userId, userToken, "");
        }

        /// <summary>
        /// Gets the latest price for a given asset
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The latest price</returns>
        public decimal GetLastPrice(Symbol symbol)
        {
            var result = _api.ReadPrices(new[] { symbol });
            if (!result.Success)
            {
                throw new Exception($"ReadPrices error: {string.Join(" - ", result.Errors)}");
            }

            var priceData = result.Prices.FirstOrDefault(x => x.Symbol == symbol);
            if (priceData == null)
            {
                throw new Exception($"No price data available for symbol: {symbol.Value}");
            }

            return priceData.Price;
        }
    }
}
