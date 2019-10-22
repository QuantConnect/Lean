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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture]
    public class ScheduledEventTests
    {
        [Test]
        public void FiresEventOnTime()
        {
            var fired = false;
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var sevent = new ScheduledEvent("test", time, (n, t) => fired = true);
            sevent.Scan(time);
            Assert.IsTrue(fired);
        }

        [Test]
        public void NextEventTimeIsMaxValueWhenNoEvents()
        {
            var sevent = new ScheduledEvent("test", new DateTime[0], (n, t) => { });
            Assert.AreEqual(DateTime.MaxValue, sevent.NextEventUtcTime);
        }

        [Test]
        public void NextEventTimeIsMaxValueWhenNoMoreEvents()
        {
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var sevent = new ScheduledEvent("test", time, (n, t) => { });
            sevent.Scan(time);
            Assert.AreEqual(DateTime.MaxValue, sevent.NextEventUtcTime);
        }

        [Test]
        public void FiresSkippedEventsInSameCallToScan()
        {
            var count = 0;
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var sevent = new ScheduledEvent("test", new[] { time.AddSeconds(-2), time.AddSeconds(-1), time}, (n, t) => count++);
            sevent.Scan(time);
            Assert.AreEqual(3, count);
        }

        [Test]
        public void SkipsEventsUntilTime()
        {
            var count = 0;
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var sevent = new ScheduledEvent("test", new[] { time.AddSeconds(-2), time.AddSeconds(-1), time }, (n, t) => count++);
            // skips all preceding events, not including the specified time
            sevent.SkipEventsUntil(time);
            Assert.AreEqual(time, sevent.NextEventUtcTime);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void SkipEventsUntilDoesNotSkipFirstEventEqualToRequestedTime()
        {
            var count = 0;
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var eventTimes = new[] {time, time.AddSeconds(1)};
            var sevent = new ScheduledEvent("test", eventTimes, (n, t) => count++);
            // skips all preceding events, not including the specified time
            sevent.SkipEventsUntil(time);
            Assert.AreEqual(time, sevent.NextEventUtcTime);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void FiresEventWhenTimeEquals()
        {
            var triggered = false;
            var se = new ScheduledEvent("test", new DateTime(2015, 08, 07), (name, triggerTime) =>
            {
                triggered = true;
            })
            { IsLoggingEnabled = true };

            se.Scan(new DateTime(2015, 08, 06));
            Assert.IsFalse(triggered);

            se.Scan(new DateTime(2015, 08, 07));
            Assert.IsTrue(triggered);
        }

        [Test]
        public void FiresEventWhenTimePasses()
        {
            var triggered = false;
            var se = new ScheduledEvent("test", new DateTime(2015, 08, 07), (name, triggerTime) =>
            {
                triggered = true;
            })
            { IsLoggingEnabled = true };

            se.Scan(new DateTime(2015, 08, 06));
            Assert.IsFalse(triggered);

            se.Scan(new DateTime(2015, 08, 08));
            Assert.IsTrue(triggered);
        }

        [Test]
        public void SchedulesNextEvent()
        {
            var first = new DateTime(2015, 08, 07);
            var second = new DateTime(2015, 08, 08);
            var dates = new[] { first, second }.ToHashSet();
            var se = new ScheduledEvent("test", dates.ToList(), (name, triggerTime) =>
            {
                dates.Remove(triggerTime);
            });

            se.Scan(first);
            Assert.IsFalse(dates.Contains(first));

            se.Scan(second);
            Assert.IsFalse(dates.Contains(second));
        }

        [Test]
        public void DoesNothingAfterEventsEnd()
        {
            var triggered = false;
            var first = new DateTime(2015, 08, 07);
            var se = new ScheduledEvent("test", first, (name, triggerTime) =>
            {
                triggered = true;
            });

            se.Scan(first);
            Assert.IsTrue(triggered);

            triggered = false;
            se.Scan(first.AddYears(100));
            Assert.IsFalse(triggered);
        }

        [Test]
        public void ScheduledEventsWithSameNameAreDifferent()
        {
            var first = DateTime.UtcNow;
            var se1 = new ScheduledEvent("test", first);
            var se2 = new ScheduledEvent("test", first);

            Assert.AreEqual(se1.Name, se2.Name);
            Assert.AreNotEqual(se1, se2);
        }

        [Test]
        public void CompareToItselfReturnsTrue()
        {
            var time = DateTime.UtcNow;
            var se1 = new ScheduledEvent("test", time);
            var se2 = se1;

            Assert.IsTrue(Equals(se1, se2));
            Assert.AreEqual(se1, se2);
        }

        [Test]
        public void CompareToNullReturnsFalse()
        {
            var time = DateTime.UtcNow;
            var se = new ScheduledEvent("test", time);

            Assert.IsFalse(Equals(se, null));
            Assert.AreNotEqual(se, null);
        }

        [Test]
        public void ToStringTest()
        {
            var name = "PepeGrillo";
            var se = new ScheduledEvent(name, DateTime.UtcNow);

            Assert.IsNotNull(se);
            Assert.AreEqual(name, se.ToString());
        }
    }
}
