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
using QuantConnect.Scheduling;

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
            int count = 0;
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var sevent = new ScheduledEvent("test", new[] { time.AddSeconds(-2), time.AddSeconds(-1), time}, (n, t) => count++);
            sevent.Scan(time);
            Assert.AreEqual(3, count);
        }

        [Test]
        public void SkipsEventsUntilTime()
        {
            int count = 0;
            var time = new DateTime(2015, 08, 11, 10, 30, 0);
            var sevent = new ScheduledEvent("test", new[] { time.AddSeconds(-2), time.AddSeconds(-1), time }, (n, t) => count++);
            // skips all preceding events, not including the specified time
            sevent.SkipEventsUntil(time);
            Assert.AreEqual(time, sevent.NextEventUtcTime);
        }
    }
}
