using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class SymbolDataGenerator
    {
        public Symbol Symbol { get; set; }
        public ITickGenerator TickGenerator { get; set; }
        public IEnumerable<Tick> GenerateTicks() => TickGenerator.GenerateTicks();
    }
}
