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
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class OptionChainSelectionTests
    {
        // Chain date: Thursday. Available expiries below are +1, +8, +15 and +36 days out, none on a holiday
        private static readonly DateTime ChainTime = new(2016, 2, 25, 10, 0, 0);
        private static readonly DateTime Expiry1 = new(2016, 2, 26);
        private static readonly DateTime Expiry2 = new(2016, 3, 4);
        private static readonly DateTime Expiry3 = new(2016, 3, 11);
        private static readonly DateTime Expiry4 = new(2016, 4, 1);

        private static OptionChain CreateChain(
            IEnumerable<(DateTime expiry, decimal strike, OptionRight right, decimal delta)> contracts,
            decimal? underlyingPrice = 100m,
            DateTime? time = null)
        {
            var chainTime = time ?? ChainTime;
            var canonical = Symbol.CreateCanonicalOption(Symbols.SPY);
            var rows = contracts.Select(x => (
                Symbol.CreateOption(Symbols.SPY, QuantConnect.Market.USA, OptionStyle.American, x.right, x.strike, x.expiry),
                100m, 0.5m, new Greeks(x.delta, 0.01m, 0.02m, -0.03m * 365m, 0.04m, 0)));
            // Like the algorithm does, the chain is built from the previous day's universe data, whose end time is the chain date
            var (universeContracts, _) = OptionChainTests.CreateUniverseData(canonical, chainTime.Date.AddDays(-1), underlyingPrice, rows);

            return new OptionChain(canonical, chainTime, universeContracts, SymbolProperties.GetDefault(Currencies.USD));
        }

        private static OptionChain CreateDefaultChain(decimal? underlyingPrice = 100m)
        {
            return CreateChain(new (DateTime, decimal, OptionRight, decimal)[]
            {
                (Expiry1, 95m, OptionRight.Call, 0.8m),
                (Expiry1, 100m, OptionRight.Call, 0.5m),
                (Expiry1, 105m, OptionRight.Call, 0.2m),
                (Expiry1, 95m, OptionRight.Put, -0.2m),
                (Expiry1, 100m, OptionRight.Put, -0.5m),
                (Expiry1, 105m, OptionRight.Put, -0.8m),
                (Expiry2, 90m, OptionRight.Call, 0.9m),
                (Expiry2, 100m, OptionRight.Call, 0.5m),
                (Expiry2, 110m, OptionRight.Call, 0.1m),
                (Expiry2, 90m, OptionRight.Put, -0.1m),
                (Expiry2, 100m, OptionRight.Put, -0.5m),
                (Expiry2, 110m, OptionRight.Put, -0.9m),
                (Expiry3, 85m, OptionRight.Put, -0.15m),
                (Expiry3, 100m, OptionRight.Put, -0.5m),
                (Expiry4, 85m, OptionRight.Put, -0.25m),
                (Expiry4, 100m, OptionRight.Put, -0.55m)
            }, underlyingPrice);
        }

        private static OptionChain CreateEmptyChain()
        {
            return CreateChain(Enumerable.Empty<(DateTime, decimal, OptionRight, decimal)>(), underlyingPrice: null);
        }

        [Test]
        public void CallsAndPutsAreFilteredAndSorted()
        {
            var chain = CreateDefaultChain();

            var calls = chain.Calls;
            Assert.AreEqual(6, calls.Count);
            Assert.IsTrue(calls.All(x => x.Right == OptionRight.Call));
            CollectionAssert.AreEqual(
                calls.OrderBy(x => x.Expiry).ThenBy(x => x.Strike).Select(x => x.Symbol),
                calls.Select(x => x.Symbol));

            var puts = chain.Puts;
            Assert.AreEqual(10, puts.Count);
            Assert.IsTrue(puts.All(x => x.Right == OptionRight.Put));
            CollectionAssert.AreEqual(
                puts.OrderBy(x => x.Expiry).ThenBy(x => x.Strike).Select(x => x.Symbol),
                puts.Select(x => x.Symbol));
        }

        [Test]
        public void ViewsAreCachedUntilContractsAreAdded()
        {
            var chain = CreateDefaultChain();
            var calls = chain.Calls;
            var puts = chain.Puts;
            var strikes = chain.StrikePrices;
            var expiries = chain.Expiries;

            Assert.AreSame(calls, chain.Calls);
            Assert.AreSame(puts, chain.Puts);
            Assert.AreSame(strikes, chain.StrikePrices);
            Assert.AreSame(expiries, chain.Expiries);

            // Slice chains get their contracts one by one as data arrives: the views follow
            var added = CreateChain(new[] { (new DateTime(2016, 5, 20), 120m, OptionRight.Call, 0.05m) }).Single();
            chain.Contracts[added.Symbol] = added;

            Assert.AreNotSame(calls, chain.Calls);
            Assert.AreEqual(calls.Count + 1, chain.Calls.Count);
            Assert.AreSame(added, chain.Calls.Last());
            Assert.AreEqual(puts.Count, chain.Puts.Count);
            Assert.AreEqual(120m, chain.StrikePrices.Last());
            Assert.AreEqual(new DateTime(2016, 5, 20), chain.Expiries.Last());
        }

        [Test]
        public void StrikePricesAreDistinctAndSorted()
        {
            var chain = CreateDefaultChain();
            CollectionAssert.AreEqual(new[] { 85m, 90m, 95m, 100m, 105m, 110m }, chain.StrikePrices);
        }

        [Test]
        public void ExpiriesAreDistinctAndSorted()
        {
            var chain = CreateDefaultChain();
            CollectionAssert.AreEqual(new[] { Expiry1, Expiry2, Expiry3, Expiry4 }, chain.Expiries);
            Assert.IsEmpty(CreateEmptyChain().Expiries);
        }

        [TestCase(97, 95)]
        // Equidistant from 95 and 100: the lower strike wins
        [TestCase(97.5, 95)]
        [TestCase(120, 110)]
        public void StrikePricesClosestTo(double price, double expected)
        {
            var chain = CreateDefaultChain();
            Assert.AreEqual((decimal)expected, chain.StrikePrices.ClosestTo((decimal)price));
        }

        [Test]
        public void StrikePricesFirstAboveAndBelowAreStrict()
        {
            var chain = CreateDefaultChain();
            var strikes = chain.StrikePrices;

            Assert.AreEqual(105m, strikes.FirstAbove(100m));
            Assert.AreEqual(95m, strikes.FirstBelow(100m));
            Assert.AreEqual(85m, strikes.FirstAbove(0m));
            Assert.AreEqual(110m, strikes.FirstBelow(1000m));
            // No strike strictly above the highest / below the lowest
            Assert.IsNull(strikes.FirstAbove(110m));
            Assert.IsNull(strikes.FirstBelow(85m));
        }

        [Test]
        public void StrikePricesHelpersAreNullSafeOnEmptyChain()
        {
            var strikes = CreateEmptyChain().StrikePrices;
            Assert.IsEmpty(strikes);
            Assert.IsNull(strikes.ClosestTo(100m));
            Assert.IsNull(strikes.FirstAbove(100m));
            Assert.IsNull(strikes.FirstBelow(100m));
        }

        [TestCase(0, null, null, "20160226")]
        [TestCase(10, null, null, "20160304")]
        [TestCase(12, null, null, "20160311")]
        [TestCase(100, null, null, "20160401")]
        // min/max window excludes the otherwise closest expiry
        [TestCase(0, 5, null, "20160304")]
        [TestCase(100, null, 20, "20160311")]
        [TestCase(10, 12, 20, "20160311")]
        // no target: defaults to the nearest expiry within the window
        [TestCase(null, null, null, "20160226")]
        [TestCase(null, 10, null, "20160311")]
        public void ClosestExpirySelectsBestMatch(int? targetDte, int? minDte, int? maxDte, string expected)
        {
            var chain = CreateDefaultChain();
            var expectedExpiry = DateTime.ParseExact(expected, "yyyyMMdd", CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedExpiry, chain.ClosestExpiry(targetDte, minDte, maxDte));
        }

        [Test]
        public void ClosestExpiryPrefersEarlierExpiryOnTies()
        {
            // Friday +1 and Wednesday +6 are equidistant from a target of 3.5, use +1 and +5 with target 3
            var chain = CreateChain(new[]
            {
                (ChainTime.Date.AddDays(1), 100m, OptionRight.Call, 0.5m),
                (ChainTime.Date.AddDays(5), 100m, OptionRight.Call, 0.5m)
            });
            Assert.AreEqual(ChainTime.Date.AddDays(1), chain.ClosestExpiry(targetDte: 3));
        }

        [Test]
        public void ClosestExpiryIsNullSafe()
        {
            Assert.IsNull(CreateEmptyChain().ClosestExpiry(targetDte: 30));
            // Window excludes all expiries
            Assert.IsNull(CreateDefaultChain().ClosestExpiry(targetDte: 50, minDte: 40, maxDte: 60));
        }

        [Test]
        public void AtFiltersContractsByExpiry()
        {
            var chain = CreateDefaultChain();
            var filtered = chain.At(Expiry2);

            Assert.AreEqual(6, filtered.Count);
            Assert.IsTrue(filtered.All(x => x.Expiry == Expiry2));
            // The filtered chain keeps the underlying data and composes with the other helpers
            Assert.AreEqual(100m, filtered.Underlying.Price);
            Assert.AreEqual(3, filtered.Calls.Count);
            Assert.AreEqual(3, filtered.Puts.Count);
            Assert.AreEqual(3, filtered.CallsOnly().Count);
            CollectionAssert.AreEqual(new[] { 90m, 100m, 110m }, filtered.StrikePrices);
            Assert.AreEqual(100m, filtered.AtTheMoney(OptionRight.Call).Strike);
        }

        [Test]
        public void AtIgnoresTimeOfDayAndIsNullSafe()
        {
            var chain = CreateDefaultChain();
            Assert.AreEqual(6, chain.At(Expiry2.AddHours(15)).Count);
            // Unknown expiry: empty chain rather than an exception
            Assert.AreEqual(0, chain.At(new DateTime(2017, 1, 1)).Count);
        }

        [Test]
        public void AtMatchesSaturdayExpiryByLastTradingDate()
        {
            // Equity options before February 2015 have Saturday expiration dates: asking for the
            // last trading date (Friday) must still match the chain
            var saturdayExpiry = new DateTime(2012, 2, 18);
            var chainTime = new DateTime(2012, 2, 13, 10, 0, 0);
            var chain = CreateChain(new[]
            {
                (saturdayExpiry, 95m, OptionRight.Call, 0.7m),
                (saturdayExpiry, 100m, OptionRight.Call, 0.5m)
            }, time: chainTime);

            Assert.AreEqual(2, chain.At(new DateTime(2012, 2, 17)).Count);
            Assert.AreEqual(2, chain.At(saturdayExpiry).Count);

            // Days to expiration are counted to the Friday last trading date: Monday the 13th -> 4 days
            Assert.AreEqual(saturdayExpiry, chain.ClosestExpiry(targetDte: 4, minDte: 4, maxDte: 4));
            Assert.IsNull(chain.ClosestExpiry(minDte: 5));
            Assert.IsTrue(chain.All(x => x.DaysToExpiry == 4));
        }

        [Test]
        public void DaysToExpiryCountsCalendarDaysToTheLastTradingDate()
        {
            var chain = CreateDefaultChain();
            CollectionAssert.AreEquivalent(new[] { 1, 8, 15, 36 }, chain.Select(x => x.DaysToExpiry).Distinct());
            Assert.AreEqual(1, chain.At(Expiry1).First().DaysToExpiry);
            Assert.AreEqual(36, chain.At(Expiry4).First().DaysToExpiry);
        }

        [TestCase(99, 100)]
        [TestCase(103, 105)]
        // Equidistant between 95 and 100: lower strike wins
        [TestCase(97.5, 95)]
        public void AtTheMoneySelectsClosestStrike(double underlyingPrice, double expectedStrike)
        {
            var chain = CreateChain(new[]
            {
                (Expiry1, 95m, OptionRight.Call, 0.8m),
                (Expiry1, 100m, OptionRight.Call, 0.5m),
                (Expiry1, 105m, OptionRight.Call, 0.2m)
            }, (decimal)underlyingPrice);

            var contract = chain.AtTheMoney(OptionRight.Call);
            Assert.IsNotNull(contract);
            Assert.AreEqual((decimal)expectedStrike, contract.Strike);
            Assert.AreEqual(OptionRight.Call, contract.Right);
        }

        [Test]
        public void AtTheMoneyWithoutRightPrefersTheNearestExpiryThenCalls()
        {
            var chain = CreateDefaultChain();
            var contract = chain.AtTheMoney();

            Assert.AreEqual(100m, contract.Strike);
            Assert.AreEqual(Expiry1, contract.Expiry);
            Assert.AreEqual(OptionRight.Call, contract.Right);
        }

        [Test]
        public void AtTheMoneyIsNullSafe()
        {
            Assert.IsNull(CreateEmptyChain().AtTheMoney(OptionRight.Call));
            // No contracts of the requested right
            var callsOnly = CreateChain(new[] { (Expiry1, 100m, OptionRight.Call, 0.5m) });
            Assert.IsNull(callsOnly.AtTheMoney(OptionRight.Put));
            // Unknown underlying price
            var noUnderlying = CreateChain(new[] { (Expiry1, 100m, OptionRight.Call, 0.5m) }, underlyingPrice: null);
            Assert.IsNull(noUnderlying.AtTheMoney(OptionRight.Call));
        }

        [Test]
        public void AtTheMoneyUsesTheContractsUnderlyingPrice()
        {
            // Chains built from universe data carry the underlying price on each contract
            var chain = CreateChain(new[] { (Expiry1, 100m, OptionRight.Call, 0.5m) }, underlyingPrice: 100.5m);

            Assert.AreEqual(100.5m, chain.Underlying.Price);
            Assert.AreEqual(100m, chain.AtTheMoney(OptionRight.Call).Strike);
        }

        [Test]
        public void SelectReplacesTheSortedComprehensionCeremony()
        {
            var chain = CreateDefaultChain();

            // The hand-rolled idiom this replaces:
            // expiry = min([c.expiry for c in chain], key=lambda e: abs((e - self.time).days - target_dte))
            // expiry_contracts = [c for c in chain if c.expiry == expiry and c.right == right]
            // contract = min(expiry_contracts, key=lambda c: abs(c.strike - spot))
            var contract = chain.Select(right: OptionRight.Put, targetDte: 8);

            Assert.IsNotNull(contract);
            Assert.AreEqual(OptionRight.Put, contract.Right);
            Assert.AreEqual(Expiry2, contract.Expiry);
            // Default target is the at-the-money strike
            Assert.AreEqual(100m, contract.Strike);
        }

        [Test]
        public void PickIsASynonymOfSelect()
        {
            var chain = CreateDefaultChain();

            Assert.AreEqual(chain.Select(right: OptionRight.Put, targetDte: 8).Symbol, chain.Pick(right: OptionRight.Put, targetDte: 8).Symbol);
            Assert.AreEqual(chain.Select(right: OptionRight.Call, strike: StrikeTarget.Delta(0.2m)).Symbol, chain.Pick(right: OptionRight.Call, strike: StrikeTarget.Delta(0.2m)).Symbol);
            Assert.IsNull(chain.Pick(minDte: 40, maxDte: 60));
        }

        [TestCase(-0.1, 90)]
        [TestCase(0.0, 100)]
        [TestCase(0.08, 110)]
        public void SelectByMoneyness(double moneyness, double expectedStrike)
        {
            var chain = CreateDefaultChain();
            var contract = chain.Select(right: OptionRight.Put, targetDte: 8, strike: StrikeTarget.Moneyness((decimal)moneyness));

            Assert.IsNotNull(contract);
            Assert.AreEqual(OptionRight.Put, contract.Right);
            Assert.AreEqual(Expiry2, contract.Expiry);
            Assert.AreEqual((decimal)expectedStrike, contract.Strike);
        }

        [TestCase(-10, 90)]
        [TestCase(0, 100)]
        [TestCase(8, 110)]
        [TestCase(-4, 100)]
        public void SelectByStrikeFromAtm(double strikeFromAtm, double expectedStrike)
        {
            var chain = CreateDefaultChain();
            var contract = chain.Select(right: OptionRight.Put, targetDte: 8, strike: StrikeTarget.FromAtm((decimal)strikeFromAtm));

            Assert.IsNotNull(contract);
            Assert.AreEqual(OptionRight.Put, contract.Right);
            Assert.AreEqual(Expiry2, contract.Expiry);
            Assert.AreEqual((decimal)expectedStrike, contract.Strike);
        }

        [TestCase(0.15)]
        [TestCase(-0.15)]
        public void SelectByDeltaIsSignInsensitive(double targetDelta)
        {
            var chain = CreateDefaultChain();

            // A "15 delta put" can be requested with either sign: put deltas are negative
            var contract = chain.Select(right: OptionRight.Put, targetDte: 8, strike: StrikeTarget.Delta((decimal)targetDelta));

            Assert.IsNotNull(contract);
            Assert.AreEqual(OptionRight.Put, contract.Right);
            Assert.AreEqual(Expiry2, contract.Expiry);
            Assert.AreEqual(90m, contract.Strike);
            Assert.AreEqual(-0.1m, contract.Greeks.Delta);
        }

        [Test]
        public void SelectByDeltaIgnoresContractsWithoutGreeks()
        {
            var chain = CreateChain(new[]
            {
                (Expiry1, 95m, OptionRight.Call, 0m),
                (Expiry1, 100m, OptionRight.Call, 0.5m)
            });

            var contract = chain.Select(right: OptionRight.Call, strike: StrikeTarget.Delta(0.05m));
            Assert.AreEqual(100m, contract.Strike);

            // A chain without any greeks data returns null instead of an arbitrary contract
            var noGreeks = CreateChain(new[]
            {
                (Expiry1, 95m, OptionRight.Call, 0m),
                (Expiry1, 100m, OptionRight.Call, 0m)
            });
            Assert.IsNull(noGreeks.Select(right: OptionRight.Call, strike: StrikeTarget.Delta(0.05m)));
        }

        [Test]
        public void SelectRespectsDteWindow()
        {
            var chain = CreateDefaultChain();

            // An explicit window never selects a nearer expiry than requested, even if the chain carries it
            var contract = chain.Select(right: OptionRight.Put, targetDte: 0, minDte: 25, maxDte: 60);
            Assert.IsNotNull(contract);
            Assert.AreEqual(Expiry4, contract.Expiry);

            Assert.IsNull(chain.Select(right: OptionRight.Put, minDte: 40, maxDte: 60));
        }

        [Test]
        public void SelectConsidersOnlyTheRequestedRightForExpirySelection()
        {
            // Expiry3/Expiry4 have puts only: asking for a call must not land on a put-only expiry
            var chain = CreateDefaultChain();
            var contract = chain.Select(right: OptionRight.Call, targetDte: 20);

            Assert.IsNotNull(contract);
            Assert.AreEqual(OptionRight.Call, contract.Right);
            Assert.AreEqual(Expiry2, contract.Expiry);
        }

        [Test]
        public void SelectWithoutCriteriaReturnsAtTheMoney()
        {
            var chain = CreateChain(new[]
            {
                (Expiry1, 95m, OptionRight.Call, 0.8m),
                (Expiry1, 99m, OptionRight.Call, 0.5m),
                (Expiry1, 105m, OptionRight.Call, 0.2m)
            });

            var contract = chain.Select();
            Assert.AreEqual(99m, contract.Strike);
        }

        [Test]
        public void SelectIsNullSafe()
        {
            Assert.IsNull(CreateEmptyChain().Select(right: OptionRight.Put, targetDte: 30, strike: StrikeTarget.Moneyness(-0.15m)));
            // Underlying price unavailable: moneyness cannot be computed
            var noUnderlying = CreateChain(new[] { (Expiry1, 100m, OptionRight.Call, 0.5m) }, underlyingPrice: null);
            Assert.IsNull(noUnderlying.Select(right: OptionRight.Call, strike: StrikeTarget.Moneyness(-0.15m)));
        }

        [Test]
        public void StrikeTargetsSelectFromAnyContractList()
        {
            var contracts = CreateDefaultChain().At(Expiry2).ToList();

            Assert.AreEqual(100m, StrikeTarget.AtTheMoney.Select(contracts, 101m).Strike);
            Assert.AreEqual(110m, StrikeTarget.Moneyness(0.08m).Select(contracts, 100m).Strike);
            Assert.AreEqual(90m, StrikeTarget.FromAtm(-8m).Select(contracts, 100m).Strike);
            // delta needs no underlying price and is sign insensitive
            Assert.AreEqual(90m, StrikeTarget.Delta(-0.1m).Select(contracts, null).Strike);
            Assert.AreEqual(OptionRight.Put, StrikeTarget.Delta(-0.1m).Select(contracts, null).Right);
            // strike based targets need the underlying price
            Assert.IsNull(StrikeTarget.Moneyness(0.08m).Select(contracts, null));
            Assert.IsNull(StrikeTarget.AtTheMoney.Select(Enumerable.Empty<OptionContract>(), 100m));

            Assert.AreEqual("AtTheMoney", StrikeTarget.AtTheMoney.ToString());
            Assert.AreEqual("Moneyness(-0.15)", StrikeTarget.Moneyness(-0.15m).ToString());
            Assert.AreEqual("FromAtm(-5)", StrikeTarget.FromAtm(-5m).ToString());
            Assert.AreEqual("Delta(0.3)", StrikeTarget.Delta(-0.3m).ToString());
        }

        [Test]
        public void SelectionHelpersAreAvailableFromPython()
        {
            var chain = CreateDefaultChain();
            var expected = chain.Select(right: OptionRight.Put, targetDte: 8, strike: StrikeTarget.Moneyness(-0.1m));

            using (Py.GIL())
            {
                using var module = PyModule.FromString(nameof(OptionChainSelectionTests), @"
from AlgorithmImports import *

def select(chain):
    return chain.select(right=OptionRight.PUT, target_dte=8, strike=StrikeTarget.moneyness(-0.1))

def pick(chain):
    return chain.pick(OptionRight.PUT, 8, strike=StrikeTarget.from_atm(-10))

def helpers(chain):
    at_expiry = chain.at(chain.closest_expiry(target_dte=8))
    return (at_expiry.strike_prices.first_above(100), at_expiry.expiries[0], len(at_expiry.puts),
        at_expiry.at_the_money(OptionRight.CALL).days_to_expiry, chain.select(min_dte=40) is None)
");
                using var pyChain = chain.ToPython();

                using var selected = module.GetAttr("select").Invoke(pyChain);
                Assert.AreEqual(expected.Symbol, selected.As<OptionContract>().Symbol);

                using var picked = module.GetAttr("pick").Invoke(pyChain);
                Assert.AreEqual(expected.Symbol, picked.As<OptionContract>().Symbol);

                using var helpers = module.GetAttr("helpers").Invoke(pyChain);
                Assert.AreEqual(110m, helpers[0].As<decimal>());
                Assert.AreEqual(Expiry2, helpers[1].As<DateTime>());
                Assert.AreEqual(3, helpers[2].As<int>());
                Assert.AreEqual(8, helpers[3].As<int>());
                Assert.IsTrue(helpers[4].As<bool>());
            }
        }
    }
}
