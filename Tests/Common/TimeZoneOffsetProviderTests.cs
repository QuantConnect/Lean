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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class TimeZoneOffsetProviderTests
    {
        [Test]
        public void ReturnsCurrentOffset()
        {
            var utcDate = new DateTime(2015, 07, 07);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, utcDate, utcDate.AddDays(1));
            var currentOffset = offsetProvider.GetOffsetTicks(utcDate);
            Assert.AreEqual(-TimeSpan.FromHours(4).TotalHours, TimeSpan.FromTicks(currentOffset).TotalHours);
        }

        [Test]
        public void ReturnsCorrectOffsetBeforeDST()
        {
            // one tick before DST goes into affect
            var utcDate = new DateTime(2015, 03, 08, 2, 0, 0).AddHours(5).AddTicks(-1);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, utcDate, utcDate.AddDays(1));
            var currentOffset = offsetProvider.GetOffsetTicks(utcDate);
            Assert.AreEqual(-TimeSpan.FromHours(5).TotalHours, TimeSpan.FromTicks(currentOffset).TotalHours);
        }

        [Test]
        public void ReturnsCorrectOffsetAfterDST()
        {
            // the exact instant DST goes into affect
            var utcDate = new DateTime(2015, 03, 08, 2, 0, 0).AddHours(5);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, utcDate, utcDate.AddDays(1));
            var currentOffset = offsetProvider.GetOffsetTicks(utcDate);
            Assert.AreEqual(-TimeSpan.FromHours(4).TotalHours, TimeSpan.FromTicks(currentOffset).TotalHours);
        }

        [Test]
        public void ConvertFromUtcAfterDST()
        {
            // the exact instant DST goes into affect
            var tzDate = new DateTime(2015, 03, 08, 2, 0, 0);
            var utcDate = tzDate.AddHours(5);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, utcDate, utcDate.AddDays(1));
            var result = offsetProvider.ConvertFromUtc(utcDate);

            // We add an hour due to the effect of DST
            Assert.AreEqual(tzDate + TimeSpan.FromHours(1), result);
        }

        [Test]
        public void ConvertToUtcAfterDST()
        {
            // the exact instant DST goes into affect
            var tzDate = new DateTime(2015, 03, 08, 2, 0, 0);
            var utcDate = tzDate.AddHours(5);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, utcDate, utcDate.AddDays(1));
            var result = offsetProvider.ConvertToUtc(tzDate);

            // We substract an hour due to the effect of DST
            Assert.AreEqual(utcDate - TimeSpan.FromHours(1), result);
        }
    }
}
