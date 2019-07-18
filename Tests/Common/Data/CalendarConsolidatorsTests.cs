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
        [Test]
        public void AggregatesTradeBarToCalendarTradeBarProperly()
        {
            // Monday
            var reference = new DateTime(2019, 3, 18);
            var bars = new List<TradeBar>
            {
                new TradeBar(reference.AddDays(1), Symbols.SPY, 9, 11, 8, 10, 100, Time.OneDay),
                new TradeBar(reference.AddDays(3), Symbols.SPY, 10, 12, 8, 11, 100, Time.OneDay),
                new TradeBar(reference.AddDays(5), Symbols.SPY, 11, 13, 9, 10, 100, Time.OneDay),
                new TradeBar(reference.AddDays(7), Symbols.SPY, 11, 13, 9, 11, 100, Time.OneDay),
                new TradeBar(reference.AddDays(14), Symbols.SPY, 11, 13, 9, 11, 100, Time.OneDay)
            };

            var weeklyConsolidator = new TradeBarConsolidator(CalendarType.Weekly);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTradeBar(
                    bars.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.SPY,
                    e);
            };

            var monthlyConsolidator = new TradeBarConsolidator(CalendarType.Monthly);
            monthlyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTradeBar(
                    bars.Take(4),
                    new DateTime(reference.Year, reference.Month, 1),
                    new DateTime(reference.Year, reference.Month + 1, 1),
                    Symbols.SPY,
                    e);
            };

            foreach (var bar in bars.Take(4))
            {
                weeklyConsolidator.Update(bar);
            }

            foreach (var bar in bars)
            {
                monthlyConsolidator.Update(bar);
            }
        }

        private void AssertTradeBar(IEnumerable<TradeBar> tradeBars, DateTime openTime, DateTime closeTime, Symbol symbol, TradeBar consolidated)
        {
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(openTime, consolidated.Time);
            Assert.AreEqual(closeTime, consolidated.EndTime);
            Assert.AreEqual(symbol, consolidated.Symbol);
            Assert.AreEqual(tradeBars.First().Open, consolidated.Open);
            Assert.AreEqual(tradeBars.Max(x => x.High), consolidated.High);
            Assert.AreEqual(tradeBars.Min(x => x.Low), consolidated.Low);
            Assert.AreEqual(tradeBars.Last().Close, consolidated.Close);
            Assert.AreEqual(tradeBars.Sum(x => x.Volume), consolidated.Volume);
        }

        [Test]
        public void AggregatesQuoteBarToCalendarQuoteBarProperly()
        {
            // Monday
            var reference = new DateTime(2019, 3, 18);
            var bars = new List<QuoteBar>
            {
                new QuoteBar(reference.AddDays(1), Symbols.EURUSD, new Bar(9, 11, 8, 10), 10, new Bar(9, 11, 8, 10), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(3), Symbols.EURUSD, new Bar(10, 12, 8, 11), 10, new Bar(10, 12, 8, 11), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(5), Symbols.EURUSD, new Bar(11, 13, 9, 10), 10, new Bar(11, 13, 9, 10), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(7), Symbols.EURUSD, new Bar(11, 13, 9, 11), 10, new Bar(11, 13, 9, 11), 10, Time.OneDay),
                new QuoteBar(reference.AddDays(14), Symbols.EURUSD, new Bar(11, 13, 9, 11), 10, new Bar(11, 13, 9, 11), 10, Time.OneDay)
            };

            var weeklyConsolidator = new QuoteBarConsolidator(CalendarType.Weekly);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertQuoteBar(
                    bars.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.EURUSD,
                    e);
            };

            var monthlyConsolidator = new QuoteBarConsolidator(CalendarType.Monthly);
            monthlyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertQuoteBar(
                    bars.Take(4),
                    new DateTime(reference.Year, reference.Month, 1),
                    new DateTime(reference.Year, reference.Month + 1, 1),
                    Symbols.EURUSD,
                    e);
            };

            foreach (var bar in bars.Take(4))
            {
                weeklyConsolidator.Update(bar);
            }

            foreach (var bar in bars.Take(5))
            {
                monthlyConsolidator.Update(bar);
            }
        }
        private void AssertQuoteBar(IEnumerable<QuoteBar> quoteBars, DateTime openTime, DateTime closeTime, Symbol symbol, QuoteBar consolidated)
        {
            Assert.AreEqual(symbol, consolidated.Symbol);
            Assert.AreEqual(openTime, consolidated.Time);
            Assert.AreEqual(closeTime, consolidated.EndTime);
            Assert.AreEqual(quoteBars.First().Open, consolidated.Open);
            Assert.AreEqual(quoteBars.First().Bid.Open, consolidated.Bid.Open);
            Assert.AreEqual(quoteBars.First().Ask.Open, consolidated.Ask.Open);
            Assert.AreEqual(quoteBars.Max(x => x.High), consolidated.High);
            Assert.AreEqual(quoteBars.Max(x => x.Bid.High), consolidated.Bid.High);
            Assert.AreEqual(quoteBars.Max(x => x.Ask.High), consolidated.Ask.High);
            Assert.AreEqual(quoteBars.Min(x => x.Low), consolidated.Low);
            Assert.AreEqual(quoteBars.Min(x => x.Bid.Low), consolidated.Bid.Low);
            Assert.AreEqual(quoteBars.Min(x => x.Ask.Low), consolidated.Ask.Low);
            Assert.AreEqual(quoteBars.Last().Close, consolidated.Close);
            Assert.AreEqual(quoteBars.Last().Bid.Close, consolidated.Bid.Close);
            Assert.AreEqual(quoteBars.Last().Ask.Close, consolidated.Ask.Close);
        }

        [Test]
        public void AggregatesTickToCalendarTradeBarProperly()
        {
            // Monday
            var reference = new DateTime(2019, 3, 18);
            var ticks = new List<Tick>
            {
                new Tick(reference.AddDays(1), Symbols.SPY, 9, 11, 8){ TickType = TickType.Trade, Quantity = 10 },
                new Tick(reference.AddDays(3), Symbols.SPY, 10, 12, 8){ TickType = TickType.Trade,  Quantity = 10 },
                new Tick(reference.AddDays(5), Symbols.SPY, 11, 13, 9){ TickType = TickType.Trade,  Quantity = 10 },
                new Tick(reference.AddDays(7), Symbols.SPY, 11, 13, 9){ TickType = TickType.Trade,  Quantity = 10 },
                new Tick(reference.AddDays(14), Symbols.SPY, 11, 13, 9){ TickType = TickType.Trade,  Quantity = 10 }
            };

            var weeklyConsolidator = new TickConsolidator(CalendarType.Weekly);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTickTradeBar(
                    ticks.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.SPY,
                    e);
            };

            var monthlyConsolidator = new TickConsolidator(CalendarType.Monthly);
            monthlyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTickTradeBar(
                    ticks.Take(4),
                    new DateTime(reference.Year, reference.Month, 1),
                    new DateTime(reference.Year, reference.Month + 1, 1),
                    Symbols.SPY,
                    e);
            };

            foreach (var tick in ticks.Take(4))
            {
                weeklyConsolidator.Update(tick);
            }

            foreach (var tick in ticks)
            {
                monthlyConsolidator.Update(tick);
            }
        }

        private void AssertTickTradeBar(IEnumerable<Tick> ticks, DateTime openTime, DateTime closeTime, Symbol symbol, TradeBar consolidated)
        {
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(openTime, consolidated.Time);
            Assert.AreEqual(closeTime, consolidated.EndTime);
            Assert.AreEqual(symbol, consolidated.Symbol);
            Assert.AreEqual(ticks.First().Value, consolidated.Open);
            Assert.AreEqual(ticks.Max(x => x.Value), consolidated.High);
            Assert.AreEqual(ticks.Min(x => x.Value), consolidated.Low);
            Assert.AreEqual(ticks.Last().Value, consolidated.Close);
            Assert.AreEqual(ticks.Sum(x => x.Quantity), consolidated.Volume);
        }

        [Test]
        public void AggregatesTickToCalendarQuoteBarProperly()
        {
            // Monday
            var reference = new DateTime(2019, 3, 18);
            var ticks = new List<Tick>
            {
                new Tick(reference.AddDays(1), Symbols.EURUSD, 9, 11, 8){ Quantity = 10 },
                new Tick(reference.AddDays(3), Symbols.EURUSD, 10, 12, 8){ Quantity = 10 },
                new Tick(reference.AddDays(5), Symbols.EURUSD, 11, 13, 9){ Quantity = 10 },
                new Tick(reference.AddDays(7), Symbols.EURUSD, 11, 13, 9){ Quantity = 10 },
                new Tick(reference.AddDays(14), Symbols.EURUSD, 11, 13, 9){ Quantity = 10 }
            };

            var weeklyConsolidator = new TickQuoteBarConsolidator(CalendarType.Weekly);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTickQuoteBar(
                    ticks.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.EURUSD,
                    e);
            };

            var monthlyConsolidator = new TickQuoteBarConsolidator(CalendarType.Monthly);
            monthlyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTickQuoteBar(
                    ticks.Take(4),
                    new DateTime(reference.Year, reference.Month, 1),
                    new DateTime(reference.Year, reference.Month + 1, 1),
                    Symbols.EURUSD,
                    e);
            };

            foreach (var tick in ticks.Take(4))
            {
                weeklyConsolidator.Update(tick);
            }

            foreach (var tick in ticks)
            {
                monthlyConsolidator.Update(tick);
            }
        }

        private void AssertTickQuoteBar(IEnumerable<Tick> ticks, DateTime openTime, DateTime closeTime, Symbol symbol, QuoteBar consolidated)
        {
            Assert.IsNotNull(consolidated);        
            Assert.AreEqual(openTime, consolidated.Time);
            Assert.AreEqual(closeTime, consolidated.EndTime);
            Assert.AreEqual(symbol, consolidated.Symbol);
            Assert.AreEqual(ticks.First().BidPrice, consolidated.Bid.Open);
            Assert.AreEqual(ticks.First().AskPrice, consolidated.Ask.Open);
            Assert.AreEqual(ticks.Max(x => x.BidPrice), consolidated.Bid.High);
            Assert.AreEqual(ticks.Max(x => x.AskPrice), consolidated.Ask.High);
            Assert.AreEqual(ticks.Min(x => x.BidPrice), consolidated.Bid.Low);
            Assert.AreEqual(ticks.Min(x => x.AskPrice), consolidated.Ask.Low);
            Assert.AreEqual(ticks.Last().BidPrice, consolidated.Bid.Close);
            Assert.AreEqual(ticks.Last().AskPrice, consolidated.Ask.Close);
        }

        [Test]
        public void AggregatesBaseDataToCalendarTradeBarProperly()
        {
            // Monday
            var reference = new DateTime(2019, 3, 18);
            var ticks = new List<Tick>
            {
                new Tick(reference.AddDays(1), Symbols.SPY, 9, 11, 8){ Quantity = 10 },
                new Tick(reference.AddDays(3), Symbols.SPY, 10, 12, 8){ Quantity = 10 },
                new Tick(reference.AddDays(5), Symbols.SPY, 11, 13, 9){ Quantity = 10 },
                new Tick(reference.AddDays(7), Symbols.SPY, 11, 13, 9){ Quantity = 10 },
                new Tick(reference.AddDays(14), Symbols.SPY, 11, 13, 9){ Quantity = 10 }
            };

            var weeklyConsolidator = new BaseDataConsolidator(CalendarType.Weekly);
            weeklyConsolidator.DataConsolidated += (s, e) => 
            {
                AssertBaseTradeBar(
                    ticks.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.SPY,
                    e);
            };

            var monthlyConsolidator = new BaseDataConsolidator(CalendarType.Monthly);
            monthlyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertBaseTradeBar(
                    ticks.Take(4),
                    new DateTime(reference.Year, reference.Month, 1),
                    new DateTime(reference.Year, reference.Month + 1, 1),
                    Symbols.SPY,
                    e);
            };

            foreach (var tick in ticks.Take(4))
            {
                weeklyConsolidator.Update(tick);
            }

            foreach (var tick in ticks)
            {
                monthlyConsolidator.Update(tick);
            }
        }

        private void AssertBaseTradeBar(IEnumerable<Tick> ticks, DateTime openTime, DateTime closeTime, Symbol symbol, TradeBar consolidated)
        {
            Assert.AreEqual(openTime, consolidated.Time);
            Assert.AreEqual(closeTime, consolidated.EndTime);
            Assert.AreEqual(symbol, consolidated.Symbol);
            Assert.AreEqual(ticks.First().Value, consolidated.Open);
            Assert.AreEqual(ticks.Max(x => x.Value), consolidated.High);
            Assert.AreEqual(ticks.Min(x => x.Value), consolidated.Low);
            Assert.AreEqual(ticks.Last().Value, consolidated.Close);
            Assert.AreEqual(0, consolidated.Volume);
        }

        private SimpleMovingAverage indicator;

        [Test]
        public void AllCalendarsConsolidatesWithRegisterIndicator()
        {
            CalendarConsolidatesWithRegisterIndicator(CalendarType.Weekly);
            CalendarConsolidatesWithRegisterIndicator(CalendarType.Monthly);
        }

        private void CalendarConsolidatesWithRegisterIndicator(Func<DateTime, CalendarInfo> calendarType)
        {
            var consolidator = new TradeBarConsolidator(calendarType);
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