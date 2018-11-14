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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class CorporateEventBaseEnumeratorTests
    {
        private SubscriptionDataConfig _config;
        private TestTradableDayNotifier _tradableDayNotifier;
        private Delisting _delistingEvent;

        [SetUp]
        public void Setup()
        {
            _config = SecurityTests.CreateTradeBarConfig();
            _tradableDayNotifier = new TestTradableDayNotifier();
            _delistingEvent = new Delisting(_config.Symbol, new DateTime(2009, 1, 1), 1, DelistingType.Delisted);
        }

        [Test]
        public void IsSetToNullIfNoDataAlwaysReturnsTrue()
        {
            var enumerator = new TestableCorporateEventBaseEnumerator(
                _config,
                _tradableDayNotifier,
                true
            );

            enumerator.Data.Enqueue(_delistingEvent);
            _tradableDayNotifier.TriggerEvent();

            Assert.Null(enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.Null(enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.Null(enumerator.Current);
        }
    }

    class TestableCorporateEventBaseEnumerator : CorporateEventBaseEnumerator
    {
        public Queue<BaseData> Data { get; }

        public TestableCorporateEventBaseEnumerator(
            SubscriptionDataConfig config,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
            : base(config, tradableDayNotifier, includeAuxiliaryData)
        {
            Data = new Queue<BaseData>();
        }

        protected override IEnumerable<BaseData> GetCorporateEvents(NewTradableDateEventArgs eventArgs)
        {
            yield return Data.Dequeue();
        }
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
