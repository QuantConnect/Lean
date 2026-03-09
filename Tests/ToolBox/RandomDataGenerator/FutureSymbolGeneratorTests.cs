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

using Moq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.ToolBox.RandomDataGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class FutureSymbolGeneratorTests
    {
        private const int Seed = 123456789;
        private static readonly IRandomValueGenerator _randomValueGenerator = new RandomValueGenerator(Seed);

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
                _randomValueGenerator);
        }

        [Test]
        [TestCase(SecurityType.Future)]
        public void ReturnsFutureSymbolGeneratorInstance(SecurityType securityType)
        {
            Assert.IsInstanceOf<FutureSymbolGenerator>(BaseSymbolGenerator.Create(
                new RandomDataGeneratorSettings { SecurityType = securityType },
                Mock.Of<IRandomValueGenerator>()
            ));
        }

        [Test]
        public void GetAvailableSymbolCount()
        {
            Assert.AreEqual(int.MaxValue, _symbolGenerator.GetAvailableSymbolCount());
        }

        [Test]
        public void GeneratesFutureSymbolWithCorrectExpiryDate()
        {
            var startDate = new DateTime(2020, 01, 01);
            var endDate = new DateTime(2020, 01, 31);
            var futureSymbolGenerator = new FutureSymbolGenerator(
                new RandomDataGeneratorSettings()
                {
                    Market = Market.NYMEX,
                    Start = startDate,
                    End = endDate
                },
                _randomValueGenerator);

            // Generate a future symbol using a specific ticker "NG"
            var symbols = BaseSymbolGeneratorTests.GenerateAssetWithTicker(futureSymbolGenerator, "NG").ToList();
            Assert.AreEqual(1, symbols.Count);

            var symbol = symbols.First();
            var expiry = symbol.ID.Date;
            bool hasExpiryFunction = FuturesExpiryFunctions.FuturesExpiryDictionary.TryGetValue(symbol.Canonical, out var expiryFuncWithTicker);
            Assert.IsTrue(hasExpiryFunction);
            // Add one month to simulate how the expiry function takes the first day of the next month and subtracts 3 business days
            Assert.IsTrue(expiryFuncWithTicker(expiry.AddMonths(1)).Equals(expiry));
            Assert.AreEqual(expiry, new DateTime(2020, 01, 29));
            Assert.Greater(expiry, startDate);
            Assert.LessOrEqual(expiry, endDate);

            // Generate a future symbol without specifying ticker
            symbols = BaseSymbolGeneratorTests.GenerateAsset(futureSymbolGenerator).ToList();
            Assert.AreEqual(1, symbols.Count);
            symbol = symbols.First();
            expiry = symbol.ID.Date;

            // Ensure the expiry falls within the configured start and end range
            Assert.Greater(expiry, startDate);
            Assert.LessOrEqual(expiry, endDate);
        }

        [Test]
        public void GeneratesFutureSymbolWithCorrectExpiryDateOverWiderDateRange()
        {
            var startDate = new DateTime(2020, 01, 01);
            var endDate = new DateTime(2024, 01, 31);
            var futureSymbolGenerator = new FutureSymbolGenerator(
                new RandomDataGeneratorSettings()
                {
                    Market = Market.NYMEX,
                    Start = startDate,
                    End = endDate
                },
                _randomValueGenerator);

            var expiries = new HashSet<DateTime>();
            for (int i = 0; i < 500; i++)
            {
                // Generate a future symbol using a specific ticker "NG"
                var symbols = BaseSymbolGeneratorTests.GenerateAssetWithTicker(futureSymbolGenerator, "NG").ToList();
                Assert.AreEqual(1, symbols.Count);
                var symbol = symbols.First();
                var expiry = symbol.ID.Date;
                bool hasExpiryFunction = FuturesExpiryFunctions.FuturesExpiryDictionary.TryGetValue(symbol.Canonical, out var expiryFuncWithTicker);
                Assert.IsTrue(hasExpiryFunction);
                Assert.IsTrue(expiryFuncWithTicker(expiry.AddMonths(1)).Equals(expiry));
                expiries.Add(expiry);
            }
            Assert.Greater(expiries.Count, 1);
        }

        [TestCase("TEST")]
        [TestCase("NG")]
        public void StopsExecutionAfterReturningSingleSymbolWhenNoExpiryFunctionOrValidExpiry(string ticker)
        {
            // Define a small date range that does not contain any valid expiries
            var startDate = new DateTime(2020, 01, 15);
            var endDate = new DateTime(2020, 01, 17);
            var futureSymbolGenerator = new FutureSymbolGenerator(
                new RandomDataGeneratorSettings()
                {
                    Market = Market.NYMEX,
                    Start = startDate,
                    End = endDate
                },
                _randomValueGenerator);

            // Generate a future symbol using a specific ticker
            var symbols = BaseSymbolGeneratorTests.GenerateAssetWithTicker(futureSymbolGenerator, ticker);
            var enumerator = symbols.GetEnumerator();

            // At least one symbol should be produced
            Assert.IsTrue(enumerator.MoveNext());

            // No additional symbol should be generated
            Assert.IsFalse(enumerator.MoveNext());
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
