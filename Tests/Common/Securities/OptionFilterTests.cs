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
    public class OptionFilterTests
    {
        [Test]
        [TestCase(9.5)]
        [TestCase(10)]
        public void FiltersStrikeRange(decimal underlyingPrice)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Strikes(-2, 3)
                                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
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

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(underlyingPrice == 10 ? 6 : 5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0]);
            Assert.AreEqual(symbols[4], filtered[1]);
            Assert.AreEqual(symbols[5], filtered[2]);
            Assert.AreEqual(symbols[6], filtered[3]);
            Assert.AreEqual(symbols[7], filtered[4]);
            if (underlyingPrice == 10)
            {
                Assert.AreEqual(symbols[8], filtered[5]);
            }
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        [TestCase(7.5)]
        [TestCase(8)]
        public void FiltersStrikeRangeWithVaryingDistance(decimal underlyingPrice)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Strikes(-2, 2)
                                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
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

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(underlyingPrice == 8 ? 5 : 4, filtered.Count);
            Assert.AreEqual(symbols[1], filtered[0]);
            Assert.AreEqual(symbols[2], filtered[1]);
            Assert.AreEqual(symbols[3], filtered[2]);
            Assert.AreEqual(symbols[4], filtered[3]);
            if (underlyingPrice == 8)
            {
                Assert.AreEqual(symbols[5], filtered[4]);
            }
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        [TestCase(14)]
        [TestCase(15)]
        public void FiltersStrikeRangeWithNegativeMaxStrike(decimal underlyingPrice)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                .Strikes(-3, -1)
                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
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

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[5], filtered[0]);
            Assert.AreEqual(symbols[6], filtered[1]);
            Assert.AreEqual(symbols[7], filtered[2]);
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        [TestCase(14)]
        [TestCase(15)]
        public void FiltersStrikeRangeWithNegativeMaxStrikeOutOfRange(decimal underlyingPrice)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                .Strikes(-3, -1)
                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 1
            };

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(0, filtered.Count);
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        public void FiltersStrikeRangeWithPositiveMinStrike(decimal underlyingPrice)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                .Strikes(1, 3)
                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
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

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[2], filtered[0]);
            Assert.AreEqual(symbols[3], filtered[1]);
            Assert.AreEqual(symbols[4], filtered[2]);
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        [TestCase(20)]
        [TestCase(21)]
        public void FiltersStrikeRangeWithPositiveMinStrikeOutOfRange(decimal underlyingPrice)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                .Strikes(1, 3)
                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 1
            };

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(0, filtered.Count);
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        public void FiltersStrikeRangeWhenEmpty()
        {
            var underlying = new Tick { Value = 7.5m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                .Strikes(-2, 2)
                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new Symbol[] { };

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(0, filtered.Count);
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        public void FiltersExpiryRange()
        {
            var time = new DateTime(2016, 02, 26);
            var underlying = new Tick { Value = 10m, Time = time };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                    .Strikes(-10, 10)
                    .Expiration(TimeSpan.FromDays(3), TimeSpan.FromDays(7));

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
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
            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0]);
            Assert.AreEqual(symbols[4], filtered[1]);
            Assert.AreEqual(symbols[5], filtered[2]);
            Assert.AreEqual(symbols[6], filtered[3]);
            Assert.AreEqual(symbols[7], filtered[4]);
            Assert.AreEqual(true, filterUniverse.IsDynamic);
        }

        [Test]
        public void FiltersOutWeeklys()
        {
            var expiry1 = new DateTime(2017, 01, 04);
            var expiry2 = new DateTime(2017, 01, 06);
            var expiry3 = new DateTime(2017, 01, 11);
            var expiry4 = new DateTime(2017, 01, 13);
            var expiry5 = new DateTime(2017, 01, 18);
            var expiry6 = new DateTime(2017, 01, 20); // standard
            var expiry7 = new DateTime(2017, 01, 25);
            var expiry8 = new DateTime(2017, 01, 27);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 12, 29) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe;

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry2),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry3),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry4),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry5),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry6), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry6), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry6), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry7), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry8), // 9
            };

            var filtered = filter.Filter(new OptionFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[5], filtered[0]);
            Assert.AreEqual(symbols[6], filtered[1]);
            Assert.AreEqual(symbols[7], filtered[2]);
        }

        [Test]
        public void FiltersOutWeeklysIfFridayHoliday()
        {
            var expiry1 = new DateTime(2017, 01, 04);
            var expiry2 = new DateTime(2017, 01, 06);
            var expiry3 = new DateTime(2017, 01, 11);
            var expiry4 = new DateTime(2017, 01, 13);
            var expiry5 = new DateTime(2017, 01, 18);
            var expiry6 = new DateTime(2017, 01, 19); // standard monthly contract expiration. Friday -holiday
            var expiry7 = new DateTime(2017, 01, 25);
            var expiry8 = new DateTime(2017, 01, 27);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 12, 29) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe;

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry2),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry3),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry4),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry5),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry6), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry6), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry6), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry7), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry8), // 9
            };

            var filtered = filter.Filter(new OptionFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[5], filtered[0]);
            Assert.AreEqual(symbols[6], filtered[1]);
            Assert.AreEqual(symbols[7], filtered[2]);
        }

        [Test]
        public void FiltersOutStandardContracts()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.WeeklysOnly();

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry1),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry1),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry1),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry2),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry2), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry3), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry3), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry4), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry4), // 9
            };

            var filtered = filter.Filter(new OptionFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(8, filtered.Count);
        }

        [Test]
        public void FiltersOutNothingAfterFilteringByType()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.IncludeWeeklys();

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry1),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry1),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry1),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry2),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry2), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry3), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry3), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry4), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry4), // 9
            };
            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(10, filtered.Count);
            Assert.AreEqual(false, filterUniverse.IsDynamic);
        }

        [Test]
        public void FiltersFrontMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.FrontMonth();

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry1),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry1),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry1),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry2),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry2), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry3), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry3), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry4), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry4), // 9
            };

            var filtered = filter.Filter(new OptionFilterUniverse(symbols, underlying)).ToList();
            Assert.AreEqual(4, filtered.Count);
        }

        [Test]
        public void FiltersBackMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.BackMonth();

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry1),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry1),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry1),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry2),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry2), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry2), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry3), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry4), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry4), // 9
            };

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(false, filterUniverse.IsDynamic);
        }

        [Test]
        public void LinqExpressionsImmediatelyMakeUniverseDynamic()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => from x in universe
                                                                                        where x.ID.Date > new DateTime(2016, 12, 15)
                                                                                        select x;

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry1),  // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry1),  // 1
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry1),  // 2
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry1),  // 3
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry2),  // 4
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry2), // 5
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry2), // 6
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry3), // 7
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry4), // 8
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry4), // 9
            };

            var u = new OptionFilterUniverse(symbols, underlying);
            var filtered = filter.Filter(u).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(true, u.IsDynamic);
        }
    }
}
