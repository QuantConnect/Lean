using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public abstract class SymbolGenerator
    {
        protected IRandomValueGenerator Random { get; }
        protected RandomDataGeneratorSettings Settings { get; }

        protected SymbolGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
        {
            Settings = settings;
            Random = random;
        }

        public IEnumerable<Symbol> GenerateRandomSymbols()
        {
            for (int i = 0; i < Settings.SymbolCount; i++)
            {
                yield return GenerateSingle();
            }
        }

        protected abstract Symbol GenerateSingle();
    }
}
