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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveDelistingEventProviderEnumeratorTests
    {
        [Test]
        public void EmitsDelistingEventsBasedOnCurrentTime()
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY_C_192_Feb19_2016,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var delistingDate = config.Symbol.GetDelistingDate();
            var time = delistingDate.AddDays(-10);
            var cache = new SecurityCache();
            cache.AddData(new Tick(DateTime.UtcNow, config.Symbol, 20, 10));
            var timeProvider = new ManualTimeProvider(time);

            IEnumerator<BaseData> enumerator;
            Assert.IsTrue(LiveDelistingEventProviderEnumerator.TryCreate(config, timeProvider, null, cache, new LocalDiskMapFileProvider(), out enumerator));

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // advance until delisting date, take into account 5 hour offset of NY
            timeProvider.Advance(TimeSpan.FromDays(10));
            timeProvider.Advance(TimeSpan.FromHours(5));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(config.Symbol, (enumerator.Current as Delisting).Symbol);
            Assert.AreEqual(delistingDate, (enumerator.Current as Delisting).Time);
            Assert.AreEqual(15, (enumerator.Current as Delisting).Price);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // when the day ends the delisted event will pass through
            timeProvider.Advance(TimeSpan.FromDays(1));
            cache.AddData(new Tick(DateTime.UtcNow, config.Symbol, 40, 20));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(config.Symbol, (enumerator.Current as Delisting).Symbol);
            Assert.AreEqual(delistingDate.AddDays(1), (enumerator.Current as Delisting).Time);
            Assert.AreEqual(30, (enumerator.Current as Delisting).Price);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }
    }
}