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

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random option <see cref="Symbol"/>.
    /// </summary>
    public class OptionSymbolGenerator : BaseSymbolGenerator
    {
        private readonly DateTime _minExpiry;
        private readonly DateTime _maxExpiry;
        private readonly string _market;
        private readonly int _symbolChainSize;
        private readonly decimal _underlyingPrice;
        private readonly decimal _maximumStrikePriceDeviation;
        private readonly SecurityType _underlyingSecurityType  = SecurityType.Equity;

        public OptionSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random, decimal underlyingPrice, decimal maximumStrikePriceDeviation)
            : base(settings, random)
        {
            // We add seven days more because TickGenerator for options needs first three underlying data points to warm up
            // the price generator, so if the expiry date is before settings.Start plus three days no quote or trade data is
            // generated for this option
            _minExpiry = (settings.Start).AddDays(7);
            _maxExpiry = (settings.End).AddDays(7);
            _market = settings.Market;
            _underlyingPrice = underlyingPrice;
            _symbolChainSize = settings.ChainSymbolCount;
            _maximumStrikePriceDeviation = maximumStrikePriceDeviation;
        }

        /// <summary>
        /// Generates a new random option <see cref="Symbol"/>. The generated option contract Symbol will have an
        /// expiry between the specified min and max expiration. The strike
        /// price will be within the specified maximum strike price deviation of the underlying symbol price
        /// and should be rounded to reasonable value for the given price. For example, a price of 100 dollars would round
        /// to 5 dollar increments and a price of 5 dollars would round to 50 cent increments
        /// </summary>
        /// <param name="ticker">Optionally can provide a ticker that should be used</param>
        /// <remarks>
        /// Standard contracts expiry on the third Friday.
        /// Weekly contracts expiry every week on Friday
        /// </remarks>
        /// <returns>A new option contract Symbol within the specified expiration and strike price parameters along with its underlying symbol</returns>
        protected override IEnumerable<Symbol> GenerateAsset(string ticker = null)
        {
            // first generate the underlying
            var underlying = NextSymbol(_underlyingSecurityType, _market, ticker);
            yield return underlying;

            var marketHours = MarketHoursDatabase.GetExchangeHours(_market, underlying, _underlyingSecurityType);
            var expiry = GetRandomExpiration(marketHours, _minExpiry, _maxExpiry);

            var strikes = new HashSet<decimal>();
            for (var i = 0; i < _symbolChainSize; i++)
            {
                decimal strike;
                do
                {
                    // generate a random strike while respecting the maximum deviation from the underlying's price
                    // since these are underlying prices, use Equity as the security type
                    strike = Random.NextPrice(_underlyingSecurityType, _market, _underlyingPrice,
                        _maximumStrikePriceDeviation);

                    // round the strike price to something reasonable
                    var order = 1 + Math.Log10((double)strike);
                    strike = strike.RoundToSignificantDigits((int)order);
                }
                // don't allow duplicate strikes
                while (!strikes.Add(strike));

                foreach (var optionRight in new [] { OptionRight.Put, OptionRight.Call })
                {
                    // when providing a null option w/ an expiry, it will automatically create the OSI ticker string for the Value
                    yield return Symbol.CreateOption(underlying, _market, underlying.SecurityType.DefaultOptionStyle(), optionRight, strike, expiry);
                }
            }
        }

        /// <summary>
        /// Returns the number of symbols with the specified parameters can be generated.
        /// There is no limit for the options.
        /// </summary>
        /// <returns>returns int.MaxValue</returns>
        public override int GetAvailableSymbolCount() => int.MaxValue;
    }
}
