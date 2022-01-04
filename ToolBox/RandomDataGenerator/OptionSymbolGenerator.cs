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

using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random option <see cref="Symbol"/>. The generated option contract Symbol will have an
    /// expiry between the specified <paramref name="minExpiry"/> and <paramref name="maxExpiry"/>. The strike
    /// price will be within the specified <paramref name="maximumStrikePriceDeviation"/> of the <paramref name="underlyingPrice"/>
    /// and should be rounded to reasonable value for the given price. For example, a price of 100 dollars would round
    /// to 5 dollar increments and a price of 5 dollars would round to 50 cent increments
    /// </summary>
    /// <remarks>
    /// Standard contracts expiry on the third Friday.
    /// Weekly contracts expiry every week on Friday
    /// </remarks>
    /// <param name="market">The market of the generated Symbol</param>
    /// <param name="minExpiry">The minimum expiry date, inclusive</param>
    /// <param name="maxExpiry">The maximum expiry date, inclusive</param>
    /// <param name="underlyingPrice">The option's current underlying price</param>
    /// <param name="maximumStrikePriceDeviation">The strike price's maximum percent deviation from the underlying price</param>
    /// <returns>A new option contract Symbol within the specified expiration and strike price parameters</returns>
    public class OptionSymbolGenerator : BaseSymbolGenerator
    {
        private readonly DateTime _minExpiry;
        private readonly DateTime _maxExpiry;
        private readonly string _market;
        private readonly decimal _underlyingPrice;
        private readonly decimal _maximumStrikePriceDeviation;
        private readonly SecurityType _underlyingSecurityType  = SecurityType.Equity;

        public OptionSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random, decimal underlyingPrice, decimal maximumStrikePriceDeviation)
            : base(settings, random)
        {
            _minExpiry = settings.Start;
            _maxExpiry = settings.End;
            _market = settings.Market;
            _underlyingPrice = underlyingPrice;
            _maximumStrikePriceDeviation = maximumStrikePriceDeviation;
        }

        public override IEnumerable<Symbol> GenerateAsset()
        {
            // first generate the underlying
            var underlying = NextSymbol(_underlyingSecurityType, _market);

            yield return underlying;

            var marketHours = MarketHoursDatabase.GetExchangeHours(_market, underlying, _underlyingSecurityType);
            var expiry = GetRandomExpiration(marketHours, _minExpiry, _maxExpiry);

            // generate a random strike while respecting the maximum deviation from the underlying's price
            // since these are underlying prices, use Equity as the security type
            var strike = Random.NextPrice(_underlyingSecurityType, _market, _underlyingPrice, _maximumStrikePriceDeviation);

            // round the strike price to something reasonable
            var order = 1 + Math.Log10((double)strike);
            strike = strike.RoundToSignificantDigits((int)order);

            var optionRight = Random.NextBool(0.5)
                ? OptionRight.Call
                : OptionRight.Put;

            // when providing a null option w/ an expiry, it will automatically create the OSI ticker string for the Value
            yield return Symbol.CreateOption(underlying, _market, underlying.SecurityType.DefaultOptionStyle(), optionRight, strike, expiry);
        }

        public override int GetAvailableSymbolCount() => int.MaxValue;
    }
}
