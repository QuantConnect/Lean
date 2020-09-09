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
using Moq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.All)]
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

        [Test]
        public void SubscriptionIsDisposed()
        {
            var dataPoints = 10;
            var enumerator = new TestDataEnumerator { MoveNextTrueCount = dataPoints };
            var factorFileProfider = new Mock<IFactorFileProvider>();
            factorFileProfider.Setup(s => s.Get(It.IsAny<Symbol>())).Returns(FactorFile.Read(_security.Symbol.Value, _config.Market));

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
                factorFileProfider.Object,
                false);

            var count = 0;
            while (enumerator.MoveNextTrueCount > 8)
            {
                if (count++ > 100)
                {
                    Assert.Fail($"Timeout waiting for producer. {enumerator.MoveNextTrueCount}");
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
            var factorFileProfider = new Mock<IFactorFileProvider>();
            factorFileProfider.Setup(s => s.Get(It.IsAny<Symbol>())).Returns(FactorFile.Read(_security.Symbol.Value, _config.Market));

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
                factorFileProfider.Object,
                false);

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

            // enumerator is disposed by the producer
            count = 0;
            while (!enumerator.Disposed)
            {
                if (count++ > 100)
                {
                    Assert.Fail("Timeout waiting for producer");
                }
                Thread.Sleep(1);
            }
        }

        [Test]
        // This unit tests reproduces GH 3885 where the consumer hanged forever
        public void ConsumerDoesNotHang()
        {
            for (var i = 0; i < 10000; i++)
            {
                var dataPoints = 10;

                var enumerator = new TestDataEnumerator {MoveNextTrueCount = dataPoints};
                var factorFileProfider = new Mock<IFactorFileProvider>();
                factorFileProfider.Setup(s => s.Get(It.IsAny<Symbol>())).Returns(FactorFile.Read(_security.Symbol.Value, _config.Market));

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
                    factorFileProfider.Object,
                    false);

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
