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
    public class FutureFilterTests
    {
        [Test]
        public void FiltersExpiryRange()
        {
            var time = new DateTime(2016, 02, 26);
            var underlying = new Tick { Value = 10m, Time = time };

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe
                    .Expiration(TimeSpan.FromDays(3), TimeSpan.FromDays(7));

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(0)), // 0
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(1)), // 1
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(2)), // 2
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(3)), // 3
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(4)), // 4
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(5)), // 5
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(6)), // 6
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(7)), // 7
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(8)), // 8
                Symbol.CreateFuture("SPY", Market.USA, time.AddDays(9)), // 9
            };

            var filtered = filter.Filter(new FutureFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0]);
            Assert.AreEqual(symbols[4], filtered[1]);
            Assert.AreEqual(symbols[5], filtered[2]);
            Assert.AreEqual(symbols[6], filtered[3]);
            Assert.AreEqual(symbols[7], filtered[4]);
        }

        [Test]
        public void FiltersFrontMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); 
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.FrontMonth();

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 0
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 1
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 2
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 3
                Symbol.CreateFuture("SPY", Market.USA, expiry2),  // 4
                Symbol.CreateFuture("SPY", Market.USA, expiry2), // 5
                Symbol.CreateFuture("SPY", Market.USA, expiry3), // 6
                Symbol.CreateFuture("SPY", Market.USA, expiry3), // 7
                Symbol.CreateFuture("SPY", Market.USA, expiry4), // 8
                Symbol.CreateFuture("SPY", Market.USA, expiry4), // 9
            };

            var filtered = filter.Filter(new FutureFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(4, filtered.Count);
        }

        [Test]
        public void FiltersBackMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16);
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.BackMonth();

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 0
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 1
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 2
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 3
                Symbol.CreateFuture("SPY", Market.USA, expiry2),  // 4
                Symbol.CreateFuture("SPY", Market.USA, expiry2), // 5
                Symbol.CreateFuture("SPY", Market.USA, expiry2), // 6
                Symbol.CreateFuture("SPY", Market.USA, expiry3), // 7
                Symbol.CreateFuture("SPY", Market.USA, expiry4), // 8
                Symbol.CreateFuture("SPY", Market.USA, expiry4), // 9
            };

            var filtered = filter.Filter(new FutureFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(3, filtered.Count);
        }

        [Test]
        public void FiltersExpirationCycles()
        {
            var expiry1 = new DateTime(2016, 1, 02);
            var expiry2 = new DateTime(2016, 3, 09);
            var expiry3 = new DateTime(2016, 8, 16); 
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.ExpirationCycle(FutureExpirationCycles.March);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 0
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 1
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 2
                Symbol.CreateFuture("SPY", Market.USA, expiry1),  // 3
                Symbol.CreateFuture("SPY", Market.USA, expiry2),  // 4
                Symbol.CreateFuture("SPY", Market.USA, expiry2), // 5
                Symbol.CreateFuture("SPY", Market.USA, expiry2), // 6
                Symbol.CreateFuture("SPY", Market.USA, expiry3), // 7
                Symbol.CreateFuture("SPY", Market.USA, expiry4), // 8
                Symbol.CreateFuture("SPY", Market.USA, expiry4), // 9
            };

            var filtered = filter.Filter(new FutureFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(5, filtered.Count);
        }
    }
}
