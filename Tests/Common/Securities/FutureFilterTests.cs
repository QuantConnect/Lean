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
using QuantConnect.Data.UniverseSelection;
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

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe
                    .Expiration(TimeSpan.FromDays(3), TimeSpan.FromDays(7));

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(0)), // 0
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(1)), // 1
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(2)), // 2
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(3)), // 3
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(4)), // 4
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(5)), // 5
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(6)), // 6
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(7)), // 7
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(8)), // 8
                Symbol.CreateFuture("SPY", Market.CME, time.AddDays(9)), // 9
            };
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, time)).Select(x => x.Symbol).ToList();
            Assert.AreEqual(5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0]);
            Assert.AreEqual(symbols[4], filtered[1]);
            Assert.AreEqual(symbols[5], filtered[2]);
            Assert.AreEqual(symbols[6], filtered[3]);
            Assert.AreEqual(symbols[7], filtered[4]);
        }

        [Test]
        public void FiltersOutWeeklysByDefault()
        {
            var time = new DateTime(2016, 02, 17, 13, 0, 0);

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe;

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(0)), // 0 Standard!!
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(1)), // 1
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(2)), // 2
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(8)), // 8
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(16)), // 16
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(28)), // 28 Standard!!
            };
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, time)).Select(x => x.Symbol).ToList();
            Assert.AreEqual(2, filtered.Count);
            Assert.AreEqual(symbols[0], filtered[0]);
            Assert.AreEqual(symbols[5], filtered[1]);
        }

        [Test]
        public void WeeklysFilterDoesNotFilterStandardContractWithExpiryMonthPriorOrAfterContractMonth()
        {
            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe;

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("CL", Market.NYMEX, new DateTime(2020, 11, 20)),
                Symbol.CreateFuture("DC", Market.CME, new DateTime(2021, 2, 2)),
                Symbol.CreateFuture("HO", Market.NYMEX, new DateTime(2020, 12, 31)),
                Symbol.CreateFuture("DY", Market.CME, new DateTime(2021, 2, 2)),
                Symbol.CreateFuture("YO", Market.NYMEX, new DateTime(2021, 4, 30)),
                Symbol.CreateFuture("NG", Market.NYMEX, new DateTime(2020, 11, 25))
            };
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var standardContracts = filter.Filter(new FutureFilterUniverse(data, new DateTime(2020, 1, 1))).Select(x => x.Symbol).ToList();
            Assert.AreEqual(6, standardContracts.Count);
            Assert.AreEqual(symbols[0], standardContracts[0]);
            Assert.AreEqual(symbols[1], standardContracts[1]);
            Assert.AreEqual(symbols[2], standardContracts[2]);
            Assert.AreEqual(symbols[3], standardContracts[3]);
            Assert.AreEqual(symbols[4], standardContracts[4]);
            Assert.AreEqual(symbols[5], standardContracts[5]);
        }

        [Test]
        public void FilterAllowBothTypes()
        {
            var time = new DateTime(2016, 02, 17, 13, 0, 0);

            // Include Weeklys to get both types of contracts through
            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.IncludeWeeklys();

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(0)), // 0 Standard!!
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(1)), // 1
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(2)), // 2
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(8)), // 8
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(16)), // 16
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(28)), // 28 Standard!!
            };
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, time)).Select(x => x.Symbol).ToList();
            Assert.AreEqual(6, filtered.Count);
            Assert.AreEqual(symbols, filtered);
        }

        [Test]
        public void FilterOutStandards()
        {
            var time = new DateTime(2016, 02, 17, 13, 0, 0);

            // Weeklys only to drop standard contracts
            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.WeeklysOnly();

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(0)), // 0 Standard!!
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(1)), // 1
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(2)), // 2
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(8)), // 8
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(16)), // 16
                Symbol.CreateFuture("VX", Market.CFE, time.AddDays(28)), // 28 Standard!!
            };
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, time)).Select(x => x.Symbol).ToList();
            Assert.AreEqual(4, filtered.Count);
            Assert.AreEqual(symbols[1], filtered[0]);
            Assert.AreEqual(symbols[2], filtered[1]);
            Assert.AreEqual(symbols[3], filtered[2]);
            Assert.AreEqual(symbols[4], filtered[3]);
        }

        [Test]
        public void FiltersFrontMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16);
            var expiry4 = new DateTime(2016, 12, 23);

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.FrontMonth();

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
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
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, new DateTime(2016, 02, 26))).ToList();
            Assert.AreEqual(4, filtered.Count);
        }

        [Test]
        public void FiltersBackMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16);
            var expiry4 = new DateTime(2016, 12, 23);

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.BackMonth();

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
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
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, new DateTime(2016, 02, 26))).ToList();
            Assert.AreEqual(3, filtered.Count);
        }

        [Test]
        public void FiltersExpirationCycles()
        {
            var expiry1 = new DateTime(2016, 1, 02);
            var expiry2 = new DateTime(2016, 3, 09);
            var expiry3 = new DateTime(2016, 8, 16);
            var expiry4 = new DateTime(2016, 12, 23);

            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe.ExpirationCycle(FutureExpirationCycles.March);

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
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
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            var filtered = filter.Filter(new FutureFilterUniverse(data, new DateTime(2016, 02, 26))).ToList();
            Assert.AreEqual(5, filtered.Count);
        }

        [Test]
        public void FilterTypeDoesNotBreakOnMissingExpiryFunction()
        {
            var time = new DateTime(2016, 02, 17, 13, 0, 0);
            var underlying = new Tick { Value = 10m, Time = time };

            // By Default only includes standards
            Func<FutureFilterUniverse, FutureFilterUniverse> universeFunc = universe => universe;

            Func<IDerivativeSecurityFilterUniverse<FutureUniverse>, IDerivativeSecurityFilterUniverse<FutureUniverse>> func =
                universe => universeFunc(universe as FutureFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<FutureUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateFuture("VX", Market.USA, time.AddDays(0)), // There is no Expiry function for VX on Market.USA
            };
            var data = symbols.Select(x => new FutureUniverse() { Symbol = x });

            // Since this is a unidentifiable symbol for our expiry functions it will return true and be passed through
            var filtered = filter.Filter(new FutureFilterUniverse(data, time)).Select(x => x.Symbol).ToList();
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(symbols[0], filtered[0]);
        }
    }
}
