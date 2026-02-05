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
    /// Prediction Market holdings implementation of the base securities class
    /// </summary>
    /// <remarks>
    /// Prediction market contracts have a price between 0 and 1 (representing $0.00-$1.00)
    /// and settle at either $0 or $1. Each contract represents a $1 max payout.
    /// Position value = quantity * current price (0-1 range)
    /// </remarks>
    /// <seealso cref="SecurityHolding"/>
    public class PredictionMarketHolding : SecurityHolding
    {
        /// <summary>
        /// Prediction Market Holding Class constructor
        /// </summary>
        /// <param name="security">The prediction market security being held</param>
        /// <param name="currencyConverter">A currency converter instance</param>
        public PredictionMarketHolding(Security security, ICurrencyConverter currencyConverter)
            : base(security, currencyConverter)
        {
        }
    }
}
