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

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class FuturesChainTests
    {
        private static readonly DateTime Date = new(2013, 10, 7);
        private static readonly Symbol Canonical = Symbol.Create("ES", SecurityType.Future, QuantConnect.Market.CME);
        // The quarterly ES contracts listed in the sample data, with their standard expiration dates
        private static readonly DateTime[] Expiries =
        {
            new(2013, 12, 20), new(2014, 3, 21), new(2014, 6, 20), new(2014, 9, 19), new(2014, 12, 19)
        };
        private static readonly decimal[] Volumes = { 5000m, 4000m, 3000m, 2000m, 1000m };
        private static readonly decimal[] OpenInterests = { 900000m, 60000m, 9000m, 2000m, 500m };

        private List<FutureUniverse> _data;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _data = CreateUniverseData(Date, Expiries, Volumes, OpenInterests);
        }

        private static IEnumerable<TestCaseData> FilterCases()
        {
            yield return Case("Expiration(0, 90)", u => u.Expiration(0, 90), c => c.Expiration(0, 90));
            yield return Case("Expiration(TimeSpan)", u => u.Expiration(TimeSpan.FromDays(60), TimeSpan.FromDays(300)),
                c => c.Expiration(TimeSpan.FromDays(60), TimeSpan.FromDays(300)));
            yield return Case("Expiration(500, 600)", u => u.Expiration(500, 600), c => c.Expiration(500, 600), empty: true);
            yield return Case("Expiration(dates)", u => u.Expiration([Expiries[1], Expiries[3].AddHours(10)]), c => c.Expiration([Expiries[1], Expiries[3].AddHours(10)]));
            yield return Case("Expiration(no dates)", u => u.Expiration([]), c => c.Expiration([]), empty: true);
            yield return Case("ExpiringAfter", u => u.ExpiringAfter(Expiries[0]), c => c.ExpiringAfter(Expiries[0]));
            yield return Case("ExpiringBefore", u => u.ExpiringBefore(Expiries[2]), c => c.ExpiringBefore(Expiries[2]));
            yield return Case("ExpiringAfter.ExpiringBefore", u => u.ExpiringAfter(Expiries[0]).ExpiringBefore(Expiries[3]), c => c.ExpiringAfter(Expiries[0]).ExpiringBefore(Expiries[3]));
            yield return Case("ZeroDte", u => u.ZeroDte(), c => c.ZeroDte(), empty: true);
            yield return Case("StandardsOnly", u => u.StandardsOnly(), c => c.StandardsOnly());
            yield return Case("WeeklysOnly", u => u.WeeklysOnly(), c => c.WeeklysOnly(), empty: true);
            yield return Case("FrontMonth", u => u.FrontMonth(), c => c.FrontMonth());
            yield return Case("BackMonths", u => u.BackMonths(), c => c.BackMonths());
            yield return Case("BackMonth", u => u.BackMonth(), c => c.BackMonth());
            yield return Case("FarthestExpiration", u => u.FarthestExpiration(), c => c.FarthestExpiration());
            yield return Case("StandardsOnly.FrontMonth", u => u.StandardsOnly().FrontMonth(), c => c.StandardsOnly().FrontMonth());
            yield return Case("ExpirationCycle(3, 9)", u => u.ExpirationCycle([3, 9]), c => c.ExpirationCycle([3, 9]));
            yield return Case("ExpirationCycle(1)", u => u.ExpirationCycle([1]), c => c.ExpirationCycle([1]), empty: true);
            yield return Case("ContractMonths(3, 12)", u => u.ContractMonths([3, 12]), c => c.ContractMonths([3, 12]));
            yield return Case("ContractMonths(1)", u => u.ContractMonths([1]), c => c.ContractMonths([1]), empty: true);
            yield return Case("OpenInterest(1000, 100000)", u => u.OpenInterest(1000, 100000), c => c.OpenInterest(1000, 100000));
            yield return Case("OI(0, 600)", u => u.OI(0, 600), c => c.OI(0, 600));
            yield return Case("OpenInterest(0, 0)", u => u.OpenInterest(0, 0), c => c.OpenInterest(0, 0), empty: true);
            yield return Case("Volume(2000, 4000)", u => u.Volume(2000, 4000), c => c.Volume(2000, 4000));
            yield return Case("Volume.FrontMonth", u => u.Volume(1, long.MaxValue).FrontMonth(), c => c.Volume(1, long.MaxValue).FrontMonth());
            yield return Case("Expiration.BackMonths.ExpirationCycle", u => u.Expiration(0, 300).BackMonths().ExpirationCycle([3, 6]),
                c => c.Expiration(0, 300).BackMonths().ExpirationCycle([3, 6]));
        }

        [TestCaseSource(nameof(FilterCases))]
        public void ChainFiltersMatchUniverseFilters(Func<FutureFilterUniverse, FutureFilterUniverse> universeFilter,
            Func<FuturesChain, FuturesChain> chainFilter, bool expectEmpty)
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
            var universeFilters = typeof(FutureFilterUniverse)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.ReturnType == typeof(FutureFilterUniverse) && x.Name != "Contracts" && !x.IsDefined(typeof(ObsoleteAttribute)))
                .ToList();

            Assert.IsNotEmpty(universeFilters);
            var interfaces = typeof(IFutureContractFilters<FuturesChain>).GetInterfaces().Prepend(typeof(IFutureContractFilters<FuturesChain>)).ToList();
            foreach (var universeFilter in universeFilters)
            {
                var parameters = universeFilter.GetParameters().Select(x => x.ParameterType).ToArray();
                var chainFilter = interfaces.Select(x => x.GetMethod(universeFilter.Name, parameters)).FirstOrDefault(x => x != null);
                Assert.IsNotNull(chainFilter, $"{universeFilter.Name}({string.Join(", ", parameters.Select(x => x.Name))}) is not a futures chain filter");
            }
        }

        [Test]
        public void FiltersLeaveTheSourceChainUntouched()
        {
            var chain = CreateChain();
            var filtered = chain.FrontMonth();

            Assert.AreEqual(Expiries.Length, chain.Count);
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(chain.Symbol, filtered.Symbol);
            Assert.AreEqual(chain.Time, filtered.Time);
            Assert.AreSame(chain.Contracts[filtered.Single().Symbol], filtered.Single());
        }

        [Test]
        public void FiltersOnAnEmptyChainReturnAnEmptyChain()
        {
            var chain = new FuturesChain(Canonical, Date);

            foreach (var testCase in FilterCases())
            {
                var filter = (Func<FuturesChain, FuturesChain>)testCase.Arguments[1];
                Assert.AreEqual(0, filter(chain).Count, testCase.TestName);
            }
        }

        [Test]
        public void FiltersUseTheExchangeTimeAsTheReferenceDate()
        {
            // The engine stamps slice chains in the algorithm time zone, which can already be the day after the exchange date
            var exchangeDate = Expiries[0];
            var data = CreateUniverseData(exchangeDate, [exchangeDate, Expiries[1]], Volumes.Take(2).ToArray(), OpenInterests.Take(2).ToArray());
            var chain = new FuturesChain(Canonical, exchangeDate, data) { Time = exchangeDate.AddDays(1) };
            var universe = new FutureFilterUniverse(data, exchangeDate).Expiration(0, 0).ToList();

            Assert.AreEqual(1, universe.Count);
            Assert.AreEqual(0, chain.Expiration(0, 0).Count);

            chain.ExchangeTime = exchangeDate;
            var filtered = chain.Expiration(0, 0);

            CollectionAssert.AreEquivalent(universe.Select(x => x.Symbol.Value), filtered.Select(x => x.Symbol.Value));
            Assert.AreEqual(chain.Time, filtered.Time);
            Assert.AreEqual(exchangeDate, filtered.ExchangeTime);
            Assert.AreEqual(1, chain.ZeroDte().Count);
        }

        [Test]
        public void TypeFiltersApplyToTheChainContractsInAnyOrder()
        {
            // A contract expiring off its standard date, 2014-01-17 for January, is a non standard one. It cannot go through the
            // universe file format, which only carries the contract month, so the rows are built directly
            var weekly = new FutureUniverse { Symbol = Symbol.CreateFuture("ES", QuantConnect.Market.CME, new DateTime(2014, 1, 10)), Time = Date };
            var data = _data.Concat([weekly]).ToList();
            var chain = new FuturesChain(Canonical, Date, data);
            var universe = new FutureFilterUniverse(data, Date);

            Assert.AreEqual(Expiries.Length + 1, chain.Count);
            CollectionAssert.AreEquivalent(_data.Select(x => x.Symbol.Value), chain.StandardsOnly().Select(x => x.Symbol.Value));
            CollectionAssert.AreEqual(new[] { weekly.Symbol.Value }, chain.WeeklysOnly().Select(x => x.Symbol.Value));
            CollectionAssert.AreEquivalent(universe.StandardsOnly().FrontMonth().ToList().Select(x => x.Symbol.Value), chain.StandardsOnly().FrontMonth().Select(x => x.Symbol.Value));

            // The front month, December 2013, is standard; the chain composes the type filters in either order, the universe does not
            Assert.AreEqual(0, chain.FrontMonth().WeeklysOnly().Count);
            Assert.AreEqual(weekly.Symbol, chain.WeeklysOnly().FrontMonth().Single().Symbol);
            Assert.Throws<InvalidOperationException>(() => new FutureFilterUniverse(data, Date).FrontMonth().StandardsOnly());
        }

        [Test]
        public void FiltersAreAvailableFromPython()
        {
            var chain = CreateChain();
            var expectedFiltered = chain.Expiration(0, 300).BackMonths().ExpirationCycle([3, 6]).Select(x => x.Symbol).ToList();
            var expectedSets = chain.Expiration([Expiries[1], Expiries[3]]).OpenInterest(1000, 100000).Select(x => x.Symbol).ToList();
            var expectedWhere = chain.Where(x => x.Volume >= 3000).Select(x => x.Symbol).ToList();
            var expectedContractMonths = chain.ContractMonths([3, 12]).ExpiringAfter(Expiries[0]).Select(x => x.Symbol).ToList();
            Assert.AreEqual(2, expectedFiltered.Count);
            Assert.AreEqual(2, expectedSets.Count);
            Assert.AreEqual(3, expectedWhere.Count);
            Assert.AreEqual(2, expectedContractMonths.Count);

            using (Py.GIL())
            {
                using var module = PyModule.FromString(nameof(FuturesChainTests), @"
from AlgorithmImports import *

def filter_chain(chain):
    return chain.expiration(0, 300).back_months().expiration_cycle([3, 6])

def sets(chain):
    return chain.expiration([datetime(2014, 3, 21), datetime(2014, 9, 19)]).open_interest(1000, 100000)

def where_chain(chain):
    return chain.where(lambda contract: contract.volume >= 3000)

def contract_months(chain):
    return chain.contract_months([3, 12]).expiring_after(datetime(2013, 12, 20))
");
                using var pyChain = chain.ToPython();

                using var filtered = module.GetAttr("filter_chain").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedFiltered, filtered.As<FuturesChain>().Select(x => x.Symbol).ToList());

                using var sets = module.GetAttr("sets").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedSets, sets.As<FuturesChain>().Select(x => x.Symbol).ToList());

                using var where = module.GetAttr("where_chain").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedWhere, where.As<FuturesChain>().Select(x => x.Symbol).ToList());

                using var contractMonths = module.GetAttr("contract_months").Invoke(pyChain);
                CollectionAssert.AreEqual(expectedContractMonths, contractMonths.As<FuturesChain>().Select(x => x.Symbol).ToList());
            }
        }

        private static TestCaseData Case(string name, Func<FutureFilterUniverse, FutureFilterUniverse> universeFilter,
            Func<FuturesChain, FuturesChain> chainFilter, bool empty = false)
        {
            return new TestCaseData(universeFilter, chainFilter, empty).SetName("{m}(" + name + ")");
        }

        private FutureFilterUniverse CreateUniverse()
        {
            return new FutureFilterUniverse(_data, Date);
        }

        private FuturesChain CreateChain()
        {
            return new FuturesChain(Canonical, Date, _data);
        }

        /// <summary>
        /// Creates futures universe data by writing a universe file with the same code the data generator uses,
        /// <see cref="FutureUniverse.ToCsv"/>, and reading it back with <see cref="FutureUniverse.Reader"/>,
        /// so the tests follow the file format instead of hard coding it
        /// </summary>
        private static List<FutureUniverse> CreateUniverseData(DateTime date, DateTime[] expiries, decimal[] volumes, decimal[] openInterests)
        {
            var symbols = expiries.Select(expiry => Symbol.CreateFuture("ES", QuantConnect.Market.CME, expiry)).ToList();
            var csv = new StringBuilder();
            csv.AppendLine("#" + FutureUniverse.CsvHeader);
            for (var i = 0; i < symbols.Count; i++)
            {
                var price = 1600 + i;
                csv.AppendLine(FutureUniverse.ToCsv(symbols[i], price, price, price, price, volumes[i], openInterests[i]));
            }

            var config = new SubscriptionDataConfig(typeof(FutureUniverse), Canonical, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
            var data = new List<FutureUniverse>();
            var factory = new FutureUniverse();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = (FutureUniverse)factory.Reader(config, reader, date, false);
                if (line != null)
                {
                    data.Add(line);
                }
            }

            // the file format is the data generator's, so the rows must come back as written
            Assert.AreEqual(symbols.Count, data.Count);
            for (var i = 0; i < symbols.Count; i++)
            {
                Assert.AreEqual(symbols[i], data[i].Symbol);
                Assert.AreEqual(volumes[i], data[i].Volume);
                Assert.AreEqual(openInterests[i], data[i].OpenInterest);
            }
            return data;
        }
    }
}
