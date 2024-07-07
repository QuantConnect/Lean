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
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class DelistingEnumeratorTests
    {
        private SubscriptionDataConfig _config;

        [SetUp]
        public void SetUp()
        {
            var symbol = Symbol.CreateFuture("ASD", Market.USA, new DateTime(2018, 01, 01));

            _config = new SubscriptionDataConfig(
                typeof(TradeBar),
                symbol,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false
            );
        }

        [Test]
        public void EmitsBothEventsIfDateIsPastDelisted()
        {
            var eventProvider = new DelistingEventProvider();
            eventProvider.Initialize(_config, null, null, DateTime.UtcNow);

            var enumerator = eventProvider
                .GetEvents(
                    new NewTradableDateEventArgs(
                        DateTime.UtcNow,
                        new Tick(DateTime.UtcNow, _config.Symbol, 10, 5),
                        _config.Symbol,
                        null
                    )
                )
                .GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(_config.Symbol.ID.Date, (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(
                _config.Symbol.ID.Date.AddDays(1),
                (enumerator.Current as Delisting).Time.Date
            );
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Dispose();
        }

        [Test]
        public void EmitsWarningAsOffDelistingDate()
        {
            var eventProvider = new DelistingEventProvider();
            eventProvider.Initialize(_config, null, null, DateTime.UtcNow);

            // should NOT emit
            var enumerator = eventProvider
                .GetEvents(
                    new NewTradableDateEventArgs(
                        _config.Symbol.ID.Date.Subtract(TimeSpan.FromMinutes(1)),
                        new Tick(DateTime.UtcNow, _config.Symbol, 10, 5),
                        _config.Symbol,
                        null
                    )
                )
                .GetEnumerator();

            Assert.IsFalse(enumerator.MoveNext());

            // should emit
            enumerator = eventProvider
                .GetEvents(
                    new NewTradableDateEventArgs(
                        _config.Symbol.ID.Date,
                        new Tick(DateTime.UtcNow, _config.Symbol, 10, 5),
                        _config.Symbol,
                        null
                    )
                )
                .GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(_config.Symbol.ID.Date, (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Dispose();
        }

        [Test]
        public void EmitsDelistedAfterDelistingDate()
        {
            var eventProvider = new DelistingEventProvider();
            eventProvider.Initialize(_config, null, null, DateTime.UtcNow);

            // should emit warning
            var enumerator = eventProvider
                .GetEvents(
                    new NewTradableDateEventArgs(
                        _config.Symbol.ID.Date,
                        new Tick(DateTime.UtcNow, _config.Symbol, 10, 5),
                        _config.Symbol,
                        null
                    )
                )
                .GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);

            // should NOT emit if not AFTER delisting date
            enumerator = eventProvider
                .GetEvents(
                    new NewTradableDateEventArgs(
                        _config.Symbol.ID.Date,
                        new Tick(DateTime.UtcNow, _config.Symbol, 10, 5),
                        _config.Symbol,
                        null
                    )
                )
                .GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());

            // should emit AFTER delisting date
            enumerator = eventProvider
                .GetEvents(
                    new NewTradableDateEventArgs(
                        _config.Symbol.ID.Date.AddMinutes(1),
                        new Tick(DateTime.UtcNow, _config.Symbol, 10, 5),
                        _config.Symbol,
                        null
                    )
                )
                .GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(
                _config.Symbol.ID.Date.AddDays(1),
                (enumerator.Current as Delisting).Time.Date
            );
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Dispose();
        }

        [Test]
        public void EmitsDelistedWarningOnNonTradableDay()
        {
            // Unit test to simulate and reproduce #5545

            // Give us two tradable days before and after expiration
            var tradableDays = new List<DateTime>
            {
                new DateTime(2021, 01, 01),
                new DateTime(2021, 01, 04)
            };

            // Set expiration as 1/2/21 a Saturday, not included in our tradble days
            var expiration = new DateTime(2021, 01, 02);
            var symbol = Symbol.CreateFuture("ASD", Market.USA, expiration);
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                symbol,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false
            );

            var eventProvider = new DelistingEventProvider();
            eventProvider.Initialize(config, null, null, DateTime.UtcNow);

            var tradableDateEvents = tradableDays
                .Select(day => new NewTradableDateEventArgs(
                    day,
                    new Tick(day, config.Symbol, 10, 5),
                    config.Symbol,
                    null
                ))
                .GetEnumerator();

            // Pass in the day before expiration should be nothing
            tradableDateEvents.MoveNext();
            var enumerator = eventProvider.GetEvents(tradableDateEvents.Current).GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());

            // Pass in the following monday, should be both but warning first and still scheduled for saturday
            tradableDateEvents.MoveNext();
            enumerator = eventProvider.GetEvents(tradableDateEvents.Current).GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(config.Symbol.ID.Date, (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(
                config.Symbol.ID.Date.AddDays(1),
                (enumerator.Current as Delisting).Time.Date
            );
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsFalse(enumerator.MoveNext());

            tradableDateEvents.Dispose();
            enumerator.Dispose();
        }
    }
}
