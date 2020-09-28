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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class FillForwardEnumeratorTests
    {
        [Test]
        // reproduces GH issue 4392 causing fill forward bars not to advance
        // the nature of the bug was rounding down in exchange tz versus data timezone
        public void GetReferenceDateIntervals_RoundDown()
        {
            var dataResolution = Time.OneDay;
            var fillForwardResolution = Time.OneMinute;

            var previous = new DateTime(2017, 7, 20, 20, 0, 0);
            var next = new DateTime(2017, 7, 22, 20, 0, 0);
            var enumerator = new List<BaseData>
            {
                new TradeBar { Time = previous, Value = 1, Period = dataResolution, Volume = 100},
                new TradeBar { Time = next, Value = 2, Period = dataResolution, Volume = 100}
            }.GetEnumerator();

            var dataTimeZone = TimeZones.Utc;
            var exchange = new ForexExchange();
            // to reproduce this bug it's important for data tz to be UTC and exchange tz NewYork.
            Assert.AreEqual(TimeZones.NewYork, exchange.TimeZone);
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(fillForwardResolution), isExtendedMarketHours, next.AddDays(1), dataResolution, dataTimeZone, previous);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(previous, fillForwardEnumerator.Current.Time);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.AreEqual(100, (fillForwardEnumerator.Current as TradeBar).Volume);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            // Time should advance!
            Assert.AreEqual(new DateTime(2017, 7, 22, 17, 1, 0), fillForwardEnumerator.Current.Time);
            Assert.AreEqual(new DateTime(2017, 7, 23, 17, 1, 0), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.AreEqual(0, (fillForwardEnumerator.Current as TradeBar).Volume);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardMidDay()
        {
            var dataResolution = Time.OneMinute;

            var reference = new DateTime(2015, 6, 25, 9, 30, 0);
            var data = Enumerable.Range(0, 2).Select(x => new TradeBar
            {
                Time = reference.AddMinutes(x*2),
                Value = x,
                Period = dataResolution,
                Volume = (x + 1) * 100
            }).ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardResolution = TimeSpan.FromMinutes(1);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(fillForwardResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 9:31
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:32 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:33
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
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
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddMinutes(4),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 9:29
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:31 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:32 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(4), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:33
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(5), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardFromPreMarketMinuteToSecond()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2011, 4, 26, 8, 39, 0);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 1,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.Date.Add(new TimeSpan(9, 30, 0)),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            const bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromSeconds(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 8:40:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:30:01 to 9:30:59 (ff)
            for (var i = 1; i < 60; i++)
            {
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.AreEqual(reference.Date.Add(new TimeSpan(9, 30, i)), fillForwardEnumerator.Current.EndTime);
                Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
                Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
                Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
                Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);
            }

            // 9:31:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.Date.Add(new TimeSpan(9, 31, 0)), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(2, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
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
                Period = dataResolution,
                Volume = 100
            }).ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.AddMinutes(3), dataResolution, exchange.TimeZone, data.First().EndTime);

            // 3:58
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 3:59 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
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
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();

            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.AddMinutes(3), dataResolution, exchange.TimeZone, data.First().EndTime);

            // 3:58
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 3:59 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
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
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.Date.AddHours(16), dataResolution, exchange.TimeZone, data.First().EndTime);

            // 3:58
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 3:39 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(3), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
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
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = end - dataResolution,
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, end, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 3:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 10:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(end, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
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
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddHours(3),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                },
                new TradeBar
                {
                    Time = reference.Date.AddDays(1).AddHours(10) - dataResolution,
                    Value = 2,
                    Period = dataResolution,
                    Volume = 300
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, end, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 3:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 6:00 - this is raw data, the FF enumerator doesn't try to perform filtering per se, just filtering on when to FF
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(4), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 10:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(end, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(2, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(300, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardDailyOnHoursInMarketHours()
        {
            var dataResolution = Time.OneDay;
            var reference = new DateTime(2015, 6, 25);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = Time.OneDay, Volume = 100},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.AddDays(1), Period = Time.OneDay, Volume = 200},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 12:00am
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 10:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(10), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 11:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(11), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 12:00pm (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(12), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 1:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(13), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 2:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(14), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 3:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(15), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1).AddHours(16), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 12:00am
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardDailyMissingDays()
        {
            var dataResolution = Time.OneDay;
            var reference = new DateTime(2015, 6, 25);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = Time.OneDay, Volume = 100},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.AddDays(5), Period = Time.OneDay, Volume = 200},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), isExtendedMarketHours, data.Last().EndTime.AddDays(1), dataResolution, exchange.TimeZone, data.First().EndTime);

            // 6/25
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 6/26
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 6/29
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(5), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 6/30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(6), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 7/1
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddDays(7), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.IsTrue(((TradeBar)fillForwardEnumerator.Current).Period == dataResolution);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillForwardHoursAtEndOfDayByHalfHour()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2015, 6, 25, 14, 0, 0);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = dataResolution, Volume = 100},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.Date.AddDays(1), Period = dataResolution, Volume = 200},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var ffResolution = TimeSpan.FromMinutes(30);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 3:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 3:30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1.5), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 4:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 12:00am
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data.Last().EndTime, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardHourlyOnMinutesBeginningOfDay()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2015, 6, 25);
            var data = new BaseData[]
            {
                // thurs 6/25
                new TradeBar{Value = 0, Time = reference, Period = dataResolution, Volume = 100},
                // fri 6/26
                new TradeBar{Value = 1, Time = reference.Date.AddHours(9), Period = dataResolution, Volume = 200},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var ffResolution = TimeSpan.FromMinutes(15);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 12:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:45 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(9.75), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 10:00
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddHours(10), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardMissingDaysOnFillForwardResolutionOfAnHour()
        {
            var dataResolution = Time.OneDay;
            var reference = new DateTime(2015, 6, 23);
            var data = new BaseData[]
            {
                // tues 6/23
                new TradeBar{Value = 0, Time = reference, Period = dataResolution, Volume = 100},
                // wed 7/1
                new TradeBar{Value = 1, Time = reference.AddDays(8), Period = dataResolution, Volume = 200},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var ffResolution = TimeSpan.FromHours(1);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

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
                    Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);
                }
            }

            // we expect 7 daily bars here, beginning tues, wed, thurs, fri, mon, tues, wed
            Assert.AreEqual(7, dailyBars);

            // we expect 6 days worth of ff hourly bars at 7 bars a day
            Assert.AreEqual(42, hourlyBars);
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void OandaFillsForwardDailyForexOnWeekends()
        {
            var dailyBarsEmitted = 0;
            var fillForwardBars = new List<BaseData>();

            // 3 QuoteBars as they would be read from the EURUSD oanda daily file by QuoteBar.Reader()
            // The conversion from dataTimeZone to exchangeTimeZone has been done by hand
            // dataTimeZone == UTC
            /*
                20120719 00:00,1.22769,1.2324,1.22286,1.22759,0,1.22781,1.23253,1.22298,1.22771,0
                20120720 00:00,1.22757,1.22823,1.21435,1.21542,0,1.22769,1.22835,1.21449,1.21592,0
                20120722 00:00,1.21542,1.21542,1.21037,1.21271,0,1.21592,1.21592,1.21087,1.21283,0
                20120723 00:00,1.21273,1.21444,1.20669,1.21238,0,1.21285,1.21454,1.20685,1.21249,0
             */
            var data = new BaseData[]
            {
                // fri 7/20
                new QuoteBar{Value = 0, Time = new DateTime(2012, 7, 19, 20, 0, 0), Period = Time.OneDay},
                // sunday 7/22
                new QuoteBar{Value = 1, Time = new DateTime(2012, 7, 21, 20, 0, 0), Period = Time.OneDay},
                // monday 7/23
                new QuoteBar{Value = 2, Time = new DateTime(2012, 7, 22, 20, 0, 0), Period = Time.OneDay},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var market = Market.Oanda;
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, market);

            var marketHours = MarketHoursDatabase.FromDataFolder();
            var exchange = new ForexExchange(marketHours.GetExchangeHours(market, symbol, SecurityType.Forex));

            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), false, data.Last().EndTime, Time.OneDay, TimeZones.Utc, data.First().EndTime);

            while (fillForwardEnumerator.MoveNext())
            {
                fillForwardBars.Add(fillForwardEnumerator.Current);
                Console.WriteLine(fillForwardEnumerator.Current.Time.DayOfWeek + " " + fillForwardEnumerator.Current.Time + " - " + fillForwardEnumerator.Current.EndTime.DayOfWeek + " " + fillForwardEnumerator.Current.EndTime);
                dailyBarsEmitted++;
            }

            Assert.AreEqual(3, dailyBarsEmitted);
            Assert.AreEqual(new DateTime(2012, 7, 19, 20, 0, 0), fillForwardBars[0].Time);
            Assert.AreEqual(new DateTime(2012, 7, 21, 20, 0, 0), fillForwardBars[1].Time);
            Assert.AreEqual(new DateTime(2012, 7, 22, 20, 0, 0), fillForwardBars[2].Time);
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void HandlesDaylightSavingTimeChange()
        {
            var dailyBarsEmitted = 0;
            var fillForwardBars = new List<BaseData>();

            // 3 QuoteBars as they would be read from the EURUSD oanda daily file by QuoteBar.Reader()
            // The conversion from dataTimeZone to exchangeTimeZone has been done by hand
            // dataTimeZone == UTC
            /*
                20180311 00:00,1.2308,1.2308,1.2308,1.2308,0,1.23096,1.23096,1.23096,1.23096,0
                20180312 00:00,1.23082,1.23449,1.22898,1.23382,0,1.23097,1.23463,1.22911,1.23396,0
            */
            var data = new BaseData[]
            {
                // Sunday 3/11
                new QuoteBar{Value = 0, Time = new DateTime(2018, 3, 10, 19, 0, 0), Period = Time.OneDay},
                // Monday 3/12
                new QuoteBar{Value = 1, Time = new DateTime(2018, 3, 11, 20, 0, 0), Period = Time.OneDay},
            }.ToList();
            var enumerator = data.GetEnumerator();

            var market = Market.Oanda;
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, market);

            var marketHours = MarketHoursDatabase.FromDataFolder();
            var exchange = new ForexExchange(marketHours.GetExchangeHours(market, symbol, SecurityType.Forex));

            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), false, data.Last().EndTime, Time.OneDay, TimeZones.Utc, data.First().EndTime);

            while (fillForwardEnumerator.MoveNext())
            {
                fillForwardBars.Add(fillForwardEnumerator.Current);
                Console.WriteLine(fillForwardEnumerator.Current.Time.DayOfWeek + " " + fillForwardEnumerator.Current.Time + " - " + fillForwardEnumerator.Current.EndTime.DayOfWeek + " " + fillForwardEnumerator.Current.EndTime);
                dailyBarsEmitted++;
            }

            Assert.AreEqual(2, dailyBarsEmitted);
            Assert.AreEqual(new DateTime(2018, 3, 10, 19, 0, 0), fillForwardBars[0].Time);
            Assert.AreEqual(new DateTime(2018, 3, 11, 20, 0, 0), fillForwardBars[1].Time);
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void HandlesDaylightSavingTimeChange_InifinteLoop()
        {
            var dailyBarsEmitted = 0;
            var fillForwardBars = new List<BaseData>();

            var data = new BaseData[]
            {
                new QuoteBar{Value = 0, Time = new DateTime(2019, 10, 4, 10, 0, 0), Period = Time.OneDay},
                new QuoteBar{Value = 1, Time = new DateTime(2019, 10, 8, 11, 0, 0), Period = Time.OneDay}
            }.ToList();
            var enumerator = data.GetEnumerator();

            var algo = new AlgorithmStub();
            var market = Market.Oanda;
            var security = algo.AddCfd("AU200AUD", Resolution.Daily, market);

            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, security.Exchange, Ref.Create(TimeSpan.FromDays(1)), false, data.Last().EndTime, Time.OneDay, TimeZones.Utc, data.First().EndTime);

            while (fillForwardEnumerator.MoveNext())
            {
                fillForwardBars.Add(fillForwardEnumerator.Current);
                Console.WriteLine(fillForwardEnumerator.Current.Time.DayOfWeek + " " + fillForwardEnumerator.Current.Time + " - " + fillForwardEnumerator.Current.EndTime.DayOfWeek + " " + fillForwardEnumerator.Current.EndTime + " " + fillForwardEnumerator.Current.IsFillForward);
                dailyBarsEmitted++;
            }

            Assert.AreEqual(4, dailyBarsEmitted);
            Assert.AreEqual(new DateTime(2019, 10, 4, 10, 0, 0), fillForwardBars[0].Time);
            Assert.AreEqual(new DateTime(2019, 10, 6, 11, 0, 0), fillForwardBars[1].Time);
            Assert.AreEqual(new DateTime(2019, 10, 7, 11, 0, 0), fillForwardBars[2].Time);
            Assert.AreEqual(new DateTime(2019, 10, 8, 11, 0, 0), fillForwardBars[3].Time);
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardAfterMarketOpen_DataSecond_FillForwardMinute()
        {
            var dataResolution = Time.OneSecond;
            var reference = new DateTime(2015, 6, 25, 9, 49, 59);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddMinutes(4),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 9:49:59 -> 9:50
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddSeconds(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:50 -> 9:50:01 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddSeconds(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardOnMarketOpen_DataSecond_FillForwardMinute()
        {
            var dataResolution = Time.OneSecond;
            var reference = new DateTime(2015, 6, 25, 9, 29, 59);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddMinutes(4),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 9:29:59 -> 9:30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddSeconds(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:30 -> 9:30:01 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddSeconds(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardOnMarketOpen_DataMinute_FillForwardSecond()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2015, 6, 25, 9, 29, 0);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddMinutes(4),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromSeconds(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 9:29 -> 9:30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:30 -> 9:30:01 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1).AddSeconds(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardOnMarketOpen_DataMinute_FillForwardMinute()
        {
            var dataResolution = Time.OneMinute;
            var reference = new DateTime(2015, 6, 25, 9, 29, 0);
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddMinutes(4),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone, data.First().EndTime);

            // 9:29 -> 9:30
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(1), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:30 -> 9:31 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(reference.AddMinutes(2), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            fillForwardEnumerator.Dispose();
        }

        [TestCase(Resolution.Second, Resolution.Second)]
        [TestCase(Resolution.Second, Resolution.Minute)]
        [TestCase(Resolution.Minute, Resolution.Second)]
        [TestCase(Resolution.Minute, Resolution.Minute)]
        [TestCase(Resolution.Minute, Resolution.Daily)]
        [TestCase(Resolution.Daily, Resolution.Minute)]
        public void FillForwardBarsForDifferentResolutions(Resolution resolution, Resolution anotherSymbolResolution)
        {
            FillForwardTestAlgorithm.FillForwardBars.Clear();

            FillForwardTestAlgorithm.Resolution = resolution;
            FillForwardTestAlgorithm.ResolutionAnotherSymbol = anotherSymbolResolution;

            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(FillForwardTestAlgorithm),
                new Dictionary<string, string>(),
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                setupHandler: "FillForwardTestSetupHandler");

            var expectedDataFile = $"ff_{resolution}_{anotherSymbolResolution}.txt";

            // updates expected data
            if (false)
            {
                QuantConnect.Compression.ZipCreateAppendData(
                    "../../TestData/FillForwardBars.zip", expectedDataFile, FillForwardTestAlgorithm.Result.Value);
            }
            QuantConnect.Compression.Unzip("TestData/FillForwardBars.zip", "./", overwrite: true);
            var expected = File.ReadAllLines(expectedDataFile);

            Assert.AreEqual(expected.Length, FillForwardTestAlgorithm.FillForwardBars.Count);
            Assert.IsTrue(expected.SequenceEqual(FillForwardTestAlgorithm.FillForwardBars));
        }

        private static TestCaseData[] SubscriptionStarts => new[] {
            new TestCaseData(new DateTime(2011, 7, 7, 20, 0, 0)), // no move
            new TestCaseData(new DateTime(2011, 11, 3, 20, 0, 0)), // move to EST
            new TestCaseData(new DateTime(2012, 3, 8, 19, 0, 0))   // move to EDT
        };

        [Test, TestCaseSource(nameof(SubscriptionStarts))]
        public void FillsForwardDaylightSavingTime(DateTime reference)
        {
            var dataResolution = Time.OneDay;
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference,
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(2),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(3),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(3),
                    Value = 3,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(4),
                    Value = 4,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new ForexExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator, 
                exchange, 
                Ref.Create(TimeSpan.FromDays(1)), 
                isExtendedMarketHours, 
                data.Last().EndTime, 
                dataResolution, 
                DateTimeZone.Utc, 
                data.First().EndTime);

            for (int i = 0; i < data.Count; i++)
            {
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.AreEqual(data[i].Time.AddDays(1), fillForwardEnumerator.Current.EndTime);
                Assert.AreEqual(i, fillForwardEnumerator.Current.Value);
                Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
                Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            }

            fillForwardEnumerator.Dispose();
        }

        internal class FillForwardTestAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
        {
            private Symbol _symbol;
            public static List<string> FillForwardBars = new List<string>();
            public static Lazy<string> Result { get; set; }
            public static Resolution Resolution { get; set; }
            public static Resolution ResolutionAnotherSymbol { get; set; }
            public override void Initialize()
            {
                SetStartDate(2013, 10, 04);
                SetEndDate(2013, 10, 07);
                AddEquity("SPY", ResolutionAnotherSymbol);
                _symbol = AddEquity("AIG", Resolution).Symbol;
            }
            public override void OnData(Slice data)
            {
                if (data.ContainsKey(_symbol))
                {
                    var tradeBar = data[_symbol] as TradeBar;
                    if (tradeBar != null && tradeBar.IsFillForward)
                    {
                        FillForwardBars.Add($"{tradeBar.EndTime:d H:m:s} {Time:d H:m:s}");
                    }
                }
            }
            public override void OnEndOfAlgorithm()
            {
                Result = new Lazy<string>(() => string.Join(Environment.NewLine, FillForwardBars));
            }

            public bool CanRunLocally { get; } = true;
            public Language[] Languages { get; } = { Language.CSharp };
            public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>();
        }

        internal class FillForwardTestSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            internal static FillForwardTestAlgorithm TestAlgorithm { get; set; }

            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = TestAlgorithm = new FillForwardTestAlgorithm();
                return Algorithm;
            }
        }
    }
}
