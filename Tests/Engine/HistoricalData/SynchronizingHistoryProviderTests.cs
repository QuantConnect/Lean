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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Common.Securities;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Engine.HistoricalData
{
    [TestFixture]
    public class SynchronizingHistoryProviderTests
    {
        private readonly DateTime _start = new DateTime(2013, 10, 07);

        [Test]
        public void ToleratesSubscriptionAdvancingWithoutData()
        {
            // a null entry reproduces an enumerator that advances without producing data, which
            // Subscription.MoveNext surfaces as true with a null Current
            var bar = new TradeBar(_start, Symbols.SPY, 1, 1, 1, 1, 1, Time.OneMinute);
            using var subscription = GetSubscription(new List<SubscriptionData>
            {
                null,
                new SubscriptionData(bar, _start.ConvertToUtc(TimeZones.NewYork))
            });

            var historyProvider = new TestSynchronizingHistoryProvider();

            List<Slice> slices = null;
            Assert.DoesNotThrow(() => slices = historyProvider
                .GetSlices(new List<Subscription> { subscription }, TimeZones.NewYork).ToList());

            // the empty advance is pumped through, the data behind it is still emitted
            Assert.AreEqual(1, slices.Count);
            Assert.AreEqual(bar.EndTime, slices[0][Symbols.SPY].EndTime);
        }

        [Test]
        public void ToleratesSubscriptionWithoutAnyData()
        {
            using var subscription = GetSubscription(new List<SubscriptionData> { null });

            var historyProvider = new TestSynchronizingHistoryProvider();

            List<Slice> slices = null;
            Assert.DoesNotThrow(() => slices = historyProvider
                .GetSlices(new List<Subscription> { subscription }, TimeZones.NewYork).ToList());

            Assert.IsEmpty(slices);
        }

        private Subscription GetSubscription(List<SubscriptionData> data)
        {
            var end = _start.AddDays(1);
            var security = SecurityTests.GetSecurity();
            var config = SecurityTests.CreateTradeBarConfig(Resolution.Minute);
            var request = new SubscriptionRequest(false, null, security, config, _start, end);

            return new Subscription(request, data.GetEnumerator(),
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, end));
        }

        private class TestSynchronizingHistoryProvider : SynchronizingHistoryProvider
        {
            public override void Initialize(HistoryProviderInitializeParameters parameters)
            {
            }

            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                return null;
            }

            public IEnumerable<Slice> GetSlices(List<Subscription> subscriptions, DateTimeZone sliceTimeZone)
            {
                return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
            }
        }
    }
}
