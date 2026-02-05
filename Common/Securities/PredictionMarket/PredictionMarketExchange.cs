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

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Prediction Market exchange class - information and helper tools for prediction market exchange properties
    /// </summary>
    /// <seealso cref="SecurityExchange"/>
    public class PredictionMarketExchange : SecurityExchange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionMarketExchange"/> class using market hours
        /// derived from the market-hours-database for the prediction market
        /// </summary>
        public PredictionMarketExchange(string market)
            : base(MarketHoursDatabase.FromDataFolder().GetExchangeHours(market, null, SecurityType.PredictionMarket))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionMarketExchange"/> class using the specified
        /// exchange hours to determine open/close times
        /// </summary>
        /// <param name="exchangeHours">Contains the weekly exchange schedule plus holidays</param>
        public PredictionMarketExchange(SecurityExchangeHours exchangeHours)
            : base(exchangeHours)
        {
        }
    }
}
