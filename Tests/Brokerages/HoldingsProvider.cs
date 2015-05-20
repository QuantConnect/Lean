using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides a test implementation of a holdings provider
    /// </summary>
    public class HoldingsProvider : IHoldingsProvider
    {
        private readonly IDictionary<string, Holding> _holdings;

        public HoldingsProvider(IDictionary<string, Holding> holdings)
        {
            _holdings = holdings;
        }

        public HoldingsProvider()
        {
            _holdings = new Dictionary<string, Holding>();
        }

        public Holding this[string symbol]
        {
            get { return _holdings[symbol]; }
            set { _holdings[symbol] = value; }
        }

        public Holding GetHoldings(string symbol)
        {
            Holding holding;
            _holdings.TryGetValue(symbol, out holding);
            return holding;
        }

        public bool TryGetValue(string symbol, out Holding holding)
        {
            return _holdings.TryGetValue(symbol, out holding);
        }
    }
}