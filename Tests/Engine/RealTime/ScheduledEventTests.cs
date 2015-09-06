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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Scheduling;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.RealTime
{
    [TestFixture]
    public class ScheduledEventTests
    {
        [Test]
        public void FiresEventWhenTimeEquals()
        {
            var triggered = false;
            var se = new ScheduledEvent("test", new DateTime(2015, 08, 07), (name, triggerTime) =>
            {
                triggered = true;
            });
            se.IsLoggingEnabled = true;

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
            });
            se.IsLoggingEnabled = true;

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
    }
}
