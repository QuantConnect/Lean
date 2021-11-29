using System;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random future <see cref="Symbol"/>. The generates future contract Symbol will have an
    /// expiry between the specified time range.
    /// </summary>
    /// <returns>A new future contract Symbol with the specified expiration parameters</returns>
    public class FutureSymbolGenerator : SymbolGenerator
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
        public override IEnumerable<Symbol> GenerateAsset()
        {
            // get a valid ticker from the Symbol properties database
            var ticker = NextTickerFromSymbolPropertiesDatabase(SecurityType.Future, _market);

            var marketHours = MarketHoursDatabase.GetExchangeHours(_market, ticker, SecurityType.Future);
            var expiry = GetRandomExpiration(marketHours, _minExpiry, _maxExpiry);

            yield return Symbol.CreateFuture(ticker, _market, expiry);
        }

        public override int GetAvailableSymbolCount() => int.MaxValue;
    }
}
