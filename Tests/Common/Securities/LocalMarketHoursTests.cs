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
using System.Globalization;
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

        [TestCase("1.00:00:00", null, false)]
        [TestCase(null, "00:00:00", false)]
        [TestCase("1.00:00:00", "00:00:00", true)]
        [TestCase("0.10:00:00", "10:00:00", false)]
        [TestCase("0.18:00:00", "00:00:00", false)]
        [TestCase("1.00:00:00", "00:01:00", false)]
        [TestCase("1.00:00:00", "10:00:00", false)]
        public void IsContinuousMarketOpenTests(
            string previousSegmentEndStr,
            string nextSegmentStartStr,
            bool expected
        )
        {
            TimeSpan? previousSegmentEnd = null;
            TimeSpan? nextSegmentStart = null;
            if (previousSegmentEndStr != null)
            {
                previousSegmentEnd = TimeSpan.ParseExact(
                    previousSegmentEndStr,
                    "d\\.hh\\:mm\\:ss",
                    CultureInfo.InvariantCulture
                );
            }
            if (nextSegmentStartStr != null)
            {
                nextSegmentStart = TimeSpan.ParseExact(
                    nextSegmentStartStr,
                    "hh\\:mm\\:ss",
                    CultureInfo.InvariantCulture
                );
            }

            Assert.AreEqual(
                expected,
                LocalMarketHours.IsContinuousMarketOpen(previousSegmentEnd, nextSegmentStart)
            );
        }

        [TestCaseSource(nameof(GetMarketOpenTestCases))]
        public void GetsCorrectMarketOpen(
            TimeSpan referenceTime,
            bool extendedMarket,
            TimeSpan prevDayLastSegmentEnd,
            TimeSpan? expectedMarketOpen
        )
        {
            var marketHours = GetFutureWeekDayMarketHours();

            Assert.AreEqual(
                expectedMarketOpen,
                marketHours.GetMarketOpen(referenceTime, extendedMarket, prevDayLastSegmentEnd)
            );
        }

        [TestCaseSource(nameof(GetMarketCloseTestCases))]
        public void GetsCorrectMarketClose(
            TimeSpan referenceTime,
            bool extendedMarket,
            TimeSpan nextDayFirstSegmentStart,
            TimeSpan? expectedMarketClose
        )
        {
            var marketHours = GetFutureWeekDayMarketHours();

            Assert.AreEqual(
                expectedMarketClose,
                marketHours.GetMarketClose(referenceTime, extendedMarket, nextDayFirstSegmentStart)
            );
        }

        private static LocalMarketHours GetUsEquityWeekDayMarketHours()
        {
            return new LocalMarketHours(
                DayOfWeek.Friday,
                USEquityPreOpen,
                USEquityOpen,
                USEquityClose,
                USEquityPostClose
            );
        }

        private static LocalMarketHours GetFutureWeekDayMarketHours()
        {
            return new LocalMarketHours(
                DayOfWeek.Monday,
                new MarketHoursSegment[]
                {
                    new MarketHoursSegment(
                        MarketHoursState.PreMarket,
                        new TimeSpan(0, 0, 0),
                        new TimeSpan(8, 0, 0)
                    ),
                    new MarketHoursSegment(
                        MarketHoursState.Market,
                        new TimeSpan(8, 0, 0),
                        new TimeSpan(16, 0, 0)
                    ),
                    new MarketHoursSegment(
                        MarketHoursState.PostMarket,
                        new TimeSpan(17, 0, 0),
                        new TimeSpan(1, 0, 0, 0)
                    )
                }
            );
        }

        private static TestCaseData[] GetMarketOpenTestCases()
        {
            return new[]
            {
                // Prev day last segment continues to current day
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    false,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(8, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(8, 0, 0),
                    false,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(8, 0, 0)
                ),
                new TestCaseData(new TimeSpan(16, 0, 0), false, new TimeSpan(1, 0, 0, 0), null),
                new TestCaseData(new TimeSpan(17, 0, 0), false, new TimeSpan(1, 0, 0, 0), null),
                new TestCaseData(new TimeSpan(18, 0, 0), false, new TimeSpan(1, 0, 0, 0), null),
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    true,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(8, 0, 0),
                    true,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(16, 0, 0),
                    true,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(17, 0, 0),
                    true,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(18, 0, 0),
                    true,
                    new TimeSpan(1, 0, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                // // Prev day last segment ends before end of prev day
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    false,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(8, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(8, 0, 0),
                    false,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(8, 0, 0)
                ),
                new TestCaseData(new TimeSpan(16, 0, 0), false, new TimeSpan(17, 0, 0), null),
                new TestCaseData(new TimeSpan(17, 0, 0), false, new TimeSpan(17, 0, 0), null),
                new TestCaseData(new TimeSpan(18, 0, 0), false, new TimeSpan(17, 0, 0), null),
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    true,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(0, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(8, 0, 0),
                    true,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(16, 0, 0),
                    true,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(17, 0, 0),
                    true,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(18, 0, 0),
                    true,
                    new TimeSpan(17, 0, 0),
                    new TimeSpan(17, 0, 0)
                ),
                // // No prev day last segment
                new TestCaseData(new TimeSpan(0, 0, 0), false, null, new TimeSpan(8, 0, 0)),
                new TestCaseData(new TimeSpan(8, 0, 0), false, null, new TimeSpan(8, 0, 0)),
                new TestCaseData(new TimeSpan(16, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(17, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(18, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(0, 0, 0), true, null, new TimeSpan(0, 0, 0)),
                new TestCaseData(new TimeSpan(8, 0, 0), true, null, new TimeSpan(17, 0, 0)),
                new TestCaseData(new TimeSpan(16, 0, 0), true, null, new TimeSpan(17, 0, 0)),
                new TestCaseData(new TimeSpan(17, 0, 0), true, null, new TimeSpan(17, 0, 0)),
                new TestCaseData(new TimeSpan(18, 0, 0), true, null, new TimeSpan(17, 0, 0)),
            };
        }

        private static TestCaseData[] GetMarketCloseTestCases()
        {
            return new[]
            {
                // Next day's first segment continues from current day's last segment (see GetFutureWeekDayMarketHours, the last segment ends
                // with 1.00:00:00, so starting next day at 00:00:00, it means the segments are continouous)
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    false,
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(12, 0, 0),
                    false,
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(new TimeSpan(16, 0, 0), false, new TimeSpan(0, 0, 0), null),
                new TestCaseData(new TimeSpan(17, 0, 0), false, new TimeSpan(0, 0, 0), null),
                new TestCaseData(new TimeSpan(18, 0, 0), false, new TimeSpan(0, 0, 0), null),
                new TestCaseData(new TimeSpan(20, 0, 0), false, new TimeSpan(0, 0, 0), null),
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    true,
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(12, 0, 0),
                    true,
                    new TimeSpan(0, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(new TimeSpan(16, 0, 0), true, new TimeSpan(0, 0, 0), null),
                new TestCaseData(new TimeSpan(17, 0, 0), true, new TimeSpan(0, 0, 0), null),
                new TestCaseData(new TimeSpan(18, 0, 0), true, new TimeSpan(0, 0, 0), null),
                new TestCaseData(new TimeSpan(20, 0, 0), true, new TimeSpan(0, 0, 0), null),
                // Next day's first segment starts after midnight
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    false,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(12, 0, 0),
                    false,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(new TimeSpan(16, 0, 0), false, new TimeSpan(18, 0, 0), null),
                new TestCaseData(new TimeSpan(17, 0, 0), false, new TimeSpan(18, 0, 0), null),
                new TestCaseData(new TimeSpan(18, 0, 0), false, new TimeSpan(18, 0, 0), null),
                new TestCaseData(new TimeSpan(20, 0, 0), false, new TimeSpan(18, 0, 0), null),
                new TestCaseData(
                    new TimeSpan(0, 0, 0),
                    true,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(12, 0, 0),
                    true,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(16, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(16, 0, 0),
                    true,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(1, 0, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(17, 0, 0),
                    true,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(1, 0, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(18, 0, 0),
                    true,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(1, 0, 0, 0)
                ),
                new TestCaseData(
                    new TimeSpan(20, 0, 0),
                    true,
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(1, 0, 0, 0)
                ),
                // No next day's first segment
                new TestCaseData(new TimeSpan(0, 0, 0), false, null, new TimeSpan(16, 0, 0)),
                new TestCaseData(new TimeSpan(12, 0, 0), false, null, new TimeSpan(16, 0, 0)),
                new TestCaseData(new TimeSpan(16, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(17, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(18, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(20, 0, 0), false, null, null),
                new TestCaseData(new TimeSpan(0, 0, 0), true, null, new TimeSpan(16, 0, 0)),
                new TestCaseData(new TimeSpan(12, 0, 0), true, null, new TimeSpan(16, 0, 0)),
                new TestCaseData(new TimeSpan(16, 0, 0), true, null, null),
                new TestCaseData(new TimeSpan(17, 0, 0), true, null, null),
                new TestCaseData(new TimeSpan(18, 0, 0), true, null, null),
                new TestCaseData(new TimeSpan(20, 0, 0), true, null, null),
            };
        }
    }
}
