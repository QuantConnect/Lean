using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class SymbolGenerator
    {
        private readonly IRandomValueGenerator _random;
        private readonly RandomDataGeneratorSettings _settings;

        public SymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
        {
            _settings = settings;
            _random = random;
        }

        public IEnumerable<Symbol> GenerateRandomSymbols()
        {
            for (int i = 0; i < _settings.SymbolCount; i++)
            {
                switch (_settings.SecurityType)
                {
                    case SecurityType.Option:
                        yield return _random.NextOption(_settings.Market, _settings.Start, _settings.End, 100m, 75m);
                        break;

                    case SecurityType.Future:
                        yield return _random.NextFuture(_settings.Market, _settings.Start, _settings.End);
                        break;

                    default:
                        yield return _random.NextSymbol(_settings.SecurityType, _settings.Market);
                        break;
                }
            }
        }
    }
}