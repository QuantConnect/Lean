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
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Tests.Common.Util.RateLimit
{
    [TestFixture]
    public class FixedIntervalRefillStrategyTests
    {
        [Test]
        public void DoesNotRefillUntilIntervalHasElapsed()
        {
            var time = new DateTime(2000, 01, 01);
            var timeProvider = new ManualTimeProvider(time);

            const int refillAmount = 1;
            var refillInterval = TimeSpan.FromMinutes(1);
            var strategy = new FixedIntervalRefillStrategy(timeProvider, refillAmount, refillInterval);

            var refill = strategy.Refill();
            Assert.AreEqual(0, refill);

            timeProvider.Advance(refillInterval.Subtract(TimeSpan.FromTicks(1)));
            refill = strategy.Refill();
            Assert.AreEqual(0, refill);

            timeProvider.Advance(TimeSpan.FromTicks(1));
            refill = strategy.Refill();
            Assert.AreEqual(refillAmount, refill);
        }

        [Test]
        public void ComputesRefillsBasedOnNumberOfPassedIntervals()
        {
            var time = new DateTime(2000, 01, 01);
            var timeProvider = new ManualTimeProvider(time);

            const int refillAmount = 1;
            var refillInterval = TimeSpan.FromMinutes(1);
            var strategy = new FixedIntervalRefillStrategy(timeProvider, refillAmount, refillInterval);

            var intervals = 3.5;
            var advance = TimeSpan.FromTicks((long) (refillInterval.Ticks * intervals));
            timeProvider.Advance(advance);

            var refill = strategy.Refill();
            var expected = (int) intervals * refillAmount;
            Assert.AreEqual(expected, refill);

            timeProvider.Advance(advance);
            refill = strategy.Refill();
            expected = (int) (intervals * 2) * refillAmount - expected;
            Assert.AreEqual(expected, refill);
        }
    }
}