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
using QuantConnect.Securities.Future;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random future <see cref="Symbol"/>. The generates future contract Symbol will have an
    /// expiry between the specified time range.
    /// </summary>
    public class FutureSymbolGenerator : BaseSymbolGenerator
    {
        private readonly DateTime _minExpiry;
        private readonly DateTime _maxExpiry;
        private readonly string _market;

        public FutureSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
            _minExpiry = settings.Start;
            _maxExpiry = settings.End;
            _market = settings.Market;
        }

        /// <summary>
        /// Generates a new random future <see cref="Symbol"/>. The generates future contract Symbol will have an
        /// expiry between the specified minExpiry and maxExpiry.
        /// </summary>
        /// <param name="ticker">Optionally can provide a ticker that should be used</param>
        /// <returns>A new future contract Symbol with the specified expiration parameters</returns>
        protected override IEnumerable<Symbol> GenerateAsset(string ticker = null)
        {
            if (ticker == null)
            {
                // get a valid ticker from the Symbol properties database
                ticker = NextTickerFromSymbolPropertiesDatabase(SecurityType.Future, _market);
            }

            var marketHours = MarketHoursDatabase.GetExchangeHours(_market, ticker, SecurityType.Future);
            var expiry = GetRandomExpiration(marketHours, _minExpiry, _maxExpiry);

            // Attempt to refine the expiry using the FuturesExpiryFunctions if available
            var symbol = Symbol.CreateFuture(ticker, _market, SecurityIdentifier.DefaultDate);
            if (FuturesExpiryFunctions.FuturesExpiryDictionary.TryGetValue(symbol, out var expiryFunction))
            {
                // Iterate backwards from one month after max expiry to find a valid expiry within the range
                for (var date = _maxExpiry.AddMonths(1); date > _minExpiry; date = date.AddDays(-1))
                {
                    expiry = expiryFunction(date);
                    if (_minExpiry < expiry && expiry <= _maxExpiry)
                    {
                        break;
                    }
                }
            }

            yield return Symbol.CreateFuture(ticker, _market, expiry);
        }

        /// <summary>
        /// There is no limit for the future symbols.
        /// </summary>
        /// <returns>Returns int.MaxValue</returns>
        public override int GetAvailableSymbolCount() => int.MaxValue;
    }
}
