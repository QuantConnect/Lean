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

using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Option;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(fillForwardResolution), isExtendedMarketHours, next.AddDays(1), dataResolution, dataTimeZone);

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
                Time = reference.AddMinutes(x * 2),
                Value = x,
                Period = dataResolution,
                Volume = (x + 1) * 100
            }).ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardResolution = TimeSpan.FromMinutes(1);
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(fillForwardResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromSeconds(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
                Time = reference.AddMinutes(x * 2),
                Value = x,
                Period = dataResolution,
                Volume = 100
            }).ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.AddMinutes(3), dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.AddMinutes(3), dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, reference.Date.AddHours(16), dataResolution, exchange.TimeZone);

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
                    Time = end - dataResolution,
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            bool isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, end, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, end, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromHours(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), isExtendedMarketHours, data.Last().EndTime.AddDays(1), dataResolution, exchange.TimeZone);

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
        public void FillsForwardHoursAtEndOfDayByHalfHour()
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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(ffResolution), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

            int dailyBars = 0;
            int hourlyBars = 0;
            while (fillForwardEnumerator.MoveNext())
            {
                Log.Trace(fillForwardEnumerator.Current.EndTime.ToStringInvariant());
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

            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), false, data.Last().EndTime, Time.OneDay, TimeZones.Utc);

            while (fillForwardEnumerator.MoveNext())
            {
                fillForwardBars.Add(fillForwardEnumerator.Current);
                Log.Trace(fillForwardEnumerator.Current.Time.DayOfWeek + " " + fillForwardEnumerator.Current.Time + " - " + fillForwardEnumerator.Current.EndTime.DayOfWeek + " " + fillForwardEnumerator.Current.EndTime);
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

            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromDays(1)), false, data.Last().EndTime, Time.OneDay, TimeZones.Utc);

            while (fillForwardEnumerator.MoveNext())
            {
                fillForwardBars.Add(fillForwardEnumerator.Current);
                Log.Trace(fillForwardEnumerator.Current.Time.DayOfWeek + " " + fillForwardEnumerator.Current.Time + " - " + fillForwardEnumerator.Current.EndTime.DayOfWeek + " " + fillForwardEnumerator.Current.EndTime);
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

            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, security.Exchange, Ref.Create(TimeSpan.FromDays(1)), false, data.Last().EndTime, Time.OneDay, TimeZones.Utc);

            while (fillForwardEnumerator.MoveNext())
            {
                fillForwardBars.Add(fillForwardEnumerator.Current);
                Log.Trace(fillForwardEnumerator.Current.Time.DayOfWeek + " " + fillForwardEnumerator.Current.Time + " - " + fillForwardEnumerator.Current.EndTime.DayOfWeek + " " + fillForwardEnumerator.Current.EndTime + " " + fillForwardEnumerator.Current.IsFillForward);
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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromSeconds(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

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
        public void FillsForwardBarsForDifferentResolutions(Resolution resolution, Resolution anotherSymbolResolution)
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
            new TestCaseData(new DateTime(2011, 1, 21, 0, 0, 0), new ForexExchange()),  // no move
            new TestCaseData(new DateTime(2011, 3, 11, 0, 0, 0), new ForexExchange()),   // move to EDT
            new TestCaseData(new DateTime(2011, 7, 8, 0, 0, 0), new ForexExchange()),  // no move
            new TestCaseData(new DateTime(2011, 11, 4, 0, 0, 0), new ForexExchange()), // move to EST

            new TestCaseData(new DateTime(2011, 1, 21, 0, 0, 0), new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex))),  // no move
            new TestCaseData(new DateTime(2011, 3, 11, 0, 0, 0), new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex))),   // move to EDT
            new TestCaseData(new DateTime(2011, 7, 8, 0, 0, 0), new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex))),  // no move
            new TestCaseData(new DateTime(2011, 11, 4, 0, 0, 0), new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex))), // move to EST

            new TestCaseData(new DateTime(2011, 1, 21, 0, 0, 0), new EquityExchange()),  // no move
            new TestCaseData(new DateTime(2011, 3, 11, 0, 0, 0), new EquityExchange()),   // move to EDT
            new TestCaseData(new DateTime(2011, 7, 8, 0, 0, 0),  new EquityExchange()),  // no move
            new TestCaseData(new DateTime(2011, 11, 4, 0, 0, 0), new EquityExchange()), // move to EST
        };

        private static IEnumerable<TestCaseData> DaylightSavingCases(int offsetInHours)
        {
            return SubscriptionStarts.Select(origin =>
            {
                var list = new List<object>(origin.Arguments) { DateTimeZone.ForOffset(Offset.FromHours(offsetInHours)) };

                return new TestCaseData(list.ToArray());
            });
        }

        [Test]
        [TestCaseSource(nameof(DaylightSavingCases), new object[] { -5 })]
        [TestCaseSource(nameof(DaylightSavingCases), new object[] { 0 })]
        [TestCaseSource(nameof(DaylightSavingCases), new object[] { -3 })]
        public void FillsForwardDaylightSavingTime(DateTime reference, SecurityExchange exchange, DateTimeZone dataTimeZone)
        {
            var dataResolution = Time.OneDay;
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference.ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(2).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(3).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(TimeSpan.FromDays(1)),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[0].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[1].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[2].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            fillForwardEnumerator.Dispose();
        }

        [Test, TestCaseSource(nameof(SubscriptionStarts))]
        public void FillsForwardDaylightSavingTimeUtcPlus5(DateTime reference, SecurityExchange exchange)
        {
            var dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(+5));
            var dataResolution = Time.OneDay;
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference.ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(2).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(3).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(TimeSpan.FromDays(1)),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[0].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[0].Time.AddTicks(2 * dataResolution.Ticks), fillForwardEnumerator.Current.EndTime);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[1].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[2].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            fillForwardEnumerator.Dispose();
        }

        private static TestCaseData[] NoMoveSubscriptionStarts => new[] {
            new TestCaseData(new DateTime(2011, 7, 4, 0, 0, 0), new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5))),  // no move
            new TestCaseData(new DateTime(2011, 1, 17, 0, 0, 0), new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5))),  // no move
            new TestCaseData(new DateTime(2011, 7, 4, 0, 0, 0), new EquityExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5))),  // no move
            new TestCaseData(new DateTime(2011, 1, 17, 0, 0, 0), new EquityExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5))),  // no move

            new TestCaseData(new DateTime(2011, 7, 4, 0, 0, 0), new ForexExchange(), DateTimeZone.Utc),  // no move
            new TestCaseData(new DateTime(2011, 1, 17, 0, 0, 0), new ForexExchange(), DateTimeZone.Utc),  // no move
            new TestCaseData(new DateTime(2011, 7, 4, 0, 0, 0), new EquityExchange(), DateTimeZone.Utc),  // no move
            new TestCaseData(new DateTime(2011, 1, 17, 0, 0, 0), new EquityExchange(), DateTimeZone.Utc),  // no move

            new TestCaseData(new DateTime(2011, 7, 4, 0, 0, 0), new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(+5))),  // no move
            new TestCaseData(new DateTime(2011, 1, 17, 0, 0, 0), new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(+5))),  // no move
            new TestCaseData(new DateTime(2011, 7, 4, 0, 0, 0), new EquityExchange(), DateTimeZone.ForOffset(Offset.FromHours(+5))),  // no move
            new TestCaseData(new DateTime(2011, 1, 17, 0, 0, 0), new EquityExchange(), DateTimeZone.ForOffset(Offset.FromHours(+5)))  // no move
        };

        [Test, TestCaseSource(nameof(NoMoveSubscriptionStarts))]
        public void FillsForwardMiddleWeek(DateTime reference, SecurityExchange exchange, DateTimeZone dataTimeZone)
        {
            var dataResolution = Time.OneDay;
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference.ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(2).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(3).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(TimeSpan.FromDays(1)),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[0].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[0].Time.AddTicks(2 * dataResolution.Ticks), fillForwardEnumerator.Current.EndTime);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[1].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[2].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardFromPreMarketWhenDaylightMove()
        {
            var dataResolution = Time.OneMinute;
            var data = new[]
            {
                new TradeBar
                {
                    Time = new DateTime(2008, 3, 7, 16, 20, 0),
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = new DateTime(2008, 3, 10, 8, 33, 0),
                    Value = 1,
                    Period = dataResolution,
                    Volume = 200
                },
                new TradeBar
                {
                    Time = new DateTime(2008, 3, 10, 9, 28, 0),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 300
                },
                new TradeBar
                {
                    Time = new DateTime(2008, 3, 10, 9, 32, 0),
                    Value = 3,
                    Period = dataResolution,
                    Volume = 400
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var exchange = new EquityExchange();
            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(enumerator, exchange, Ref.Create(TimeSpan.FromMinutes(1)), isExtendedMarketHours, data.Last().EndTime, dataResolution, exchange.TimeZone);

            // 2008-03-07 16:50
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[0].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(0, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(100, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 2008-03-10 08:33 (pre-market)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[1].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(1, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(200, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 2008-03-10 09:28 (pre-market)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[2].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(2, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(300, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:30 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(new DateTime(2008, 3, 10, 9, 31, 0), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(2, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:31 (ff)
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(new DateTime(2008, 3, 10, 9, 32, 0), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(2, fillForwardEnumerator.Current.Value);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(0, ((TradeBar)fillForwardEnumerator.Current).Volume);

            // 9:32
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(data[3].Time.Add(dataResolution), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(3, fillForwardEnumerator.Current.Value);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(dataResolution, fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            Assert.AreEqual(400, ((TradeBar)fillForwardEnumerator.Current).Volume);

            Assert.IsFalse(fillForwardEnumerator.MoveNext());
            fillForwardEnumerator.Dispose();
        }

        private static TestCaseData[] ExchangeSet => new[] {
            new TestCaseData(new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5)), Resolution.Minute),
            new TestCaseData(new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5)), Resolution.Hour),
            new TestCaseData(new ForexExchange(), DateTimeZone.ForOffset(Offset.FromHours(-5)), Resolution.Daily),

            new TestCaseData(new EquityExchange(), TimeZones.NewYork, Resolution.Minute),
            new TestCaseData(new EquityExchange(), TimeZones.NewYork, Resolution.Hour),
            new TestCaseData(new EquityExchange(), TimeZones.NewYork, Resolution.Daily),

            new TestCaseData(new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex)), DateTimeZone.Utc, Resolution.Minute),
            new TestCaseData(new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex)), DateTimeZone.Utc, Resolution.Hour),
            new TestCaseData(new SecurityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex)), DateTimeZone.Utc, Resolution.Daily)
        };

        private static IEnumerable<TestCaseData> ExchangeSettings(string daylight, DateTime start, params object[] extra)
        {
            return ExchangeSet.Select(origin =>
            {
                var list = new List<object>(origin.Arguments)
                {
                    daylight,
                    start
                };

                if (extra?.Any() == true)
                {
                    list.AddRange(extra);
                }

                return new TestCaseData(list.ToArray());
            });
        }

        private static IEnumerable<TestCaseData> ExchangeDaylightTimeSet(int durationInDays, Resolution fillforwardResolution)
        {
            return ExchangeSettings("DST", new DateTime(2011, 3, 7), durationInDays, fillforwardResolution);
        }

        private static IEnumerable<TestCaseData> ExchangeStandardTimeSet(int durationInDays, Resolution fillforwardResolution)
        {
            return ExchangeSettings("ST", new DateTime(2011, 10, 31), durationInDays, fillforwardResolution);
        }

        [Test]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 6, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 7, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 14, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 6, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 7, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 14, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 7, Resolution.Minute })]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 7, Resolution.Hour })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 7, Resolution.Minute })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 7, Resolution.Hour })]
        public void FillsForwardBarsAroundDaylightMovementForDifferentResolutions_Enumerator(SecurityExchange exchange, DateTimeZone dataTimeZone, Resolution resolution, string dst, DateTime reference, int durationInDays, Resolution fillforwardResolution)
        {
            var data = new[]
            {
                new TradeBar
                {
                    Time = reference.ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 0,
                    Period = resolution.ToTimeSpan(),
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(durationInDays).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 1,
                    Period = resolution.ToTimeSpan(),
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var isExtendedMarketHours = false;
            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(fillforwardResolution.ToTimeSpan()),
                isExtendedMarketHours,
                data.Last().EndTime,
                resolution.ToTimeSpan(),
                dataTimeZone);

            var ffbars = new List<string>();
            while (fillForwardEnumerator.MoveNext())
            {
                if (fillForwardEnumerator.Current?.IsFillForward == true)
                {
                    var bar = fillForwardEnumerator.Current;
                    ffbars.Add($"{bar.Time:yyyy.MM.dd H:m:s} - {bar.EndTime:yyyy.MM.dd H:m:s}");
                }
            }

            fillForwardEnumerator.Dispose();

            var expectedDataFile = $"enum_{dst}_{durationInDays}_{exchange.TimeZone.Id.Replace("/", "_")}_{dataTimeZone.Id.Replace("/", "_")}_{resolution}_{fillforwardResolution}.txt";

            // updates expected data
            if (false)
            {
                QuantConnect.Compression.ZipCreateAppendData(
                    "../../TestData/FillForwardBars.zip",
                    expectedDataFile,
                    string.Join(Environment.NewLine, ffbars),
                    overrideEntry: true);
            }
            QuantConnect.Compression.Unzip("TestData/FillForwardBars.zip", "./", overwrite: true);
            var expected = File.ReadAllLines(expectedDataFile);

            Assert.AreEqual(expected.Length, ffbars.Count);
            Assert.IsTrue(expected.SequenceEqual(ffbars));
        }

        [TestCase(15)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(21)]
        public void FillsForwardUntilDelisted(int warningDay)
        {
            var exchange = new OptionExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            DateTimeZone dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-5));
            var reference = new DateTime(2014, 6, 5)
                .ConvertTo(dataTimeZone, exchange.TimeZone);
            var dataResolution = Time.OneDay;
            var expiry = new DateTime(2014, 6, warningDay)
                .ConvertTo(dataTimeZone, exchange.TimeZone);
            var delisted = new DateTime(2014, 6, 22)
                .ConvertTo(dataTimeZone, exchange.TimeZone);

            var spy = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry);

            var data = new BaseData[]
            {
                new BaseDataCollection(
                    reference,
                    reference.Add(dataResolution),
                    spy,
                    new List<BaseData>{new TradeBar
                        {
                            Time = reference,
                            Value = 1,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new BaseDataCollection(
                    reference.AddDays(1),
                    reference.AddDays(1).Add(dataResolution),
                    spy,
                    new List<BaseData>{new TradeBar
                        {
                            Time = reference.AddDays(1),
                            Value = 2,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new Delisting(spy, expiry.Date.ConvertTo(dataTimeZone, exchange.TimeZone), 100, DelistingType.Warning),
            }.ToList();

            // add intermediate values between warning and delisted
            int intermediateDay = (delisted.Day - expiry.Day) / 2;
            if (intermediateDay > 0)
            {
                data.Add(new BaseDataCollection(
                    expiry.AddDays(intermediateDay),
                    expiry.AddDays(intermediateDay).Add(dataResolution),
                    spy,
                    new List<BaseData>
                    {
                        new TradeBar
                        {
                            Time = expiry.AddDays(intermediateDay),
                            Value = 1,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }));
            }

            // add delisted
            data.Add(new Delisting(spy, delisted, 100, DelistingType.Delisted));
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(TimeSpan.FromDays(1)),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());  // 2014.06.05
            Assert.IsTrue(fillForwardEnumerator.MoveNext());  // 2014.06.06

            var counter = 0;
            var previous = fillForwardEnumerator.Current;
            while (fillForwardEnumerator.MoveNext())
            {
                Assert.NotNull(fillForwardEnumerator.Current);
                Assert.GreaterOrEqual(fillForwardEnumerator.Current.Time, previous?.Time);
                Assert.GreaterOrEqual(fillForwardEnumerator.Current.EndTime, previous?.EndTime);
                Assert.AreEqual(
                    fillForwardEnumerator.Current.DataType != MarketDataType.Auxiliary,
                    fillForwardEnumerator.Current.IsFillForward || (intermediateDay != 0 && fillForwardEnumerator.Current.Time.Day == expiry.Day + intermediateDay));
                if (fillForwardEnumerator.Current.IsFillForward)
                {
                    Assert.AreNotEqual(MarketDataType.Auxiliary, fillForwardEnumerator.Current.DataType);
                    counter++;
                }
                else
                {
                    Assert.True(fillForwardEnumerator.Current.DataType == MarketDataType.Auxiliary
                        || fillForwardEnumerator.Current.Time == data[3].Time);
                }
                previous = fillForwardEnumerator.Current;
            }

            Assert.AreEqual(
                (int)(data.Last().EndTime - data[1].EndTime).TotalDays - (intermediateDay > 0 ? 1 : 0),
                counter);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardUntilDelistedMinuteResolution()
        {
            var exchange = new OptionExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            DateTimeZone dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-5));
            var reference = new DateTime(2014, 6, 5, 10, 10, 0)
                .ConvertTo(dataTimeZone, exchange.TimeZone);
            var dataResolution = Time.OneMinute;
            var expiry = new DateTime(2014, 6, 15)
                .ConvertTo(dataTimeZone, exchange.TimeZone);
            var delisted = new DateTime(2014, 6, 22)
                .ConvertTo(dataTimeZone, exchange.TimeZone);

            var spy = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry);

            var data = new BaseData[]
            {
                new BaseDataCollection(
                    reference,
                    reference.Add(dataResolution),
                    spy,
                    new List<BaseData>{new TradeBar
                        {
                            Time = reference,
                            Value = 1,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new BaseDataCollection(
                    reference.AddDays(1),
                    reference.AddDays(1).Add(dataResolution),
                    spy,
                    new List<BaseData>{new TradeBar
                        {
                            Time = reference.AddDays(1),
                            Value = 2,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new Delisting(spy, expiry.Date, 100, DelistingType.Warning),
                new BaseDataCollection(
                    reference.AddDays(12),
                    reference.AddDays(12).Add(dataResolution),
                    spy,
                    new List<BaseData>
                    {
                        new TradeBar
                        {
                            Time = reference.AddDays(12),
                            Value = 1,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new Delisting(spy, delisted, 100, DelistingType.Delisted)
            }.ToList();

            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(TimeSpan.FromDays(1)),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            // Fast forward 2014.06.05 - 06
            while (fillForwardEnumerator.MoveNext())
            {
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                if (fillForwardEnumerator.Current.Time.Day == 7)
                {
                    break;
                }
            }

            var dateSet = new HashSet<DateTime>();
            while (fillForwardEnumerator.MoveNext())
            {
                Assert.NotNull(fillForwardEnumerator.Current);
                if (fillForwardEnumerator.Current.IsFillForward)
                {
                    Assert.AreNotEqual(MarketDataType.Auxiliary, fillForwardEnumerator.Current.DataType);
                    dateSet.Add(fillForwardEnumerator.Current.Time.Date);
                }
                else
                {
                    Assert.True(fillForwardEnumerator.Current.DataType == MarketDataType.Auxiliary
                        || fillForwardEnumerator.Current.Time == data[3].Time);
                }
            }

            // '+1' means receiving not-Auxiliary minute data on last day of period
            Assert.AreEqual(
                (int)(data.Last().EndTime - data[1].EndTime).TotalDays + 1,
                dateSet.Count);

            fillForwardEnumerator.Dispose();
        }

        [Test]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 6, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 7, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeDaylightTimeSet), new object[] { 14, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 6, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 7, Resolution.Daily })]
        [TestCaseSource(nameof(ExchangeStandardTimeSet), new object[] { 14, Resolution.Daily })]
        public void FillsForwardBarsAroundDaylightMovementForDifferentResolutions_Algorithm(SecurityExchange exchange, DateTimeZone dataTimeZone, Resolution resolution, string dst, DateTime reference, int durationInDays, Resolution ffResolution)
        {
            MarketHoursDatabase MarketHours = MarketHoursDatabase.FromDataFolder();
            MarketHours.SetEntry(
                Market.FXCM,
                "EURUSD",
                SecurityType.Forex,
                exchange.Hours,
                dataTimeZone);
            FillForwardDaylightMovementTestAlgorithm.FillForwardBars.Clear();
            FillForwardDaylightMovementTestAlgorithm.Resolution = resolution;
            FillForwardDaylightMovementTestAlgorithm.RefDateTime = reference;
            FillForwardDaylightMovementTestAlgorithm.DurationInDays = durationInDays;

            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(FillForwardDaylightMovementTestAlgorithm),
                new Dictionary<string, string>(),
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                setupHandler: "FillForwardDaylightMovementTestSetupHandler");

            var expectedDataFile = $"alg_{dst}_{durationInDays}_{exchange.TimeZone.Id.Replace("/", "_")}_{dataTimeZone.Id.Replace("/", "_")}_{resolution}.txt";

            // updates expected data
            if (false)
            {
                QuantConnect.Compression.ZipCreateAppendData(
                    "../../TestData/FillForwardBars.zip",
                    expectedDataFile,
                    string.Join(Environment.NewLine, FillForwardDaylightMovementTestAlgorithm.Result.Value),
                    overrideEntry: true);
            }
            QuantConnect.Compression.Unzip("TestData/FillForwardBars.zip", "./", overwrite: true);
            var expected = File.ReadAllLines(expectedDataFile);

            Assert.AreEqual(expected.Length, FillForwardDaylightMovementTestAlgorithm.FillForwardBars.Count);
            Assert.IsTrue(expected.SequenceEqual(FillForwardDaylightMovementTestAlgorithm.FillForwardBars));
        }

        [Test]
        public void SkipFF2AMOfSundayDST()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2011, 3, 12);
            var dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-5));
            var exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));

            var data = new[]
            {
                new TradeBar
                {
                    Time = reference.ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(2).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(dataResolution),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            int count = 0;
            while (fillForwardEnumerator.MoveNext())
            {
                if (fillForwardEnumerator.Current?.IsFillForward == true)
                {
                    if (fillForwardEnumerator.Current.Time.DayOfWeek == DayOfWeek.Sunday &&
                        fillForwardEnumerator.Current.Time.Hour == 2)
                    {
                        Assert.Fail("Shouldn't fill forward bar of 1AM of Sunday when changed Daylight Saving Time.");
                    }
                }

                count++;
            }

            Assert.Greater(count, 0);
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForward2AMOfSundayST()
        {
            var dataResolution = Time.OneHour;
            var reference = new DateTime(2011, 11, 5);
            var dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-5));
            var exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));

            var data = new[]
            {
                new TradeBar
                {
                    Time = reference.ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 0,
                    Period = dataResolution,
                    Volume = 100
                },
                new TradeBar
                {
                    Time = reference.AddDays(2).ConvertTo(dataTimeZone, exchange.TimeZone),
                    Value = 2,
                    Period = dataResolution,
                    Volume = 100
                }
            }.ToList();
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(dataResolution),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            int count = 0;
            while (fillForwardEnumerator.MoveNext())
            {
                if (fillForwardEnumerator.Current?.IsFillForward == true)
                {
                    if (fillForwardEnumerator.Current.Time.DayOfWeek == DayOfWeek.Sunday &&
                        fillForwardEnumerator.Current.Time.Hour == 2)
                    {
                        count++;
                    }
                }
            }

            Assert.AreEqual(1, count);
            fillForwardEnumerator.Dispose();
        }

        [Test]
        public void FillsForwardNotDelistingAuxiliary()
        {
            var exchange = new OptionExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            DateTimeZone dataTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-5));
            var reference = new DateTime(2014, 6, 5)
                .ConvertTo(dataTimeZone, exchange.TimeZone);
            var dataResolution = Time.OneDay;
            var expiry = new DateTime(2014, 6, 15)
                .ConvertTo(dataTimeZone, exchange.TimeZone);

            var spy = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 2, expiry);

            var data = new BaseData[]
            {
                new BaseDataCollection(
                    reference,
                    reference.Add(dataResolution),
                    spy,
                    new List<BaseData>{new TradeBar
                        {
                            Time = reference,
                            Value = 1,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new BaseDataCollection(
                    reference.AddDays(1),
                    reference.AddDays(1).Add(dataResolution),
                    spy,
                    new List<BaseData>{new TradeBar
                        {
                            Time = reference.AddDays(1),
                            Value = 2,
                            Period = dataResolution,
                            Volume = 100
                        }
                    }),
                new Dividend
                {
                    DataType = MarketDataType.Auxiliary,
                    Distribution = 0.5m,
                    ReferencePrice = decimal.MaxValue - 10000m,

                    Symbol = spy,
                    Time = reference.AddDays(5),
                    Value = 0.5m
                }
            }.ToList();

            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new FillForwardEnumerator(
                enumerator,
                exchange,
                Ref.Create(TimeSpan.FromDays(1)),
                false,
                data.Last().EndTime,
                dataResolution,
                dataTimeZone);

            Assert.IsTrue(fillForwardEnumerator.MoveNext());  // 2014.06.05
            Assert.IsTrue(fillForwardEnumerator.MoveNext());  // 2014.06.06

            var counter = 0;
            var previous = fillForwardEnumerator.Current;
            while (fillForwardEnumerator.MoveNext())
            {
                Assert.NotNull(fillForwardEnumerator.Current);
                Assert.GreaterOrEqual(fillForwardEnumerator.Current.Time, previous?.Time ?? DateTime.MinValue);
                Assert.GreaterOrEqual(fillForwardEnumerator.Current.EndTime, previous?.EndTime ?? DateTime.MinValue);
                Assert.AreEqual(
                    fillForwardEnumerator.Current.DataType != MarketDataType.Auxiliary,
                    fillForwardEnumerator.Current.IsFillForward);
                if (fillForwardEnumerator.Current.IsFillForward)
                {
                    counter++;
                }

                previous = fillForwardEnumerator.Current;
            }

            Assert.AreEqual(
                (int)(data.Last().EndTime - data[1].EndTime).TotalDays,
                counter);

            fillForwardEnumerator.Dispose();
        }

        public class FillForwardTestAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
        {
            protected Symbol _symbol;
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

        public class FillForwardDaylightMovementTestAlgorithm : FillForwardTestAlgorithm
        {
            public static DateTime RefDateTime { get; set; }
            public static int DurationInDays { get; set; }

            public override void Initialize()
            {
                SetStartDate(RefDateTime);
                SetEndDate(RefDateTime.AddDays(DurationInDays));
                _symbol = AddForex("EURUSD", Resolution, market: Market.FXCM).Symbol;
            }

            public override void OnData(Slice data)
            {
                if (data.ContainsKey(_symbol))
                {
                    var bar = data[_symbol] as QuoteBar;
                    if (bar != null && bar.IsFillForward)
                    {
                        FillForwardBars.Add($"{bar.Time:yyyy.MM.dd H:m:s} - {bar.EndTime:yyyy.MM.dd H:m:s}");
                    }
                }
            }
        }

        public class FillForwardTestSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            internal static FillForwardTestAlgorithm TestAlgorithm { get; set; }

            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = TestAlgorithm = new FillForwardTestAlgorithm();
                return Algorithm;
            }
        }

        public class FillForwardDaylightMovementTestSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            internal static FillForwardTestAlgorithm TestAlgorithm { get; set; }

            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = TestAlgorithm = new FillForwardDaylightMovementTestAlgorithm();
                return Algorithm;
            }
        }
    }
}
