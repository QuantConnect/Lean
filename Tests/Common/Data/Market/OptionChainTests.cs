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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class OptionChainTests
    {
        // Chain date: Thursday. Available expiries below are +1, +8, +15 and +29 days out
        private static readonly DateTime ChainTime = new(2015, 12, 24, 10, 0, 0);
        private static readonly DateTime Expiry1 = new(2015, 12, 25);
        private static readonly DateTime Expiry2 = new(2016, 1, 1);
        private static readonly DateTime Expiry3 = new(2016, 1, 8);
        private static readonly DateTime Expiry4 = new(2016, 1, 22);

        private static OptionChain CreateChain(
            IEnumerable<(DateTime expiry, decimal strike, OptionRight right, decimal delta)> contracts,
            decimal? underlyingPrice = 100m,
            DateTime? time = null)
        {
            var chainTime = time ?? ChainTime;
            var canonical = Symbol.CreateCanonicalOption(Symbols.SPY);
            var universeContracts = contracts.Select(x =>
            {
                var symbol = Symbol.CreateOption(Symbols.SPY, QuantConnect.Market.USA, OptionStyle.American, x.right, x.strike, x.expiry);
                // csv: open,high,low,close,volume,open_interest,implied_volatility,delta,gamma,vega,theta,rho
                return new OptionUniverse(chainTime.Date, symbol, $"1,1,1,1,10,100,0.5,{x.delta},0.01,0.02,-0.03,0.04");
            });

            var chain = new OptionChain(canonical, chainTime, universeContracts, SymbolProperties.GetDefault(Currencies.USD));
            if (underlyingPrice.HasValue)
            {
                chain.Underlying = new Tick { Symbol = Symbols.SPY, Value = underlyingPrice.Value, Time = chainTime };
            }
            return chain;
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
        public void StrikesAreDistinctAndSorted()
        {
            var chain = CreateDefaultChain();
            CollectionAssert.AreEqual(new[] { 85m, 90m, 95m, 100m, 105m, 110m }, chain.Strikes);
        }

        [TestCase(97, 95)]
        // Equidistant from 95 and 100: the lower strike wins
        [TestCase(97.5, 95)]
        [TestCase(120, 110)]
        public void StrikesClosestTo(double price, double expected)
        {
            var chain = CreateDefaultChain();
            Assert.AreEqual((decimal)expected, chain.Strikes.ClosestTo((decimal)price));
        }

        [Test]
        public void StrikesFirstAboveAndBelowAreStrict()
        {
            var chain = CreateDefaultChain();
            var strikes = chain.Strikes;

            Assert.AreEqual(105m, strikes.FirstAbove(100m));
            Assert.AreEqual(95m, strikes.FirstBelow(100m));
            Assert.AreEqual(85m, strikes.FirstAbove(0m));
            Assert.AreEqual(110m, strikes.FirstBelow(1000m));
            // No strike strictly above the highest / below the lowest
            Assert.IsNull(strikes.FirstAbove(110m));
            Assert.IsNull(strikes.FirstBelow(85m));
        }

        [Test]
        public void StrikesHelpersAreNullSafeOnEmptyChain()
        {
            var strikes = CreateEmptyChain().Strikes;
            Assert.IsEmpty(strikes);
            Assert.IsNull(strikes.ClosestTo(100m));
            Assert.IsNull(strikes.FirstAbove(100m));
            Assert.IsNull(strikes.FirstBelow(100m));
        }

        [TestCase(0, null, null, "20151225")]
        [TestCase(10, null, null, "20160101")]
        [TestCase(12, null, null, "20160108")]
        [TestCase(100, null, null, "20160122")]
        // min/max window excludes the otherwise closest expiry
        [TestCase(0, 5, null, "20160101")]
        [TestCase(100, null, 20, "20160108")]
        [TestCase(10, 12, 20, "20160108")]
        // no target: defaults to the nearest expiry within the window
        [TestCase(null, null, null, "20151225")]
        [TestCase(null, 10, null, "20160108")]
        public void ClosestExpirySelectsBestMatch(int? targetDte, int? minDte, int? maxDte, string expected)
        {
            var chain = CreateDefaultChain();
            var expectedExpiry = DateTime.ParseExact(expected, "yyyyMMdd", null);
            Assert.AreEqual(expectedExpiry, chain.ClosestExpiry(targetDte, minDte, maxDte));
        }

        [Test]
        public void ClosestExpiryPrefersEarlierExpiryOnTies()
        {
            // +1 and +8 days, target 4.5 rounded is not possible: use +1 and +3 with target 2
            var chain = CreateChain(new[]
            {
                (ChainTime.Date.AddDays(1), 100m, OptionRight.Call, 0.5m),
                (ChainTime.Date.AddDays(3), 100m, OptionRight.Call, 0.5m)
            });
            Assert.AreEqual(ChainTime.Date.AddDays(1), chain.ClosestExpiry(targetDte: 2));
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
            CollectionAssert.AreEqual(new[] { 90m, 100m, 110m }, filtered.Strikes);
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
            // Pre-2015 equity option metadata uses Saturday expiration dates: a user asking for the
            // last trading date (Friday) must still match the chain (strict
            // date(2012, 2, 17) equality matched zero contracts because metadata says 2012-02-18)
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
        public void AtTheMoneyFallsBackToContractUnderlyingPrice()
        {
            // Chains built from universe data carry the underlying price on each contract
            var canonical = Symbol.CreateCanonicalOption(Symbols.SPY);
            var symbol = Symbol.CreateOption(Symbols.SPY, QuantConnect.Market.USA, OptionStyle.American, OptionRight.Call, 100m, Expiry1);
            var contractData = new OptionUniverse(ChainTime.Date, symbol, "1,1,1,1,10,100,0.5,0.5,0.01,0.02,-0.03,0.04");
            var underlyingData = new OptionUniverse(ChainTime.Date, Symbols.SPY, "99,101,98,100.5,1000,,,,,,,");
            contractData.Underlying = underlyingData;

            var chain = new OptionChain(canonical, ChainTime, new[] { contractData }, SymbolProperties.GetDefault(Currencies.USD));

            // The chain-level underlying is populated from the contracts data
            Assert.AreEqual(100.5m, chain.Underlying.Price);
            Assert.AreEqual(100m, chain.AtTheMoney(OptionRight.Call).Strike);
        }

        [Test]
        public void SelectReplacesTheSortedComprehensionCeremony()
        {
            var chain = CreateDefaultChain();

            // The ubiquitous hand-rolled idiom this replaces:
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

        [TestCase(-0.1, 90)]
        [TestCase(0.0, 100)]
        [TestCase(0.08, 110)]
        public void SelectByMoneyness(double moneyness, double expectedStrike)
        {
            var chain = CreateDefaultChain();
            var contract = chain.Select(right: OptionRight.Put, targetDte: 8, moneyness: (decimal)moneyness);

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
            var contract = chain.Select(right: OptionRight.Put, targetDte: 8, targetDelta: (decimal)targetDelta);

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

            var contract = chain.Select(right: OptionRight.Call, targetDelta: 0.05m);
            Assert.AreEqual(100m, contract.Strike);

            // A chain without any greeks data returns null instead of an arbitrary contract
            var noGreeks = CreateChain(new[]
            {
                (Expiry1, 95m, OptionRight.Call, 0m),
                (Expiry1, 100m, OptionRight.Call, 0m)
            });
            Assert.IsNull(noGreeks.Select(right: OptionRight.Call, targetDelta: 0.05m));
        }

        [Test]
        public void SelectRespectsDteWindow()
        {
            var chain = CreateDefaultChain();

            // Guards against the "already-subscribed contracts outside the filter window" trap:
            // an explicit window never selects a nearer expiry than requested
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
            Assert.IsNull(CreateEmptyChain().Select(right: OptionRight.Put, targetDte: 30, moneyness: -0.15m));
            // Underlying price unavailable: moneyness cannot be computed
            var noUnderlying = CreateChain(new[] { (Expiry1, 100m, OptionRight.Call, 0.5m) }, underlyingPrice: null);
            Assert.IsNull(noUnderlying.Select(right: OptionRight.Call, moneyness: -0.15m));
        }

        [Test]
        public void SelectThrowsWhenMoneynessAndDeltaAreBothSet()
        {
            var chain = CreateDefaultChain();
            Assert.Throws<ArgumentException>(() => chain.Select(moneyness: -0.15m, targetDelta: 0.3m));
        }
    }
}
