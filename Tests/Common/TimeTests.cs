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
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class TimeTests
    {
        [Test]
        public void GetStartTimeForTradeBarsRoundsDown()
        {
            // 2015.09.01 @ noon
            var end = new DateTime(2015, 09, 01, 12, 0, 1);
            var barSize = TimeSpan.FromMinutes(1);
            var hours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 1, false);
            // round down and back up a single bar
            Assert.AreEqual(end.RoundDown(barSize).Subtract(barSize), start);
        }

        [Test]
        public void GetStartTimeForTradeBarsHandlesOverNight()
        {
            // 2015.09.01 @ noon
            var end = new DateTime(2015, 09, 01, 12, 0, 0);
            var barSize = TimeSpan.FromHours(1);
            var hours = SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours();
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 7, false);
            // from noon, back up to 9am (3 hours) then skip night, so from 4pm, back up to noon, 4 more hours
            Assert.AreEqual(end.AddDays(-1), start);
        }

        [Test]
        public void GetStartTimeForTradeBarsHandlesWeekends()
        {
            // 2015.09.01 @ noon
            var end = new DateTime(2015, 09, 01, 12, 0, 0);
            var expectedStart = new DateTime(2015, 08, 21);
            var barSize = TimeSpan.FromDays(1);
            var hours = SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours();
            var start = Time.GetStartTimeForTradeBars(hours, end, barSize, 7, false);
            // from noon, back up to 9am (3 hours) then skip night, so from 4pm, back up to noon, 4 more hours
            Assert.AreEqual(expectedStart, start);
        }
    }
}
