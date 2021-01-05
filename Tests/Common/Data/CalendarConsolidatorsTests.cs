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
using Python.Runtime;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class CalendarConsolidatorsTests
    {
        private Dictionary<Language, dynamic> _dailyFuncDictionary;
        private Dictionary<Language, dynamic> _weeklyFuncDictionary;
        private Dictionary<Language, dynamic> _monthlyFuncDictionary;

        [OneTimeSetUp]
        public void SetUp()
        {
            _dailyFuncDictionary = new Dictionary<Language, dynamic> { { Language.CSharp, TimeSpan.FromDays(1) } };
            _weeklyFuncDictionary = new Dictionary<Language, dynamic> { { Language.CSharp, Calendar.Weekly } };
            _monthlyFuncDictionary = new Dictionary<Language, dynamic> { { Language.CSharp, Calendar.Monthly } };

            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(
                    "PythonCalendar",
                    @"
from datetime import timedelta
from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *
from QuantConnect.Data import *
from QuantConnect.Data.Consolidators import *
oneday = timedelta(1)

def Weekly(dt):
    value = 8 - dt.isoweekday()
    if value == 8: value = 1   # Sunday
    start = (dt + timedelta(value)).date() - timedelta(7)
    return CalendarInfo(start, timedelta(7))

def Monthly(dt):
    start = dt.replace(day=1).date()
    end = dt.replace(day=28) + timedelta(4)
    end = (end - timedelta(end.day-1)).date()
    return CalendarInfo(start, end - start)"
                );

                _dailyFuncDictionary[Language.Python] = module.GetAttr("oneday");
                _weeklyFuncDictionary[Language.Python] = module.GetAttr("Weekly");
                _monthlyFuncDictionary[Language.Python] = module.GetAttr("Monthly");
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AggregatesTradeBarToCalendarTradeBarProperly(Language language)
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

            var weeklyConsolidator = new TradeBarConsolidator(_weeklyFuncDictionary[language]);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTradeBar(
                    bars.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.SPY,
                    e);
            };

            var monthlyConsolidator = new TradeBarConsolidator(_monthlyFuncDictionary[language]);
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
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AggregatesQuoteBarToCalendarQuoteBarProperly(Language language)
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

            var weeklyConsolidator = new QuoteBarConsolidator(_weeklyFuncDictionary[language]);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertQuoteBar(
                    bars.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.EURUSD,
                    e);
            };

            var monthlyConsolidator = new QuoteBarConsolidator(_monthlyFuncDictionary[language]);
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
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AggregatesTickToCalendarTradeBarProperly(Language language)
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

            var weeklyConsolidator = new TickConsolidator(_weeklyFuncDictionary[language]);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTickTradeBar(
                    ticks.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.SPY,
                    e);
            };

            var monthlyConsolidator = new TickConsolidator(_monthlyFuncDictionary[language]);
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
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AggregatesTickToCalendarQuoteBarProperly(Language language)
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

            var weeklyConsolidator = new TickQuoteBarConsolidator(_weeklyFuncDictionary[language]);
            weeklyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTickQuoteBar(
                    ticks.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.EURUSD,
                    e);
            };

            var monthlyConsolidator = new TickQuoteBarConsolidator(_monthlyFuncDictionary[language]);
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
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AggregatesBaseDataToCalendarTradeBarProperly(Language language)
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

            var weeklyConsolidator = new BaseDataConsolidator(_weeklyFuncDictionary[language]);
            weeklyConsolidator.DataConsolidated += (s, e) => 
            {
                AssertBaseTradeBar(
                    ticks.Take(3),
                    reference,
                    reference.AddDays(7),
                    Symbols.SPY,
                    e);
            };

            var monthlyConsolidator = new BaseDataConsolidator(_monthlyFuncDictionary[language]);
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

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AggregatesTradeBarToDailyTradeBarProperly(Language language)
        {
            // Monday
            var reference = new DateTime(2019, 3, 18);
            var bars = new List<TradeBar>
            {
                new TradeBar(reference.AddHours(6), Symbols.SPY, 9, 11, 8, 10, 100, Time.OneHour),
                new TradeBar(reference.AddHours(12), Symbols.SPY, 10, 12, 8, 11, 100, Time.OneHour),
                new TradeBar(reference.AddHours(18), Symbols.SPY, 11, 13, 9, 10, 100, Time.OneHour),
                new TradeBar(reference.AddHours(21), Symbols.SPY, 11, 13, 9, 11, 100, Time.OneHour),
                new TradeBar(reference.AddHours(25), Symbols.SPY, 11, 13, 9, 11, 100, Time.OneHour)
            };

            var dailyConsolidator = new TradeBarConsolidator(_dailyFuncDictionary[language]);
            dailyConsolidator.DataConsolidated += (s, e) =>
            {
                AssertTradeBar(
                    bars.Take(4),
                    reference,
                    reference.AddDays(1),
                    Symbols.SPY,
                    e);
            };

            foreach (var bar in bars)
            {
                dailyConsolidator.Update(bar);
            }
        }

        private void AssertDailyTradeBar(IEnumerable<TradeBar> tradeBars, DateTime openTime, DateTime closeTime, Symbol symbol, TradeBar consolidated)
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


        private SimpleMovingAverage indicator;

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void AllCalendarsConsolidatesWithRegisterIndicator(Language language)
        {
            CalendarConsolidatesWithRegisterIndicator(_weeklyFuncDictionary[language]);
            CalendarConsolidatesWithRegisterIndicator(_monthlyFuncDictionary[language]);
        }

        [Test]
        public void Weekly()
        {
            var quarterly = Calendar.Weekly;

            var calendarInfo = quarterly(new DateTime(2020, 2, 20));

            Assert.AreEqual(new DateTime(2020, 2, 17), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(7), calendarInfo.Period);

            calendarInfo = quarterly(new DateTime(2018, 11, 2));

            Assert.AreEqual(new DateTime(2018, 10, 29), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(7), calendarInfo.Period);

            calendarInfo = quarterly(new DateTime(2018, 12, 31));

            Assert.AreEqual(new DateTime(2018, 12, 31), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(7), calendarInfo.Period);
        }

        [Test]
        public void Monthly()
        {
            var quarterly = Calendar.Monthly;

            var calendarInfo = quarterly(new DateTime(2020, 5, 11));

            Assert.AreEqual(new DateTime(2020, 5, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(31), calendarInfo.Period);

            calendarInfo = quarterly(new DateTime(2018, 11, 13));

            Assert.AreEqual(new DateTime(2018, 11, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(30), calendarInfo.Period);

            calendarInfo = quarterly(new DateTime(2018, 12, 31));

            Assert.AreEqual(new DateTime(2018, 12, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(31), calendarInfo.Period);
        }

        [Test]
        public void Quarterly()
        {
            var quarterly = Calendar.Quarterly;

            var calendarInfo = quarterly(new DateTime(2020, 5, 1));

            Assert.AreEqual(new DateTime(2020, 4, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(91), calendarInfo.Period);

            calendarInfo = quarterly(new DateTime(2018, 11, 13));

            Assert.AreEqual(new DateTime(2018, 10, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(92), calendarInfo.Period);

            calendarInfo = quarterly(new DateTime(2018, 12, 31));

            Assert.AreEqual(new DateTime(2018, 10, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(92), calendarInfo.Period);
        }

        [Test]
        public void Yearly()
        {
            var quarterly = Calendar.Yearly;
            var calendarInfo = quarterly(new DateTime(2020, 5, 1));

            Assert.AreEqual(new DateTime(2020, 1, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(366), calendarInfo.Period);   // leap year

            calendarInfo = quarterly(new DateTime(2021, 11, 1));

            Assert.AreEqual(new DateTime(2021, 1, 1), calendarInfo.Start);
            Assert.AreEqual(TimeSpan.FromDays(365), calendarInfo.Period);
        }

        private void CalendarConsolidatesWithRegisterIndicator(dynamic calendarType)
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