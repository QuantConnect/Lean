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
 *
*/

using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class FillForwardEnumeratorTest
    {
        [Test]
        public void FillsForwardMidDay()
        {
            var dataResolution = Time.OneMinute;
            
            var reference = new DateTime(2015, 6, 25, 9, 30, 0);
            var data = Enumerable.Range(0, 2).Select(x => new TradeBar
            {
                Time = reference.AddMinutes(x*2),
                Value = x,
                Period = dataResolution
            }).ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardResolution = TimeSpan.FromMinutes(1);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(fillForwardResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution);

            // 9:31
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 9:32 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);


            // 9:33
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            
            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardFromPreMarket()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2015, 6, 25, 9, 28, 0);
            var data = new []
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution
                },
                new TradeBar
                {
                    Time = reference.AddMinutes(4),
                    Value = 1,
                    Period = dataResolution
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution);

            // 9:29
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 9:31 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 9:32 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(4), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 9:33
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(5), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardRestOfDay()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2015, 6, 25, 15, 57, 0);
            var data = Enumerable.Range(0, 1).Select(x => new TradeBar
            {
                Time = reference.AddMinutes(x*2),
                Value = x,
                Period = dataResolution
            }).ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.AddMinutes(3), dataResolution);

            // 3:58
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 3:59 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardEndOfSubscription()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2015, 6, 25, 15, 57, 0);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution
                }
            }.ToList();

            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.AddMinutes(3), dataResolution);

            // 3:58
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 3:59 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardGapBeforeEndOfSubscription()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2015, 6, 25, 15, 57, 0);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.Date.AddHours(16), dataResolution);

            // 3:58
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 3:39 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardToNextDay()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2015, 6, 25, 14, 0, 0);
            var end = reference.Date.AddDays(1).AddHours(10);
            var data = new []
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution
                },
                new TradeBar
                {
                    Time = end - dataResolution,
                    Value = 1,
                    Period = dataResolution
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, end, dataResolution);

            // 3:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 10:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(end, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void SkipsAfterMarketData()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2015, 6, 25, 14, 0, 0);
            var end = reference.Date.AddDays(1).AddHours(10);
            var data = new BaseData[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution
                }, 
                new TradeBar
                {
                    Time = reference.AddHours(3),
                    Value = 1,
                    Period = dataResolution
                },
                new TradeBar
                {
                    Time = reference.Date.AddDays(1).AddHours(10) - dataResolution,
                    Value = 2,
                    Period = dataResolution
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, end, dataResolution);

            // 3:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 6:00 - this is raw data, the FF enumerator doesn't try to perform filtering per se, just filtering on when to FF
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(4), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 10:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(end, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(2, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardDailyOnHoursInMarketHours()
        {
            var dataResolution = Time.OneDay;
            var reference = new DateTime(2015, 6, 25);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = Time.OneDay},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.AddDays(1), Period = Time.OneDay},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution);

            // 12:00am
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 10:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(10), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 11:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(11), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 12:00pm (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(12), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 1:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(13), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 2:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(14), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 3:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(15), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(16), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            // 12:00am
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            
            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardDailyMissingDays()
        {
            var dataResolution = Time.OneDay;
            var reference = new DateTime(2015, 6, 25);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = Time.OneDay},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.AddDays(5), Period = Time.OneDay},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), isExtendedMarketHours, data.Last().EndTime.AddDays(1), dataResolution);

            // 6/25
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);

            // 6/26
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);

            // 6/29
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(5), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);

            // 6/30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(6), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);

            // 7/1
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(7), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillForwardHoursAtEndOfDayByHalfHour()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2015, 6, 25, 14, 0, 0);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = dataResolution},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.Date.AddDays(1), Period = dataResolution},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var ffResolution = TimeSpan.FromMinutes(30);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution);

            // 3:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);

            // 3:30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1.5), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);

            // 4:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);

            // 12:00am
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data.Last().EndTime, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardHourlyOnMinutesBeginningOfDay()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2015, 6, 25);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = dataResolution},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.Date.AddHours(9), Period = dataResolution},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var ffResolution = TimeSpan.FromMinutes(15);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution);

            // 12:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);

            // 9:45 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(9.75), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);

            // 10:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(10), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
        }

        [Test]
        public void FillsForwardMissingDaysOnFillForwardResolutionOfAnHour()
        {
            var dataResolution = Time.OneDay;
            var reference = new DateTime(2015, 6, 23);
            var data = new BaseData[]
            {
                // tues 6/23
                new TradeBar{Value = 0, Time = reference, Period = dataResolution},
                // wed 7/1
                new TradeBar{Value = 1, Time = reference.AddDays(8), Period = dataResolution},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var ffResolution = TimeSpan.FromHours(1);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution);

            int dailyBars = 0;
            int hourlyBars = 0;
            while (fillForwardEnumerator.MoveNext())
            {
                Console.WriteLine(fillForwardEnumerator.Current.EndTime);
                if (fillForwardEnumerator.Current.Time.TimeOfDay == TimeSpan.Zero)
                {
                    dailyBars++;
                }
                else
                {
                    hourlyBars++;
                }
            }

            // we expect 7 daily bars here, beginning tues, wed, thurs, fri, mon, tues, wed
            Assert.AreEqual(7, dailyBars);

            // we expect 6 days worth of ff hourly bars at 7 bars a day
            Assert.AreEqual(42, hourlyBars);
        }
    }
}
