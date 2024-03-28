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
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data.Shortable;

namespace QuantConnect.Tests.Common.Data.Shortable
{
    [TestFixture]
    public class ShortableProviderTests
    {
        private readonly Dictionary<string, Dictionary<Symbol, ShortableData>[]> _resultsByBrokerage = new();
        private Symbol[] _symbols;
        
        [SetUp]
        public void SetupConfig()
        {
            Config.Set("data-folder", "TestData");
            Globals.Reset();

            _symbols = new[] { "AAPL", "GOOG", "BAC" }
               .Select(x => new Symbol(SecurityIdentifier.GenerateEquity(x, QuantConnect.Market.USA, mappingResolveDate: new DateTime(2021, 1, 4)), x))
               .ToArray();

            _resultsByBrokerage["testinteractivebrokers"] = new[]
            {
                new Dictionary<Symbol, ShortableData>
                {
                    { _symbols[0], new(2000, 0.0507m, 0.0025m) },
                    { _symbols[1], new(5000, 0.0517m, 0.0035m) },
                    { _symbols[2], new(null, 0, 0) } // we have no data for this symbol
                },
                new Dictionary<Symbol, ShortableData>
                {
                    { _symbols[0], new(4000, 0.0509m, 0.003m) },
                    { _symbols[1], new(10000, 0.0519m, 0.004m) },
                    { _symbols[2], new(null, 0, 0) } // we have no data for this symbol
                }
            };

            _resultsByBrokerage["testbrokerage"] = new[]
{
                new Dictionary<Symbol, ShortableData>
                {
                    { _symbols[0], new(2000, 0, 0) },
                    { _symbols[1], new(5000, 0, 0) },
                    { _symbols[2], new(null, 0, 0) } // we have no data for this symbol
                },
                new Dictionary<Symbol, ShortableData>
                {
                    { _symbols[0], new(4000, 0, 0) },
                    { _symbols[1], new(10000, 0, 0) },
                    { _symbols[2], new(null, 0, 0) } // we have no data for this symbol
                }
            };
        }

        [TearDown]
        public void ResetConfig()
        {
            Config.Reset();
            Globals.Reset();
        }
        
        [TestCase("testbrokerage")]
        [TestCase("testinteractivebrokers")]
        public void LocalDiskShortableProviderGetsDataBySymbol(string brokerage)
        {
            var shortableProvider = new LocalDiskShortableProvider(brokerage);
            var results = _resultsByBrokerage[brokerage];

            var dates = new[]
            {
                new DateTime(2020, 12, 21),
                new DateTime(2020, 12, 22)
            };

            foreach (var symbol in _symbols)
            {
                for (var i = 0; i < dates.Length; i++)
                {
                    var date = dates[i];
                    var shortableQuantity = shortableProvider.ShortableQuantity(symbol, date);
                    var rebateRate = shortableProvider.RebateRate(symbol, date);
                    var feeRate = shortableProvider.FeeRate(symbol, date);

                    Assert.AreEqual(results[i][symbol].ShortableQuantity, shortableQuantity);
                    Assert.AreEqual(results[i][symbol].RebateRate, rebateRate);
                    Assert.AreEqual(results[i][symbol].FeeRate, feeRate);
                }
            }
        }

        [TestCase("AAPL", "nobrokerage")]
        [TestCase("SPY", "testbrokerage")]
        public void LocalDiskShortableProviderDefaultsToNullForMissingData(string ticker, string brokerage)
        {
            var provider = new LocalDiskShortableProvider(brokerage);
            var date = new DateTime(2020, 12, 21);
            var symbol = new Symbol(SecurityIdentifier.GenerateEquity(ticker, QuantConnect.Market.USA, mappingResolveDate: date), ticker);

            Assert.IsFalse(provider.ShortableQuantity(symbol, date).HasValue);
            Assert.AreEqual(0, provider.RebateRate(symbol, date));
            Assert.AreEqual(0, provider.FeeRate(symbol, date));
        }

        private record ShortableData(long? ShortableQuantity, decimal RebateRate, decimal FeeRate);
    }
}
