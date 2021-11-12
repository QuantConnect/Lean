using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public interface ITickGenerator
    {
        IEnumerable<Tick> GenerateTicks(Symbol symbol);
    }
}
