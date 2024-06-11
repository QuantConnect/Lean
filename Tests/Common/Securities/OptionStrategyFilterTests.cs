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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class OptionStrategyFilterTests
    {
        private static Dictionary<int, DateTime> _expiries = new Dictionary<int, DateTime>
        {
            { 1, new DateTime(2016, 3, 4) },
            { 2, new DateTime(2016, 5, 4) },
            { 3, new DateTime(2017, 5, 4) }
        };

        [TestCase(100, 0, 100, 10, false)]
        [TestCase(100, -5, 95, 10, false)]
        [TestCase(100, 5, 105, 10, false)]
        [TestCase(100, 10.05, 110, 10, false)]
        [TestCase(105.5, 0, 105, 10, false)]
        [TestCase(100, 0, 100, 90, true)]
        public void FiltersSingleCall(decimal underlyingPrice, decimal strikeFromAtm, decimal expectedStrike, int daysTillExpiry,
            bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);
            
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .SingleCall(daysTillExpiry, strikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(OptionRight.Call, filtered[0].ID.OptionRight);
            Assert.AreEqual(expectedExpiry, filtered[0].ID.Date);
            Assert.AreEqual(expectedStrike, filtered[0].ID.StrikePrice);
        }

        [TestCase(100, 0, 100, 10, false)]
        [TestCase(100, -5, 95, 10, false)]
        [TestCase(100, 5, 105, 10, false)]
        [TestCase(100, 10.05, 110, 10, false)]
        [TestCase(105.5, 0, 105, 10, false)]
        [TestCase(100, 0, 100, 90, true)]
        public void FiltersSinglePut(decimal underlyingPrice, decimal strikeFromAtm, decimal expectedStrike, int daysTillExpiry = 10,
            bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .SinglePut(daysTillExpiry, strikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(OptionRight.Put, filtered[0].ID.OptionRight);
            Assert.AreEqual(expectedExpiry, filtered[0].ID.Date);
            Assert.AreEqual(expectedStrike, filtered[0].ID.StrikePrice);
        }

        [TestCase(100, 0, null)]    // equal strikes
        [TestCase(100, -5, null)]   // low strikes > high strike
        [TestCase(100, 0, 0)]       // equal strikes
        [TestCase(110, -5, -5)]     // equal strikes
        [TestCase(100, -5, 5)]      // low strikes > high strike
        public void FailsCallSpread(decimal underlyingPrice, decimal higherStrikeFromAtm, decimal? lowerStrikeFromAtm)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .CallSpread(10, higherStrikeFromAtm, lowerStrikeFromAtm);

            FailsFiltering(underlyingPrice, universeFunc);
        }

        [TestCase(100, 5, 2, 95, 105, 10, false)]
        [TestCase(100, 10, 2, 90, 110, 10, false)]
        [TestCase(100, 10.5, 2, 90, 110, 10, false)]
        [TestCase(105.5, 5, 2, 100, 110, 10, false)]
        [TestCase(1000, 10, 0, 0, 0, 10, false)]            // extreme strike will have no matching pair, returning no contract
        [TestCase(1, 5, 2, 85, 90, 10, false)]
        [TestCase(100, 5, 2, 95, 105, 90, true)]
        public void FiltersCallSpread(decimal underlyingPrice, decimal strikeFromAtm, int expectedCount, decimal lowerExpectedStrike, 
            decimal higherExpectedStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);
            
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .CallSpread(daysTillExpiry, strikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var orderedCalls = filtered.OrderBy(x => x.ID.StrikePrice);
            var lowerStrikeCall = orderedCalls.First();
            var higherStrikeCall = orderedCalls.Last();

            Assert.AreEqual(OptionRight.Call, lowerStrikeCall.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, lowerStrikeCall.ID.Date);
            Assert.AreEqual(lowerExpectedStrike, lowerStrikeCall.ID.StrikePrice);

            Assert.AreEqual(OptionRight.Call, higherStrikeCall.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, higherStrikeCall.ID.Date);
            Assert.AreEqual(higherExpectedStrike, higherStrikeCall.ID.StrikePrice);
        }

        [TestCase(100, -5, 5, 2, 95, 105, 10, false)]
        [TestCase(100, -5, 10, 2, 95, 110, 10, false)]
        [TestCase(100, -10.5, 5.6, 2, 90, 105, 10, false)]
        [TestCase(105.5, -4.9, 5.1, 2, 100, 110, 10, false)]
        [TestCase(1000, -10, 10, 0, 0, 0, 10, false)]       // extreme strike will have no matching pair, returning no contract
        [TestCase(1, -5, 5, 2, 85, 90, 10, false)]
        [TestCase(100, -5, 5, 2, 95, 105, 90, true)]
        public void FiltersCallSpread(decimal underlyingPrice, decimal lowerStrikeFromAtm, decimal higherStrikeFromAtm, int expectedCount,
            decimal lowerExpectedStrike, decimal higherExpectedStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .CallSpread(daysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var orderedCalls = filtered.OrderBy(x => x.ID.StrikePrice);
            var lowerStrikeCall = orderedCalls.First();
            var higherStrikeCall = orderedCalls.Last();

            Assert.AreEqual(OptionRight.Call, lowerStrikeCall.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, lowerStrikeCall.ID.Date);
            Assert.AreEqual(lowerExpectedStrike, lowerStrikeCall.ID.StrikePrice);

            Assert.AreEqual(OptionRight.Call, higherStrikeCall.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, higherStrikeCall.ID.Date);
            Assert.AreEqual(higherExpectedStrike, higherStrikeCall.ID.StrikePrice);
        }

        [TestCase(100, 0, null)]    // equal strikes
        [TestCase(100, -5, null)]   // low strikes > high strike
        [TestCase(100, 0, 0)]       // equal strikes
        [TestCase(110, -5, -5)]     // equal strikes
        [TestCase(100, -5, 5)]      // low strikes > high strike
        public void FailsPutSpread(decimal underlyingPrice, decimal higherStrikeFromAtm, decimal? lowerStrikeFromAtm)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .PutSpread(10, higherStrikeFromAtm, lowerStrikeFromAtm);

            FailsFiltering(underlyingPrice, universeFunc);
        }

        [TestCase(100, 5, 2, 95, 105, 10, false)]
        [TestCase(100, 10, 2, 90, 110, 10, false)]
        [TestCase(100, 10.5, 2, 90, 110, 10, false)]
        [TestCase(105.5, 5, 2, 100, 110, 10, false)]
        [TestCase(1000, 10, 0, 0, 0, 10, false)]            // extreme strike will have no matching pair, returning no contract
        [TestCase(1, 5, 2, 85, 90, 10, false)]
        [TestCase(100, 5, 2, 95, 105, 90, true)]
        public void FiltersPutSpread(decimal underlyingPrice, decimal strikeFromAtm, decimal lowerExpectedStrike, int expectedCount,
            decimal higherExpectedStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);
            
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .PutSpread(daysTillExpiry, strikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var orderedPuts = filtered.OrderBy(x => x.ID.StrikePrice);
            var lowerStrikePut = orderedPuts.First();
            var higherStrikePut = orderedPuts.Last();

            Assert.AreEqual(OptionRight.Put, lowerStrikePut.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, lowerStrikePut.ID.Date);
            Assert.AreEqual(lowerExpectedStrike, lowerStrikePut.ID.StrikePrice);

            Assert.AreEqual(OptionRight.Put, higherStrikePut.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, higherStrikePut.ID.Date);
            Assert.AreEqual(higherExpectedStrike, higherStrikePut.ID.StrikePrice);
        }

        [TestCase(100, -5, 5, 2, 95, 105, 10, false)]
        [TestCase(100, -5, 10, 2, 95, 110, 10, false)]
        [TestCase(100, -10.5, 5.6, 2, 90, 105, 10, false)]
        [TestCase(105.5, -4.9, 5.1, 2, 100, 110, 10, false)]
        [TestCase(1000, -10, 10, 0, 0, 0, 10, false)]       // extreme strike will have no matching pair, returning no contract
        [TestCase(1, -5, 5, 2, 85, 90, 10, false)]
        [TestCase(100, -5, 5, 2, 95, 105, 90, true)]
        public void FiltersPutSpread(decimal underlyingPrice, decimal lowerStrikeFromAtm, decimal higherStrikeFromAtm, int expectedCount,
            decimal lowerExpectedStrike, decimal higherExpectedStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .PutSpread(daysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var orderedPuts = filtered.OrderBy(x => x.ID.StrikePrice);
            var lowerStrikePut = orderedPuts.First();
            var higherStrikePut = orderedPuts.Last();

            Assert.AreEqual(OptionRight.Put, lowerStrikePut.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, lowerStrikePut.ID.Date);
            Assert.AreEqual(lowerExpectedStrike, lowerStrikePut.ID.StrikePrice);

            Assert.AreEqual(OptionRight.Put, higherStrikePut.ID.OptionRight);
            Assert.AreEqual(expectedExpiry, higherStrikePut.ID.Date);
            Assert.AreEqual(higherExpectedStrike, higherStrikePut.ID.StrikePrice);
        }

        [TestCase(100, 0, 10, 10)]      // equal expiry
        [TestCase(105, -5, 50, 10)]     // near expiry > far expiry
        [TestCase(105, -5, -10, -5)]    // negative
        [TestCase(105, -5, 0, -5)]      // negative
        public void FailsCallCalendarSpread(decimal underlyingPrice, decimal strikeFromAtm, int nearExpiry, int farExpiry)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .CallCalendarSpread(strikeFromAtm, nearExpiry, farExpiry);

            FailsFiltering(underlyingPrice, universeFunc);
        }

        // Expected expiry: (1) 2016/3/4, (2) 2016/5/4, (3) 2017/5/4
        [TestCase(100, 0, 10, 90, 1, 2, 2)]
        [TestCase(100, 10, 10, 90, 1, 2, 2)]
        [TestCase(100, 0, 10, 400, 1, 3, 2)]
        [TestCase(100, 0, 100, 450, 2, 3, 2)]
        [TestCase(105, 0, 100, 101, 2, 3, 2)]       // only select later contracts for far expiry
        [TestCase(105, 0, 500, 1000, 0, 0, 0)]      // select none if no further contracts available
        public void FiltersCallCalendarSpread(decimal underlyingPrice, decimal strikeFromAtm, int nearExpiry, int farExpiry, int expectedNearExpiryCase,
            int expectedFarExpiryCase, int expectedCount)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .CallCalendarSpread(strikeFromAtm, nearExpiry, farExpiry);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var orderedCalls = filtered.OrderBy(x => x.ID.Date);
            var nearCall = orderedCalls.First();
            var farCall = orderedCalls.Last();

            Assert.AreEqual(OptionRight.Call, nearCall.ID.OptionRight);
            Assert.AreEqual(_expiries[expectedNearExpiryCase], nearCall.ID.Date);
            Assert.AreEqual(underlyingPrice + strikeFromAtm, nearCall.ID.StrikePrice);

            Assert.AreEqual(OptionRight.Call, farCall.ID.OptionRight);
            Assert.AreEqual(_expiries[expectedFarExpiryCase], farCall.ID.Date);
            Assert.AreEqual(underlyingPrice + strikeFromAtm, farCall.ID.StrikePrice);
        }

        [TestCase(100, 0, 10, 10)]      // equal expiry
        [TestCase(105, -5, 50, 10)]     // near expiry > far expiry
        [TestCase(105, -5, -10, -5)]    // negative
        [TestCase(105, -5, 0, -5)]      // negative
        public void FailsPutCalendarSpread(decimal underlyingPrice, decimal strikeFromAtm, int nearExpiry, int farExpiry)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .PutCalendarSpread(strikeFromAtm, nearExpiry, farExpiry);

            FailsFiltering(underlyingPrice, universeFunc);
        }

        // Expected expiry: (1) 2016/3/4, (2) 2016/5/4, (3) 2017/5/4
        [TestCase(100, 0, 10, 90, 1, 2, 2)]
        [TestCase(100, 10, 10, 90, 1, 2, 2)]
        [TestCase(100, 0, 10, 400, 1, 3, 2)]
        [TestCase(100, 0, 100, 450, 2, 3, 2)]
        [TestCase(105, 0, 100, 101, 2, 3, 2)]       // only select later contracts for far expiry
        [TestCase(105, 0, 500, 1000, 0, 0, 0)]      // select none if no further contracts available
        public void FiltersPutCalendarSpread(decimal underlyingPrice, decimal strikeFromAtm, int nearExpiry, int farExpiry, int expectedNearExpiryCase,
            int expectedFarExpiryCase, int expectedCount)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .PutCalendarSpread(strikeFromAtm, nearExpiry, farExpiry);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var orderedCalls = filtered.OrderBy(x => x.ID.Date);
            var nearCall = orderedCalls.First();
            var farCall = orderedCalls.Last();

            Assert.AreEqual(OptionRight.Put, nearCall.ID.OptionRight);
            Assert.AreEqual(_expiries[expectedNearExpiryCase], nearCall.ID.Date);
            Assert.AreEqual(underlyingPrice + strikeFromAtm, nearCall.ID.StrikePrice);

            Assert.AreEqual(OptionRight.Put, farCall.ID.OptionRight);
            Assert.AreEqual(_expiries[expectedFarExpiryCase], farCall.ID.Date);
            Assert.AreEqual(underlyingPrice + strikeFromAtm, farCall.ID.StrikePrice);
        }

        [TestCase(100, 0, -5)]      // 0 call
        [TestCase(100, 5, 0)]       // 0 put
        [TestCase(100, -1, -5)]     // negative call
        [TestCase(100, 5, 1)]       // positive put
        [TestCase(100, -5, 5)]      // negative call & positive put
        public void FailsStrangle(decimal underlyingPrice, decimal callStrikeFromAtm, decimal putStrikeFromAtm)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Strangle(10, callStrikeFromAtm, putStrikeFromAtm);

            FailsFiltering(underlyingPrice, universeFunc);
        }

        [TestCase(100, 5, -5, 2, 105, 95, 10, false)]
        [TestCase(100, 10, -5, 2, 110, 95, 10, false)]
        [TestCase(100, 10.5, -11.2, 2, 110, 90, 10, false)]
        [TestCase(105.5, 5, -5, 2, 110, 100, 10, false)]
        [TestCase(1000, 5, -5, 0, 0, 0, 10, false)]             // extreme strike causing no OTM contract
        [TestCase(1, 5, -5, 0, 0, 0, 10, false)]                // extreme strike causing no OTM contract
        [TestCase(100, 5, -5, 2, 105, 95, 90, true)]
        public void FiltersStrangle(decimal underlyingPrice, decimal callStrikeFromAtm, decimal putStrikeFromAtm, int expectedCount,
            decimal expectedCallStrike, decimal expectedPutStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Strangle(daysTillExpiry, callStrikeFromAtm, putStrikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var call = filtered.Single(x => x.ID.OptionRight == OptionRight.Call);
            var put = filtered.Single(x => x.ID.OptionRight == OptionRight.Put);

            Assert.AreEqual(expectedExpiry, call.ID.Date);
            Assert.AreEqual(expectedCallStrike, call.ID.StrikePrice);

            Assert.AreEqual(expectedExpiry, put.ID.Date);
            Assert.AreEqual(expectedPutStrike, put.ID.StrikePrice);
        }

        [TestCase(100, 100, 10, false)]
        [TestCase(105, 105, 10, false)]
        [TestCase(101.20, 100, 10, false)]
        [TestCase(100, 100, 90, true)]
        public void FiltersStraddle(decimal underlyingPrice, decimal expectedStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Straddle(daysTillExpiry);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(2, filtered.Count);

            var call = filtered.Single(x => x.ID.OptionRight == OptionRight.Call);
            var put = filtered.Single(x => x.ID.OptionRight == OptionRight.Put);

            Assert.AreEqual(expectedExpiry, call.ID.Date);
            Assert.AreEqual(expectedStrike, call.ID.StrikePrice);

            Assert.AreEqual(expectedExpiry, put.ID.Date);
            Assert.AreEqual(expectedStrike, put.ID.StrikePrice);
        }

        [TestCase(100, -1, 10)]     // put > call
        [TestCase(100, 5, 5)]       // put = call
        public void FailsProtectiveCollar(decimal underlyingPrice, decimal callStrikeFromAtm, decimal putStrikeFromAtm)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Strangle(10, callStrikeFromAtm, putStrikeFromAtm);

            FailsFiltering(underlyingPrice, universeFunc);
        }

        [TestCase(100, 5, -5, 2, 105, 95, 10, false)]
        [TestCase(100, -1, -5, 2, 100, 95, 10, false)]
        [TestCase(100, 10, 5, 2, 110, 105, 10, false)]
        [TestCase(105.5, 5.2, -5.3, 2, 110, 100, 10, false)]
        [TestCase(1000, 5, -5, 0, 0, 0, 10, false)]             // extreme strike -> put strike = call strike
        [TestCase(1, 5, -5, 0, 0, 0, 10, false)]                // extreme strike -> put strike = call strike
        [TestCase(100, 5, -5, 2, 105, 95, 90, true)]
        public void FiltersProtectiveCollar(decimal underlyingPrice, decimal callStrikeFromAtm, decimal putStrikeFromAtm, int expectedCount,
            decimal expectedCallStrike, decimal expectedPutStrike, int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);

            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .ProtectiveCollar(daysTillExpiry, callStrikeFromAtm, putStrikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(expectedCount, filtered.Count);
            if (expectedCount == 0)
            {
                return;
            }

            var call = filtered.Single(x => x.ID.OptionRight == OptionRight.Call);
            var put = filtered.Single(x => x.ID.OptionRight == OptionRight.Put);

            Assert.AreEqual(expectedExpiry, call.ID.Date);
            Assert.AreEqual(expectedCallStrike, call.ID.StrikePrice);

            Assert.AreEqual(expectedExpiry, put.ID.Date);
            Assert.AreEqual(expectedPutStrike, put.ID.StrikePrice);
        }

        [TestCase(100, 0, 100, 10, false)]
        [TestCase(100, -5, 95, 10, false)]
        [TestCase(100, 5, 105, 10, false)]
        [TestCase(105.5, 5.2, 110, 10, false)]
        [TestCase(100, 0, 100, 90, true)]
        public void FiltersConversion(decimal underlyingPrice, decimal strikeFromAtm, decimal expectedStrike, 
            int daysTillExpiry, bool far = false)
        {
            var expectedExpiry = far ? new DateTime(2016, 5, 4) : new DateTime(2016, 3, 4);
            
            Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc = universe => universe
                                .Conversion(daysTillExpiry, strikeFromAtm);
            var filtered = Filtering(underlyingPrice, universeFunc);

            Assert.AreEqual(2, filtered.Count);

            var call = filtered.Single(x => x.ID.OptionRight == OptionRight.Call);
            var put = filtered.Single(x => x.ID.OptionRight == OptionRight.Put);

            Assert.AreEqual(expectedExpiry, call.ID.Date);
            Assert.AreEqual(expectedStrike, call.ID.StrikePrice);

            Assert.AreEqual(expectedExpiry, put.ID.Date);
            Assert.AreEqual(expectedStrike, put.ID.StrikePrice);
        }

        private List<Symbol> Filtering(decimal underlyingPrice, Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc)
        {
            return BaseFiltering(underlyingPrice, universeFunc, false);
        }

        private void FailsFiltering(decimal underlyingPrice, Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc)
        {
            BaseFiltering(underlyingPrice, universeFunc, true);
        }

        private List<Symbol> BaseFiltering(decimal underlyingPrice, Func<OptionFilterUniverse, OptionFilterUniverse> universeFunc, bool fails)
        {
            var underlying = new Tick { Value = underlyingPrice, Time = new DateTime(2016, 02, 26) };

            Func<IDerivativeSecurityFilterUniverse, IDerivativeSecurityFilterUniverse> func =
                universe => universeFunc(universe as OptionFilterUniverse);

            var filter = new FuncSecurityDerivativeFilter(func);
            var symbols = CreateOptionUniverse();

            var filterUniverse = new OptionFilterUniverse(symbols, underlying);
            filterUniverse.Refresh(symbols, underlying, underlying.EndTime);

            if (fails)
            {
                Assert.Throws<ArgumentException>(() => filter.Filter(filterUniverse));
                return null;
            }
            return filter.Filter(filterUniverse).ToList();
        }

        static Symbol[] CreateOptionUniverse()
        {
            var ticker = "SPY";
            var expiry = new DateTime(2016, 3, 4);
            var farExpiry = new DateTime(2016, 5, 4);
            var veryFarExpiry = new DateTime(2017, 5, 4);

            return new[] {
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 85, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 90, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 95, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 100, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 105, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 110, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 115, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 85, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 90, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 95, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 100, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 105, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 110, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 115, expiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 85, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 90, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 95, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 100, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 105, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 110, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 115, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 85, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 90, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 95, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 100, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 105, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 110, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 115, farExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 85, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 90, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 95, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 100, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 105, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 110, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Put, 115, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 85, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 90, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 95, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 100, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 105, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 110, veryFarExpiry),
                Symbol.CreateOption(ticker, Market.USA, OptionStyle.American, OptionRight.Call, 115, veryFarExpiry),
            };
        }
    }
}
