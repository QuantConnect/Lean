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
using System.Linq;
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

            // Try to get the specific expiry function for this future, if available
            var symbol = Symbol.CreateFuture(ticker, _market, SecurityIdentifier.DefaultDate);
            if (!FuturesExpiryFunctions.FuturesExpiryDictionary.TryGetValue(symbol, out var expiryFunction))
            {
                // If no expiry function is found, return the future using the previously chosen expiry
                yield return Symbol.CreateFuture(ticker, _market, expiry);
                yield break;
            }

            // Get all valid expiries in range using the expiry function
            // HashSet ensures unique expiry dates since multiple reference dates may map to same expiry
            var validExpiries = new HashSet<DateTime>();

            // Extends range by ±1 month to catch all potential expiries.
            // Some futures (like NG) calculate expiry based on next month's date
            // (e.g., "3 business days before 1st of next month"), so we need to look ahead.
            // This buffer ensures we don't miss expiries near range boundaries.
            for (var date = _minExpiry.AddMonths(-1); date <= _maxExpiry.AddMonths(1); date = date.AddDays(1))
            {
                // Calculate expiry date using the futures-specific function
                var newExpiry = expiryFunction(date);

                // Only include expiries within our target range
                if (_minExpiry < newExpiry && newExpiry <= _maxExpiry)
                {
                    // Add to set of valid expiries (automatically handles duplicates)
                    validExpiries.Add(newExpiry);
                }
            }

            if (validExpiries.Count == 0)
            {
                yield return Symbol.CreateFuture(ticker, _market, expiry);
                yield break;
            }

            // Randomly select one expiry from the valid set
            var skip = Random.NextInt(validExpiries.Count);
            expiry = validExpiries.Skip(skip).First();

            // Return the future contract using the randomly selected valid expiry
            yield return Symbol.CreateFuture(ticker, _market, expiry);
        }

        /// <summary>
        /// There is no limit for the future symbols.
        /// </summary>
        /// <returns>Returns int.MaxValue</returns>
        public override int GetAvailableSymbolCount() => int.MaxValue;
    }
}
