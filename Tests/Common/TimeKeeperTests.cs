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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class TimeKeeperTests
    {
        [Test]
        public void ConstructsLocalTimeKeepers()
        {
            var reference = new DateTime(2000, 01, 01);
            var timeKeeper = new TimeKeeper(reference, new[] { TimeZones.NewYork });
            Assert.IsNotNull(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
        }

        [Test]
        public void TimeKeeperReportsUpdatedLocalTimes()
        {
            var reference = new DateTime(2000, 01, 01);
            var timeKeeper = new TimeKeeper(reference, new[] { TimeZones.NewYork });
            var localTime = timeKeeper.GetTimeIn(TimeZones.NewYork);

            timeKeeper.SetUtcDateTime(reference.AddDays(1));

            Assert.AreEqual(localTime.AddDays(1), timeKeeper.GetTimeIn(TimeZones.NewYork));

            timeKeeper.SetUtcDateTime(reference.AddDays(2));

            Assert.AreEqual(localTime.AddDays(2), timeKeeper.GetTimeIn(TimeZones.NewYork));
        }

        [Test]
        public void LocalTimeKeepersGetTimeUpdates()
        {
            var reference = new DateTime(2000, 01, 01);
            var timeKeeper = new TimeKeeper(reference, new[] { TimeZones.NewYork });
            var localTimeKeeper = timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            var localTime = localTimeKeeper.LocalTime;

            timeKeeper.SetUtcDateTime(reference.AddDays(1));

            Assert.AreEqual(localTime.AddDays(1), localTimeKeeper.LocalTime);

            timeKeeper.SetUtcDateTime(reference.AddDays(2));

            Assert.AreEqual(localTime.AddDays(2), localTimeKeeper.LocalTime);
        }

        [Test]
        public void AddingDuplicateTimeZoneDoesntAdd()
        {
            var reference = new DateTime(2000, 01, 01);
            var timeKeeper = new TimeKeeper(reference, new[] { TimeZones.NewYork });
            var localTimeKeeper = timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);

            timeKeeper.AddTimeZone(TimeZones.NewYork);

            Assert.AreEqual(localTimeKeeper, timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
        }
    }
}
