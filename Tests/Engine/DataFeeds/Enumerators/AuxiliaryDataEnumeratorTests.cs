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
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class AuxiliaryDataEnumeratorTests
    {
        private SubscriptionDataConfig _config;
        private TestTradableDayNotifier _tradableDayNotifier;
        private Delisting _delistingEvent;

        [SetUp]
        public void Setup()
        {
            _config = SecurityTests.CreateTradeBarConfig();
            _tradableDayNotifier = new TestTradableDayNotifier();
            _tradableDayNotifier.Symbol = _config.Symbol;
            _delistingEvent = new Delisting(_config.Symbol, new DateTime(2009, 1, 1), 1, DelistingType.Delisted);
        }

        [Test]
        public void IsSetToNullIfNoDataAlwaysReturnsTrue()
        {
            var eventProvider = new TestableEventProvider();
            var enumerator = new AuxiliaryDataEnumerator(
                _config,
                null,
                null,
                new ITradableDateEventProvider[] { eventProvider },
                _tradableDayNotifier,
                true,
                DateTime.UtcNow
            );

            eventProvider.Data.Enqueue(_delistingEvent);
            _tradableDayNotifier.TriggerEvent();

            Assert.Null(enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.Null(enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.Null(enumerator.Current);

            enumerator.Dispose();
        }
    }

    class TestableEventProvider : ITradableDateEventProvider
    {
        public readonly Queue<BaseData> Data = new Queue<BaseData>();

        public IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            yield return Data.Dequeue();
        }

        public void Initialize(SubscriptionDataConfig config, FactorFile factorFile, MapFile mapFile, DateTime startTime)
        {
        }
    }

    class TestTradableDayNotifier : ITradableDatesNotifier
    {
        public event EventHandler<NewTradableDateEventArgs> NewTradableDate;
        public DateTime TradableDate { get; set; }
        public BaseData LastBaseData { get; set; }
        public Symbol Symbol { get; set; }

        public void TriggerEvent()
        {
            NewTradableDate?.Invoke(this, new NewTradableDateEventArgs(TradableDate, LastBaseData, Symbol, null));
        }
    }
}
