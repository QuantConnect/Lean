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
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data.Shortable;

namespace QuantConnect.Tests.Common.Data.Shortable
{
    [TestFixture]
    public class ShortableProviderTests
    {
        [SetUp]
        public void SetupConfig()
        {
            Config.Set("data-folder", "TestData");
            Globals.Reset();
        }

        [TearDown]
        public void ResetConfig()
        {
            Config.Reset();
            Globals.Reset();
        }

        [Test]
        public void LocalDiskShortableProviderGetsDataByDate()
        {
            var shortableProvider = new LocalDiskShortableProvider(SecurityType.Equity, "testbrokerage", QuantConnect.Market.USA);
            var symbols = new[]
            {
                new Symbol(SecurityIdentifier.GenerateEquity("AAPL", QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), "AAPL"),
                new Symbol(SecurityIdentifier.GenerateEquity("GOOG", QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), "GOOG"),
                new Symbol(SecurityIdentifier.GenerateEquity("BAC", QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), "BAC")
            };
            var results = new[]
            {
                new Dictionary<Symbol, long>
                {
                    { symbols[0], 2000 },
                    { symbols[1], 5000 },
                    { symbols[2], 200 }
                },
                new Dictionary<Symbol, long>
                {
                    { symbols[0], 4000 },
                    { symbols[1], 10000 },
                    { symbols[2], 400 }
                }
            };

            var dates = new[]
            {
                new DateTime(2020, 12, 21),
                new DateTime(2020, 12, 22)
            };

            for (var i = 0; i < dates.Length; i++)
            {
                var date = dates[i];
                var shortableSymbols = shortableProvider.AllShortableSymbols(date);

                Assert.AreEqual(results[i], shortableSymbols);
            }
        }

        [Test]
        public void LocalDiskShortableProviderGetsDataBySymbol()
        {
            var shortableProvider = new LocalDiskShortableProvider(SecurityType.Equity, "testbrokerage", QuantConnect.Market.USA);
            var symbols = new[]
            {
                new Symbol(SecurityIdentifier.GenerateEquity("AAPL", QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), "AAPL"),
                new Symbol(SecurityIdentifier.GenerateEquity("GOOG", QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), "GOOG"),
                new Symbol(SecurityIdentifier.GenerateEquity("BAC", QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), "BAC")
            };
            var results = new[]
            {
                new Dictionary<Symbol, long?>
                {
                    { symbols[0], 2000 },
                    { symbols[1], 5000 },
                    { symbols[2], 0 }
                },
                new Dictionary<Symbol, long?>
                {
                    { symbols[0], 4000 },
                    { symbols[1], 10000 },
                    { symbols[2], 0 }
                }
            };

            var dates = new[]
            {
                new DateTime(2020, 12, 21),
                new DateTime(2020, 12, 22)
            };

            foreach (var symbol in symbols)
            {
                for (var i = 0; i < dates.Length; i++)
                {
                    var date = dates[i];
                    var shortableQuantity = shortableProvider.ShortableQuantity(symbol, date);

                    Assert.AreEqual(results[i][symbol], shortableQuantity);
                }
            }
        }

        [Test]
        public void LocalDiskShortableProviderReturnsPopulatedDictionaryUsingLookback()
        {
            var provider = new LocalDiskShortableProvider(SecurityType.Equity, "testbrokerage", QuantConnect.Market.USA);
            Assert.AreEqual(3, provider.AllShortableSymbols(new DateTime(2020, 12, 23)).Count);
        }

        [Test]
        public void LocalDiskShortableProviderReturnsEmptyDictionaryForMissingDataUsingDate()
        {
            var provider = new LocalDiskShortableProvider(SecurityType.Equity, "testbrokerage", QuantConnect.Market.USA);
            Assert.AreEqual(0, provider.AllShortableSymbols(new DateTime(2020, 12, 31)).Count);
        }

        [TestCase("AAPL", "nobrokerage")]
        [TestCase("SPY", "testbrokerage")]
        public void LocalDiskShortableProviderDefaultsToZeroForMissingData(string ticker, string brokerage)
        {
            var provider = new LocalDiskShortableProvider(SecurityType.Equity, brokerage, QuantConnect.Market.USA);
            var date = new DateTime(2020, 12, 21);
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(ticker, QuantConnect.Market.USA, mappingResolveDate: date), ticker);

            Assert.AreEqual(0, provider.ShortableQuantity(symbol, date).Value);
        }
    }
}
