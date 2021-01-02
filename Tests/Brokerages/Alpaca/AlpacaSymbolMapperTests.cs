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
*/

using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Alpaca;
using QuantConnect.Data.Auxiliary;
using System;

namespace QuantConnect.Tests.Brokerages.Alpaca
{
    [TestFixture]
    public class AlpacaSymbolMapperTests
    {
        private ISymbolMapper _symbolMapper;

        [SetUp]
        public void Setup()
        {
            _symbolMapper = new AlpacaSymbolMapper(new LocalDiskMapFileProvider());
        }

        [TestCase("AAPL")]
        [TestCase("TWX")]
        [TestCase("FOXA")]
        public void ReturnsCorrectLeanSymbol(string brokerageSymbol)
        {
            var symbol = _symbolMapper.GetLeanSymbol(brokerageSymbol, SecurityType.Equity, Market.USA);
            Assert.AreEqual(brokerageSymbol, symbol.Value);
            Assert.AreEqual(SecurityType.Equity, symbol.ID.SecurityType);
            Assert.AreEqual(Market.USA, symbol.ID.Market);
        }

        [TestCase("AAPL", "AAPL")]
        [TestCase("AOL", "TWX")]
        [TestCase("NWSA", "FOXA")]
        [TestCase("TWX", "TWX")]
        [TestCase("FOXA", "FOXA")]
        public void MapCorrectBrokerageSymbol(string ticker, string alpacaSymbol)
        {
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            Assert.AreEqual(alpacaSymbol, brokerageSymbol);
        }

        [TestCase("")]
        [TestCase(null)]
        public void ThrowsIfEmptyTicker(string ticker)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _symbolMapper.GetLeanSymbol(ticker, SecurityType.Equity, Market.USA);
            });
        }

        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Option)]
        public void ThrowsIfNotEquity(SecurityType securityType)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _symbolMapper.GetLeanSymbol("AAPL", securityType, Market.USA);
            });
        }

        [Test]
        public void ThrowsOnNullOrEmptyOrInvalidSymbol()
        {
            var mapper = new AlpacaSymbolMapper(new LocalDiskMapFileProvider());

            var symbol = Symbol.Empty;
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = null;
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = Symbol.Create("", SecurityType.Forex, Market.FXCM);
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));
        }
    }
}
