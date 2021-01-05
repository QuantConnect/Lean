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

using System;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data.Custom;
using Bitcoin = QuantConnect.Algorithm.CSharp.LiveTradingFeaturesAlgorithm.Bitcoin;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SymbolCacheTests
    {
        [SetUp]
        public void Setup()
        {
            // always start fresh
            SymbolCache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // clean up any lingering mess
            SymbolCache.Clear();
        }

        [Test]
        public void HandlesRoundTripAccessSymbolToTicker()
        {
            var ticker = "ticker";
            SymbolCache.Set(ticker, Symbols.EURUSD);
            var actual = SymbolCache.GetSymbol(ticker);
            Assert.AreEqual(Symbols.EURUSD, actual);
        }

        [Test]
        public void HandlesRoundTripAccessTickerToSymbol()
        {
            var expected = "ticker";
            expected = Symbols.EURUSD.Value;
            SymbolCache.Set(expected, Symbols.EURUSD);
            var actual = SymbolCache.GetTicker(Symbols.EURUSD);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(SecurityType.Base)]
        [TestCase(SecurityType.Cfd)]
        //[TestCase(SecurityType.Commodity)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Option)]
        public void GetSymbol_ReturnsSymbolMappedByTicker_WhenExactlyOneMatch(SecurityType securityType)
        {
            var ticker = "ticker";
            var symbol = Symbol.Create(ticker, securityType, Market.USA);
            SymbolCache.Set(ticker, symbol);
            Assert.AreEqual(symbol, SymbolCache.GetSymbol(ticker));
        }

        [Test]
        public void GetSymbol_ThrowsInvalidOperationException_WhenNoMatches()
        {
            Assert.Throws<InvalidOperationException>(
                // make random string guaranteed to not exist
                () => SymbolCache.GetSymbol(Guid.NewGuid().ToString())
            );
        }

        [Test]
        public void GetSymbol_ThrowsInvalidOperation_WhenTooManyCustomDataSymbolMatches()
        {
            // SID.GenerateBase uses <ticker>.<base-data-type-name> as the alias for custom data types
            var ticker = "ticker";

            var symbol1 = Symbol.Create(ticker, SecurityType.Base, Market.USA, baseDataType: typeof(Quandl));
            SymbolCache.Set(symbol1.ID.Symbol, symbol1);

            var symbol2 = Symbol.Create(ticker, SecurityType.Base, Market.USA, baseDataType: typeof(QuandlVix));
            SymbolCache.Set(symbol2.ID.Symbol, symbol2);

            Assert.Throws<InvalidOperationException>(
                () => SymbolCache.GetSymbol(ticker)
            );
        }

        [Test]
        public void TryGetSymbol()
        {
            SymbolCache.Set("EURUSD", Symbols.EURUSD);

            Symbol actual;
            Assert.IsTrue(SymbolCache.TryGetSymbol("EURUSD", out actual));
            Assert.AreEqual(Symbols.EURUSD, actual);

            Assert.IsFalse(SymbolCache.TryGetSymbol("EURUSD1", out actual));
            Assert.AreEqual(default(Symbol), actual);
        }

        [Test]
        public void TryGetTicker()
        {
            SymbolCache.Set("EURUSD", Symbols.EURUSD);

            string ticker;
            Assert.IsTrue(SymbolCache.TryGetTicker(Symbols.EURUSD, out ticker));
            Assert.AreEqual(Symbols.EURUSD.Value, ticker);

            var symbol = new Symbol(SecurityIdentifier.GenerateForex("NOT-A-FOREX-PAIR", Market.FXCM), "EURGBP");
            Assert.IsFalse(SymbolCache.TryGetTicker(symbol, out ticker));
            Assert.AreEqual(default(string), ticker);
        }

        [Test]
        public void TryGetSymbolFromSidString()
        {
            var sid = Symbols.EURUSD.ID.ToString();
            var symbol = SymbolCache.GetSymbol(sid);
            Assert.AreEqual(Symbols.EURUSD, symbol);
        }

        [Test]
        public void TryGetTickerFromUncachedSymbol()
        {
            var symbol = Symbol.Create("My-Ticker", SecurityType.Equity, Market.USA);
            var ticker = SymbolCache.GetTicker(symbol);
            Assert.AreEqual(symbol.ID.ToString(), ticker);
        }

        [Test]
        public void TryGetSymbol_FromTicker_WithConflictingSymbolWithCustomDataSuffix()
        {
            var ticker = "My-Ticker";
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);
            SymbolCache.Set(ticker, symbol);

            var customTicker = $"{ticker}.CustomDataTypeName";
            var customSymbol = Symbol.Create(customTicker, SecurityType.Base, Market.USA, baseDataType: typeof(Bitcoin));
            SymbolCache.Set(customTicker, customSymbol);

            Symbol fetchedSymbol;
            Assert.IsTrue(SymbolCache.TryGetSymbol(ticker, out fetchedSymbol));
            Assert.AreEqual(symbol, fetchedSymbol);

            Symbol fetchedCustomSymbol;
            Assert.IsTrue(SymbolCache.TryGetSymbol(customTicker, out fetchedCustomSymbol));
            Assert.AreEqual(customSymbol, fetchedCustomSymbol);
        }

        [Test]
        public void TryGetSymbol_FromTicker_WithoutCustomDataSuffix()
        {
            var ticker = "My-Ticker";
            var symbol = Symbol.Create(ticker, SecurityType.Base, Market.USA, baseDataType: typeof(Bitcoin));

            // alias is the SID's symbol of the form: <symbol>.<type>
            var alias = symbol.ID.Symbol;
            SymbolCache.Set(alias, symbol);

            Symbol fetchedSymbol;
            Assert.IsTrue(SymbolCache.TryGetSymbol(ticker, out fetchedSymbol));
            Assert.AreEqual(symbol, fetchedSymbol);
        }

        [Test]
        public void TryRemoveSymbolRemovesSymbolMappings()
        {
            string ticker;
            Symbol symbol;
            SymbolCache.Set("SPY", Symbols.SPY);
            Assert.IsTrue(SymbolCache.TryRemove(Symbols.SPY));
            Assert.IsFalse(SymbolCache.TryGetSymbol("SPY", out symbol));
            Assert.IsFalse(SymbolCache.TryGetTicker(Symbols.SPY, out ticker));
        }

        [Test]
        public void TryRemoveTickerRemovesSymbolMappings()
        {
            string ticker;
            Symbol symbol;
            SymbolCache.Set("SPY", Symbols.SPY);
            Assert.IsTrue(SymbolCache.TryRemove("SPY"));
            Assert.IsFalse(SymbolCache.TryGetSymbol("SPY", out symbol));
            Assert.IsFalse(SymbolCache.TryGetTicker(Symbols.SPY, out ticker));
        }
    }
}
