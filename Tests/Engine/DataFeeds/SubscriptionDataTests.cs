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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class SubscriptionDataTests
    {
        [Test]
        public void CreatedSubscriptionRoundsTimeDownForDataWithPeriod()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY
            };

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var subscription = SubscriptionData.Create(config, exchangeHours, offsetProvider, tb);

            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 0, 0), subscription.Data.Time);
            Assert.AreEqual(new DateTime(2020, 5, 21, 9, 0, 0), subscription.Data.EndTime);
        }

        [Test]
        public void CreatedSubscriptionDoesNotRoundDownForPeriodLessData()
        {
            var data = new MyCustomData
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY
            };

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var subscription = SubscriptionData.Create(config, exchangeHours, offsetProvider, data);

            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 9, 0), subscription.Data.Time);
            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 9, 0), subscription.Data.EndTime);
        }

        internal class MyCustomData : BaseData
        {
        }
    }
}
