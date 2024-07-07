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

namespace QuantConnect.Securities.CryptoFuture
{
    /// <summary>
    /// Crypto future exchange class - information and helper tools for Crypto future exchange properties
    /// </summary>
    /// <seealso cref="SecurityExchange"/>
    public class CryptoFutureExchange : SecurityExchange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoFutureExchange"/> class using market hours
        /// derived from the market-hours-database for the Crypto future market
        /// </summary>
        public CryptoFutureExchange(string market)
            : base(
                MarketHoursDatabase
                    .FromDataFolder()
                    .GetExchangeHours(market, null, SecurityType.CryptoFuture)
            ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoFutureExchange"/> class using the specified
        /// exchange hours to determine open/close times
        /// </summary>
        /// <param name="exchangeHours">Contains the weekly exchange schedule plus holidays</param>
        public CryptoFutureExchange(SecurityExchangeHours exchangeHours)
            : base(exchangeHours) { }
    }
}
