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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class QuoteBarConsolidatorTests: BaseConsolidatorTests
    {
        [Test]
        public void ThrowsWhenPeriodIsSmallerThanDataPeriod()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(Time.OneHour);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = new DateTime(2022, 6, 6, 13, 30, 1);
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = TimeSpan.FromDays(1)
            };
            Assert.Throws<ArgumentException>(() => creator.Update(bar1));
        }

        [Test]
        public void MultipleResolutionConsolidation()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(Time.OneDay);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = new DateTime(2022, 6, 6);
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = TimeSpan.FromDays(1)
            };
            creator.Update(bar1);
            Assert.IsNull(quoteBar);
            creator.Scan(bar1.EndTime);
            Assert.IsNotNull(quoteBar);
            quoteBar = null;

            // now let's send in other resolution data
            var previousBar = bar1;
            for (int i = 0; i <= 24; i++)
            {
                previousBar = new QuoteBar
                {
                    Time = previousBar.EndTime,
                    Symbol = Symbols.SPY,
                    Bid = new Bar(1, 2, 0.75m, 1.25m),
                    LastBidSize = 3,
                    Ask = null,
                    LastAskSize = 0,
                    Value = 1,
                    Period = TimeSpan.FromHours(1)
                };
                creator.Update(previousBar);

                if (i < 24)
                {
                    Assert.IsNull(quoteBar, $"{i} {previousBar.EndTime}");
                }
                else
                {
                    Assert.IsNotNull(quoteBar, $"{i} {previousBar.EndTime}");
                }
            }
        }

        [Test]
        public void GentlyHandlesPeriodAndDataAreSameResolution()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(Time.OneDay);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = new DateTime(2022, 6, 6, 13, 30, 1);
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = TimeSpan.FromDays(1)
            };
            creator.Update(bar1);
            Assert.IsNull(quoteBar);
            creator.Scan(bar1.EndTime);
            Assert.IsNotNull(quoteBar);

            Assert.AreEqual(bar1.Symbol, quoteBar.Symbol);
            Assert.AreEqual(bar1.Ask, quoteBar.Ask);
            Assert.AreEqual(bar1.Bid.Open, quoteBar.Bid.Open);
            Assert.AreEqual(bar1.Bid.High, quoteBar.Bid.High);
            Assert.AreEqual(bar1.Bid.Low, quoteBar.Bid.Low);
            Assert.AreEqual(bar1.Bid.Close, quoteBar.Bid.Close);
            Assert.AreEqual(bar1.LastBidSize, quoteBar.LastBidSize);
            Assert.AreEqual(bar1.LastAskSize, quoteBar.LastAskSize);
            Assert.AreEqual(bar1.Value, quoteBar.Value);
            Assert.AreEqual(bar1.EndTime, quoteBar.EndTime);
            Assert.AreEqual(bar1.Time, quoteBar.Time);
            Assert.AreEqual(bar1.Period, quoteBar.Period);
        }

        [Test]
        public void AggregatesNewCountQuoteBarProperlyDaily()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(1);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = new DateTime(2022, 6, 6, 13, 30, 1);
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = TimeSpan.FromDays(1)
            };
            creator.Update(bar1);
            Assert.IsNotNull(quoteBar);

            Assert.AreEqual(bar1.Symbol, quoteBar.Symbol);
            Assert.AreEqual(bar1.Ask, quoteBar.Ask);
            Assert.AreEqual(bar1.Bid.Open, quoteBar.Bid.Open);
            Assert.AreEqual(bar1.Bid.High, quoteBar.Bid.High);
            Assert.AreEqual(bar1.Bid.Low, quoteBar.Bid.Low);
            Assert.AreEqual(bar1.Bid.Close, quoteBar.Bid.Close);
            Assert.AreEqual(bar1.LastBidSize, quoteBar.LastBidSize);
            Assert.AreEqual(bar1.LastAskSize, quoteBar.LastAskSize);
            Assert.AreEqual(bar1.Value, quoteBar.Value);
            Assert.AreEqual(bar1.EndTime, quoteBar.EndTime);
            Assert.AreEqual(bar1.Time, quoteBar.Time);
            Assert.AreEqual(bar1.Period, quoteBar.Period);
        }

        [Test]
        public void AggregatesNewCountQuoteBarProperly()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(4);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = DateTime.Today;
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar1);
            Assert.IsNull(quoteBar);

            var bar2 = new QuoteBar
            {
                Time = bar1.EndTime,
                Symbol = Symbols.SPY,
                Bid = new Bar(1.1m, 2.2m, 0.9m, 2.1m),
                LastBidSize = 3,
                Ask = new Bar(2.2m, 4.4m, 3.3m, 3.3m),
                LastAskSize = 0,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar2);
            Assert.IsNull(quoteBar);

            var bar3 = new QuoteBar
            {
                Time = bar2.EndTime,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.5m, 1.75m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar3);
            Assert.IsNull(quoteBar);

            var bar4 = new QuoteBar
            {
                Time = bar3.EndTime,
                Symbol = Symbols.SPY,
                Bid = null,
                LastBidSize = 0,
                Ask = new Bar(1, 7, 0.5m, 4.4m),
                LastAskSize = 4,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar4);
            Assert.IsNotNull(quoteBar);
            Assert.AreEqual(bar1.Symbol, quoteBar.Symbol);
            Assert.AreEqual(bar1.Bid.Open, quoteBar.Bid.Open);
            Assert.AreEqual(bar2.Ask.Open, quoteBar.Ask.Open);
            Assert.AreEqual(bar2.Bid.High, quoteBar.Bid.High);
            Assert.AreEqual(bar4.Ask.High, quoteBar.Ask.High);
            Assert.AreEqual(bar3.Bid.Low, quoteBar.Bid.Low);
            Assert.AreEqual(bar4.Ask.Low, quoteBar.Ask.Low);
            Assert.AreEqual(bar3.Bid.Close, quoteBar.Bid.Close);
            Assert.AreEqual(bar4.Ask.Close, quoteBar.Ask.Close);
            Assert.AreEqual(bar3.LastBidSize, quoteBar.LastBidSize);
            Assert.AreEqual(bar4.LastAskSize, quoteBar.LastAskSize);
            Assert.AreEqual(bar1.Value, quoteBar.Value);

            Assert.AreEqual(bar1.Time, quoteBar.Time);
            Assert.AreEqual(bar4.EndTime, quoteBar.EndTime);
            Assert.AreEqual(TimeSpan.FromMinutes(4), quoteBar.Period);
        }

        [Test]
        public void AggregatesNewTimeSpanQuoteBarProperly()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(TimeSpan.FromMinutes(2));
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = DateTime.Today;
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar1);
            Assert.IsNull(quoteBar);

            var bar2 = new QuoteBar
            {
                Time = bar1.EndTime,
                Symbol = Symbols.SPY,
                Bid = new Bar(1.1m, 2.2m, 0.9m, 2.1m),
                LastBidSize = 3,
                Ask = new Bar(2.2m, 4.4m, 3.3m, 3.3m),
                LastAskSize = 0,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar2);
            Assert.IsNull(quoteBar);

            // pushing another bar to force the fire
            var bar3 = new QuoteBar
            {
                Time = bar2.EndTime,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.5m, 1.75m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = Time.OneMinute
            };
            creator.Update(bar3);


            Assert.IsNotNull(quoteBar);

            
            Assert.AreEqual(bar1.Symbol, quoteBar.Symbol);
            Assert.AreEqual(bar1.Time, quoteBar.Time);
            Assert.AreEqual(bar2.EndTime, quoteBar.EndTime);
            Assert.AreEqual(TimeSpan.FromMinutes(2), quoteBar.Period);

            // Bid
            Assert.AreEqual(bar1.Bid.Open, quoteBar.Bid.Open);
            Assert.AreEqual(bar2.Bid.Close, quoteBar.Bid.Close);
            Assert.AreEqual(Math.Max(bar2.Bid.High, bar1.Bid.High), quoteBar.Bid.High);
            Assert.AreEqual(Math.Min(bar2.Bid.Low, bar1.Bid.Low), quoteBar.Bid.Low);

            // Ask
            Assert.AreEqual(bar2.Ask.Open, quoteBar.Ask.Open);
            Assert.AreEqual(bar2.Ask.Close, quoteBar.Ask.Close);
            Assert.AreEqual(bar2.Ask.High, quoteBar.Ask.High);
            Assert.AreEqual(bar2.Ask.Low, quoteBar.Ask.Low);
            Assert.AreEqual(bar1.LastAskSize, quoteBar.LastAskSize);

            Assert.AreEqual(1, quoteBar.Value);
                        
        }

        [Test]
        public void DoesNotConsolidateDifferentSymbols()
        {
            using var consolidator = new QuoteBarConsolidator(2);

            var time = DateTime.Today;
            var period = TimeSpan.FromMinutes(1);

            var bar1 = new QuoteBar
            {
                Symbol = Symbols.AAPL,
                Time = time,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = period
            };

            var bar2 = new QuoteBar
            {
                Symbol = Symbols.ZNGA,
                Time = time,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = period
            };

            consolidator.Update(bar1);

            Exception ex = Assert.Throws<InvalidOperationException>(() => consolidator.Update(bar2));
            Assert.IsTrue(ex.Message.Contains("is not the same", StringComparison.InvariantCultureIgnoreCase));
        }

        [Test]
        public void LastCloseAndCurrentOpenPriceShouldBeSameConsolidatedOnTimeSpan()
        {
            QuoteBar quoteBar = null;
            using var creator = new QuoteBarConsolidator(TimeSpan.FromMinutes(2));
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = DateTime.Today;
            var period = TimeSpan.FromMinutes(1);
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = period
            };
            creator.Update(bar1);
            Assert.IsNull(quoteBar);

            var bar2 = new QuoteBar
            {
                Time = time + TimeSpan.FromMinutes(1),
                Symbol = Symbols.SPY,
                Bid = null,
                LastBidSize = 0,
                Ask = new Bar(2.2m, 4.4m, 3.3m, 3.3m),
                LastAskSize = 10,
                Value = 1,
                Period = period
            };
            creator.Update(bar2);
            Assert.IsNull(quoteBar);

            // pushing another bar to force the fire
            var bar3 = new QuoteBar
            {
                Time = time + TimeSpan.FromMinutes(2),
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.5m, 1.75m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0,
                Value = 1,
                Period = period
            };
            creator.Update(bar3);
            Assert.IsNotNull(quoteBar);

            //force the consolidator to emit DataConsolidated
            creator.Scan(time.AddMinutes(4));

            Assert.AreEqual(bar1.Symbol, quoteBar.Symbol);
            Assert.AreEqual(time + TimeSpan.FromMinutes(4), quoteBar.EndTime);
            Assert.AreEqual(TimeSpan.FromMinutes(2), quoteBar.Period);

            // Bid
            Assert.AreEqual(quoteBar.Bid.Open, bar1.Bid.Close);
            // Ask
            Assert.AreEqual(quoteBar.Ask.Open, bar2.Ask.Close);
        }

        protected override IDataConsolidator CreateConsolidator()
        {
            return new QuoteBarConsolidator(2);
        }

        protected override IEnumerable<IBaseData> GetTestValues()
        {
            var time = DateTime.Today;
            return new List<QuoteBar>()
            {
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(1, 2, 0.5m, 1.75m), Ask = new Bar(2.2m, 4.4m, 3.3m, 3.3m), LastBidSize = 10, LastAskSize = 0 },
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(0, 4, 0.4m, 3.75m), Ask = new Bar(2.3m, 9.4m, 2.3m, 4.5m), LastBidSize = 5, LastAskSize = 4 },
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(2, 2, 0.9m, 1.45m), Ask = new Bar(2.7m, 8.4m, 3.6m, 3.6m), LastBidSize = 8, LastAskSize = 4 },
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(2, 6, 2.5m, 5.55m), Ask = new Bar(3.2m, 6.4m, 2.3m, 5.3m), LastBidSize = 9, LastAskSize = 4 },
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(1, 2, 1.5m, 0.34m), Ask = new Bar(3.6m, 9.4m, 3.7m, 3.8m), LastBidSize = 5, LastAskSize = 8 },
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(1, 2, 1.1m, 0.75m), Ask = new Bar(3.8m, 8.4m, 7.3m, 5.3m), LastBidSize = 9, LastAskSize = 5 },
                new QuoteBar(){Time = time, Symbol = Symbols.SPY, Bid = new Bar(3, 3, 2.2m, 1.12m), Ask = new Bar(4.5m, 7.2m, 7.1m, 6.1m), LastBidSize = 6, LastAskSize = 3 },
            };
        }
    }
}
