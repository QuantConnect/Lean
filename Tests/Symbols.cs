using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Provides symbol instancs for unit tests
    /// </summary>
    public static class Symbols
    {
        public static readonly Symbol SPY = CreateEquitySymbol("SPY");
        public static readonly Symbol AAPL = CreateEquitySymbol("AAPL");
        public static readonly Symbol MSFT = CreateEquitySymbol("MSFT");
        public static readonly Symbol ZNGA = CreateEquitySymbol("ZNGA");
        public static readonly Symbol FXE = CreateEquitySymbol("FXE");

        public static readonly Symbol USDJPY = CreateForexSymbol("USDJPY");
        public static readonly Symbol EURUSD = CreateForexSymbol("EURUSD");
        public static readonly Symbol EURGBP = CreateForexSymbol("EURGBP");
        public static readonly Symbol GBPUSD = CreateForexSymbol("GBPUSD");

        private static Symbol CreateForexSymbol(string symbol)
        {
            return Symbol.Create(symbol, SecurityType.Forex, Market.FXCM);
        }

        private static Symbol CreateEquitySymbol(string symbol)
        {
            return Symbol.Create(symbol, SecurityType.Equity, Market.USA);
        }
    }
}
