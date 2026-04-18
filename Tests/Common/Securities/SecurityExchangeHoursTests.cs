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
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Securities;
using DayOfWeek = System.DayOfWeek;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityExchangeHoursTests
    {
        private static Lazy<HashSet<DateTime>> _mhdbUSHolidays = new Lazy<HashSet<DateTime>>(() => MarketHoursDatabase.FromDataFolder().GetEntry(Market.USA, (string)null, SecurityType.Equity).ExchangeHours.Holidays);

        public void IsAlwaysOpen()
        {
            var cryptoMarketHourDbEntry = MarketHoursDatabase.FromDataFolder().GetEntry(Market.Coinbase, (string)null, SecurityType.Crypto);
            var cryptoExchangeHours = cryptoMarketHourDbEntry.ExchangeHours;

            var futureExchangeHours = CreateUsFutureSecurityExchangeHours();

            Assert.IsTrue(cryptoExchangeHours.IsMarketAlwaysOpen);
            Assert.IsFalse(futureExchangeHours.IsMarketAlwaysOpen);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LastMarketCloseOfDay(bool extendedMarketHours)
        {
            var marketHourDbEntry = MarketHoursDatabase.FromDataFolder()
                .GetEntry(Market.HKFE, QuantConnect.Securities.Futures.Indices.HangSeng, SecurityType.Future);
            var exchangeHours = marketHourDbEntry.ExchangeHours;

            var date = new DateTime(2025, 1, 7);

            var expectedFirstMarketClose = new DateTime(2025, 1, 7, 12, 0, 0);
            var expectedLastMarketClose = new DateTime(2025, 1, 7, 16, 30, 0);
            if (extendedMarketHours)
            {
                expectedFirstMarketClose = new DateTime(2025, 1, 7, 3, 0, 0);
                expectedLastMarketClose = new DateTime(2025, 1, 8);
            }

            var nextMarketClose = exchangeHours.GetNextMarketClose(date, extendedMarketHours);
            Assert.AreEqual(expectedFirstMarketClose, nextMarketClose);

            if (extendedMarketHours)
            {
                Assert.AreEqual(new DateTime(2025, 1, 7, 12, 0, 0), exchangeHours.GetNextMarketClose(nextMarketClose, extendedMarketHours));
            }
            else
            {
                Assert.AreEqual(new DateTime(2025, 1, 7, 16, 30, 0), exchangeHours.GetNextMarketClose(nextMarketClose, extendedMarketHours));
            }

            var lastMarketClose = exchangeHours.GetLastDailyMarketClose(date, extendedMarketHours);
            Assert.AreEqual(expectedLastMarketClose, lastMarketClose);
        }

        [Test]
        public void StartIsOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 21);
            var marketOpen = exchangeHours.MarketHours[DayOfWeek.Sunday].GetMarketOpen(TimeSpan.Zero, false);
            Assert.IsTrue(marketOpen.HasValue);
            var time = (date + marketOpen.Value).AddTicks(-1);
            Assert.IsFalse(exchangeHours.IsOpen(time, false));

            time = time + TimeSpan.FromTicks(1);
            Assert.IsTrue(exchangeHours.IsOpen(time, false));
        }

        [Test]
        public void EndIsClosed()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 19);
            var localMarketHours = exchangeHours.MarketHours[DayOfWeek.Friday];
            var marketClose = localMarketHours.GetMarketClose(TimeSpan.Zero, false);
            Assert.IsTrue(marketClose.HasValue);
            var time = (date + marketClose.Value).AddTicks(-1);
            Assert.IsTrue(exchangeHours.IsOpen(time, false));

            time = time + TimeSpan.FromTicks(1);
            Assert.IsFalse(exchangeHours.IsOpen(time, false));
        }

        [Test]
        public void IntervalOverlappingStartIsOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 21);
            var marketOpen = exchangeHours.MarketHours[DayOfWeek.Sunday].GetMarketOpen(TimeSpan.Zero, false);
            Assert.IsTrue(marketOpen.HasValue);
            var startTime = (date + marketOpen.Value).AddMinutes(-1);

            Assert.IsFalse(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));

            // now the end is 1 tick after open, should return true
            startTime = startTime + TimeSpan.FromTicks(1);
            Assert.IsTrue(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));
        }

        [Test]
        public void IntervalOverlappingEndIsOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 19);
            var marketClose = exchangeHours.MarketHours[DayOfWeek.Friday].GetMarketClose(TimeSpan.Zero, false);
            Assert.IsTrue(marketClose.HasValue);
            var startTime = (date + marketClose.Value).AddMinutes(-1);

            Assert.IsTrue(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));

            // now the start is on the close, returns false
            startTime = startTime.AddMinutes(1);
            Assert.IsFalse(exchangeHours.IsOpen(startTime, startTime.AddMinutes(1), false));
        }

        [Test]
        public void MultiDayInterval()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var date = new DateTime(2015, 6, 19);
            var marketClose = exchangeHours.MarketHours[DayOfWeek.Friday].GetMarketClose(TimeSpan.Zero, false);
            Assert.IsTrue(marketClose.HasValue);
            var startTime = date + marketClose.Value;

            Assert.IsFalse(exchangeHours.IsOpen(startTime, startTime.AddDays(2), false));

            // if we back up one tick it means the bar started at the last moment before market close, this should be included
            Assert.IsTrue(exchangeHours.IsOpen(startTime.AddTicks(-1), startTime.AddDays(2).AddTicks(-1), false));

            // if we advance one tick, it means the bar closed in the first moment after market open
            Assert.IsTrue(exchangeHours.IsOpen(startTime.AddTicks(1), startTime.AddDays(2).AddTicks(1), false));
        }

        [Test]
        public void MarketIsOpenBeforeEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var localDateTime = new DateTime(2016, 11, 25, 12, 0, 0);
            Assert.IsTrue(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsNotOpenAfterEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var localDateTime = new DateTime(2016, 11, 25, 14, 0, 0);
            Assert.IsFalse(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsNotOpenForIntervalAfterEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startLocalDateTime = new DateTime(2016, 11, 25, 13, 0, 0);
            var endLocalDateTime = new DateTime(2016, 11, 25, 13, 30, 0);
            Assert.IsFalse(exchangeHours.IsOpen(startLocalDateTime, endLocalDateTime, false));
        }

        [Test]
        public void GetMarketHoursWithLateOpen()
        {
            var exchangeHours = CreateSecurityExchangeHoursWithMultipleOpeningHours();

            var startTime = new DateTime(2018, 12, 10);
            // From 2:00am, the next close would normally be 3:00am.
            // Because there is a late open at 4am.
            var marketHoursSegments = exchangeHours.GetMarketHours(startTime).Segments;
            var expectedMarketHoursSegments = new List<MarketHoursSegment>() {
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(17, 0, 0), new TimeSpan(17, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(18, 0, 0), new TimeSpan(18, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(19, 0, 0), TimeSpan.FromTicks(Time.OneDay.Ticks - 1))
            };

            for (int i = 0; i <= marketHoursSegments.Count - 1; i++)
            {
                var marketHoursSegment = marketHoursSegments.ElementAt(i);
                var expectedMarketHoursSegment = expectedMarketHoursSegments.ElementAt(i);

                Assert.AreEqual(expectedMarketHoursSegment.Start, marketHoursSegment.Start);
                Assert.AreEqual(expectedMarketHoursSegment.End, marketHoursSegment.End);
                Assert.AreEqual(expectedMarketHoursSegment.State, marketHoursSegment.State);
            }
        }

        [Test]
        public void GetMarketHoursWithEarlyClose()
        {
            var exchangeHours = CreateSecurityExchangeHoursWithMultipleOpeningHours();

            var startTime = new DateTime(2018, 12, 31);
            var marketHoursSegment = exchangeHours.GetMarketHours(startTime).Segments.FirstOrDefault();
            var expectedMarketHoursSegment = new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(3, 0, 0), new TimeSpan(3, 30, 0));
            Assert.AreEqual(expectedMarketHoursSegment.Start, marketHoursSegment.Start);
            Assert.AreEqual(expectedMarketHoursSegment.End, marketHoursSegment.End);
            Assert.AreEqual(expectedMarketHoursSegment.State, marketHoursSegment.State);
        }

        [Test]
        public void GetMarketHoursWithEarlyCloseAndLateOpen()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 11, 25);
            var marketHoursSegment = exchangeHours.GetMarketHours(startTime).Segments.FirstOrDefault();
            var expectedMarketHoursSegment = new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(10, 0, 0), new TimeSpan(13, 0, 0));
            Assert.AreEqual(expectedMarketHoursSegment.Start, marketHoursSegment.Start);
            Assert.AreEqual(expectedMarketHoursSegment.End, marketHoursSegment.End);
            Assert.AreEqual(expectedMarketHoursSegment.State, marketHoursSegment.State);

        }

        [Test]
        public void GetNextMarketOpenIsNonInclusiveOfStartTime()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 30, 9, 30, 0);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(startTime.AddDays(1), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenWorksOnHoliday()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 9, 5, 8, 0, 0);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2016, 9, 6, 9, 30, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenWorksOverWeekends()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 26, 9, 30, 1);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2015, 6, 29, 9, 30, 0), nextMarketOpen);
        }

        // The purpose of define explicitly the exchange market hours for futures,
        // which is pretty similar to exchange market hours for ES, was to consider
        // the case when the market opens at 00 hours on Sunday but the input date
        // was the Saturday. In that case when GetNextMarketOpen() processed the Sunday
        // it should behave as an inclusive  method because for Sunday at 00 it should
        // return Sunday at 00
        [Test]
        public void GetNextMarketOpenForContinuousSchedulesOverWeekends()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours();

            var startTime = new DateTime(2022, 1, 1);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2022, 1, 2), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenForContinuousSchedules()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours();

            var startTime = new DateTime(2022, 1, 3);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2022, 1, 3, 16, 30, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenForContinuousSchedulesIsNotInclusiveOfStartTime()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours();

            var startTime = new DateTime(2022, 1, 2);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2022, 1, 3, 16, 30, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketCloseForContinuousSchedulesOverWeekends()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours();

            var startTime = new DateTime(2022, 1, 1);
            var nextMarketOpen = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2022, 1, 3, 16, 15, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenForEarlyCloses()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours();

            // Thanksgiving day
            var startTime = new DateTime(2013, 11, 28);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2013, 11, 29), nextMarketOpen);
        }

        [TestCaseSource(nameof(GetNextMarketOpenTestCases))]
        public void GetNextMarketOpen(DateTime startTime, DateTime expectedNextMarketOpen, bool extendedMarket)
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHoursWithExtendedHours();

            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, extendedMarket);
            Assert.AreEqual(expectedNextMarketOpen, nextMarketOpen);
        }

        [Test]
        public void GetLastMarketOpenForContinuousSchedules()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours();

            var startTime = new DateTime(2022, 03, 18, 5, 0, 0);
            var nextMarketOpen = exchangeHours.GetPreviousMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2022, 03, 17, 18, 0, 0), nextMarketOpen);
        }

        [Test]
        public void GetLastMarketOpenWithExtendedMarket()
        {
            var marketHourDbEntry = MarketHoursDatabase.FromDataFolder().GetEntry(Market.USA, (string)null, SecurityType.Equity);
            var exchangeHours = marketHourDbEntry.ExchangeHours;

            var startTime = new DateTime(2022, 03, 18, 9, 29, 0);
            var nextMarketOpen = exchangeHours.GetPreviousMarketOpen(startTime, true);
            Assert.AreEqual(new DateTime(2022, 03, 18, 4, 0, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketCloseIsNonInclusiveOfStartTime()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 30, 16, 0, 0);
            var nextMarketOpen = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(startTime.AddDays(1), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketCloseWorksOnHoliday()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 9, 5, 10, 0, 0);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2016, 9, 6, 16, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksOverWeekends()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2015, 6, 26, 16, 0, 1);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2015, 6, 29, 16, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksBeforeEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 11, 25, 10, 0, 0);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2016, 11, 25, 13, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksAfterEarlyClose()
        {
            var exchangeHours = CreateUsEquitySecurityExchangeHours();

            var startTime = new DateTime(2016, 11, 25, 14, 0, 0);
            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2016, 11, 28, 16, 0, 0), nextMarketClose);
        }

        [Test]
        public void GetNextMarketCloseWorksAfterLateOpen()
        {
            var exchangeHours = CreateSecurityExchangeHoursWithMultipleOpeningHours();

            var startTime = new DateTime(2018, 12, 10, 2, 0, 1);
            // From 2:00am, the next close would normally be 3:00am.
            // Because there is a late open at 4am, the next close is the close of the session after that open.
            var nextMarketOpen = exchangeHours.GetNextMarketClose(startTime, false);
            Assert.AreEqual(new DateTime(2018, 12, 10, 17, 30, 0), nextMarketOpen);
        }

        [TestCaseSource(nameof(GetNextMarketCloseTestCases))]
        public void GetNextMarketClose(DateTime startTime, DateTime expectedNextMarketClose, bool extendedMarket)
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHoursWithExtendedHours();

            var nextMarketClose = exchangeHours.GetNextMarketClose(startTime, extendedMarket);
            Assert.AreEqual(expectedNextMarketClose, nextMarketClose);
        }

        [Test]
        public void MarketIsNotOpenBeforeLateOpenIfNotEarlyClose()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var localDateTime = new DateTime(2019, 1, 1, 16, 59, 59);
            Assert.IsFalse(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsOpenBeforeLateOpenIfEarlyClose()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours(true);

            var localDateTime = new DateTime(2013, 11, 29, 10, 0, 0);
            Assert.IsTrue(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsOpenAfterEarlyCloseIfLateOpen()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours(true);

            var localDateTime = new DateTime(2013, 11, 29, 16, 45, 0);
            Assert.IsTrue(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketResumesAfterEarlyCloseIfLateOpen()
        {
            var exchangeHours = CreateUsFutureSecurityExchangeHours(true);
            var localDateTime = new DateTime(2013, 11, 28, 0, 0, 0);
            var nextDay = new DateTime(2013, 11, 29, 0, 0, 0);
            var earlyClose = new TimeSpan(10, 30, 0);
            var lateOpen = new TimeSpan(19, 0, 0);

            var minutes = 0;
            while (localDateTime < nextDay)
            {
                if (localDateTime.TimeOfDay < earlyClose || lateOpen < localDateTime.TimeOfDay)
                {
                    Assert.IsTrue(exchangeHours.IsOpen(localDateTime, false));
                }
                else
                {
                    Assert.IsFalse(exchangeHours.IsOpen(localDateTime, false));
                }

                minutes++;
                localDateTime = localDateTime.AddMinutes(minutes);
            }
        }

        [Test]
        public void MarketIsOpenAfterLateOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var localDateTime = new DateTime(2019, 1, 1, 17, 0, 1);
            Assert.IsTrue(exchangeHours.IsOpen(localDateTime, false));
        }

        [Test]
        public void MarketIsNotOpenForIntervalBeforeLateOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var startLocalDateTime = new DateTime(2019, 1, 1, 16, 30, 0);
            var endLocalDateTime = new DateTime(2019, 1, 1, 17, 0, 0);
            Assert.IsFalse(exchangeHours.IsOpen(startLocalDateTime, endLocalDateTime, false));
        }

        [Test]
        public void GetNextMarketOpenWorksBeforeLateOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var startTime = new DateTime(2019, 1, 1, 16, 59, 59); // Friday
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2019, 1, 1, 17, 0, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenWorksAfterLateOpen()
        {
            var exchangeHours = CreateForexSecurityExchangeHours();

            var startTime = new DateTime(2019, 1, 1, 17, 0, 1);
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2019, 1, 6, 17, 0, 0), nextMarketOpen);
        }

        [Test]
        public void GetNextMarketOpenWorksAfterEarlyClose()
        {
            var exchangeHours = CreateSecurityExchangeHoursWithMultipleOpeningHours();

            var startTime = new DateTime(2018, 12, 31, 17, 0, 1);
            // From 5:00pm, the next open would normally be 6:00pm.
            // Because there is an early close at 5pm, the next open is the open of the session on the following day (+ a late open).
            var nextMarketOpen = exchangeHours.GetNextMarketOpen(startTime, false);
            Assert.AreEqual(new DateTime(2019, 1, 1, 02, 0, 0), nextMarketOpen);
        }

        [Test]
        public void Benchmark()
        {
            var forex = CreateForexSecurityExchangeHours();

            var reference = new DateTime(1991, 06, 20);
            forex.IsOpen(reference, false);
            forex.IsOpen(reference, reference.AddDays(1), false);

            const int length = 1000 * 1000 * 1;

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < length; i++)
            {
                forex.IsOpen(reference.AddMinutes(1), false);
            }
            stopwatch.Stop();

            Log.Trace("forex1: " + stopwatch.Elapsed);
        }

        [Test]
        public void RegularMarketDurationIsFromMostCommonLocalMarketHours()
        {
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, Enumerable.Empty<DateTime>(),
                new Dictionary<DayOfWeek, LocalMarketHours>
                {
                    // fake market hours schedule with random durations, the most common of which is 5 hours and 2 hours, it will pick the larger
                    {DayOfWeek.Sunday, new LocalMarketHours(DayOfWeek.Sunday, TimeSpan.FromHours(4), TimeSpan.FromHours(6))},           //2hr
                    {DayOfWeek.Monday, new LocalMarketHours(DayOfWeek.Monday, TimeSpan.FromHours(13), TimeSpan.FromHours(15))},         //2hr
                    {DayOfWeek.Tuesday, new LocalMarketHours(DayOfWeek.Tuesday, TimeSpan.FromHours(5), TimeSpan.FromHours(10))},        //5hr
                    {DayOfWeek.Wednesday, new LocalMarketHours(DayOfWeek.Wednesday, TimeSpan.FromHours(5), TimeSpan.FromHours(10))},    //5hr
                    {DayOfWeek.Thursday, new LocalMarketHours(DayOfWeek.Thursday, TimeSpan.FromHours(1), TimeSpan.FromHours(23))},      //22hr
                    {DayOfWeek.Friday, new LocalMarketHours(DayOfWeek.Friday, TimeSpan.FromHours(0), TimeSpan.FromHours(23))},          //23hr
                    {DayOfWeek.Saturday, new LocalMarketHours(DayOfWeek.Saturday, TimeSpan.FromHours(3), TimeSpan.FromHours(23))},      //20hr
                }, new Dictionary<DateTime, TimeSpan>(), new Dictionary<DateTime, TimeSpan>());

            Assert.AreEqual(TimeSpan.FromHours(5), exchangeHours.RegularMarketDuration);
        }

        [TestCaseSource(nameof(GetTestCases))]
        public void GetMarketHoursWorksCorrectly(DateTime earlyClose, DateTime lateOpen, LocalMarketHours expected)
        {
            var testDate = new DateTime(2020, 7, 3); // Friday
            var exchangeHours = CreateCustomFutureExchangeHours(earlyClose, lateOpen);
            var actual = exchangeHours.GetMarketHours(testDate);

            // Extracts the time segments for detailed comparison
            var actualSegments = actual.Segments;
            var expectedSegments = expected.Segments;

            // Must have the same number of segments
            Assert.AreEqual(expectedSegments.Count, actualSegments.Count);

            // 1. Market State (PreMarket/Market/PostMarket/Closed)
            // 2. Start Time (Validates late open adjustments)
            // 3. End Time (Validates early close adjustments)
            for (int i = 0; i < expectedSegments.Count; i++)
            {
                Assert.AreEqual(expectedSegments[i].State, actualSegments[i].State, $"Segment {i} state mismatch");
                Assert.AreEqual(expectedSegments[i].Start, actualSegments[i].Start, $"Segment {i} start time mismatch");
                Assert.AreEqual(expectedSegments[i].End, actualSegments[i].End, $"Segment {i} end time mismatch");
            }
        }

        private static TestCaseData[] GetTestCases()
        {
            return new[]
            {
                // 1. Regular hours (no early close, no late open)
                new TestCaseData(
                    new DateTime(), // No early close
                    new DateTime(), // No late open
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)))
                ),

                // 2. Early close only scenarios
                // 2.1 Early close during regular market hours
                new TestCaseData(
                    new DateTime(2020, 7, 3, 12, 0, 0), // Early close at noon
                    new DateTime(),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(12, 0, 0)))
                ),
                // 2.2 Early close before market opens (should remove market segment)
                new TestCaseData(
                    new DateTime(2020, 7, 3, 7, 0, 0), // Early close before open
                    new DateTime(),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(7, 0, 0)))
                ),
                // 2.3 Early close after market closes (should have no effect)
                new TestCaseData(
                    new DateTime(2020, 7, 3, 17, 0, 0), // Early close after regular close
                    new DateTime(),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)))
                ),

                // 3. Late open only scenarios
                // 3.1 Late open during regular market hours (should adjust market open)
                new TestCaseData(
                    new DateTime(),
                    new DateTime(2020, 7, 3, 10, 0, 0), // Late open at 10am
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(10, 0, 0), new TimeSpan(16, 0, 0)))
                ),
                // 3.2 Late open before market opens (should delay premarket start)
                new TestCaseData(
                    new DateTime(),
                    new DateTime(2020, 7, 3, 7, 0, 0), // Late open before market
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(7, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)))
                ),
                // 3.3 Late open after market close (regular hours)
                new TestCaseData(
                    new DateTime(),
                    new DateTime(2020, 7, 3, 17, 0, 0), // Late open at 17
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)))
                ),

                // 4. Both early close and late open scenarios
                // 4.1 Open <= Earlyclose <= Close and EarlyClose < LateOpen (market closes then reopens)
                new TestCaseData(
                    new DateTime(2020, 7, 3, 12, 0, 0), // Close at noon
                    new DateTime(2020, 7, 3, 13, 0, 0), // Reopen at 1pm
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(12, 0, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(13, 0, 0), new TimeSpan(16, 0, 0)))
                ),
                // 4.2 Open <= Earlyclose <= Close and EarlyClose > LateOpen (only one market segment should exist)
                new TestCaseData(
                    new DateTime(2020, 7, 3, 15, 0, 0), // Close at 3pm
                    new DateTime(2020, 7, 3, 14, 0, 0), // Late open at 2pm
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)))
                ),
                // 4.3 Earlyclose <= Open and EarlyClose < LateOpen <= Close
                new TestCaseData(
                    new DateTime(2020, 7, 3, 7, 0, 0),
                    new DateTime(2020, 7, 3, 14, 0, 0),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(7, 0, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(14, 0, 0), new TimeSpan(16, 0, 0)))
                ),
                // 4.4 LateOpen < Earlyclose <= Open
                new TestCaseData(
                    new DateTime(2020, 7, 3, 7, 0, 0),
                    new DateTime(2020, 7, 3, 6, 0, 0),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(6, 0, 0), new TimeSpan(7, 0, 0)))
                ),

                // 5. Edge cases
                // 5.1 Early close exactly at market open (no market segment)
                new TestCaseData(
                    new DateTime(2020, 7, 3, 8, 30, 0),
                    new DateTime(),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)))
                ),
                // 5.2 Late open exactly at market close (market segment has zero duration)
                new TestCaseData(
                    new DateTime(),
                    new DateTime(2020, 7, 3, 16, 0, 0),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(16, 0, 0), new TimeSpan(16, 0, 0)))
                ),
                // 5.3 Early close and late open at the same time (split into two segments with zero-duration overlap)
                new TestCaseData(
                    new DateTime(2020, 7, 3, 13, 0, 0),
                    new DateTime(2020, 7, 3, 13, 0, 0),
                    new LocalMarketHours(DayOfWeek.Friday,
                        new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(13, 0, 0)),
                        new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(13, 0, 0), new TimeSpan(13, 0, 0)))
                ),
            };
        }

        private static SecurityExchangeHours CreateCustomFutureExchangeHours(DateTime earlyClose, DateTime lateOpen)
        {
            var sunday = new LocalMarketHours(
                DayOfWeek.Sunday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(17, 0, 0), new TimeSpan(25, 0, 0)) // 1.00:00:00 = 25 horas
            );

            var monday = new LocalMarketHours(
                DayOfWeek.Monday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(17, 0, 0), new TimeSpan(25, 0, 0))
            );

            var tuesday = new LocalMarketHours(
                DayOfWeek.Tuesday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(17, 0, 0), new TimeSpan(25, 0, 0))
            );

            var wednesday = new LocalMarketHours(
                DayOfWeek.Wednesday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(17, 0, 0), new TimeSpan(25, 0, 0))
            );

            var thursday = new LocalMarketHours(
                DayOfWeek.Thursday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(17, 0, 0), new TimeSpan(25, 0, 0))
            );

            var friday = new LocalMarketHours(
                DayOfWeek.Friday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(8, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(8, 30, 0), new TimeSpan(16, 0, 0))
            );

            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan>
            {
                { earlyClose.Date, earlyClose.TimeOfDay }
            };

            var lateOpens = new Dictionary<DateTime, TimeSpan>
            {
                { lateOpen.Date, lateOpen.TimeOfDay }
            };

            var holidays = new List<DateTime>
            {
                new DateTime(2025, 4, 18)
            };

            var exchangeHours = new SecurityExchangeHours(
                TimeZones.Chicago,
                holidays,
                new[]
                {
                    sunday,
                    monday,
                    tuesday,
                    wednesday,
                    thursday,
                    friday,
                    saturday
                }.ToDictionary(x => x.DayOfWeek),
                earlyCloses,
                lateOpens
            );

            return exchangeHours;
        }

        public static SecurityExchangeHours CreateForexSecurityExchangeHours()
        {
            var sunday = new LocalMarketHours(DayOfWeek.Sunday, new TimeSpan(17, 0, 0), TimeSpan.FromTicks(Time.OneDay.Ticks - 1));
            var monday = LocalMarketHours.OpenAllDay(DayOfWeek.Monday);
            var tuesday = LocalMarketHours.OpenAllDay(DayOfWeek.Tuesday);
            var wednesday = LocalMarketHours.OpenAllDay(DayOfWeek.Wednesday);
            var thursday = LocalMarketHours.OpenAllDay(DayOfWeek.Thursday);
            var friday = new LocalMarketHours(DayOfWeek.Friday, TimeSpan.Zero, new TimeSpan(17, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var holidays = _mhdbUSHolidays.Value;

            holidays.Remove(new DateTime(2019, 1, 1));  // not a forex holiday

            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2018, 12, 31), new TimeSpan(17, 0, 0) } };
            var lateOpens = new Dictionary<DateTime, TimeSpan> { { new DateTime(2019, 1, 1), new TimeSpan(17, 0, 0) } };
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, holidays, new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday//, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return exchangeHours;
        }

        public static SecurityExchangeHours CreateSecurityExchangeHoursWithMultipleOpeningHours()
        {
            var sunday = LocalMarketHours.OpenAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(
                DayOfWeek.Monday,
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(3, 0, 0), new TimeSpan(3, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(17, 0, 0), new TimeSpan(17, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(18, 0, 0), new TimeSpan(18, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(19, 0, 0), TimeSpan.FromTicks(Time.OneDay.Ticks - 1))
            );
            var tuesday = LocalMarketHours.OpenAllDay(DayOfWeek.Tuesday);
            var wednesday = LocalMarketHours.OpenAllDay(DayOfWeek.Wednesday);
            var thursday = LocalMarketHours.OpenAllDay(DayOfWeek.Thursday);
            var friday = LocalMarketHours.OpenAllDay(DayOfWeek.Friday);
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var holidays = new List<DateTime>();
            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2018, 12, 31), new TimeSpan(17, 0, 0) } };
            var lateOpens = new Dictionary<DateTime, TimeSpan> { {new DateTime(2019, 01, 01), new TimeSpan(2, 0, 0)},
                { new DateTime(2018, 12, 10), new TimeSpan(4, 0, 0) } };
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, holidays, new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday//, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return exchangeHours;
        }

        public static SecurityExchangeHours CreateUsEquitySecurityExchangeHours()
        {
            var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2016, 11, 25), new TimeSpan(13, 0, 0) } };
            var lateOpens = new Dictionary<DateTime, TimeSpan>() { { new DateTime(2016, 11, 25), new TimeSpan(10, 0, 0) } };
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, _mhdbUSHolidays.Value, new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return exchangeHours;
        }

        public static SecurityExchangeHours CreateUsFutureSecurityExchangeHours(bool addLateOpens = false)
        {
            var sunday = LocalMarketHours.OpenAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(
                DayOfWeek.Monday,
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(0, 0, 0), new TimeSpan(16, 15, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(18, 0, 0), new TimeSpan(24, 0, 0))
            );
            var tuesday = new LocalMarketHours(
                DayOfWeek.Tuesday,
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(0, 0, 0), new TimeSpan(16, 15, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(18, 0, 0), new TimeSpan(24, 0, 0))
            );
            var wednesday = new LocalMarketHours(
                DayOfWeek.Wednesday,
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(0, 0, 0), new TimeSpan(16, 15, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(18, 0, 0), new TimeSpan(24, 0, 0))
            );
            var thursday = new LocalMarketHours(
                DayOfWeek.Thursday,
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(0, 0, 0), new TimeSpan(16, 15, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(18, 0, 0), new TimeSpan(24, 0, 0))
            );
            var friday = new LocalMarketHours(
                DayOfWeek.Friday,
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(0, 0, 0), new TimeSpan(16, 15, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0))
            );
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2013, 11, 28), new TimeSpan(10, 30, 0) },
                { new DateTime(2013, 11, 29), new TimeSpan(12, 15, 0)} };
            Dictionary<DateTime, TimeSpan> lateOpens = null;
            if (addLateOpens)
            {
                lateOpens = new Dictionary<DateTime, TimeSpan> { { new DateTime(2013, 11, 28), new TimeSpan(19, 00, 0) }, { new DateTime(2013, 11, 29), new TimeSpan(16, 40, 0) } };
            }
            else
            {
                lateOpens = new Dictionary<DateTime, TimeSpan>();
            }

            var holidays = _mhdbUSHolidays.Value.Select(x => x.Date).Where(x => !earlyCloses.ContainsKey(x)).ToList();
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, holidays, new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return exchangeHours;
        }

        public static SecurityExchangeHours CreateUsFutureSecurityExchangeHoursWithExtendedHours()
        {
            var sunday = new LocalMarketHours(
                DayOfWeek.Sunday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(18, 0, 0), new TimeSpan(1, 0, 0, 0))
            );
            var monday = new LocalMarketHours(
                DayOfWeek.Monday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(9, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(18, 0, 0), new TimeSpan(1, 0, 0, 0))
            );
            var tuesday = new LocalMarketHours(
                DayOfWeek.Tuesday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(9, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(18, 0, 0), new TimeSpan(1, 0, 0, 0))
            );
            var wednesday = new LocalMarketHours(
                DayOfWeek.Wednesday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(9, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(18, 0, 0), new TimeSpan(1, 0, 0, 0))
            );
            var thursday = new LocalMarketHours(
                DayOfWeek.Thursday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(9, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0)),
                new MarketHoursSegment(MarketHoursState.PostMarket, new TimeSpan(18, 0, 0), new TimeSpan(1, 0, 0, 0))
            );
            var friday = new LocalMarketHours(
                DayOfWeek.Friday,
                new MarketHoursSegment(MarketHoursState.PreMarket, new TimeSpan(0, 0, 0), new TimeSpan(9, 30, 0)),
                new MarketHoursSegment(MarketHoursState.Market, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0))
            );
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan> { { new DateTime(2013, 11, 28), new TimeSpan(10, 30, 0) },
                { new DateTime(2013, 11, 29), new TimeSpan(12, 15, 0)} };
            var lateOpens = new Dictionary<DateTime, TimeSpan>();
            var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork, _mhdbUSHolidays.Value, new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return exchangeHours;
        }

        private static TestCaseData[] GetNextMarketOpenTestCases()
        {
            return new[]
            {
                new TestCaseData(new DateTime(2022, 1, 1), new DateTime(2022, 1, 3, 9, 30, 0), false),
                new TestCaseData(new DateTime(2022, 1, 3, 8, 0, 0), new DateTime(2022, 1, 3, 9, 30, 0), false),
                new TestCaseData(new DateTime(2022, 1, 2, 18, 0, 0), new DateTime(2022, 1, 3, 18, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 3, 12, 0, 0), new DateTime(2022, 1, 3, 18, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 3, 16, 0, 0), new DateTime(2022, 1, 3, 18, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 1), new DateTime(2022, 1, 2, 18, 0, 0), true)
            };
        }

        private static TestCaseData[] GetNextMarketCloseTestCases()
        {
            return new[]
            {
                new TestCaseData(new DateTime(2022, 1, 1), new DateTime(2022, 1, 3, 16, 0, 0), false),
                new TestCaseData(new DateTime(2022, 1, 2), new DateTime(2022, 1, 3, 16, 0, 0), false),
                new TestCaseData(new DateTime(2022, 1, 3), new DateTime(2022, 1, 3, 16, 0, 0), false),
                new TestCaseData(new DateTime(2022, 1, 3, 10, 0, 0), new DateTime(2022, 1, 3, 16, 0, 0), false),
                new TestCaseData(new DateTime(2022, 1, 3, 18, 0, 0), new DateTime(2022, 1, 4, 16, 0, 0), false),
                new TestCaseData(new DateTime(2022, 1, 1), new DateTime(2022, 1, 3, 16, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 2), new DateTime(2022, 1, 3, 16, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 2, 18, 0, 0), new DateTime(2022, 1, 3, 16, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 3), new DateTime(2022, 1, 3, 16, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 3, 10, 0, 0), new DateTime(2022, 1, 3, 16, 0, 0), true),
                new TestCaseData(new DateTime(2022, 1, 3, 18, 0, 0), new DateTime(2022, 1, 4, 16, 0, 0), true),
            };
        }
    }
}
