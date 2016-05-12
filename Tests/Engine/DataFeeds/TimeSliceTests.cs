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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class TimeSliceTests
    {
        [Test]
        public void HandlesTicks_ExpectInOrderWithNoDuplicates()
        {
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(Tick), 
                Symbols.EURUSD, 
                Resolution.Tick, 
                TimeZones.Utc, 
                TimeZones.Utc, 
                true, 
                true, 
                false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), 
                subscriptionDataConfig, 
                new Cash(CashBook.AccountCurrency, 0, 1m), 
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            DateTime refTime = DateTime.UtcNow;

            Tick[] rawTicks = Enumerable
                .Range(0, 10)
                .Select(i => new Tick(refTime.AddSeconds(i), Symbols.EURUSD, 1.3465m, 1.34652m))
                .ToArray();

            IEnumerable<TimeSlice> timeSlices = rawTicks.Select(t => TimeSlice.Create(
                t.Time,
                TimeZones.Utc,
                new CashBook(),
                new List<DataFeedPacket> {new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>() {t})},
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>())));

            Tick[] timeSliceTicks = timeSlices.SelectMany(ts => ts.Slice.Ticks.Values.SelectMany(x => x)).ToArray();

            Assert.AreEqual(rawTicks.Length, timeSliceTicks.Length);
            for (int i = 0; i < rawTicks.Length; i++)
            {
                Assert.IsTrue(Compare(rawTicks[i], timeSliceTicks[i]));
            }
        }

        private bool Compare(Tick expected, Tick actual)
        {
            return expected.Time == actual.Time
                   && expected.BidPrice == actual.BidPrice
                   && expected.AskPrice == actual.AskPrice
                   && expected.Quantity == actual.Quantity;
        }
    }
}
