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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Index;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class OptionFilterTests
    {

        [TestCaseSource(nameof(FiltersStrikeRangeTests))]
        public void FiltersStrikeRange(decimal underlyingPrice, Symbol[] symbols, int filteredNumber)
        {
            var expiry = new DateTime(2016, 03, 04);
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Strikes(-2, 3)
                                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);

            var underlyingScaleFactor = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(Market.USA, symbols.First(), symbols.First().SecurityType, "USD").StrikeMultiplier;
            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var filterUniverse = new OptionFilterUniverse(option, data, underlying, underlyingScaleFactor);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(filteredNumber, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0].Symbol);
            Assert.AreEqual(symbols[4], filtered[1].Symbol);
            Assert.AreEqual(symbols[5], filtered[2].Symbol);
            Assert.AreEqual(symbols[6], filtered[3].Symbol);
            Assert.AreEqual(symbols[7], filtered[4].Symbol);
            if (underlyingPrice == 10)
            {
                Assert.AreEqual(symbols[8], filtered[5].Symbol);
            }
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(underlyingPrice == 8 ? 5 : 4, filtered.Count);
            Assert.AreEqual(symbols[1], filtered[0].Symbol);
            Assert.AreEqual(symbols[2], filtered[1].Symbol);
            Assert.AreEqual(symbols[3], filtered[2].Symbol);
            Assert.AreEqual(symbols[4], filtered[3].Symbol);
            if (underlyingPrice == 8)
            {
                Assert.AreEqual(symbols[5], filtered[4].Symbol);
            }
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[5], filtered[0].Symbol);
            Assert.AreEqual(symbols[6], filtered[1].Symbol);
            Assert.AreEqual(symbols[7], filtered[2].Symbol);
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 1
            };

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(0, filtered.Count);
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[2], filtered[0].Symbol);
            Assert.AreEqual(symbols[3], filtered[1].Symbol);
            Assert.AreEqual(symbols[4], filtered[2].Symbol);
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
            var symbols = new[]
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 0
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 1
            };

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(0, filtered.Count);
        }

        [Test]
        public void FiltersStrikeRangeWhenEmpty()
        {
            var underlying = new Tick { Value = 7.5m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                .Strikes(-2, 2)
                .Expiration(TimeSpan.Zero, TimeSpan.MaxValue);

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
            var symbols = new Symbol[] { };

            var underlyingSymbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var canonical = Symbol.CreateCanonicalOption(underlyingSymbol);
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(0, filtered.Count);
        }

        [Test]
        public void FiltersExpiryRange()
        {
            var time = new DateTime(2016, 02, 26);
            var underlying = new Tick { Value = 10m, Time = time };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                    .Strikes(-10, 10)
                    .Expiration(TimeSpan.FromDays(3), TimeSpan.FromDays(7));

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(5, filtered.Count);
            Assert.AreEqual(symbols[3], filtered[0].Symbol);
            Assert.AreEqual(symbols[4], filtered[1].Symbol);
            Assert.AreEqual(symbols[5], filtered[2].Symbol);
            Assert.AreEqual(symbols[6], filtered[3].Symbol);
            Assert.AreEqual(symbols[7], filtered[4].Symbol);
        }

        [Test]
        public void FiltersExpiryRangeAfterNonTradableDay()
        {
            var time = new DateTime(2023, 12, 30); // Saturday
            var underlying = new TradeBar { Value = 10m, Time = time.AddDays(-1), EndTime = time };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.Expiration(0, 5);

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
            var symbols = Enumerable.Range(3, 10)
                .SelectMany(i =>
                    Enumerable.Range(1, 3).Select(j => Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 10 * j, time.AddDays(i))))
                .ToArray();

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();

            // Expiry range is 0 to 5 days, so 6 days times 3 strikes per day
            var expectedSelections = 6 * 3;
            Assert.AreEqual(expectedSelections, filtered.Count);
            for (int i = 0; i < expectedSelections; i++)
            {
                Assert.AreEqual(symbols[i], filtered[i].Symbol);
            }
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filtered = filter.Filter(new OptionFilterUniverse(option, data, underlying)).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[5], filtered[0].Symbol);
            Assert.AreEqual(symbols[6], filtered[1].Symbol);
            Assert.AreEqual(symbols[7], filtered[2].Symbol);
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filtered = filter.Filter(new OptionFilterUniverse(option, data, underlying)).ToList();
            Assert.AreEqual(3, filtered.Count);
            Assert.AreEqual(symbols[5], filtered[0].Symbol);
            Assert.AreEqual(symbols[6], filtered[1].Symbol);
            Assert.AreEqual(symbols[7], filtered[2].Symbol);
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filtered = filter.Filter(new OptionFilterUniverse(option, data, underlying)).ToList();
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

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse).ApplyTypesFilter();

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(10, filtered.Count);
        }

        [Test]
        public void FiltersFrontMonth()
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.IncludeWeeklys().FrontMonth();

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filtered = filter.Filter(new OptionFilterUniverse(option, data, underlying)).ToList();
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

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe.IncludeWeeklys().BackMonth();

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(3, filtered.Count);
        }

        [TestCase("[data.symbol for data in universe][:5]")]
        [TestCase("lambda contracts_data: [data.symbol for data in contracts_data][:5]")]
        public void SetsContractsPython(string code)
        {
            var expiry1 = new DateTime(2016, 12, 02);
            var expiry2 = new DateTime(2016, 12, 09);
            var expiry3 = new DateTime(2016, 12, 16); // standard
            var expiry4 = new DateTime(2016, 12, 23);

            var underlying = new Tick { Value = 10m, Time = new DateTime(2016, 02, 26) };

            using var _ = Py.GIL();
            var module = PyModule.FromString("SetsContractsPython",
                        @$"
from AlgorithmImports import *

def set_filter(universe: OptionFilterUniverse) -> OptionFilterUniverse:
    return universe.Contracts({code})
        ");
            var setFilter = module.GetAttr("set_filter");

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe =>
            {
                using var _ = Py.GIL();
                using var pyUniverse = universe.ToPython();
                return setFilter.Invoke(pyUniverse).GetAndDispose<OptionFilterUniverse>();
            };

            Func<IDerivativeSecurityFilterUniverse<OptionUniverse>, IDerivativeSecurityFilterUniverse<OptionUniverse>> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter<OptionUniverse>(func);
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

            var canonical = symbols[0].Canonical;
            var option = CreateOptionSecurity(canonical);

            var data = symbols.Select(x => new OptionUniverse() { Symbol = x });
            var filterUniverse = new OptionFilterUniverse(option, data, underlying);
            var filtered = filter.Filter(filterUniverse).ToList();
            Assert.AreEqual(5, filtered.Count);
        }

        static Symbol[] CreateOptions(string ticker, string targetOption = null)
        {
            var expiry = new DateTime(2016, 03, 04);
            if (string.IsNullOrEmpty(targetOption))
            {
                return new[] {
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry),  // 0
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry),  // 1
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry),  // 2
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry),  // 3
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry),  // 4
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry), // 5
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry), // 6
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry), // 7
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 8
                    Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 9
                };
            }
            else
            {
                var indexSymbol = Symbol.Create(ticker, SecurityType.Index, Market.USA);
                return new[] {
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry),  // 0
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 5, expiry),  // 1
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 7, expiry),  // 2
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 8, expiry),  // 3
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 9, expiry),  // 4
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 10, expiry), // 5
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 11, expiry), // 6
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 12, expiry), // 7
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 15, expiry), // 8
                    Symbol.CreateOption(indexSymbol, targetOption, Market.USA, OptionStyle.American, OptionRight.Put, 20, expiry), // 9
                };
            }
        }

        public static object[] FiltersStrikeRangeTests =
        {
            new object[] {9.5m, CreateOptions("SPY", null), 5},
            new object[] {10m, CreateOptions("SPY", null), 6},
            new object[] {45.5m, CreateOptions("NDX", "NQX"), 5},
            new object[] {50m, CreateOptions("NDX", "NQX"), 6}
        };

        private static Option CreateOptionSecurity(Symbol canonical)
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar), canonical, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);

            if (canonical.SecurityType == SecurityType.Option)
            {
                return new Option(
                    MarketHoursDatabase.FromDataFolder().GetExchangeHours(config),
                    config,
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null);
            }

            var indexConfig = new SubscriptionDataConfig(typeof(TradeBar), canonical.Underlying, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            var index = new QuantConnect.Securities.Index.Index(
                MarketHoursDatabase.FromDataFolder().GetExchangeHours(indexConfig),
                new Cash(Currencies.USD, 0, 1m),
                indexConfig,
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);

            return new QuantConnect.Securities.IndexOption.IndexOption(
                canonical,
                MarketHoursDatabase.FromDataFolder().GetExchangeHours(config),
                new Cash(Currencies.USD, 0, 1m),
                new QuantConnect.Securities.IndexOption.IndexOptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                index);
        }
    }
}
