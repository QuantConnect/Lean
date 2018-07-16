using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.CurrencyConversion
{
    public interface ICurrencyConversion
    {
        string SourceCurrency { get; }

        string DestinationCurrency { get; }

        decimal Update(); // pokes the component so it updates its state, and returns latest conversion

        decimal GetConversion(); // gets the current conversion rate
    }
}
