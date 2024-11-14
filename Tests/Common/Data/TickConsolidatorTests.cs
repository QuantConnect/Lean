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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class TickConsolidatorTests: BaseConsolidatorTests
    {
        [Test]
        public void AggregatesNewTradeBarsProperly()
        {
            TradeBar newTradeBar = null;
            using var consolidator = new TickConsolidator(4);
            consolidator.DataConsolidated += (sender, tradeBar) =>
            {
                newTradeBar = tradeBar;
            };
            var reference = DateTime.Today;
            var bar1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference,
                Value = 5,
                Quantity = 10
            };
            consolidator.Update(bar1);
            Assert.IsNull(newTradeBar);

            var bar2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(1),
                Value = 10,
                Quantity = 20
            };
            consolidator.Update(bar2);
            Assert.IsNull(newTradeBar);
            var bar3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(2),
                Value = 1,
                Quantity = 10
            };
            consolidator.Update(bar3);
            Assert.IsNull(newTradeBar);

            var bar4 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(3),
                Value = 9,
                Quantity = 20
            };
            consolidator.Update(bar4);
            Assert.IsNotNull(newTradeBar);

            Assert.AreEqual(Symbols.SPY, newTradeBar.Symbol);
            Assert.AreEqual(bar1.Time, newTradeBar.Time);
            Assert.AreEqual(bar1.Value, newTradeBar.Open);
            Assert.AreEqual(bar2.Value, newTradeBar.High);
            Assert.AreEqual(bar3.Value, newTradeBar.Low);
            Assert.AreEqual(bar4.Value, newTradeBar.Close);
            Assert.AreEqual(bar4.EndTime, newTradeBar.EndTime);
            Assert.AreEqual(bar1.Quantity + bar2.Quantity + bar3.Quantity + bar4.Quantity, newTradeBar.Volume);
        }

        [Test]
        public void DoesNotConsolidateDifferentSymbols()
        {
            using var consolidator = new TickConsolidator(2);

            var reference = DateTime.Today;

            var tick1 = new Tick
            {
                Symbol = Symbols.AAPL,
                Time = reference,
                BidPrice = 1000,
                BidSize = 20,
                TickType = TickType.Quote,
            };

            var tick2 = new Tick
            {
                Symbol = Symbols.ZNGA,
                Time = reference,
                BidPrice = 20,
                BidSize = 30,
                TickType = TickType.Quote,
            };

            consolidator.Update(tick1);

            Exception ex = Assert.Throws<InvalidOperationException>(() => consolidator.Update(tick2));
            Assert.IsTrue(ex.Message.Contains("is not the same"));
        }

        [Test]
        public void AggregatesPeriodInCountModeWithDailyData()
        {
            TradeBar consolidated = null;
            using var consolidator = new TickConsolidator(2);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new Tick { Time = reference});
            Assert.IsNull(consolidated);

            consolidator.Update(new Tick { Time = reference.AddMilliseconds(1)});
            Assert.IsNotNull(consolidated);

            // sadly the first emit will be off by the data resolution since we 'swallow' a point, so to
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), consolidated.Period);
            consolidated = null;

            consolidator.Update(new Tick { Time = reference.AddMilliseconds(2)});
            Assert.IsNull(consolidated);

            consolidator.Update(new Tick { Time = reference.AddMilliseconds(3)});
            Assert.IsNotNull(consolidated);

            Assert.AreEqual(TimeSpan.FromMilliseconds(2), consolidated.Period);
        }

        [Test]
        public void AggregatesPeriodInPeriodModeWithDailyData()
        {
            TradeBar consolidated = null;
            using var consolidator = new TickConsolidator(TimeSpan.FromDays(1));
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new Tick { Time = reference});
            Assert.IsNull(consolidated);

            consolidator.Update(new Tick { Time = reference.AddDays(1)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(TimeSpan.FromDays(1), consolidated.Period);
            consolidated = null;

            consolidator.Update(new Tick { Time = reference.AddDays(2)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(TimeSpan.FromDays(1), consolidated.Period);
            consolidated = null;

            consolidator.Update(new Tick { Time = reference.AddDays(3)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(TimeSpan.FromDays(1), consolidated.Period);
        }

        [Test]
        public void AggregatesPeriodInPeriodModeWithDailyDataAndRoundedTime()
        {
            TradeBar consolidated = null;
            using var consolidator = new TickConsolidator(TimeSpan.FromDays(1));
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new Tick { Time = reference.AddSeconds(5) });
            Assert.IsNull(consolidated);

            consolidator.Update(new Tick { Time = reference.AddDays(1).AddSeconds(15) });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(TimeSpan.FromDays(1), consolidated.Period);
            Assert.AreEqual(reference, consolidated.Time);
            consolidated = null;

            consolidator.Update(new Tick { Time = reference.AddDays(2).AddMinutes(1) });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(TimeSpan.FromDays(1), consolidated.Period);
            Assert.AreEqual(reference.AddDays(1), consolidated.Time);
            consolidated = null;

            consolidator.Update(new Tick { Time = reference.AddDays(3).AddMinutes(5) });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(TimeSpan.FromDays(1), consolidated.Period);
            Assert.AreEqual(reference.AddDays(2), consolidated.Time);
        }

        [Test]
        public void AggregatesNewTicksInPeriodWithRoundedTime()
        {
            TradeBar consolidated = null;
            using var consolidator = new TickConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 06, 02);
            var tick1 = new Tick
            {
                Symbol = Symbols.EURUSD,
                Time = reference.AddSeconds(3),
                Value = 1.1000m
            };
            consolidator.Update(tick1);
            Assert.IsNull(consolidated);

            var tick2 = new Tick
            {
                Symbol = Symbols.EURUSD,
                Time = reference.AddSeconds(10),
                Value = 1.1005m
            };
            consolidator.Update(tick2);
            Assert.IsNull(consolidated);

            var tick3 = new Tick
            {
                Symbol = Symbols.EURUSD,
                Time = reference.AddSeconds(61),
                Value = 1.1010m
            };
            consolidator.Update(tick3);
            Assert.IsNotNull(consolidated);

            Assert.AreEqual(consolidated.Time, reference);
            Assert.AreEqual(consolidated.Open, tick1.Value);
            Assert.AreEqual(consolidated.Close, tick2.Value);

            var tick4 = new Tick
            {
                Symbol = Symbols.EURUSD,
                Time = reference.AddSeconds(70),
                Value = 1.1015m
            };
            consolidator.Update(tick4);
            Assert.IsNotNull(consolidated);

            var tick5 = new Tick
            {
                Symbol = Symbols.EURUSD,
                Time = reference.AddSeconds(118),
                Value = 1.1020m
            };
            consolidator.Update(tick5);
            Assert.IsNotNull(consolidated);

            var tick6 = new Tick
            {
                Symbol = Symbols.EURUSD,
                Time = reference.AddSeconds(140),
                Value = 1.1025m
            };
            consolidator.Update(tick6);
            Assert.IsNotNull(consolidated);

            Assert.AreEqual(consolidated.Time, reference.AddSeconds(60));
            Assert.AreEqual(consolidated.Open, tick3.Value);
            Assert.AreEqual(consolidated.Close, tick5.Value);
        }

        [Test]
        public void ProcessesTradeTicksOnly()
        {
            TradeBar consolidated = null;
            using var consolidator = new TickConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 06, 02);
            var tick1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(3),
                Value = 200m
            };
            consolidator.Update(tick1);
            Assert.IsNull(consolidated);

            var tick2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(10),
                Value = 20000m,
                TickType = TickType.OpenInterest
            };
            consolidator.Update(tick2);
            Assert.IsNull(consolidated);

            var tick3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(10),
                Value = 10000m,
                TickType = TickType.Quote
            };
            consolidator.Update(tick3);
            Assert.IsNull(consolidated);

            var tick4 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(61),
                Value = 250m
            };
            consolidator.Update(tick4);
            Assert.IsNotNull(consolidated);

            Assert.AreEqual(consolidated.Time, reference);
            Assert.AreEqual(consolidated.Open, tick1.Value);
            Assert.AreEqual(consolidated.Close, tick1.Value);
        }

        protected override IDataConsolidator CreateConsolidator()
        {
            return new TickConsolidator(2);
        }

        protected override dynamic GetTestValues()
        {
            var time = DateTime.Today;
            return new List<Tick>()
            {
                new Tick(){Symbol = Symbols.SPY, Time = time, Value = 10 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(1), Value = 2 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(2), Value = 8 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(3), Value = 5 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(4), Value = 13 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(5), Value = 15 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(6), Value = 10 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(7), Value = 11 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(8), Value = 11 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(9), Value = 4 },
                new Tick(){Symbol = Symbols.SPY, Time = time.AddSeconds(10), Value = 7 },
            };
        }
    }
}
