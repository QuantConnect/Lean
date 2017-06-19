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
    public class TradeBarConsolidatorTests
    {
        [Test]
        public void ZeroCountAlwaysFires()
        {
            // defining a TradeBarConsolidator with a zero max count should cause it to always fire identity

            TradeBar consolidated = null;
            var consolidator = new TradeBarConsolidator(0);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            consolidator.Update(new TradeBar());
            Assert.IsNotNull(consolidated);
        }

        [Test]
        public void OneCountAlwaysFires()
        {
            // defining a TradeBarConsolidator with a one max count should cause it to always fire identity

            TradeBar consolidated = null;
            var consolidator = new TradeBarConsolidator(1);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            consolidator.Update(new TradeBar());
            Assert.IsNotNull(consolidated);
        }

        [Test]
        public void TwoCountFiresEveryOther()
        {
            // defining a TradeBarConsolidator with a two max count should cause it to fire every other TradeBar

            TradeBar consolidated = null;
            var consolidator = new TradeBarConsolidator(2);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            consolidator.Update(new TradeBar());
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar());
            Assert.IsNotNull(consolidated);

            consolidated = null;

            consolidator.Update(new TradeBar());
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar());
            Assert.IsNotNull(consolidated);
        }

        [Test]
        public void ZeroSpanAlwaysFires()
        {
            // defining a TradeBarConsolidator with a zero period should cause it to always fire identity

            TradeBar consolidated = null;
            var consolidator = new TradeBarConsolidator(TimeSpan.Zero);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2014, 12, 01, 01, 01, 00);
            consolidator.Update(new TradeBar {Close = 1m, Time = reference});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(1, consolidated.Close);

            consolidator.Update(new TradeBar {Close = 2m, Time = reference});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(2, consolidated.Close);

            consolidator.Update(new TradeBar {Close = 3m, Time = reference});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(3, consolidated.Close);
        }

        [Test]
        public void OneMinuteAlwaysFiresEveryTimeOnMinuteDataExceptFirstPoint()
        {
            // defining a TradeBarConsolidator with the same period as the resolution of input data will cause
            // it to not fire on the first piece of data as it is initializing, but will then fire for each
            // consecutive data point

            TradeBar consolidated = null;
            var consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(1));
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2014, 12, 01, 01, 01, 00);
            consolidator.Update(new TradeBar {Close = 1m, Time = reference});
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar {Close = 2m, Time = reference.AddMinutes(1)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(1, consolidated.Close);

            consolidator.Update(new TradeBar {Close = 3m, Time = reference.AddMinutes(2)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(2, consolidated.Close);
        }

        [Test]
        public void ConsolidatesOHLCV()
        {
            // verifies that the TradeBarConsolidator correctly consolidates OHLCV data into a new TradeBar instance

            TradeBar consolidated = null;
            var consolidator = new TradeBarConsolidator(3);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var tb1 = new TradeBar
            {
                Symbol = Symbols.SPY,
                Open = 10,
                High = 100,
                Low = 1,
                Close = 50,
                Volume = 75,
                DataType = MarketDataType.TradeBar
            };

            var tb2 = new TradeBar
            {
                Symbol = Symbols.SPY,
                Open = 50,
                High = 123,
                Low = 35,
                Close = 75,
                Volume = 100,
                DataType = MarketDataType.TradeBar  
            };

            var tb3 = new TradeBar
            {
                Symbol = Symbols.SPY,
                Open = 75,
                High = 100,
                Low = 50,
                Close = 83,
                Volume = 125,
                DataType = MarketDataType.TradeBar
            };

            consolidator.Update(tb1);
            consolidator.Update(tb2);
            consolidator.Update(tb3);

            Assert.IsNotNull(consolidated);
            Assert.AreEqual(Symbols.SPY, consolidated.Symbol);
            Assert.AreEqual(10m, consolidated.Open);
            Assert.AreEqual(123m, consolidated.High);
            Assert.AreEqual(1m, consolidated.Low);
            Assert.AreEqual(83m, consolidated.Close);
            Assert.AreEqual(300L, consolidated.Volume);
        }

        [Test]
        public void ConsolidatedTimeIsFromBeginningOfBar()
        {
            // verifies that the consolidated bar uses the time from the beginning of the first bar
            // in the period that covers the current bar

            var consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(1));

            TradeBar consolidated = null;
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2014, 12, 1, 10, 00, 0);

            //10:00 - new
            consolidator.Update(new TradeBar {Time = reference});
            Assert.IsNull(consolidated);

            //10:01 - aggregate/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(1)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(reference, consolidated.Time);

            //10:02 - new/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(2)});
            Assert.AreEqual(reference.AddMinutes(1), consolidated.Time);

            //10:03 - new/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(3)});
            Assert.AreEqual(reference.AddMinutes(2), consolidated.Time);

            //10:05 - new/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(5)});
            Assert.AreEqual(reference.AddMinutes(3), consolidated.Time);

            //10:08 - new/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(8)});
            Assert.AreEqual(reference.AddMinutes(5), consolidated.Time);

            //10:08:01 - new
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(8).AddSeconds(1)});
            Assert.AreEqual(reference.AddMinutes(5), consolidated.Time);

            //10:09 - new/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(9)});
            Assert.AreEqual(reference.AddMinutes(8), consolidated.Time);

        }

        [Test]
        public void HandlesDataGapsInMixedMode()
        {
            // define a three minute consolidator on a one minute stream of data
            var consolidator = new TradeBarConsolidator(3, TimeSpan.FromMinutes(3));

            TradeBar consolidated = null;
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2014, 12, 1, 10, 00, 0);

            //10:00 - new
            consolidator.Update(new TradeBar {Time = reference});
            Assert.IsNull(consolidated);

            //10:01 - aggregate
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(1)});
            Assert.IsNull(consolidated);
            
            //10:02 - fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(2)});
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(reference, consolidated.Time);

            //10:03 - new
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(3)});
            Assert.AreEqual(reference, consolidated.Time);

            //10:06 - aggregate/fire
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(6)});
            Assert.AreEqual(reference.AddMinutes(3), consolidated.Time);

            //10:08 - new/fire -- will have timestamp from 10:08, instead of 10:06
            consolidator.Update(new TradeBar {Time = reference.AddMinutes(8)});
            Assert.AreEqual(reference.AddMinutes(8), consolidated.Time);
        }

        [Test]
        public void HandlesGappingAcrossDays()
        {
            // this test requires inspection to verify we're getting clean bars on the correct times

            var consolidator = new TradeBarConsolidator(TimeSpan.FromHours(1));

            TradeBar consolidated = null;
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            // from 1/1 9:30 to 1/2 12:00 by minute
            var start = new DateTime(2014, 01, 01, 09, 30, 00, 00);
            var end =   new DateTime(2014, 01, 02, 12, 00, 00, 00);
            foreach (var bar in StreamTradeBars(start, end, TimeSpan.FromMinutes(1)))
            {
                consolidator.Update(bar);
            }
        }

        /// <summary>
        /// Testing the behaviors where, the bar range is closed on the left and open on 
        /// the right in time span mode: [T, T+TimeSpan).
        /// For example, if time span is 1 minute, we have [10:00, 10:01): so data at 
        /// 10:01 is not included in the bar starting at 10:00.
        /// </summary>
        [Test]
        public void ClosedLeftOpenRightInTimeSpanModeTest()
        {
            // define a three minute consolidator 
            int timeSpanUnits = 3;
            var consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(timeSpanUnits));

            TradeBar consolidated = null;
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var refDateTime = new DateTime(2014, 12, 1, 10, 00, 0);

            // loop for 3 times the timeSpanUnits + 1, so it would consolidate the bars 3 times
            for (int i=0; i < 3*timeSpanUnits + 1 ; ++i) 
            {
                consolidator.Update(new TradeBar { Time = refDateTime });

                if (i < timeSpanUnits)  // before initial consolidation happens
                {
                    Assert.IsNull(consolidated);
                }
                else 
                {
                    Assert.IsNotNull(consolidated);
                    if (i % timeSpanUnits == 0) // i = 3, 6, 9
                    {
                        Assert.AreEqual(refDateTime.AddMinutes(-timeSpanUnits), consolidated.Time);
                    }
                }

                refDateTime = refDateTime.AddMinutes(1);
            }
        }

        [Test]
        public void AggregatesPeriodInCountModeWithDailyData()
        {
            TradeBar consolidated = null;
            var period = TimeSpan.FromDays(1);
            var consolidator = new TradeBarConsolidator(2);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new TradeBar { Time = reference, Period = period});
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar { Time = reference.AddDays(1), Period = period });
            Assert.IsNotNull(consolidated);

            Assert.AreEqual(TimeSpan.FromDays(2), consolidated.Period);
            consolidated = null;

            consolidator.Update(new TradeBar { Time = reference.AddDays(2), Period = period });
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar { Time = reference.AddDays(3), Period = period });
            Assert.IsNotNull(consolidated);

            Assert.AreEqual(TimeSpan.FromDays(2), consolidated.Period);
        }

        [Test]
        public void AggregatesPeriodInPeriodModeWithDailyData()
        {
            TradeBar consolidated = null;
            var period = TimeSpan.FromDays(1);
            var consolidator = new TradeBarConsolidator(period);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new TradeBar { Time = reference, Period = period});
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar { Time = reference.AddDays(1), Period = period });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(period, consolidated.Period);
            consolidated = null;

            consolidator.Update(new TradeBar { Time = reference.AddDays(2), Period = period });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(period, consolidated.Period);
            consolidated = null;

            consolidator.Update(new TradeBar { Time = reference.AddDays(3), Period = period });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(period, consolidated.Period);
        }

        [Test]
        public void AggregatesPeriodInPeriodModeWithDailyDataAndRoundedTime()
        {
            TradeBar consolidated = null;
            var period = TimeSpan.FromDays(1);
            var consolidator = new TradeBarConsolidator(period);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new TradeBar { Time = reference.AddSeconds(45), Period = period });
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar { Time = reference.AddDays(1).AddMinutes(1), Period = period });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(period, consolidated.Period);
            Assert.AreEqual(reference, consolidated.Time);
            consolidated = null;

            consolidator.Update(new TradeBar { Time = reference.AddDays(2).AddHours(1), Period = period });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(period, consolidated.Period);
            Assert.AreEqual(reference.AddDays(1), consolidated.Time);
            consolidated = null;

            consolidator.Update(new TradeBar { Time = reference.AddDays(3).AddMinutes(1).AddSeconds(1), Period = period });
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(period, consolidated.Period);
            Assert.AreEqual(reference.AddDays(2), consolidated.Time);
        }

        [Test]
        public void FiresEventAfterTimePassesViaScan()
        {
            TradeBar consolidated = null;
            var period = TimeSpan.FromDays(1);
            var consolidator = new TradeBarConsolidator(period);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            consolidator.Update(new TradeBar { Time = reference.AddSeconds(45), Period = period });
            Assert.IsNull(consolidated);

            consolidator.Scan(reference + period);
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(reference, consolidated.Time);
        }

        [Test]
        public void ConsolidatedPeriodEqualsTimeBasedConsolidatorPeriod()
        {
            TradeBar consolidated = null;
            var period = TimeSpan.FromMinutes(2);
            var consolidator = new TradeBarConsolidator(period);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13, 10, 20, 0);
            var time = reference;

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneMinute });
            time = reference.Add(period);
            consolidator.Scan(time);
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(reference, consolidated.Time);
            Assert.AreEqual(period, consolidated.Period);
        }

        [Test]
        public void FiresEventAfterTimePassesViaScanWithMultipleResolutions()
        {
            TradeBar consolidated = null;
            var period = TimeSpan.FromMinutes(2);
            var consolidator = new TradeBarConsolidator(period);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13, 10, 20, 0);
            var time = reference; 

            for (int i = 0; i < 10; i++)
            {
                consolidator.Update(new TradeBar {Time = time, Period = Time.OneSecond});
                time = time.AddSeconds(1);
                consolidator.Scan(time);
                Assert.IsNull(consolidated);
            }

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneMinute });
            time = reference.Add(period);
            consolidator.Scan(time);
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(reference, consolidated.Time);
            Assert.AreEqual(period, consolidated.Period);

            consolidated = null;

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneSecond });
            time = time.AddSeconds(1);
            consolidator.Scan(time);
            Assert.IsNull(consolidated);

            time = time.AddSeconds(-1);
            consolidator.Update(new TradeBar { Time = time, Period = Time.OneMinute });
            time = time.AddMinutes(1);
            consolidator.Scan(time);
            Assert.IsNull(consolidated);

            consolidator.Update(new TradeBar { Time = time, Period = Time.OneMinute });
            time = time.AddMinutes(1);
            consolidator.Scan(time);
            Assert.IsNotNull(consolidated);
            Assert.AreEqual(reference.AddMinutes(2), consolidated.Time);
            Assert.AreEqual(period, consolidated.Period);
        }

        private readonly TimeSpan marketStop = new DateTime(2000, 1, 1, 12 + 4, 0, 0).TimeOfDay;
        private readonly TimeSpan marketStart = new DateTime(2000, 1, 1, 9, 30, 0).TimeOfDay;
        private IEnumerable<TradeBar> StreamTradeBars(DateTime start, DateTime end, TimeSpan resolution, bool skipAferMarketHours = true)
        {
            DateTime current = start;
            while (current < end)
            {
                var timeOfDay = current.TimeOfDay;
                if (skipAferMarketHours && (marketStart > timeOfDay || marketStop < timeOfDay))
                {
                    // set current to the next days market start
                    current = current.Date.AddDays(1).Add(marketStart);
                    continue;
                }
                
                // either we don't care about after market hours or it's within regular market hours
                yield return new TradeBar {Time = current};
                current = current + resolution;
            }
        }
    }
}
