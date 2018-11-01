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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class CorporateEventBaseEnumeratorTests
    {
        private SubscriptionDataConfig _config;
        private TestTradableDayNotifier _tradableDayNotifier;
        private Tick _testBaseData;
        private Delisting _delistingEvent;

        [SetUp]
        public void Setup()
        {
            _config = SecurityTests.CreateTradeBarConfig();
            _tradableDayNotifier = new TestTradableDayNotifier();
            _testBaseData = new Tick(new DateTime(2009, 1, 2), _config.Symbol, 1, 1, 1);
            _delistingEvent = new Delisting(_config.Symbol, new DateTime(2009, 1, 1), 1, DelistingType.Delisted);
        }

        [Test]
        public void UnderlyingEnumeratorDataIsSent()
        {
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.MoveNextReturnValue = true;
            underlyingEnumerator.Data.Enqueue(_testBaseData);
            underlyingEnumerator.Data.Enqueue(_delistingEvent);
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_testBaseData, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);
        }

        [Test]
        public void DataSynchronization()
        {
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.MoveNextReturnValue = true;
            underlyingEnumerator.Data.Enqueue(_testBaseData);
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            enumerator.Data.Enqueue(_delistingEvent);
            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_testBaseData, enumerator.Current);
        }

        [Test]
        public void DataSynchronizationInBetweenOrder()
        {
            var tick1 = new Tick(new DateTime(2009, 1, 2, 1, 0, 0), _config.Symbol, 1, 1, 1);
            var tick2 = new Tick(new DateTime(2009, 1, 2, 2, 0, 0), _config.Symbol, 1, 1, 1);
            var tick3 = new Tick(new DateTime(2009, 1, 2, 3, 0, 0), _config.Symbol, 1, 1, 1);
            var tick4 = new Tick(new DateTime(2009, 1, 2, 4, 0, 0), _config.Symbol, 1, 1, 1);
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.Data.Enqueue(tick1);
            underlyingEnumerator.Data.Enqueue(tick2);
            underlyingEnumerator.Data.Enqueue(tick3);
            underlyingEnumerator.Data.Enqueue(tick4);

            var delisting = new Delisting(_config.Symbol, new DateTime(2009, 1, 2, 3, 1, 0), 1, DelistingType.Delisted);
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(tick1, enumerator.Current);

            enumerator.Data.Enqueue(delisting);
            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(tick2, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(tick3, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(delisting, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(tick4, enumerator.Current);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(tick4, enumerator.Current);
        }

        [Test]
        public void EmitsDataEvenIfUnderlyingReturnsFalse()
        {
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.MoveNextReturnValue = false;
            underlyingEnumerator.UseMoveNextReturnValue = true;
            underlyingEnumerator.CurrentData = _testBaseData;
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            enumerator.Data.Enqueue(_delistingEvent);
            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.AreEqual(_testBaseData, enumerator.Current);
        }

        [Test]
        public void EmitsDataEvenIfUnderlyingIsNull()
        {
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.MoveNextReturnValue = false;
            underlyingEnumerator.UseMoveNextReturnValue = true;
            underlyingEnumerator.Data.Enqueue(null);
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            enumerator.Data.Enqueue(_delistingEvent);
            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void EmitsDataEvenIfUnderlyingIsNullAndReturnedTrue()
        {
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.MoveNextReturnValue = true;
            underlyingEnumerator.UseMoveNextReturnValue = true;
            underlyingEnumerator.Data.Enqueue(null);
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            enumerator.Data.Enqueue(_delistingEvent);
            _tradableDayNotifier.TriggerEvent();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.NotNull(enumerator.Current);
            Assert.AreEqual(_delistingEvent, enumerator.Current);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void ReturnsFalseIfUnderlyingIsNull()
        {
            var underlyingEnumerator = new UnderlyingEnumerator();
            underlyingEnumerator.MoveNextReturnValue = true;
            underlyingEnumerator.UseMoveNextReturnValue = true;
            underlyingEnumerator.Data.Enqueue(null);
            var enumerator = new TestableCorporateEventBaseEnumerator(
                underlyingEnumerator,
                _config,
                _tradableDayNotifier,
                true
            );

            Assert.IsFalse(enumerator.MoveNext());
            Assert.Null(enumerator.Current);
        }
    }

    class TestableCorporateEventBaseEnumerator : CorporateEventBaseEnumerator
    {
        public Queue<BaseData> Data { get; }

        public TestableCorporateEventBaseEnumerator(
            IEnumerator<BaseData> enumerator,
            SubscriptionDataConfig config,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
            : base(enumerator, config, tradableDayNotifier, includeAuxiliaryData)
        {
            Data = new Queue<BaseData>();
        }

        protected override BaseData CheckNewEvent(DateTime date)
        {
            return Data.Dequeue();
        }
    }

    class UnderlyingEnumerator : IEnumerator<BaseData>
    {
        public Queue<BaseData> Data { get; }
        public BaseData CurrentData { get; set; }
        public bool MoveNextReturnValue { get; set; }
        public bool UseMoveNextReturnValue { get; set; }

        public UnderlyingEnumerator()
        {
            Data = new Queue<BaseData>();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            if (UseMoveNextReturnValue)
            {
                return MoveNextReturnValue;
            }
            var value = Data.Any();
            if (value)
            {
                CurrentData = Data.Dequeue();
            }
            return value;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public BaseData Current => CurrentData;

        object IEnumerator.Current => Current;
    }

    class TestTradableDayNotifier : ITradableDatesNotifier
    {
        public event EventHandler<DateTime> NewTradableDate;

        public void TriggerEvent()
        {
            NewTradableDate?.Invoke(this, DateTime.UtcNow);
        }
    }
}
