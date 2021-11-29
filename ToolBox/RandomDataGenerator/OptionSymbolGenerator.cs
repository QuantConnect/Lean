using System;
using System.Collections.Generic;
using QuantConnect.Securities;

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
    public class OptionSymbolGenerator : SymbolGenerator
    {
        private readonly DateTime _minExpiry;
        private readonly DateTime _maxExpiry;
        private readonly string _market;
        private readonly decimal _underlyingPrice;
        private readonly decimal _maximumStrikePriceDeviation;

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
            var underlying = NextSymbol(SecurityType.Equity, _market);

            yield return underlying;

            var marketHours = MarketHoursDatabase.GetExchangeHours(_market, underlying, SecurityType.Equity);
            var expiry = GetRandomExpiration(marketHours, _minExpiry, _maxExpiry);

            // generate a random strike while respecting the maximum deviation from the underlying's price
            // since these are underlying prices, use Equity as the security type
            var strike = Random.NextPrice(SecurityType.Equity, _market, _underlyingPrice, _maximumStrikePriceDeviation);

            // round the strike price to something reasonable
            var order = 1 + Math.Log10((double)strike);
            strike = strike.RoundToSignificantDigits((int)order);

            var optionRight = Random.NextBool(0.5)
                ? OptionRight.Call
                : OptionRight.Put;

            // when providing a null option w/ an expiry, it will automatically create the OSI ticker string for the Value
            yield return Symbol.CreateOption(underlying, _market, OptionStyle.American, optionRight, strike, expiry);
        }

        public override int GetAvailableSymbolCount() => int.MaxValue;
    }
}
