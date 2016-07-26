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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class StrikeExpiryOptionFilterTests
    {
        [Test]
        public void FiltersStrikeRange()
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };
            var filter = new StrikeExpiryOptionFilter(-2, 3, TimeSpan.Zero, TimeSpan.MaxValue);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 9
            };

            var filtered = filter.Filter(symbols, underlying).ToList();
            Assert.AreEqual(5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0]);
            Assert.AreEqual(symbols[4], filtered[1]);
            Assert.AreEqual(symbols[5], filtered[2]);
            Assert.AreEqual(symbols[6], filtered[3]);
            Assert.AreEqual(symbols[7], filtered[4]);
        }

        [Test]
        public void FiltersExpiryRange()
        {
            var time = new DateTime(2016, 02, 26);
            var underlying = new Tick { Value = 10m, Time = time };
            var filter = new StrikeExpiryOptionFilter(0, 0, TimeSpan.FromDays(3), TimeSpan.FromDays(7));
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(0)), // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(1)), // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(2)), // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(3)), // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(4)), // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(5)), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(6)), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(7)), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(8)), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, time.AddDays(9)), // 9
            };

            var filtered = filter.Filter(symbols, underlying).ToList();
            Assert.AreEqual(5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0]);
            Assert.AreEqual(symbols[4], filtered[1]);
            Assert.AreEqual(symbols[5], filtered[2]);
            Assert.AreEqual(symbols[6], filtered[3]);
            Assert.AreEqual(symbols[7], filtered[4]);
        }
    }
}
