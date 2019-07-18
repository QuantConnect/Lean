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

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class LocalMarketHoursTests
    {
        private static readonly TimeSpan USEquityPreOpen = new TimeSpan(4, 0, 0);
        private static readonly TimeSpan USEquityOpen = new TimeSpan(9, 30, 0);
        private static readonly TimeSpan USEquityClose = new TimeSpan(16, 0, 0);
        private static readonly TimeSpan USEquityPostClose = new TimeSpan(20, 0, 0);

        [Test]
        public void StartIsOpen()
        {
            var marketHours = GetUsEquityWeekDayMarketHours();

            // EDT is +4 or +5 depending on time of year, in june it's +4, so this is 530 edt
            Assert.IsTrue(marketHours.IsOpen(USEquityOpen, false));
        }

        [Test]
        public void EndIsClosed()
        {
            var marketHours = GetUsEquityWeekDayMarketHours();

            // EDT is +4 or +5 depending on time of year, in june it's +4, so this is 530 edt
            Assert.IsFalse(marketHours.IsOpen(USEquityClose, false));
        }

        [Test]
        public void IsOpenRangeAnyOverlap()
        {
            var marketHours = GetUsEquityWeekDayMarketHours();

            // EDT is +4 or +5 depending on time of year, in june it's +4, so this is 530 edt
            var startTime = new TimeSpan(9, 00, 0);
            var endTime = new TimeSpan(10, 00, 0);
            Assert.IsTrue(marketHours.IsOpen(startTime, endTime, false));
        }

        [Test]
        public void MarketDurationDoesNotIncludePreOrPostMarket()
        {
            var marketHours = GetUsEquityWeekDayMarketHours();
            Assert.AreEqual(TimeSpan.FromHours(6.5), marketHours.MarketDuration);
        }

        private static LocalMarketHours GetUsEquityWeekDayMarketHours()
        {
            return new LocalMarketHours(DayOfWeek.Friday, USEquityPreOpen, USEquityOpen, USEquityClose, USEquityPostClose);
        }
    }
}
