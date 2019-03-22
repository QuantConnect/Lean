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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class CalendarConsolidatorsTests
    {
        [TestCase(CalendarType.Monthly)]
        [TestCase(CalendarType.Weekly)]
        public void AggregatesTradeBarToCalendarTradeBarProperly(CalendarType calendarType)
        {
            var multiplier = calendarType == CalendarType.Weekly ? 2.5 : 5;

            TradeBar newBar = null;
            var consolidator = new TradeBarCalendarConsolidator(calendarType);
            consolidator.DataConsolidated += (s, e) => newBar = e;

            // Monday
            var reference = new DateTime(2019, 3, 17);
            var bars = new List<TradeBar>
            {
                new TradeBar(reference.AddDays(1), Symbols.SPY, 9, 11, 8, 10, 100, Time.OneDay),
                new TradeBar(reference.AddDays(1 * multiplier), Symbols.SPY, 10, 12, 8, 11, 100, Time.OneDay),
                new TradeBar(reference.AddDays(2 * multiplier), Symbols.SPY, 11, 13, 9, 10, 100, Time.OneDay),
                new TradeBar(reference.AddDays(3 * multiplier), Symbols.SPY, 11, 13, 9, 11, 100, Time.OneDay)
            };

            foreach (var bar in bars)
            {
                Assert.IsNull(newBar);
                consolidator.Update(bar);
            }
            Assert.IsNotNull(newBar);

            if (calendarType == CalendarType.Monthly)
            {
                var openTime = new DateTime(reference.Year, reference.Month, 1);
                var closeTime = new DateTime(reference.Year, reference.Month + 1, 1);
                Assert.AreEqual(openTime, newBar.Time);
                Assert.AreEqual(closeTime, newBar.EndTime);
            }
            else
            {
                Assert.AreEqual(reference, newBar.Time);
                Assert.AreEqual(reference.AddDays(7), newBar.EndTime);
            }

            Assert.AreEqual(Symbols.SPY, newBar.Symbol);
            Assert.AreEqual(bars.First().Open, newBar.Open);
            Assert.AreEqual(bars.Max(x => x.High), newBar.High);
            Assert.AreEqual(bars.Min(x => x.Low), newBar.Low);
            Assert.AreEqual(bars.Last().Close, newBar.Close);
            Assert.AreEqual(bars.Sum(x => x.Volume), newBar.Volume);
        }

        [TestCase(CalendarType.Monthly)]
        [TestCase(CalendarType.Weekly)]
        public void AggregatesQuoteBarToCalendarQuoteBarProperly(CalendarType calendarType)
        {
            var multiplier = calendarType == CalendarType.Weekly ? 2.5 : 5;

            QuoteBar newBar = null;
            var consolidator = new QuoteBarCalendarConsolidator(calendarType);
            consolidator.DataConsolidated += (s, e) => newBar = e;

            // Monday
            var reference = new DateTime(2019, 3, 17);
            var bars = new List<QuoteBar>
            {
                new QuoteBar(reference.AddDays(1), Symbols.EURUSD, new Bar(9, 11, 8, 10), 10, new Bar(9, 11, 8, 10), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(1 * multiplier), Symbols.EURUSD, new Bar(10, 12, 8, 11), 10, new Bar(10, 12, 8, 11), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(2 * multiplier), Symbols.EURUSD, new Bar(11, 13, 9, 10), 10, new Bar(11, 13, 9, 10), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(3 * multiplier), Symbols.EURUSD, new Bar(11, 13, 9, 11), 10, new Bar(11, 13, 9, 11), 10, Time.OneDay)
            };

            foreach (var bar in bars)
            {
                Assert.IsNull(newBar);
                consolidator.Update(bar);
            }
            Assert.IsNotNull(newBar);

            if (calendarType == CalendarType.Monthly)
            {
                var openTime = new DateTime(reference.Year, reference.Month, 1);
                var closeTime = new DateTime(reference.Year, reference.Month + 1, 1);
                Assert.AreEqual(openTime, newBar.Time);
                Assert.AreEqual(closeTime, newBar.EndTime);
            }
            else
            {
                Assert.AreEqual(reference, newBar.Time);
                Assert.AreEqual(reference.AddDays(7), newBar.EndTime);
            }

            Assert.AreEqual(Symbols.EURUSD, newBar.Symbol);
            Assert.AreEqual(bars.First().Open, newBar.Open);
            Assert.AreEqual(bars.First().Bid.Open, newBar.Bid.Open);
            Assert.AreEqual(bars.First().Ask.Open, newBar.Ask.Open);
            Assert.AreEqual(bars.Max(x => x.High), newBar.High);
            Assert.AreEqual(bars.Max(x => x.Bid.High), newBar.Bid.High);
            Assert.AreEqual(bars.Max(x => x.Ask.High), newBar.Ask.High);
            Assert.AreEqual(bars.Min(x => x.Low), newBar.Low);
            Assert.AreEqual(bars.Min(x => x.Bid.Low), newBar.Bid.Low);
            Assert.AreEqual(bars.Min(x => x.Ask.Low), newBar.Ask.Low);
            Assert.AreEqual(bars.Last().Close, newBar.Close);
            Assert.AreEqual(bars.Last().Bid.Close, newBar.Bid.Close);
            Assert.AreEqual(bars.Last().Ask.Close, newBar.Ask.Close);
        }

        [TestCase(CalendarType.Monthly)]
        [TestCase(CalendarType.Weekly)]
        public void AggregatesTickToCalendarTradeBarProperly(CalendarType calendarType)
        {
            var multiplier = calendarType == CalendarType.Weekly ? 2.5 : 5;

            TradeBar newBar = null;
            var consolidator = new TickCalendarConsolidator(calendarType);
            consolidator.DataConsolidated += (s, e) => newBar = e;

            // Monday
            var reference = new DateTime(2019, 3, 17);
            var ticks = new List<Tick>
            {
                new Tick(reference.AddDays(1), Symbols.SPY, 9, 11, 8){ TickType = TickType.Trade, Quantity = 10 },
                new Tick(reference.AddDays(1 * multiplier), Symbols.SPY, 10, 12, 8){ TickType = TickType.Trade,  Quantity = 10 },
                new Tick(reference.AddDays(2 * multiplier), Symbols.SPY, 11, 13, 9){ TickType = TickType.Trade,  Quantity = 10 },
                new Tick(reference.AddDays(3 * multiplier), Symbols.SPY, 11, 13, 9){ TickType = TickType.Trade,  Quantity = 10 }
            };

            foreach (var bar in ticks)
            {
                Assert.IsNull(newBar);
                consolidator.Update(bar);
            }
            Assert.IsNotNull(newBar);

            if (calendarType == CalendarType.Monthly)
            {
                var openTime = new DateTime(reference.Year, reference.Month, 1);
                var closeTime = new DateTime(reference.Year, reference.Month + 1, 1);
                Assert.AreEqual(openTime, newBar.Time);
                Assert.AreEqual(closeTime, newBar.EndTime);
            }
            else
            {
                Assert.AreEqual(reference, newBar.Time);
                Assert.AreEqual(reference.AddDays(7), newBar.EndTime);
            }

            Assert.AreEqual(Symbols.SPY, newBar.Symbol);
            Assert.AreEqual(ticks.First().Value, newBar.Open);
            Assert.AreEqual(ticks.Max(x => x.Value), newBar.High);
            Assert.AreEqual(ticks.Min(x => x.Value), newBar.Low);
            Assert.AreEqual(ticks.Last().Value, newBar.Close);
            Assert.AreEqual(ticks.Sum(x => x.Quantity), newBar.Volume);
        }

        [TestCase(CalendarType.Monthly)]
        [TestCase(CalendarType.Weekly)]
        public void AggregatesTickToCalendarQuoteBarProperly(CalendarType calendarType)
        {
            var multiplier = calendarType == CalendarType.Weekly ? 2.5 : 5;

            QuoteBar newBar = null;
            var consolidator = new TickQuoteBarCalendarConsolidator(calendarType);
            consolidator.DataConsolidated += (s, e) => newBar = e;

            // Monday
            var reference = new DateTime(2019, 3, 17);
            var ticks = new List<Tick>
            {
                new Tick(reference.AddDays(1), Symbols.EURUSD, 9, 11, 8){ Quantity = 10 },
                new Tick(reference.AddDays(1 * multiplier), Symbols.EURUSD, 10, 12, 8){ Quantity = 10 },
                new Tick(reference.AddDays(2 * multiplier), Symbols.EURUSD, 11, 13, 9){ Quantity = 10 },
                new Tick(reference.AddDays(3 * multiplier), Symbols.EURUSD, 11, 13, 9){ Quantity = 10 }
            };

            foreach (var tick in ticks)
            {
                Assert.IsNull(newBar);
                consolidator.Update(tick);
            }
            Assert.IsNotNull(newBar);

            if (calendarType == CalendarType.Monthly)
            {
                var openTime = new DateTime(reference.Year, reference.Month, 1);
                var closeTime = new DateTime(reference.Year, reference.Month + 1, 1);
                Assert.AreEqual(openTime, newBar.Time);
                Assert.AreEqual(closeTime, newBar.EndTime);
            }
            else
            {
                Assert.AreEqual(reference, newBar.Time);
                Assert.AreEqual(reference.AddDays(7), newBar.EndTime);
            }

            Assert.AreEqual(Symbols.EURUSD, newBar.Symbol);
            Assert.AreEqual(ticks.First().BidPrice, newBar.Bid.Open);
            Assert.AreEqual(ticks.First().AskPrice, newBar.Ask.Open);
            Assert.AreEqual(ticks.Max(x => x.BidPrice), newBar.Bid.High);
            Assert.AreEqual(ticks.Max(x => x.AskPrice), newBar.Ask.High);
            Assert.AreEqual(ticks.Min(x => x.BidPrice), newBar.Bid.Low);
            Assert.AreEqual(ticks.Min(x => x.AskPrice), newBar.Ask.Low);
            Assert.AreEqual(ticks.Last().BidPrice, newBar.Bid.Close);
            Assert.AreEqual(ticks.Last().AskPrice, newBar.Ask.Close);
        }

        [TestCase(CalendarType.Monthly)]
        [TestCase(CalendarType.Weekly)]
        public void AggregatesBaseDataToCalendarTradeBarProperly(CalendarType calendarType)
        {
            var multiplier = calendarType == CalendarType.Weekly ? 2.5 : 5;

            TradeBar newBar = null;
            var consolidator = new BaseDataCalendarConsolidator(calendarType);
            consolidator.DataConsolidated += (s, e) => newBar = e;

            // Monday
            var reference = new DateTime(2019, 3, 17);
            var ticks = new List<Tick>
            {
                new Tick(reference.AddDays(1), Symbols.SPY, 9, 11, 8){ Quantity = 10 },
                new Tick(reference.AddDays(1 * multiplier), Symbols.SPY, 10, 12, 8){ Quantity = 10 },
                new Tick(reference.AddDays(2 * multiplier), Symbols.SPY, 11, 13, 9){ Quantity = 10 },
                new Tick(reference.AddDays(3 * multiplier), Symbols.SPY, 11, 13, 9){ Quantity = 10 }
            };

            foreach (var bar in ticks)
            {
                Assert.IsNull(newBar);
                consolidator.Update(bar);
            }
            Assert.IsNotNull(newBar);

            if (calendarType == CalendarType.Monthly)
            {
                var openTime = new DateTime(reference.Year, reference.Month, 1);
                var closeTime = new DateTime(reference.Year, reference.Month + 1, 1);
                Assert.AreEqual(openTime, newBar.Time);
                Assert.AreEqual(closeTime, newBar.EndTime);
            }
            else
            {
                Assert.AreEqual(reference, newBar.Time);
                Assert.AreEqual(reference.AddDays(7), newBar.EndTime);
            }

            Assert.AreEqual(Symbols.SPY, newBar.Symbol);
            Assert.AreEqual(ticks.First().Value, newBar.Open);
            Assert.AreEqual(ticks.Max(x => x.Value), newBar.High);
            Assert.AreEqual(ticks.Min(x => x.Value), newBar.Low);
            Assert.AreEqual(ticks.Last().Value, newBar.Close);
            Assert.AreEqual(0, newBar.Volume);
        }

        private SimpleMovingAverage indicator;

        [TestCase(CalendarType.Monthly)]
        [TestCase(CalendarType.Weekly)]
        public void CalendarConsolidatesWithRegisterIndicator(CalendarType calendarType)
        {
            var consolidator = new TradeBarCalendarConsolidator(calendarType);
            consolidator.DataConsolidated += (s, e) =>
            {
                if (!indicator.IsReady) return;

                var previous = e.Value - e.Period.Days;
                var actual = (e.Value + previous) / indicator.Period;
                Assert.AreEqual(indicator, actual);
            };

            indicator = new SimpleMovingAverage(2);
            RegisterIndicator(indicator, consolidator);

            var reference = new DateTime(2019, 4, 1);
            for (var i = 1; i < 100; i++)
            {
                var bar = new TradeBar(reference.AddDays(i - 1), Symbols.SPY, i, i, i, i, 0);
                consolidator.Update(bar);
            }
        }

        /// <summary>
        /// Simplified version of QCAlgorithm.RegisterIndicator
        /// </summary>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        public void RegisterIndicator(IndicatorBase<IndicatorDataPoint> indicator, IDataConsolidator consolidator)
        {
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                indicator.Update(consolidated.EndTime, consolidated.Value);
            };
        }
    }
}