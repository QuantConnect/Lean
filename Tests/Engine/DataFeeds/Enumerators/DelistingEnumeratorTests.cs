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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class DelistingEnumeratorTests
    {
        private SubscriptionDataConfig _config;
        private TestTradableDayNotifier _tradableDayNotifier;

        [SetUp]
        public void SetUp()
        {
            var symbol = Symbol.CreateFuture("ASD", Market.USA, new DateTime(2018, 01, 01));

            _config = new SubscriptionDataConfig(typeof(TradeBar),
                symbol,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);

            _tradableDayNotifier = new TestTradableDayNotifier
            {
                TradableDate = DateTime.UtcNow,
                LastBaseData = new Tick(DateTime.UtcNow, symbol, 10, 5)
            };
        }

        [Test]
        public void MoveNextIsTrueCurrentNull()
        {
            var enumerator = new DelistingEnumerator(_config,
                null,
                _tradableDayNotifier,
                true);

            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void EmitsBothEventsIfDateIsPastDelisted()
        {
            var enumerator = new DelistingEnumerator(_config,
                null,
                _tradableDayNotifier,
                true);
            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());

            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(DateTime.UtcNow.Date, (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(_config.Symbol.ID.Date.AddDays(1), (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void EmitsWarningAsOffDelistingDate()
        {
            var enumerator = new DelistingEnumerator(_config,
                null,
                _tradableDayNotifier,
                true);
            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());

            // should NOT emit
            _tradableDayNotifier.TradableDate = _config.Symbol.ID.Date.Subtract(TimeSpan.FromMinutes(1));
            _tradableDayNotifier.TriggerEvent();

            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());

            // should emit
            _tradableDayNotifier.TradableDate = _config.Symbol.ID.Date;
            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(_config.Symbol.ID.Date, (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void EmitsDelistedAfterDelistingDate()
        {
            var enumerator = new DelistingEnumerator(_config,
                null,
                _tradableDayNotifier,
                true);
            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());

            // should emit warning
            _tradableDayNotifier.TradableDate = _config.Symbol.ID.Date;
            _tradableDayNotifier.TriggerEvent();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);

            // should NOT emit if not AFTER delisting date
            _tradableDayNotifier.TriggerEvent();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // should emit AFTER delisting date
            _tradableDayNotifier.TradableDate = _config.Symbol.ID.Date.AddMinutes(1);
            _tradableDayNotifier.TriggerEvent();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current as Delisting);
            Assert.AreEqual(MarketDataType.Auxiliary, enumerator.Current.DataType);
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(_config.Symbol.ID.Date.AddDays(1), (enumerator.Current as Delisting).Time.Date);
            Assert.AreEqual(7.5, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        class TestTradableDayNotifier : ITradableDatesNotifier
        {
            public event EventHandler<NewTradableDateEventArgs> NewTradableDate;
            public DateTime TradableDate { get; set; }
            public BaseData LastBaseData { get; set; }

            public void TriggerEvent()
            {
                NewTradableDate?.Invoke(this, new NewTradableDateEventArgs(TradableDate, LastBaseData));
            }
        }
    }
}
