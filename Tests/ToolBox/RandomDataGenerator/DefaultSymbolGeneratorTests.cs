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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class DefaultSymbolGeneratorTests
    {
        private const int Seed = 123456789;
        private static readonly IRandomValueGenerator _randomValueGenerator =
            new RandomValueGenerator(Seed);

        private BaseSymbolGenerator _symbolGenerator;
        private DateTime _minExpiry = new(2000, 01, 01);
        private DateTime _maxExpiry = new(2001, 01, 01);

        [SetUp]
        public void Setup()
        {
            // initialize using a seed for deterministic tests
            _symbolGenerator = new DefaultSymbolGenerator(
                new RandomDataGeneratorSettings()
                {
                    Market = Market.CME,
                    Start = _minExpiry,
                    End = _maxExpiry
                },
                _randomValueGenerator
            );
        }

        [Test]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Index)]
        public void ReturnsDefaultSymbolGeneratorInstance(SecurityType securityType)
        {
            Assert.IsInstanceOf<DefaultSymbolGenerator>(
                BaseSymbolGenerator.Create(
                    new RandomDataGeneratorSettings() { SecurityType = securityType },
                    Mock.Of<IRandomValueGenerator>()
                )
            );
        }

        [Test]
        [TestCase(SecurityType.Equity, Market.USA, true)]
        [TestCase(SecurityType.Cfd, Market.FXCM, false)]
        [TestCase(SecurityType.Cfd, Market.Oanda, false)]
        [TestCase(SecurityType.Forex, Market.FXCM, false)]
        [TestCase(SecurityType.Forex, Market.Oanda, false)]
        [TestCase(SecurityType.Crypto, Market.GDAX, false)]
        [TestCase(SecurityType.Crypto, Market.Bitfinex, false)]
        public void GetAvailableSymbolCount(
            SecurityType securityType,
            string market,
            bool expectInfinity
        )
        {
            var expected = expectInfinity
                ? int.MaxValue
                : SymbolPropertiesDatabase
                    .FromDataFolder()
                    .GetSymbolPropertiesList(market, securityType)
                    .Count();

            var symbolGenerator = new DefaultSymbolGenerator(
                new RandomDataGeneratorSettings { SecurityType = securityType, Market = market },
                Mock.Of<RandomValueGenerator>()
            );

            Assert.AreEqual(expected, symbolGenerator.GetAvailableSymbolCount());
        }

        [Test]
        [TestCase(SecurityType.Equity, Market.USA)]
        [TestCase(SecurityType.Cfd, Market.FXCM)]
        [TestCase(SecurityType.Cfd, Market.Oanda)]
        [TestCase(SecurityType.Forex, Market.FXCM)]
        [TestCase(SecurityType.Forex, Market.Oanda)]
        [TestCase(SecurityType.Crypto, Market.GDAX)]
        [TestCase(SecurityType.Crypto, Market.Bitfinex)]
        public void NextSymbol_CreatesSymbol_WithRequestedSecurityTypeAndMarket(
            SecurityType securityType,
            string market
        )
        {
            var symbolGenerator = new DefaultSymbolGenerator(
                new RandomDataGeneratorSettings { SecurityType = securityType, Market = market },
                _randomValueGenerator
            );

            var symbols = BaseSymbolGeneratorTests.GenerateAsset(symbolGenerator).ToList().ToList();
            Assert.AreEqual(1, symbols.Count);

            var symbol = symbols.First();
            Assert.AreEqual(securityType, symbol.SecurityType);
            Assert.AreEqual(market, symbol.ID.Market);
        }

        [Test]
        [TestCase(SecurityType.Equity, Market.USA)]
        [TestCase(SecurityType.Cfd, Market.FXCM)]
        [TestCase(SecurityType.Cfd, Market.Oanda)]
        [TestCase(SecurityType.Forex, Market.FXCM)]
        [TestCase(SecurityType.Forex, Market.Oanda)]
        [TestCase(SecurityType.Crypto, Market.GDAX)]
        [TestCase(SecurityType.Crypto, Market.Bitfinex)]
        public void NextSymbol_CreatesSymbol_WithEntryInSymbolPropertiesDatabase(
            SecurityType securityType,
            string market
        )
        {
            var symbolGenerator = new DefaultSymbolGenerator(
                new RandomDataGeneratorSettings { SecurityType = securityType, Market = market },
                _randomValueGenerator
            );

            var symbols = BaseSymbolGeneratorTests.GenerateAsset(symbolGenerator).ToList().ToList();
            Assert.AreEqual(1, symbols.Count);

            var symbol = symbols.First();

            var db = SymbolPropertiesDatabase.FromDataFolder();
            if (db.ContainsKey(market, SecurityDatabaseKey.Wildcard, securityType))
            {
                // there is a wildcard entry, so no need to check whether there is a specific entry for the symbol
                Assert.Pass();
            }
            else
            {
                // there is no wildcard entry, so there should be a specific entry for the symbol instead
                Assert.IsTrue(db.ContainsKey(market, symbol, securityType));
            }
        }

        [Test]
        [TestCase(SecurityType.Cfd, Market.FXCM)]
        [TestCase(SecurityType.Cfd, Market.Oanda)]
        [TestCase(SecurityType.Forex, Market.FXCM)]
        [TestCase(SecurityType.Forex, Market.Oanda)]
        [TestCase(SecurityType.Crypto, Market.GDAX)]
        [TestCase(SecurityType.Crypto, Market.Bitfinex)]
        public void NextSymbol_ThrowsNoTickersAvailableException_WhenAllSymbolsGenerated(
            SecurityType securityType,
            string market
        )
        {
            var db = SymbolPropertiesDatabase.FromDataFolder();
            var symbolCount = db.GetSymbolPropertiesList(market, securityType).Count();

            var symbolGenerator = new DefaultSymbolGenerator(
                new RandomDataGeneratorSettings { SecurityType = securityType, Market = market },
                _randomValueGenerator
            );

            for (var i = 0; i < symbolCount; i++)
            {
                var symbols = BaseSymbolGeneratorTests.GenerateAsset(symbolGenerator).ToList();
                Assert.AreEqual(1, symbols.Count);
            }

            Assert.Throws<NoTickersAvailableException>(
                () => BaseSymbolGeneratorTests.GenerateAsset(symbolGenerator).ToList()
            );
        }
    }
}
