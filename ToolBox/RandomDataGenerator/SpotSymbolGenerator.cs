using System;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates a new random <see cref="Symbol"/> object of the specified security type.
    /// All returned symbols have a matching entry in the symbol properties database.
    /// </summary>
    /// <remarks>
    /// A valid implementation will keep track of generated symbol objects to ensure duplicates
    /// are not generated.
    /// </remarks>
    /// <exception cref="ArgumentException">Throw when specifying <see cref="SecurityType.Option"/> or
    /// <see cref="SecurityType.Future"/>. To generate symbols for the derivative security types, please
    /// use <see cref="NextOption"/> and <see cref="NextFuture"/> respectively</exception>
    /// <exception cref="NoTickersAvailableException">Thrown when there are no tickers left to use for new symbols.</exception>
    /// <param name="securityType">The security type of the generated symbol</param>
    /// <param name="market">The market of the generated symbol</param>
    /// <returns>A new symbol object of the specified security type</returns>
    public class SpotSymbolGenerator : SymbolGenerator
    {
        public SpotSymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
        }

        protected override Symbol GenerateSingle()
            => NextSymbol(Settings.SecurityType, Settings.Market);
    }
}
