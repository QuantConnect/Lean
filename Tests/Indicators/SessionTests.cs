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
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class SessionTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AddMethodPreservesPreviousValuesInSessionWindow(int initialSize)
        {
            var symbol = Symbols.SPY;
            var session = GetSession(TickType.Trade, initialSize: initialSize);
            session.Size = 2;

            var date = new DateTime(2025, 8, 25);

            var bar1 = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100, 1000, TimeSpan.FromHours(1));
            session.Update(bar1);
            var bar2 = new TradeBar(date.AddHours(13), symbol, 101, 102, 100, 101, 1100, TimeSpan.FromHours(1));
            session.Update(bar2);

            // Verify current session values after multiple updates
            Assert.AreEqual(100, session[0].Open);
            Assert.AreEqual(102, session[0].High);
            Assert.AreEqual(99, session[0].Low);
            Assert.AreEqual(101, session[0].Close);
            Assert.AreEqual(2100, session[0].Volume);

            // Start of a new trading day
            date = date.AddDays(1);
            session.Scan(date);
            bar1 = new TradeBar(date.AddHours(12), symbol, 200, 201, 199, 200, 2000, TimeSpan.FromHours(1));
            session.Update(bar1);
            bar2 = new TradeBar(date.AddHours(13), symbol, 300, 301, 299, 300, 3100, TimeSpan.FromHours(1));
            session.Update(bar2);

            // Verify current session reflects new day data
            Assert.AreEqual(200, session[0].Open);
            Assert.AreEqual(301, session[0].High);
            Assert.AreEqual(199, session[0].Low);
            Assert.AreEqual(300, session[0].Close);
            Assert.AreEqual(5100, session[0].Volume);

            // Verify previous session values are preserved
            Assert.AreEqual(100, session[1].Open);
            Assert.AreEqual(102, session[1].High);
            Assert.AreEqual(99, session[1].Low);
            Assert.AreEqual(101, session[1].Close);
            Assert.AreEqual(2100, session[1].Volume);
        }

        [Test]
        public void EndTimeDoesNotOverflowWhenAccessedBeforeFirstUpdate()
        {
            var symbol = Symbols.SPY;
            var session = GetSession(TickType.Trade, 3);

            // Verify EndTime does not overflow when accessed before the first Update()
            Assert.DoesNotThrow(() =>
            {
                var currentEndTime = session.EndTime;
            });

            session.Update(new TradeBar(new DateTime(2025, 8, 25, 10, 0, 0), symbol, 100, 101, 99, 100, 1000, TimeSpan.FromHours(1)));
            Assert.AreEqual(new DateTime(2025, 8, 26), session.EndTime);
            Assert.AreEqual(100, session.Open);
            Assert.AreEqual(101, session.High);
            Assert.AreEqual(99, session.Low);
            Assert.AreEqual(100, session.Close);
            Assert.AreEqual(1000, session.Volume);
        }

        private static IEnumerable<TestCaseData> ConsolidationTestCases()
        {
            // Hour resolution during regular market hours
            yield return new TestCaseData(new DateTime(2025, 8, 25, 10, 0, 0), Resolution.Hour);

            // Daily resolution and bar emitted at midnight
            yield return new TestCaseData(new DateTime(2025, 8, 25, 0, 0, 0), Resolution.Daily);
        }

        [TestCaseSource(nameof(ConsolidationTestCases))]
        public void ConsolidatesDaily(DateTime baseDate, Resolution resolution)
        {
            var symbol = Symbols.SPY;
            var session = GetSession(TickType.Trade, 4);
            var days = new[]
            {
                new { Expected = new TradeBar(baseDate.Date,            symbol, 100, 101, 99, 100, 6000, Time.OneDay) },
                new { Expected = new TradeBar(baseDate.Date.AddDays(1), symbol, 100, 101, 99, 100, 6000, Time.OneDay) },
                new { Expected = new TradeBar(baseDate.Date.AddDays(2), symbol, 100, 101, 99, 100, 6000, Time.OneDay) },
            };

            Assert.AreEqual(1, session.Samples);

            for (int i = 0; i < days.Length; i++)
            {
                var startDate = baseDate.AddDays(i);
                var endDate = startDate.Date.AddDays(1);

                if (resolution == Resolution.Hour)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        session.Update(new TradeBar(startDate.AddHours(j), symbol, 100, 101, 99, 100, 1000, Time.OneHour));
                    }
                }
                else
                {
                    session.Update(new TradeBar(startDate, symbol, 100, 101, 99, 100, 6000, Time.OneDay));
                }

                session.Scan(endDate);
                Assert.AreEqual(i + 2, session.Samples);
                Assert.IsTrue(BarsAreEqual(days[i].Expected, session[1]));
            }
        }

        [TestCaseSource(nameof(NextSessionTradingDayCases))]
        public void CreatesNewSessionBarWithCorrectNextTradingDay(DateTime startDate, DateTime expectedDate)
        {
            var symbol = Symbols.SPY;
            var session = GetSession(TickType.Trade, 3);
            var endDate = startDate.AddHours(14);

            for (int i = 0; i < 6; i++)
            {
                session.Update(new TradeBar(startDate.AddHours(i), symbol, 100, 101, 99, 100, 1000, TimeSpan.FromHours(1)));
            }

            session.Scan(endDate);

            var sessionBar = session[0];
            Assert.AreNotEqual(DateTime.MaxValue, sessionBar.Time);
            Assert.AreEqual(expectedDate, sessionBar.Time);
            Assert.AreEqual(expectedDate.AddDays(1), sessionBar.EndTime);
            Assert.AreEqual(0, sessionBar.Open);
            Assert.AreEqual(0, sessionBar.High);
            Assert.AreEqual(0, sessionBar.Low);
            Assert.AreEqual(0, sessionBar.Close);
            Assert.AreEqual(0, sessionBar.Volume);
        }

        private static IEnumerable<TestCaseData> NextSessionTradingDayCases()
        {
            // Regular weekday: next trading day is simply the next calendar day
            yield return new TestCaseData(new DateTime(2025, 8, 25, 10, 0, 0), new DateTime(2025, 8, 26));

            // Friday before Labor Day weekend -> next trading day is Tuesday (Sep 2, 2025)
            yield return new TestCaseData(new DateTime(2025, 8, 29, 10, 0, 0), new DateTime(2025, 9, 2));
        }

        private static Session GetSession(TickType tickType, int initialSize)
        {
            var symbol = Symbols.SPY;
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var exchangeHours = marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            return new Session(tickType, exchangeHours, symbol, initialSize);
        }

        private static bool BarsAreEqual(TradeBar bar1, TradeBar bar2)
        {
            return bar1.Time == bar2.Time &&
                   bar1.EndTime == bar2.EndTime &&
                   bar1.Open == bar2.Open &&
                   bar1.High == bar2.High &&
                   bar1.Low == bar2.Low &&
                   bar1.Close == bar2.Close &&
                   bar1.Volume == bar2.Volume;
        }
    }
}
