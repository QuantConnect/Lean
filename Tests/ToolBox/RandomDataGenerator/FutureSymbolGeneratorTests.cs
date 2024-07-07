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
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class FutureSymbolGeneratorTests
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
            _symbolGenerator = new FutureSymbolGenerator(
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
        [TestCase(SecurityType.Future)]
        public void ReturnsFutureSymbolGeneratorInstance(SecurityType securityType)
        {
            Assert.IsInstanceOf<FutureSymbolGenerator>(
                BaseSymbolGenerator.Create(
                    new RandomDataGeneratorSettings { SecurityType = securityType },
                    Mock.Of<IRandomValueGenerator>()
                )
            );
        }

        [Test]
        public void GetAvailableSymbolCount()
        {
            Assert.AreEqual(int.MaxValue, _symbolGenerator.GetAvailableSymbolCount());
        }

        [Test]
        public void NextFuture_CreatesSymbol_WithFutureSecurityTypeAndRequestedMarket()
        {
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
            Assert.AreEqual(1, symbols.Count);

            var symbol = symbols.First();

            Assert.AreEqual(Market.CME, symbol.ID.Market);
            Assert.AreEqual(SecurityType.Future, symbol.SecurityType);
        }

        [Test]
        public void NextFuture_CreatesSymbol_WithFutureWithValidFridayExpiry()
        {
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
            Assert.AreEqual(1, symbols.Count);

            var symbol = symbols.First();

            var expiry = symbol.ID.Date;
            Assert.Greater(expiry, _minExpiry);
            Assert.LessOrEqual(expiry, _maxExpiry);
            Assert.AreEqual(DayOfWeek.Friday, expiry.DayOfWeek);
        }

        [Test]
        public void NextFuture_CreatesSymbol_WithEntryInSymbolPropertiesDatabase()
        {
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
            Assert.AreEqual(1, symbols.Count);

            var symbol = symbols.First();

            var db = SymbolPropertiesDatabase.FromDataFolder();
            Assert.IsTrue(db.ContainsKey(Market.CME, symbol, SecurityType.Future));
        }
    }
}
