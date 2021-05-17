using QuantConnect.Benchmarks;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Exante Brokerage Model Implementation for Back Testing.
    /// </summary>
    public class ExanteBrokerageModel : DefaultBrokerageModel
    {
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.Bitfinex);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }
    }
}
