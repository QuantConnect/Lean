using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SymbolCacheTests
    {
        [Test]
        public void HandlesRoundTripAccessSymbolToTicker()
        {
            var ticker = "ticker";
            var expected = new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), ticker);
            SymbolCache.Set(ticker, expected);
            var actual = SymbolCache.Get(ticker);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void HandlesRoundTripAccessTickerToSymbol()
        {
            var expected = "ticker";
            var symbol = new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), expected);
            expected = symbol.Value;
            SymbolCache.Set(expected, symbol);
            var actual = SymbolCache.GetTicker(symbol);
            Assert.AreEqual(expected, actual);
        }
    }
}
