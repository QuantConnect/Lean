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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class SubscriptionUtilsTests
    {
        private Security _security;
        private SubscriptionDataConfig _config;

        [SetUp]
        public void SetUp()
        {
            _security = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            _config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true, true, false);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(20)]
        public void FirstLoopLimit(int firstLoopLimit)
        {
            var dataPoints = 10;
            var enumerator = new TestDataEnumerator { MoveNextTrueCount = dataPoints };

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    DateTime.UtcNow,
                    Time.EndOfTime
                ),
                enumerator,
                firstLoopLimit: firstLoopLimit);

            var count = 0;
            var expectedValue = dataPoints - firstLoopLimit - 1;
            expectedValue = expectedValue > 0 ? expectedValue : -1;
            while (enumerator.MoveNextTrueCount != expectedValue)
            {
                if (count++ > 100)
                {
                    Assert.Fail("Timeout waiting for producer");
                }
                Thread.Sleep(1);
            }
            // producer should only have produced 'firstLoopLimit' data points, lets assert remaining to produce
            Assert.AreEqual(expectedValue, enumerator.MoveNextTrueCount);

            for (var j = 0; j < dataPoints; j++)
            {
                Assert.IsTrue(subscription.MoveNext());
            }
            Assert.IsFalse(subscription.MoveNext());
            subscription.DisposeSafely();
            Assert.IsTrue(enumerator.Disposed);
        }

        [Test]
        public void SubscriptionIsDisposed()
        {
            var dataPoints = 10;
            var enumerator = new TestDataEnumerator { MoveNextTrueCount = dataPoints };

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    DateTime.UtcNow,
                    Time.EndOfTime
                ),
                enumerator,
                firstLoopLimit: 1);

            var count = 0;
            while (enumerator.MoveNextTrueCount != 8)
            {
                if (count++ > 100)
                {
                    Assert.Fail("Timeout waiting for producer");
                }
                Thread.Sleep(1);
            }

            subscription.DisposeSafely();
            Assert.IsFalse(subscription.MoveNext());
        }

        [Test]
        public void ThrowingEnumeratorStackDisposesOfSubscription()
        {
            var enumerator = new TestDataEnumerator { MoveNextTrueCount = 10, ThrowException = true};

            var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                new SubscriptionRequest(
                    false,
                    null,
                    _security,
                    _config,
                    DateTime.UtcNow,
                    Time.EndOfTime
                ),
                enumerator,
                firstLoopLimit: 1);

            var count = 0;
            while (enumerator.MoveNextTrueCount != 9)
            {
                if (count++ > 100)
                {
                    Assert.Fail("Timeout waiting for producer");
                }
                Thread.Sleep(1);
            }

            Assert.IsFalse(subscription.MoveNext());
            Assert.IsTrue(subscription.EndOfStream);
            Assert.IsTrue(enumerator.Disposed);
        }

        [Test]
        // This unit tests reproduces GH 3885 where the consumer hanged forever
        public void ConsumerDoesNotHang()
        {
            for (var i = 0; i < 10000; i++)
            {
                var dataPoints = 10;

                var enumerator = new TestDataEnumerator {MoveNextTrueCount = dataPoints};

                var subscription = SubscriptionUtils.CreateAndScheduleWorker(
                    new SubscriptionRequest(
                        false,
                        null,
                        _security,
                        _config,
                        DateTime.UtcNow,
                        Time.EndOfTime
                    ),
                    enumerator,
                    firstLoopLimit: dataPoints / 2);

                for (var j = 0; j < dataPoints; j++)
                {
                   Assert.IsTrue(subscription.MoveNext());
                }
                Assert.IsFalse(subscription.MoveNext());
                subscription.DisposeSafely();
            }
        }

        private class TestDataEnumerator : IEnumerator<BaseData>
        {
            public bool ThrowException { get; set; }
            public bool Disposed { get; set; }
            public int MoveNextTrueCount { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }

            public bool MoveNext()
            {
                Current = new Tick(DateTime.UtcNow,Symbols.SPY, 1, 2);
                var result = --MoveNextTrueCount >= 0;
                if (ThrowException)
                {
                    throw new Exception("TestDataEnumerator.MoveNext()");
                }
                return result;
            }

            public void Reset()
            {
            }

            public BaseData Current { get; set; }

            object IEnumerator.Current => Current;
        }
    }
}
