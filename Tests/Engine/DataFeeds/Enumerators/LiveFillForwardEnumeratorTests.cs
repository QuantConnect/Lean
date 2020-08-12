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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveFillForwardEnumeratorTests
    {
        [Test]
        public void FillsForwardOnNulls()
        {
            var reference = new DateTime(2015, 10, 08);
            var period = Time.OneSecond;
            var underlying = new List<BaseData>
            {
                // 0 seconds
                new TradeBar(reference, Symbols.SPY, 10, 20, 5, 15, 123456, period),
                // 1 seconds
                null,
                // 3 seconds
                new TradeBar(reference.AddSeconds(2), Symbols.SPY, 100, 200, 50, 150, 1234560, period),
                null,
                null,
                null,
                null
            };

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(reference);
            var exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            var fillForward = new LiveFillForwardEnumerator(timeProvider, underlying.GetEnumerator(), exchange, Ref.Create(Time.OneSecond), false, Time.EndOfTime, Time.OneSecond, exchange.TimeZone, Time.BeginningOfTime);

            // first point is always emitted
            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[0], fillForward.Current);
            Assert.AreEqual(123456, ((TradeBar)fillForward.Current).Volume);

            // stepping again without advancing time does nothing, but we'll still
            // return true as per IEnumerator contract
            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsNull(fillForward.Current);

            timeProvider.SetCurrentTime(reference.AddSeconds(1));

            // non-null next will fill forward in between
            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[0].EndTime, fillForward.Current.Time);
            Assert.AreEqual(underlying[0].Value, fillForward.Current.Value);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            // even without stepping the time this will advance since non-null data is ready
            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2], fillForward.Current);
            Assert.AreEqual(1234560, ((TradeBar)fillForward.Current).Volume);

            // step ahead into null data territory
            timeProvider.SetCurrentTime(reference.AddSeconds(4));

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsNull(fillForward.Current);

            timeProvider.SetCurrentTime(reference.AddSeconds(5));

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            timeProvider.SetCurrentTime(reference.AddSeconds(6));

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            fillForward.Dispose();
        }

        [Test]
        public void HandlesDaylightSavingTimeChange()
        {
            var reference = new DateTime(2018, 3, 10);
            var period = Time.OneDay;
            var underlying = new List<TradeBar>
            {
                new TradeBar(reference, Symbols.SPY, 10, 20, 5, 15, 123456, period),
                // Daylight Saving Time change -> add 1 hour
                new TradeBar(reference.AddDays(1).AddHours(1), Symbols.SPY, 100, 200, 50, 150, 1234560, period)
            };

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(reference);
            var exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            var fillForward = new LiveFillForwardEnumerator(
                timeProvider,
                underlying.GetEnumerator(),
                exchange,
                Ref.Create(Time.OneDay),
                false,
                Time.EndOfTime,
                Time.OneDay,
                exchange.TimeZone,
                Time.BeginningOfTime);

            // first point is always emitted
            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsFalse(fillForward.Current.IsFillForward);
            Assert.AreEqual(underlying[0], fillForward.Current);
            //Assert.AreEqual(underlying[0].EndTime, fillForward.Current.EndTime);
            Assert.AreEqual(123456, ((TradeBar)fillForward.Current).Volume);

            // Daylight Saving Time change -> add 1 hour
            timeProvider.SetCurrentTime(reference.AddDays(1).AddHours(1));

            // second data point emitted
            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsFalse(fillForward.Current.IsFillForward);
            Assert.AreEqual(underlying[1], fillForward.Current);
            //Assert.AreEqual(underlying[1].EndTime, fillForward.Current.EndTime);
            Assert.AreEqual(1234560, ((TradeBar)fillForward.Current).Volume);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(underlying[1].EndTime, fillForward.Current.Time);
            Assert.AreEqual(underlying[1].Value, fillForward.Current.Value);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            fillForward.Dispose();
        }

        [Test]
        public void LiveFillForwardEnumeratorDoesNotStall()
        {
            var now = DateTime.UtcNow;
            var timeProvider = new ManualTimeProvider(new DateTime(2020, 5, 21, 9, 40, 0, 100), TimeZones.NewYork);

            var enqueueableEnumerator = new EnqueueableEnumerator<BaseData>();
            var fillForwardEnumerator = new LiveFillForwardEnumerator(
                timeProvider,
                enqueueableEnumerator,
                new SecurityExchange(MarketHoursDatabase.FromDataFolder()
                    .ExchangeHoursListing
                    .First(kvp => kvp.Key.Market == Market.USA && kvp.Key.SecurityType == SecurityType.Equity)
                    .Value
                    .ExchangeHours),
                Ref.CreateReadOnly(() => Resolution.Minute.ToTimeSpan()),
                false,
                now,
                Resolution.Minute.ToTimeSpan(),
                TimeZones.NewYork,
                new DateTime(2020, 5, 21)
            );
            var openingBar = new TradeBar
            {
                Open = 0.01m,
                High = 0.01m,
                Low = 0.01m,
                Close = 0.01m,
                Volume = 1,
                EndTime = new DateTime(2020, 5, 21, 9, 40, 0),
                Symbol = Symbols.AAPL
            };
            var secondBar = new TradeBar
            {
                Open = 1m,
                High = 2m,
                Low = 1m,
                Close = 2m,
                Volume = 100,
                EndTime = new DateTime(2020, 5, 21, 9, 42, 0),
                Symbol = Symbols.AAPL
            };


            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // Advance the time, we expect a fill-forward bar.
            timeProvider.SetCurrentTime(new DateTime(2020, 5, 21, 9, 41, 0, 100));
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime.AddMinutes(1), fillForwardEnumerator.Current.EndTime);

            // Now we expect data. The secondBar should be fill-forwarded from here on out after the MoveNext
            timeProvider.SetCurrentTime(new DateTime(2020, 5, 21, 9, 42, 0, 100));
            enqueueableEnumerator.Enqueue(secondBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
        }
    }
}