/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2024 QuantConnect Corporation.
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
using QuantConnect.Data.Market;
using QuantConnect.Data.Common;
using QuantConnect.Data.Consolidators;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class MarketHourAwareConsolidatorTests : BaseConsolidatorTests
    {
        [Test]
        public void MarketAlwaysOpen()
        {
            var symbol = Symbols.BTCUSD;
            using var consolidator = new MarketHourAwareConsolidator(true, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 5, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 100 });

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 1 });

            Assert.IsNull(latestBar);

            time = time.AddHours(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 2 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 13, 15, 15, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 3 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 14, 0, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(time, latestBar.EndTime);
            Assert.AreEqual(time.AddDays(-1), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(100, latestBar.High);
            Assert.AreEqual(1, latestBar.Low);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Daily(bool strictEndTime)
        {
            var symbol = strictEndTime ? Symbols.SPX : Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidator(strictEndTime, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 5, 0, 0);
            // this bar will be ignored because it's during market closed hours
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 100 });

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 1 });

            Assert.IsNull(latestBar);

            time = time.AddHours(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 2 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 13, 15, 15, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 3 });

            Assert.IsNull(latestBar);

            time = strictEndTime ? time : new DateTime(2015, 04, 14, 0, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 15, 15, 0) : time, latestBar.EndTime);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 8, 30, 0) : time.AddDays(-1), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(3, latestBar.High);
            Assert.AreEqual(1, latestBar.Low);
        }

        [Test]
        public void BarIsSkippedWhenDataResolutionIsNotHourAndMarketIsClose()
        {
            var symbol = Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidator(true, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2020, 05, 01, 09, 30, 0);
            // this bar will be ignored because it's during market closed hours and the bar resolution is not Hour
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, Open = 1 });
            Assert.IsNull(latestBar);
            Assert.AreEqual(0, consolidatedBarsCount);
        }

        [Test]
        public void DailyBarCanBeConsolidatedFromHourData()
        {
            var symbol = Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidator(true, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2020, 05, 01, 09, 0, 0);
            var hourBars = new List<TradeBar>()
            {
                new TradeBar() { Time = time, Period = Time.OneHour, Symbol = symbol, Open = 2 },
                new TradeBar() { Time = time.AddHours(1), Period = Time.OneHour, Symbol = symbol, High = 200 },
                new TradeBar() { Time = time.AddHours(2), Period = Time.OneHour, Symbol = symbol, Low = 0.02m },
                new TradeBar() { Time = time.AddHours(3), Period = Time.OneHour, Symbol = symbol, Close = 20 },
                new TradeBar() { Time = time.AddHours(4), Period = Time.OneHour, Symbol = symbol, Open = 3 },
                new TradeBar() { Time = time.AddHours(5), Period = Time.OneHour, Symbol = symbol, High = 300 },
                new TradeBar() { Time = time.AddHours(6), Period = Time.OneHour, Symbol = symbol, Low = 0.03m, Close = 30 },
            };

            foreach (var bar in hourBars)
            {
                consolidator.Update(bar);
            }

            consolidator.Scan(time.AddHours(7));

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(time.AddHours(7), latestBar.EndTime);
            Assert.AreEqual(time.AddMinutes(30), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(2, latestBar.Open);
            Assert.AreEqual(300, latestBar.High);
            Assert.AreEqual(0.02, latestBar.Low);
            Assert.AreEqual(30, latestBar.Close);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DailyExtendedMarketHours(bool strictEndTime)
        {
            var symbol = strictEndTime ? Symbols.SPX : Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidatorTest(Resolution.Daily, typeof(TradeBar), TickType.Trade, true);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 8, 31, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 10 });

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 15 });

            Assert.IsNull(latestBar);

            if (!strictEndTime)
            {
                time = new DateTime(2015, 04, 13, 18, 15, 0);
                consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 20 });

                Assert.IsNull(latestBar);
            }

            time = new DateTime(2015, 04, 13, 20, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 15, 15, 0) : time, latestBar.EndTime);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 8, 30, 0) : new DateTime(2015, 04, 13, 4, 0, 0), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(strictEndTime ? 15 : 20, latestBar.High);
            Assert.AreEqual(10, latestBar.Low);
        }

        [Test]
        public void MarketHoursRespected()
        {
            using var consolidator = new MarketHourAwareConsolidator(true, Resolution.Hour, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 9, 0, 0);
            // this bar will be ignored because it's during market closed hours
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 100 });

            time = new DateTime(2015, 04, 13, 9, 31, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 1 });

            Assert.IsNull(latestBar);

            time = time.AddMinutes(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 2 });

            Assert.IsNull(latestBar);

            time = time.AddMinutes(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 3 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(time, latestBar.EndTime);
            Assert.AreEqual(new DateTime(2015, 04, 13, 9, 0, 0), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(3, latestBar.High);
            Assert.AreEqual(1, latestBar.Low);
        }

        [Test]
        public void WorksWithDailyResolutionAndPreciseEndTimeFalse()
        {
            using var consolidator = new MarketHourAwareConsolidator(false, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);

            var time = new DateTime(2015, 04, 13, 0, 0, 0);
            consolidator.Update(new TradeBar() { Time = time, Period = Time.OneDay, Symbol = Symbols.SPY, Open = 100, High = 100, Low = 100, Close = 100 });
            Assert.IsNotNull(consolidator.WorkingData);
            var workingData = (TradeBar)consolidator.WorkingData;
            Assert.AreEqual(100, workingData.Open);
            Assert.AreEqual(100, workingData.Low);
            Assert.AreEqual(100, workingData.Close);
            Assert.AreEqual(100, workingData.High);

            // Trigger the consolidation
            consolidator.Scan(time.AddDays(1));
            Assert.IsNotNull(consolidator.Consolidated);

            var consolidatedData = (TradeBar)consolidator.Consolidated;
            Assert.AreEqual(100, consolidatedData.Open);
            Assert.AreEqual(100, consolidatedData.Low);
            Assert.AreEqual(100, consolidatedData.Close);
            Assert.AreEqual(100, consolidatedData.High);
        }

        [Test]
        public void IntradayConsolidatorIsAnchoredToMarketOpen()
        {
            var symbol = Symbols.Future_ESZ18_Dec2018;
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var marketOpen = exchangeHours.GetNextMarketOpen(new DateTime(2024, 11, 30, 12, 0, 0), extendedMarketHours: true);

            using var consolidator = new MarketHourAwareConsolidator(false, TimeSpan.FromMinutes(7), typeof(TradeBar), TickType.Trade, extendedMarketHours: true);
            var bars = new List<TradeBar>();
            consolidator.DataConsolidated += (_, b) => bars.Add((TradeBar)b);

            // feed the first 30 minutes after the open, one bar per minute
            for (var i = 0; i < 30; i++)
            {
                var t = marketOpen.AddMinutes(i);
                consolidator.Update(new TradeBar { Time = t, Period = Time.OneMinute, Symbol = symbol, Open = 1, High = 1, Low = 1, Close = 1, Volume = 1 });
            }

            Assert.GreaterOrEqual(bars.Count, 3);
            Assert.AreEqual(marketOpen, bars[0].Time);
            Assert.AreEqual(marketOpen.AddMinutes(7), bars[0].EndTime);
            Assert.AreEqual(marketOpen.AddMinutes(14), bars[1].EndTime);
            Assert.AreEqual(marketOpen.AddMinutes(21), bars[2].EndTime);
        }

        [Test]
        public void IntradayConsolidatorLastBarEndsAtMarketClose()
        {
            var symbol = Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidator(false, TimeSpan.FromMinutes(7), typeof(TradeBar), TickType.Trade, extendedMarketHours: false);
            var bars = new List<TradeBar>();
            consolidator.DataConsolidated += (_, b) => bars.Add((TradeBar)b);

            void Feed(DateTime from, int minutes)
            {
                for (var i = 0; i < minutes; i++)
                {
                    var t = from.AddMinutes(i);
                    consolidator.Update(new TradeBar { Time = t, Period = Time.OneMinute, Symbol = symbol, Open = 1, High = 1, Low = 1, Close = 1, Volume = 1 });
                }
            }

            // feed the last 10 minutes of day 1 (up to the 16:00 close) and the first 10 of day 2
            Feed(new DateTime(2015, 04, 13, 15, 50, 0), 10);
            Feed(new DateTime(2015, 04, 14, 9, 30, 0), 10);

            // the last bar of day 1 should end at the 16:00 close
            var lastDay1 = bars.FindLast(b => b.Time.Date == new DateTime(2015, 04, 13));
            Assert.IsNotNull(lastDay1);
            Assert.AreEqual(new DateTime(2015, 04, 13, 16, 0, 0), lastDay1.EndTime);

            // the first bar of day 2 should start at the 9:30 open, ending at 9:37
            var day2Open = new DateTime(2015, 04, 14, 9, 30, 0);
            var firstDay2 = bars.Find(b => b.Time == day2Open);
            Assert.IsNotNull(firstDay2);
            Assert.AreEqual(day2Open.AddMinutes(7), firstDay2.EndTime);
        }

        protected override IDataConsolidator CreateConsolidator()
        {
            return new MarketHourAwareConsolidator(true, Resolution.Hour, typeof(TradeBar), TickType.Trade, false);
        }

        protected override IEnumerable<IBaseData> GetTestValues()
        {
            var time = new DateTime(2015, 04, 13, 8, 31, 0);
            return new List<TradeBar>()
            {
                new TradeBar(){ Time = time, Period = Time.OneMinute, Symbol = Symbols.SPY, High = 10 },
                new TradeBar(){ Time = time.AddMinutes(1), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 12 },
                new TradeBar(){ Time = time.AddMinutes(2), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 10 },
                new TradeBar(){ Time = time.AddMinutes(3), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 5 },
                new TradeBar(){ Time = time.AddMinutes(4), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 15 },
                new TradeBar(){ Time = time.AddMinutes(5), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 20 },
                new TradeBar(){ Time = time.AddMinutes(6), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 18 },
                new TradeBar(){ Time = time.AddMinutes(7), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 12 },
                new TradeBar(){ Time = time.AddMinutes(8), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 25 },
                new TradeBar(){ Time = time.AddMinutes(9), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 30 },
                new TradeBar(){ Time = time.AddMinutes(10), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 26 },
            };
        }

        private class MarketHourAwareConsolidatorTest : MarketHourAwareConsolidator
        {
            public MarketHourAwareConsolidatorTest(Resolution resolution, Type dataType, TickType tickType, bool extendedMarketHours)
                : base(true, resolution, dataType, tickType, extendedMarketHours)
            {
            }

            protected override bool UseStrictEndTime(Symbol symbol)
            {
                return true;
            }
        }
    }
}
