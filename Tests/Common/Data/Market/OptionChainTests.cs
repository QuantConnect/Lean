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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class OptionChainTests
    {
        private static readonly DateTime Date = new(2016, 2, 26);
        private static readonly Symbol Canonical = Symbol.CreateCanonicalOption(Symbols.SPY);
        private const decimal UnderlyingPrice = 101m;
        private static readonly DateTime[] Expiries = { new(2016, 3, 4), new(2016, 3, 18), new(2016, 4, 15), new(2016, 6, 17) };
        private static readonly decimal[] Strikes = { 90m, 95m, 97.5m, 100m, 102.5m, 105m, 110m };

        private List<OptionUniverse> _data;
        private BaseData _underlying;
        private SymbolProperties _symbolProperties;
        private Option _option;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            (_data, _underlying) = CreateUniverseData(Date, UnderlyingPrice, Expiries, Strikes);
            _symbolProperties = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(QuantConnect.Market.USA, Canonical, SecurityType.Option, Currencies.USD);
            _option = CreateOption();
        }

        private static IEnumerable<TestCaseData> FilterCases()
        {
            yield return Case("Strikes(-2, 2)", u => u.Strikes(-2, 2), c => c.Strikes(-2, 2));
            yield return Case("Strikes(0, 0)", u => u.Strikes(0, 0), c => c.Strikes(0, 0));
            yield return Case("Strikes(-1, 0)", u => u.Strikes(-1, 0), c => c.Strikes(-1, 0));
            yield return Case("Strikes(-100, -10)", u => u.Strikes(-100, -10), c => c.Strikes(-100, -10), empty: true);
            yield return Case("Expiration(0, 10)", u => u.Expiration(0, 10), c => c.Expiration(0, 10));
            yield return Case("Expiration(10, 60)", u => u.Expiration(10, 60), c => c.Expiration(10, 60));
            yield return Case("Expiration(TimeSpan)", u => u.Expiration(TimeSpan.FromDays(30), TimeSpan.FromDays(200)),
                c => c.Expiration(TimeSpan.FromDays(30), TimeSpan.FromDays(200)));
            yield return Case("Expiration(500, 600)", u => u.Expiration(500, 600), c => c.Expiration(500, 600), empty: true);
            yield return Case("Expiration(dates)", u => u.Expiration([Expiries[1], Expiries[3]]), c => c.Expiration([Expiries[1], Expiries[3]]));
            yield return Case("Expiration(date, time of day)", u => u.Expiration([Expiries[1].AddHours(10)]), c => c.Expiration([Expiries[1].AddHours(10)]));
            yield return Case("Expiration(unlisted dates)", u => u.Expiration([Date, Date.AddDays(1)]), c => c.Expiration([Date, Date.AddDays(1)]), empty: true);
            yield return Case("Expiration(no dates)", u => u.Expiration([]), c => c.Expiration([]), empty: true);
            yield return Case("ExpiringAfter", u => u.ExpiringAfter(Expiries[1]), c => c.ExpiringAfter(Expiries[1]));
            yield return Case("ExpiringBefore", u => u.ExpiringBefore(Expiries[1].AddHours(10)), c => c.ExpiringBefore(Expiries[1].AddHours(10)));
            yield return Case("ExpiringAfter.ExpiringBefore", u => u.ExpiringAfter(Expiries[0]).ExpiringBefore(Expiries[3]), c => c.ExpiringAfter(Expiries[0]).ExpiringBefore(Expiries[3]));
            yield return Case("ExpiringAfter(last)", u => u.ExpiringAfter(Expiries[3]), c => c.ExpiringAfter(Expiries[3]), empty: true);
            yield return Case("FarthestExpiration", u => u.FarthestExpiration(), c => c.FarthestExpiration());
            yield return Case("StandardsOnly.FarthestExpiration", u => u.StandardsOnly().FarthestExpiration(), c => c.StandardsOnly().FarthestExpiration());
            yield return Case("Strikes(100, 105)", u => u.Strikes([100m, 105m]), c => c.Strikes([100m, 105m]));
            yield return Case("Strikes(101)", u => u.Strikes([101m]), c => c.Strikes([101m]), empty: true);
            yield return Case("StrikesAbove", u => u.StrikesAbove(100m), c => c.StrikesAbove(100m));
            yield return Case("StrikesBelow", u => u.StrikesBelow(100m), c => c.StrikesBelow(100m));
            yield return Case("StrikesAbove.StrikesBelow", u => u.StrikesAbove(95m).StrikesBelow(105m), c => c.StrikesAbove(95m).StrikesBelow(105m));
            yield return Case("StrikesAbove(max)", u => u.StrikesAbove(110m), c => c.StrikesAbove(110m), empty: true);
            yield return Case("ZeroDte", u => u.ZeroDte(), c => c.ZeroDte(), empty: true);
            yield return Case("CallsOnly", u => u.CallsOnly(), c => c.CallsOnly());
            yield return Case("PutsOnly", u => u.PutsOnly(), c => c.PutsOnly());
            yield return Case("OutOfTheMoney", u => u.OutOfTheMoney(), c => c.OutOfTheMoney());
            yield return Case("OTM.CallsOnly", u => u.OTM().CallsOnly(), c => c.OTM().CallsOnly());
            yield return Case("InTheMoney", u => u.InTheMoney(), c => c.InTheMoney());
            yield return Case("ITM.PutsOnly.Expiration(0, 10)", u => u.ITM().PutsOnly().Expiration(0, 10), c => c.ITM().PutsOnly().Expiration(0, 10));
            yield return Case("AtTheMoney", u => u.AtTheMoney(), c => c.AtTheMoney());
            yield return Case("AtTheMoney(0)", u => u.AtTheMoney(0), c => c.AtTheMoney(0), empty: true);
            yield return Case("AtTheMoney(1)", u => u.AtTheMoney(1m), c => c.AtTheMoney(1m));
            yield return Case("Expiration(0, 10).ATM(2.5)", u => u.Expiration(0, 10).ATM(2.5m), c => c.Expiration(0, 10).ATM(2.5m));
            yield return Case("StandardsOnly", u => u.StandardsOnly(), c => c.StandardsOnly());
            yield return Case("WeeklysOnly", u => u.WeeklysOnly(), c => c.WeeklysOnly());
            yield return Case("FrontMonth", u => u.FrontMonth(), c => c.FrontMonth());
            yield return Case("BackMonth", u => u.BackMonth(), c => c.BackMonth());
            yield return Case("BackMonths", u => u.BackMonths(), c => c.BackMonths());
            yield return Case("Delta", u => u.Delta(0.4m, 0.6m), c => c.Delta(0.4m, 0.6m));
            yield return Case("D", u => u.D(-0.6m, -0.4m), c => c.D(-0.6m, -0.4m));
            yield return Case("Delta(5, 6)", u => u.Delta(5, 6), c => c.Delta(5, 6), empty: true);
            yield return Case("Gamma", u => u.Gamma(0.012m, 0.02m), c => c.Gamma(0.012m, 0.02m));
            yield return Case("G", u => u.G(0.012m, 0.02m), c => c.G(0.012m, 0.02m));
            // theta is annualized from the per day value in the file
            yield return Case("Theta", u => u.Theta(-365, -219), c => c.Theta(-365, -219));
            yield return Case("T", u => u.T(-365, -219), c => c.T(-365, -219));
            yield return Case("Vega", u => u.Vega(6, 8), c => c.Vega(6, 8));
            yield return Case("V", u => u.V(6, 8), c => c.V(6, 8));
            yield return Case("Rho", u => u.Rho(2, 4), c => c.Rho(2, 4));
            yield return Case("R", u => u.R(2, 4), c => c.R(2, 4));
            yield return Case("ImpliedVolatility", u => u.ImpliedVolatility(0.16m, 0.18m), c => c.ImpliedVolatility(0.16m, 0.18m));
            yield return Case("IV", u => u.IV(0.16m, 0.18m), c => c.IV(0.16m, 0.18m));
            yield return Case("OpenInterest", u => u.OpenInterest(200, 500), c => c.OpenInterest(200, 500));
            yield return Case("OI", u => u.OI(200, 500), c => c.OI(200, 500));
            yield return Case("CallsOnly.Expiration.Strikes", u => u.CallsOnly().Expiration(0, 30).Strikes(-1, 1),
                c => c.CallsOnly().Expiration(0, 30).Strikes(-1, 1));
            yield return Case("PutsOnly.FrontMonth.Strikes", u => u.PutsOnly().FrontMonth().Strikes(-2, 0),
                c => c.PutsOnly().FrontMonth().Strikes(-2, 0));
            yield return Case("StandardsOnly.FrontMonth", u => u.StandardsOnly().FrontMonth(), c => c.StandardsOnly().FrontMonth());
            yield return Case("WeeklysOnly.CallsOnly", u => u.WeeklysOnly().CallsOnly(), c => c.WeeklysOnly().CallsOnly());
            yield return Case("Expiration.Delta.Strikes", u => u.Expiration(0, 30).Delta(0.4m, 0.6m).Strikes(-3, 3),
                c => c.Expiration(0, 30).Delta(0.4m, 0.6m).Strikes(-3, 3));
            yield return Case("NakedCall", u => u.NakedCall(10, 0), c => c.NakedCall(10, 0));
            yield return Case("NakedPut", u => u.NakedPut(10, -5), c => c.NakedPut(10, -5));
            yield return Case("NakedCall(1000)", u => u.NakedCall(1000, 0), c => c.NakedCall(1000, 0), empty: true);
            yield return Case("CallSpread", u => u.CallSpread(10, 5), c => c.CallSpread(10, 5));
            yield return Case("PutSpread", u => u.PutSpread(10, 5, -5), c => c.PutSpread(10, 5, -5));
            yield return Case("CallCalendarSpread", u => u.CallCalendarSpread(0, 10, 40), c => c.CallCalendarSpread(0, 10, 40));
            yield return Case("PutCalendarSpread", u => u.PutCalendarSpread(0, 10, 40), c => c.PutCalendarSpread(0, 10, 40));
            yield return Case("Strangle", u => u.Strangle(10, 5, -5), c => c.Strangle(10, 5, -5));
            yield return Case("Straddle", u => u.Straddle(10), c => c.Straddle(10));
            yield return Case("ProtectiveCollar", u => u.ProtectiveCollar(10, 5, -5), c => c.ProtectiveCollar(10, 5, -5));
            yield return Case("Conversion", u => u.Conversion(10, 5), c => c.Conversion(10, 5));
            yield return Case("CallButterfly", u => u.CallButterfly(10, 5), c => c.CallButterfly(10, 5));
            yield return Case("PutButterfly", u => u.PutButterfly(10, 5), c => c.PutButterfly(10, 5));
            yield return Case("IronButterfly", u => u.IronButterfly(10, 5), c => c.IronButterfly(10, 5));
            yield return Case("IronCondor", u => u.IronCondor(10, 5, 10), c => c.IronCondor(10, 5, 10));
            yield return Case("BoxSpread", u => u.BoxSpread(10, 5), c => c.BoxSpread(10, 5));
            yield return Case("JellyRoll", u => u.JellyRoll(0, 10, 40), c => c.JellyRoll(0, 10, 40));
            yield return Case("CallLadder", u => u.CallLadder(10, 10, 5, -5), c => c.CallLadder(10, 10, 5, -5));
            yield return Case("PutLadder", u => u.PutLadder(10, 10, 5, -5), c => c.PutLadder(10, 10, 5, -5));
            yield return Case("StandardsOnly.IronCondor", u => u.StandardsOnly().IronCondor(10, 5, 10), c => c.StandardsOnly().IronCondor(10, 5, 10));
        }

        [TestCaseSource(nameof(FilterCases))]
        public void ChainFiltersMatchUniverseFilters(Func<OptionFilterUniverse, OptionFilterUniverse> universeFilter,
            Func<OptionChain, OptionChain> chainFilter, bool expectEmpty)
        {
            // the universe selection applies the contract type filters after the user filter
            var expected = universeFilter(CreateUniverse()).ApplyTypesFilter().AsEnumerable().Select(x => x.Symbol.Value).ToList();
            var actual = chainFilter(CreateChain()).Select(x => x.Symbol.Value).ToList();

            Assert.AreEqual(expectEmpty, expected.Count == 0);
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void ChainExposesEveryUniverseFilter()
        {
            // Contracts() takes explicit symbols or a selector, which only makes sense for the universe selection
            var universeFilters = typeof(OptionFilterUniverse)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.ReturnType == typeof(OptionFilterUniverse) && x.Name != "Contracts" && !x.IsDefined(typeof(ObsoleteAttribute)))
                .ToList();

            Assert.IsNotEmpty(universeFilters);
            foreach (var universeFilter in universeFilters)
            {
                var parameters = universeFilter.GetParameters().Select(x => x.ParameterType).ToArray();
                var chainFilter = typeof(IOptionContractFilters<OptionChain>).GetMethod(universeFilter.Name, parameters);
                Assert.IsNotNull(chainFilter, $"{universeFilter.Name}({string.Join(", ", parameters.Select(x => x.Name))}) is not an option chain filter");
            }
        }

        [Test]
        public void FilteredChainSharesTheAuxiliaryData()
        {
            var chain = CreateChain();
            var symbol = chain.First().Symbol;
            chain.AddData(new TestAuxData { Symbol = symbol, Time = Date, Value = 1 });

            var filtered = chain.PutsOnly();

            Assert.IsNotNull(chain.GetAux<TestAuxData>(symbol));
            Assert.AreSame(chain.GetAux<TestAuxData>(symbol), filtered.GetAux<TestAuxData>(symbol));
        }

        [Test]
        public void FilteredChainIsANewChainSharingTheSourceProperties()
        {
            var chain = CreateChain();
            var count = chain.Count;

            var filtered = chain.CallsOnly().FrontMonth();

            Assert.AreNotSame(chain, filtered);
            Assert.AreEqual(count, chain.Count);
            Assert.AreEqual(Strikes.Length, filtered.Count);
            Assert.IsTrue(filtered.All(x => x.Right == OptionRight.Call && x.Expiry == Expiries[0]));
            Assert.IsTrue(filtered.ContainsKey(filtered.First().Symbol));
            Assert.AreEqual(chain.Symbol, filtered.Symbol);
            Assert.AreEqual(chain.Time, filtered.Time);
            Assert.AreSame(chain.Underlying, filtered.Underlying);
        }

        [Test]
        public void UnderlyingIsTakenFromTheContractsData()
        {
            var chain = CreateChain();

            Assert.AreEqual(UnderlyingPrice, chain.Underlying.Price);
        }

        [Test]
        public void FiltersOnAnEmptyChainReturnAnEmptyChain()
        {
            var chain = new OptionChain(Canonical, Date);

            foreach (var testCase in FilterCases())
            {
                var filter = (Func<OptionChain, OptionChain>)testCase.Arguments[1];
                Assert.AreEqual(0, filter(chain).Count, testCase.TestName);
            }
        }

        [Test]
        public void FiltersUseTheExchangeTimeAsTheReferenceDate()
        {
            // The engine stamps slice chains in the algorithm time zone, which can already be the day after the exchange date
            var exchangeDate = new DateTime(2016, 3, 4);
            var (data, underlying) = CreateUniverseData(exchangeDate, UnderlyingPrice, new[] { exchangeDate, Expiries[1] }, Strikes);
            var chain = new OptionChain(Canonical, exchangeDate, data, _symbolProperties) { Time = exchangeDate.AddDays(1) };
            var universe = CreateUniverse(data, underlying, exchangeDate).Expiration(0, 0).ToList();

            Assert.AreEqual(2 * Strikes.Length, universe.Count);
            Assert.AreEqual(0, chain.Expiration(0, 0).Count);

            chain.ExchangeTime = exchangeDate;
            var filtered = chain.Expiration(0, 0);

            CollectionAssert.AreEquivalent(universe.Select(x => x.Symbol.Value), filtered.Select(x => x.Symbol.Value));
            Assert.AreEqual(chain.Time, filtered.Time);
            Assert.AreEqual(exchangeDate, filtered.ExchangeTime);
            CollectionAssert.AreEquivalent(universe.Where(x => x.ID.OptionRight == OptionRight.Call).Select(x => x.Symbol.Value),
                chain.CallsOnly().Expiration(0, 0).Select(x => x.Symbol.Value));
        }

        [Test]
        public void StrikesFilterIsSkippedWithoutUnderlyingPrice()
        {
            var contracts = _data.Select(x => new OptionUniverse(x) { Underlying = null }).ToList();
            var chain = new OptionChain(Canonical, Date, contracts, _symbolProperties);

            Assert.AreEqual(0, chain.Underlying.Price);
            Assert.AreEqual(chain.Count, chain.Strikes(0, 0).Count);
        }

        [Test]
        public void FiltersAreAvailableFromPython()
        {
            var chain = CreateChain();
            var expectedFiltered = chain.CallsOnly().Expiration(0, 30).Strikes(-1, 1).Select(x => x.Symbol).ToList();
            var expectedWhere = chain.Where(x => x.Right == OptionRight.Put && x.Strike > 100).Select(x => x.Symbol).ToList();
            Assert.IsNotEmpty(expectedFiltered);
            Assert.IsNotEmpty(expectedWhere);

            using (Py.GIL())
            {
                using var module = PyModule.FromString(nameof(OptionChainTests), @"
from AlgorithmImports import *

def filter_chain(chain):
    return chain.calls_only().expiration(0, 30).strikes(-1, 1)

def where_chain(chain):
    return chain.where(lambda contract: contract.right == OptionRight.PUT and contract.strike > 100)

def sets(chain):
    return chain.strikes([100, 105]).expiration([datetime(2016, 3, 18), datetime(2016, 6, 17)])

def bounds(chain):
    return chain.strikes_above(95).strikes_below(105).expiring_after(datetime(2016, 3, 4)).expiring_before(datetime(2016, 6, 17)).farthest_expiration()
");
                using var pyChain = chain.ToPython();

                using var filtered = module.GetAttr("filter_chain").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedFiltered, filtered.As<OptionChain>().Select(x => x.Symbol).ToList());

                using var where = module.GetAttr("where_chain").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedWhere, where.As<OptionChain>().Select(x => x.Symbol).ToList());

                // strike and date lists convert to the C# collections
                var expectedSets = chain.Strikes([100m, 105m]).Expiration([Expiries[1], Expiries[3]]).Select(x => x.Symbol).ToList();
                Assert.AreEqual(8, expectedSets.Count);
                using var sets = module.GetAttr("sets").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedSets, sets.As<OptionChain>().Select(x => x.Symbol).ToList());

                var expectedBounds = chain.StrikesAbove(95m).StrikesBelow(105m).ExpiringAfter(Expiries[0]).ExpiringBefore(Expiries[3]).FarthestExpiration()
                    .Select(x => x.Symbol).ToList();
                Assert.AreEqual(6, expectedBounds.Count);
                Assert.IsTrue(expectedBounds.All(x => x.ID.Date == Expiries[2]));
                using var bounds = module.GetAttr("bounds").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedBounds, bounds.As<OptionChain>().Select(x => x.Symbol).ToList());
            }
        }

        private static IEnumerable<TestCaseData> StrategyFilterCases()
        {
            yield return Case("NakedCall", u => u.NakedCall(10, 0), c => c.NakedCall(10, 0));
            yield return Case("CallSpread", u => u.CallSpread(10, 5, -5), c => c.CallSpread(10, 5, -5));
            yield return Case("CallCalendarSpread", u => u.CallCalendarSpread(0, 10, 40), c => c.CallCalendarSpread(0, 10, 40));
            yield return Case("Strangle", u => u.Strangle(10, 5, -5), c => c.Strangle(10, 5, -5));
            yield return Case("Straddle", u => u.Straddle(10), c => c.Straddle(10));
            yield return Case("ProtectiveCollar", u => u.ProtectiveCollar(10, 5, -5), c => c.ProtectiveCollar(10, 5, -5));
            yield return Case("Conversion", u => u.Conversion(10, 5), c => c.Conversion(10, 5));
            yield return Case("CallButterfly", u => u.CallButterfly(10, 5), c => c.CallButterfly(10, 5));
            yield return Case("IronButterfly", u => u.IronButterfly(10, 5), c => c.IronButterfly(10, 5));
            yield return Case("IronCondor", u => u.IronCondor(10, 5, 10), c => c.IronCondor(10, 5, 10));
            yield return Case("BoxSpread", u => u.BoxSpread(10, 5), c => c.BoxSpread(10, 5));
            yield return Case("JellyRoll", u => u.JellyRoll(0, 10, 40), c => c.JellyRoll(0, 10, 40));
            yield return Case("CallLadder", u => u.CallLadder(10, 5, 0, -5), c => c.CallLadder(10, 5, 0, -5));
        }

        [TestCaseSource(nameof(StrategyFilterCases))]
        public void StrategyFiltersSelectNothingWithoutUnderlyingPrice(Func<OptionFilterUniverse, OptionFilterUniverse> universeFilter,
            Func<OptionChain, OptionChain> chainFilter, bool _)
        {
            var contracts = _data.Select(x => new OptionUniverse(x) { Underlying = null }).ToList();
            var chain = new OptionChain(Canonical, Date, contracts, _symbolProperties);
            var universe = new OptionFilterUniverse(_option);
            universe.Refresh(contracts, null, Date);

            Assert.AreEqual(0, chain.Underlying.Price);
            Assert.AreEqual(0, universeFilter(universe).Count);
            Assert.AreEqual(0, chainFilter(chain).Count);
        }

        [Test]
        public void StrategyFiltersValidateArgumentsLikeTheUniverseFilters()
        {
            var contracts = _data.Select(x => new OptionUniverse(x) { Underlying = null }).ToList();
            var chains = new[] { CreateChain(), new OptionChain(Canonical, Date, contracts, _symbolProperties) };

            foreach (var chain in chains)
            {
                Assert.Throws<ArgumentException>(() => chain.Strangle(10, -5, 5));
                Assert.Throws<ArgumentException>(() => chain.CallSpread(10, 5, 10));
                Assert.Throws<ArgumentException>(() => chain.IronCondor(10, 10, 5));
                Assert.Throws<ArgumentException>(() => chain.CallCalendarSpread(0, 40, 10));
            }
        }

        // By default the closest strike is at the money within 1% of the price: 1 at 101, not 1.25 at 101.25 or 103.75
        [TestCase(100, null, 100)]
        [TestCase(101, null, 100)]
        [TestCase(101.25, null, null)]
        [TestCase(103.75, null, null)]
        // A zero tolerance requires a strike equal to the price
        [TestCase(100, 0, 100)]
        [TestCase(101, 0, null)]
        // Otherwise the closest strike within the tolerance
        [TestCase(101, 1, 100)]
        [TestCase(101, 0.5, null)]
        [TestCase(103.75, 1.25, 102.5)]
        // Equidistant between 100 and 102.5: the lower strike wins, unlike Strikes(0, 0)
        [TestCase(101.25, 1.25, 100)]
        [TestCase(101.25, 1, null)]
        public void MoneynessFiltersSplitTheStrikesAroundTheUnderlyingPrice(double underlyingPrice, double? tolerance, double? atmStrike)
        {
            var price = (decimal)underlyingPrice;
            var (data, _) = CreateUniverseData(Date, price, Expiries, Strikes);
            var chain = new OptionChain(Canonical, Date, data, _symbolProperties);
            Assert.AreEqual(price, chain.Underlying.Price);

            var otm = chain.OutOfTheMoney();
            var itm = chain.InTheMoney();
            Assert.IsNotEmpty(otm);
            Assert.IsNotEmpty(itm);
            Assert.IsTrue(otm.All(x => x.Right == OptionRight.Call ? x.Strike > price : x.Strike < price));
            Assert.IsTrue(itm.All(x => x.Right == OptionRight.Call ? x.Strike < price : x.Strike > price));
            // a strike equal to the price is neither out nor in the money
            Assert.AreEqual(chain.Count, otm.Count + itm.Count + chain.Strikes([price]).Count);

            var atm = chain.AtTheMoney((decimal?)tolerance);
            Assert.AreEqual(atmStrike.HasValue ? 2 * Expiries.Length : 0, atm.Count);
            Assert.IsTrue(atm.All(x => x.Strike == (decimal)atmStrike));
            Assert.Throws<ArgumentException>(() => chain.AtTheMoney(-1m));
            Assert.Throws<ArgumentException>(() => CreateUniverse().AtTheMoney(-1m));
        }

        [Test]
        public void FiltersWorkOnFutureOptionChains()
        {
            // March 2020 ES options on the March 2020 future, the universe rows carry the future price
            var future = Symbol.CreateFuture("ES", QuantConnect.Market.CME, new DateTime(2020, 3, 20));
            var canonical = Symbol.CreateCanonicalOption(future);
            var date = new DateTime(2020, 1, 3);
            var contracts = new List<(Symbol, decimal, decimal, Greeks)>();
            foreach (var strike in new[] { 3200m, 3210m, 3220m, 3230m, 3240m })
            {
                foreach (var right in new[] { OptionRight.Call, OptionRight.Put })
                {
                    var symbol = Symbol.CreateOption(future, QuantConnect.Market.CME, OptionStyle.American, right, strike, future.ID.Date);
                    contracts.Add((symbol, 100, 0.15m, new Greeks(0.5m, 0.01m, 5, -0.5m, 1, 0)));
                }
            }
            var (data, underlying) = CreateUniverseData(canonical, date, 3223.75m, contracts);
            var symbolProperties = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(QuantConnect.Market.CME, canonical, SecurityType.FutureOption, Currencies.USD);
            var chain = new OptionChain(canonical, date, data, symbolProperties);
            Assert.AreEqual(SecurityType.FutureOption, chain.Symbol.SecurityType);
            Assert.AreEqual(10, chain.Count);
            Assert.AreEqual(3223.75m, chain.Underlying.Price);

            // moneyness against the future price
            CollectionAssert.AreEquivalent(new[] { 3230m, 3240m }, chain.OutOfTheMoney().CallsOnly().Select(x => x.Strike));
            CollectionAssert.AreEquivalent(new[] { 3200m, 3210m, 3220m }, chain.OutOfTheMoney().PutsOnly().Select(x => x.Strike));
            Assert.AreEqual(0, chain.AtTheMoney(0).Count);
            CollectionAssert.AreEquivalent(new[] { 3220m, 3220m }, chain.AtTheMoney().Select(x => x.Strike));
            CollectionAssert.AreEquivalent(new[] { 3220m, 3220m }, chain.AtTheMoney(5m).Select(x => x.Strike));

            // the expiration filters count from the CME date, every ES option is a standard contract
            Assert.AreEqual(10, chain.Expiration(70, 80).Count);
            Assert.AreEqual(0, chain.ZeroDte().Count);
            Assert.AreEqual(10, chain.StandardsOnly().FarthestExpiration().Count);
            Assert.AreEqual(0, chain.WeeklysOnly().Count);
            Assert.IsTrue(chain.All(x => x.DaysToExpiry == (future.ID.Date - x.Time.Date).Days));

            // and match the universe filters of a future option over the same rows
            var universe = new OptionFilterUniverse(CreateOption(canonical), data, underlying);
            universe.Refresh(data, underlying, date);
            var expected = universe.Strikes(-1, 1).OutOfTheMoney().ExpiringBefore(new DateTime(2020, 4, 1)).ToList().Select(x => x.Symbol.Value).ToList();
            Assert.IsNotEmpty(expected);
            CollectionAssert.AreEquivalent(expected, chain.Strikes(-1, 1).OutOfTheMoney().ExpiringBefore(new DateTime(2020, 4, 1)).Select(x => x.Symbol.Value));
        }

        [Test]
        public void ContractsCountTheDaysToTheirExpiration()
        {
            var chain = CreateChain();
            // universe rows are stamped at the end of their day, so the contracts count from the next date
            var reference = chain.First().Time.Date;
            Assert.AreEqual(Date.AddDays(1), reference);
            var expected = Expiries.Select(expiry => (expiry - reference).Days).ToList();
            CollectionAssert.AreEquivalent(expected, chain.Select(x => x.DaysToExpiry).Distinct());
            Assert.AreEqual(expected[0], chain.FrontMonth().First().DaysToExpiry);
            Assert.AreEqual(expected[3], chain.FarthestExpiration().First().DaysToExpiry);
        }

        [Test]
        public void MoneynessFiltersSelectNothingWithoutUnderlyingPrice()
        {
            var contracts = _data.Select(x => new OptionUniverse(x) { Underlying = null }).ToList();
            var chain = new OptionChain(Canonical, Date, contracts, _symbolProperties);
            Assert.AreEqual(0, chain.Underlying.Price);

            Assert.AreEqual(0, chain.OutOfTheMoney().Count);
            Assert.AreEqual(0, chain.InTheMoney().Count);
            Assert.AreEqual(0, chain.AtTheMoney(100m).Count);
            Func<OptionFilterUniverse, OptionFilterUniverse>[] filters = [u => u.OutOfTheMoney(), u => u.InTheMoney(), u => u.AtTheMoney(100m)];
            foreach (var filter in filters)
            {
                var universe = new OptionFilterUniverse(_option);
                universe.Refresh(contracts, null, Date);
                Assert.AreEqual(0, filter(universe).Count);
            }
        }

        [Test]
        public void TypeFiltersApplyToTheChainContractsInAnyOrder()
        {
            var chain = CreateChain();
            var expected = CreateUniverse().StandardsOnly().FrontMonth().ToList().Select(x => x.Symbol.Value).ToList();

            // The front month, 2016-03-04, is a weekly and 2016-03-18 the first standard expiry
            Assert.AreEqual(2 * Strikes.Length, expected.Count);
            CollectionAssert.AreEquivalent(expected, chain.StandardsOnly().FrontMonth().Select(x => x.Symbol.Value));
            Assert.AreEqual(0, chain.FrontMonth().StandardsOnly().Count);
            Assert.IsTrue(chain.FrontMonth().WeeklysOnly().All(x => x.Expiry == Expiries[0]));
            Assert.Throws<InvalidOperationException>(() => CreateUniverse().FrontMonth().StandardsOnly());
        }

        [Test]
        public void StrategyFiltersAreAvailableFromPython()
        {
            var chain = CreateChain();
            var expectedIronCondor = chain.IronCondor(10, 5, 10).Select(x => x.Symbol.Value).ToList();
            var expectedNakedPut = chain.NakedPut(10, -5).Select(x => x.Symbol.Value).ToList();
            Assert.AreEqual(4, expectedIronCondor.Count);
            Assert.AreEqual(1, expectedNakedPut.Count);

            using (Py.GIL())
            {
                using var module = PyModule.FromString(nameof(OptionChainTests) + "Strategies", @"
from AlgorithmImports import *

def iron_condor(chain):
    return chain.iron_condor(10, 5, 10)

def naked_put(chain):
    return chain.naked_put(min_days_till_expiry=10, strike_from_atm=-5)
");
                using var pyChain = chain.ToPython();

                using var ironCondor = module.GetAttr("iron_condor").Invoke(pyChain);
                CollectionAssert.AreEquivalent(expectedIronCondor, ironCondor.As<OptionChain>().Select(x => x.Symbol.Value).ToList());

                using var nakedPut = module.GetAttr("naked_put").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedNakedPut, nakedPut.As<OptionChain>().Select(x => x.Symbol.Value).ToList());
            }
        }

        private class TestAuxData : BaseData
        {
        }

        private static TestCaseData Case(string name, Func<OptionFilterUniverse, OptionFilterUniverse> universeFilter,
            Func<OptionChain, OptionChain> chainFilter, bool empty = false)
        {
            return new TestCaseData(universeFilter, chainFilter, empty).SetName("{m}(" + name + ")");
        }

        private OptionFilterUniverse CreateUniverse(List<OptionUniverse> data = null, BaseData underlying = null, DateTime? date = null)
        {
            data ??= _data;
            underlying ??= _underlying;
            var universe = new OptionFilterUniverse(_option, data, underlying);
            universe.Refresh(data, underlying, date ?? Date);
            return universe;
        }

        private OptionChain CreateChain()
        {
            return new OptionChain(Canonical, Date, _data, _symbolProperties);
        }

        private static Option CreateOption(Symbol canonical = null)
        {
            canonical ??= Canonical;
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(canonical.ID.Market, canonical, canonical.SecurityType);
            return new Option(
                exchangeHours,
                new SubscriptionDataConfig(typeof(TradeBar), canonical, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
        }

        /// <summary>
        /// Creates option universe data for every expiry/strike/right combination, with synthetic but monotonic
        /// greeks, implied volatility and open interest so every range filter has a distinct answer
        /// </summary>
        private static (List<OptionUniverse>, BaseData) CreateUniverseData(DateTime date, decimal spot, DateTime[] expiries, decimal[] strikes)
        {
            var contracts = new List<(Symbol, decimal, decimal, Greeks)>();
            var i = 0;
            foreach (var expiry in expiries)
            {
                foreach (var strike in strikes)
                {
                    foreach (var right in new[] { OptionRight.Call, OptionRight.Put })
                    {
                        var symbol = Symbol.CreateOption(Canonical.Underlying, Canonical.ID.Market, OptionStyle.American, right, strike, expiry);
                        var callDelta = Math.Clamp(0.5m + (spot - strike) / 20m, 0.05m, 0.95m);
                        var delta = right == OptionRight.Call ? callDelta : callDelta - 1;
                        var greeks = new Greeks(delta, 0.01m + 0.001m * i, 5 + i, -(0.5m + 0.1m * i) * 365m, 1 + i, 0);
                        contracts.Add((symbol, 100 * (i + 1), 0.15m + 0.01m * i, greeks));
                        i++;
                    }
                }
            }

            return CreateUniverseData(Canonical, date, spot, contracts);
        }

        /// <summary>
        /// Creates option universe data by writing a universe file with the same code the data generator uses,
        /// <see cref="OptionUniverse.ToCsv"/>, and reading it back with <see cref="OptionUniverse.Reader"/>,
        /// so the tests follow the file format instead of hard coding it
        /// </summary>
        /// <param name="canonical">The canonical option symbol</param>
        /// <param name="date">The universe file date</param>
        /// <param name="spot">The underlying price, no underlying row is written when null</param>
        /// <param name="contracts">The contract rows to write</param>
        internal static (List<OptionUniverse> contracts, BaseData underlying) CreateUniverseData(Symbol canonical, DateTime date, decimal? spot,
            IEnumerable<(Symbol symbol, decimal openInterest, decimal impliedVolatility, Greeks greeks)> contracts)
        {
            var rows = contracts.ToList();
            var csv = new StringBuilder();
            csv.AppendLine("#" + OptionUniverse.CsvHeader(canonical.SecurityType));
            if (spot.HasValue)
            {
                csv.AppendLine(OptionUniverse.ToCsv(canonical.Underlying, spot.Value, spot.Value, spot.Value, spot.Value, 1000, null, null, null));
            }
            var i = 0;
            foreach (var (symbol, openInterest, impliedVolatility, greeks) in rows)
            {
                var price = 1 + i++;
                csv.AppendLine(OptionUniverse.ToCsv(symbol, price, price, price, price, i, openInterest, impliedVolatility, greeks));
            }

            var config = new SubscriptionDataConfig(typeof(OptionUniverse), canonical, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
            var data = new List<OptionUniverse>();
            BaseData underlying = null;
            var factory = new OptionUniverse();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = (OptionUniverse)factory.Reader(config, reader, date, false);
                if (line == null)
                {
                    continue;
                }
                if (line.Symbol.HasUnderlying)
                {
                    // the underlying row comes first in the file and is attached to each contract, like the universe collection does
                    line.Underlying = underlying;
                    data.Add(line);
                }
                else
                {
                    underlying = line;
                }
            }

            // Fail here if the serializer and the reader ever drift apart, rather than silently filtering the wrong values
            Assert.AreEqual(rows.Count, data.Count);
            for (var j = 0; j < rows.Count; j++)
            {
                Assert.AreEqual(rows[j].symbol, data[j].Symbol);
                Assert.AreEqual(rows[j].openInterest, data[j].OpenInterest);
                // future option universe files carry no implied volatility or greeks
                if (canonical.SecurityType != SecurityType.FutureOption)
                {
                    Assert.AreEqual(rows[j].impliedVolatility, data[j].ImpliedVolatility);
                    Assert.AreEqual(rows[j].greeks.Delta, data[j].Greeks.Delta);
                    Assert.AreEqual(rows[j].greeks.Theta, data[j].Greeks.Theta);
                    Assert.AreEqual(rows[j].greeks.Rho, data[j].Greeks.Rho);
                }
            }
            Assert.AreEqual(spot ?? 0, underlying?.Price ?? 0);

            return (data, underlying);
        }
    }
}
