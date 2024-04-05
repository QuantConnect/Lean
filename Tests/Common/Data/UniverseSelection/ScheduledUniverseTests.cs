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
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class ScheduledUniverseTests
    {
        private DateTimeZone _timezone;
        private TimeKeeper _timekeeper;
        private SecurityManager _securities;
        private DateRules _dateRules;
        private TimeRules _timeRules;

        [SetUp]
        public void Setup()
        {
            _timezone = TimeZones.NewYork;
            _timekeeper = new TimeKeeper(new DateTime(2000, 1, 1), _timezone);
            _securities = new SecurityManager(_timekeeper);

            var mhdb = MarketHoursDatabase.FromDataFolder();
            _dateRules = new DateRules(_securities, _timezone, mhdb);
            _timeRules = new TimeRules(_securities, _timezone, mhdb);
        }

        [Test]
        public void TimeTriggeredDoesNotReturnPastTimes()
        {
            // Schedule our universe for 12PM each day
            var universe = new ScheduledUniverse( 
                _dateRules.EveryDay(), _timeRules.At(12, 0),
                (time =>
                {
                    return new List<Symbol>();
                })
            );

            // For this test; start time will be 1/5/2000 wednesday at 3PM
            // which is after 12PM, this case will ensure we don't have a 1/5 12pm event
            var start = new DateTime(2000, 1, 5, 15, 0, 0);
            var end = new DateTime(2000, 1, 10);

            // Get our trigger times, these will be in UTC
            var triggerTimesUtc = universe.GetTriggerTimes(start.ConvertToUtc(_timezone), end.ConvertToUtc(_timezone), MarketHoursDatabase.AlwaysOpen);

            // Setup expectDate variables to assert behavior
            // We expect the first day to be 1/6 12PM
            var expectedDate = new DateTime(2000, 1, 6, 12, 0, 0);

            foreach (var time in triggerTimesUtc)
            {
                // Convert our UTC time back to our timezone
                var localTime = time.ConvertFromUtc(_timezone);

                // Assert we aren't receiving dates prior to our start
                Assert.IsTrue(localTime > start);

                // Verify the date
                Assert.AreEqual(expectedDate, localTime);
                expectedDate = expectedDate.AddDays(1);
            }
        }

        [Test]
        public void TriggerTimesNone()
        {
            // Test to see what happens when we expect no trigger times.
            // To do this we will create an everyday at 12pm rule, but ask for triggers times
            // on a single day from 3pm-4pm, meaning we should get none.
            var timezone = TimeZones.NewYork;
            var start = new DateTime(2000, 1, 5, 15, 0, 0);
            var end = new DateTime(2000, 1, 5, 16,0,0);

            var dateRule = _dateRules.EveryDay();
            var timeRule = _timeRules.At(12, 0);

            var universe = new ScheduledUniverse(dateRule, timeRule, time =>
            {
                return new List<Symbol>();
            });

            var triggerTimesUtc = universe.GetTriggerTimes(start.ConvertToUtc(timezone), end.ConvertToUtc(timezone),
                MarketHoursDatabase.AlwaysOpen);

            // Assert that its empty
            Assert.IsTrue(!triggerTimesUtc.Any());
        }
    }
}
