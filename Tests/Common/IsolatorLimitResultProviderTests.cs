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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class IsolatorLimitResultProviderTests
    {
        private TimeMonitor _timeMonitor;

        [OneTimeSetUp]
        public void Setup()
        {
            _timeMonitor = new TimeMonitor(monitorIntervalMs:3);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _timeMonitor.DisposeSafely();
        }

        [Test]
        public void ConsumeRequestsAdditionalTimeAfterOneMinute()
        {
            var minuteElapsed = new ManualResetEvent(false);
            var consumeCompleted = new ManualResetEvent(false);
            var consumeStarted = new ManualResetEvent(false);

            Action code = () =>
            {
                if (!minuteElapsed.WaitOne(10000))
                {
                    throw new TimeoutException("minuteElapsed");
                }
            };
            var provider = new FakeIsolatorLimitResultProvider();
            var timeProvider = new ManualTimeProvider(new DateTime(2000, 01, 01));

            Task.Run(() =>
            {
                consumeStarted.Set();
                var name = nameof(ConsumeRequestsAdditionalTimeAfterOneMinute);
                provider.Consume(timeProvider, code, _timeMonitor);
                consumeCompleted.Set();
            });
            if (!consumeStarted.WaitOne(50))
            {
                Assert.Fail("Consume should have started.");
            }

            Thread.Sleep(15);
            timeProvider.Advance(TimeSpan.FromSeconds(45));

            Assert.AreEqual(0, provider.Invocations.Count);

            timeProvider.Advance(TimeSpan.FromSeconds(15));
            Thread.Sleep(15);

            minuteElapsed.Set();
            if (!consumeCompleted.WaitOne(50))
            {
                Assert.Fail("Consume should have returned.");
            }

            Assert.AreEqual(1, provider.Invocations.Count);
            Assert.AreEqual(1, provider.Invocations[0]);

            // give time to the monitor to register the time consumer ended
            Thread.Sleep(50);
            Assert.AreEqual(0, _timeMonitor.Count);
        }

        [Test]
        public void ConsumeDoesNotRequestAdditionalTimeBeforeOneMinute()
        {
            var timeProvider = new ManualTimeProvider(new DateTime(2000, 01, 01));
            var provider = new FakeIsolatorLimitResultProvider();
            Action code = () =>
            {
                Thread.Sleep(5);
                timeProvider.Advance(TimeSpan.FromMinutes(.99));
                Thread.Sleep(5);
            };
            provider.Consume(timeProvider, code, _timeMonitor);

            Assert.IsEmpty(provider.Invocations);

            // give time to the monitor to register the time consumer ended
            Thread.Sleep(50);
            Assert.AreEqual(0, _timeMonitor.Count);
        }

        [Test]
        public void ConsumesMultipleMinutes()
        {
            var timeProvider = new ManualTimeProvider(new DateTime(2000, 01, 01));
            var provider = new FakeIsolatorLimitResultProvider();
            Action code = () =>
            {
                // lets give the monitor time to register the initial time
                Thread.Sleep(50);
                for (int i = 0; i < 4; i++)
                {
                    timeProvider.AdvanceSeconds(45);
                    // give the monitoring task time to request more time
                    Thread.Sleep(20);
                }
            };

            provider.Consume(timeProvider, code, _timeMonitor);

            Assert.AreEqual(3, provider.Invocations.Count);
            Assert.IsTrue(provider.Invocations.TrueForAll(invoc => invoc == 1));

            // give time to the monitor to register the time consumer ended
            Thread.Sleep(50);
            Assert.AreEqual(0, _timeMonitor.Count);
        }

        private class FakeIsolatorLimitResultProvider : IIsolatorLimitResultProvider
        {
            public List<int> Invocations { get; } = new List<int>();

            public IsolatorLimitResult IsWithinLimit()
            {
                return new IsolatorLimitResult(TimeSpan.Zero, string.Empty);
            }

            public void RequestAdditionalTime(int minutes)
            {
                Invocations.Add(minutes);
            }

            public bool TryRequestAdditionalTime(int minutes)
            {
                Invocations.Add(minutes);
                return true;
            }
        }
    }
}
