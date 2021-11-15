using System;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random future <see cref="Symbol"/>. The generates future contract symbol will have an
    /// expiry between the specified time range.
    /// </summary>
    /// <returns>A new future contract symbol with the specified expiration parameters</returns>
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
        /// Generates a new random future <see cref="Symbol"/>. The generates future contract symbol will have an
        /// expiry between the specified minExpiry and maxExpiry.
        /// </summary>
        protected override Symbol GenerateSingle()
        {
            // get a valid ticker from the symbol properties database
            var ticker = NextTickerFromSymbolPropertiesDatabase(SecurityType.Future, _market);

            var marketHours = MarketHoursDatabase.GetExchangeHours(_market, ticker, SecurityType.Future);
            var expiry = GetRandomExpiration(marketHours, _minExpiry, _maxExpiry);

            return Symbol.CreateFuture(ticker, _market, expiry);
        }
    }
}
