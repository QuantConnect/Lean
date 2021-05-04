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
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class ScheduledUniverseTests
    {
        [Test]
        public void TimeTriggeredIsCorrect()
        {
            // Set up for the test; start time is 1/5/2000 a wednesday at 3PM
            var timezone = TimeZones.NewYork;
            var start = new DateTime(2000, 1, 5, 15, 0, 0);
            var end = new DateTime(2000, 2, 1);
            var timekeeper = new TimeKeeper(start, timezone);

            var spy = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbol.Create("SPY", SecurityType.Equity, QuantConnect.Market.USA),
                    Resolution.Minute,
                    timezone,
                    timezone,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var securities = new SecurityManager(timekeeper)
            {
                spy
            };

            var dateRules = new DateRules(securities, timezone);
            var timeRules = new TimeRules(securities, timezone);

            // Schedule our universe for 12PM each day
            var universe = new ScheduledUniverse( 
                dateRules.EveryDay(spy.Symbol), timeRules.At(12, 0),
                (time =>
                {
                    Log.Trace($"{time} : {timekeeper.GetTimeIn(timezone)}");
                    return new List<Symbol>();
                })
            );

            // Get our trigger times, these will be in UTC
            var triggerTimesUtc = universe.GetTriggerTimes(start.ConvertToUtc(timezone), end.ConvertToUtc(timezone), MarketHoursDatabase.AlwaysOpen);

            foreach (var time in triggerTimesUtc)
            {
                // Convert our UTC time back to our timezone
                var localTime = time.ConvertFromUtc(timezone);

                // Assert we aren't receiving dates prior to our start
                Assert.IsTrue(localTime > start);

                // Assert that this time is indeed 12PM local time
                Assert.IsTrue(localTime.Hour == 12 && localTime.Minute == 0);
            }
        }
    }
}
