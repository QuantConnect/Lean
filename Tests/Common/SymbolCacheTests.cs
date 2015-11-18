/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

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
            var actual = SymbolCache.GetSymbol(ticker);
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

        [Test]
        public void TryGetSymbol()
        {
            var expected = new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD");
            SymbolCache.Set("EURUSD", expected);

            Symbol actual;
            Assert.IsTrue(SymbolCache.TryGetSymbol("EURUSD", out actual));
            Assert.AreEqual(expected, actual);

            Assert.IsFalse(SymbolCache.TryGetSymbol("EURUSD1", out actual));
            Assert.AreEqual(default(Symbol), actual);
        }

        [Test]
        public void TryGetTicker()
        {
            var symbol = new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD");
            SymbolCache.Set("EURUSD", symbol);

            string ticker;
            Assert.IsTrue(SymbolCache.TryGetTicker("EURUSD", out ticker));
            Assert.AreEqual(symbol.Value, ticker);

            symbol = new Symbol(SecurityIdentifier.GenerateForex("NOT A FOREX PAIR", Market.FXCM), "EURGBP");
            Assert.IsFalse(SymbolCache.TryGetTicker(symbol, out ticker));
            Assert.AreEqual(default(string), ticker);
        }
    }
}
