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
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

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
            var fillForward = new LiveFillForwardEnumerator(timeProvider, underlying.GetEnumerator(), exchange, Ref.Create(Time.OneSecond), false, Time.EndOfTime, Resolution.Second, exchange.TimeZone, false);

            // first point is always emitted
            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[0], fillForward.Current);
            Assert.AreEqual(123456, ((TradeBar)fillForward.Current).Volume);

            // stepping again without advancing time does nothing, but we'll still
            // return true as per IEnumerator contract
            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsNull(fillForward.Current);

            timeProvider.SetCurrentTime(reference.AddSeconds(2));

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

            // wont FF yet cause it will wait till the expected timeout
            timeProvider.SetCurrentTime(reference.AddSeconds(4));
            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsNull(fillForward.Current);

            var expectedTimeout = LiveFillForwardEnumerator.GetMaximumDataTimeout(Resolution.Second);

            // step ahead into null data territory
            timeProvider.SetCurrentTime(reference.AddSeconds(4) + expectedTimeout);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork).RoundDown(Time.OneSecond), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsNull(fillForward.Current);

            timeProvider.SetCurrentTime(reference.AddSeconds(5) + expectedTimeout);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork).RoundDown(Time.OneSecond), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            timeProvider.SetCurrentTime(reference.AddSeconds(6) + expectedTimeout);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork).RoundDown(Time.OneSecond), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            fillForward.Dispose();
        }

        [Test]
        public void HandlesDaylightSavingTimeChange()
        {
            // In 2018, Daylight Saving Time (DST) began at 2 AM on Sunday, March 11
            // This means that clocks were moved forward one hour on March 11
            var reference = new DateTime(2018, 3, 10);
            var period = Time.OneDay;
            var underlying = new List<TradeBar>
            {
                new TradeBar(reference, Symbols.SPY, 10, 20, 5, 15, 123456, period),
                // Daylight Saving Time change, the data still goes from midnight to midnight
                new TradeBar(reference.AddDays(1), Symbols.SPY, 100, 200, 50, 150, 1234560, period)
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
                Resolution.Daily,
                exchange.TimeZone, false);

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
            var timeProvider = new ManualTimeProvider(new DateTime(2020, 5, 21, 9, 40, 0, 100), TimeZones.NewYork);

            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, Resolution.Minute, out var enqueueableEnumerator, false);
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
            timeProvider.SetCurrentTime(new DateTime(2020, 5, 21, 9, 41, 0, 100) + LiveFillForwardEnumerator.GetMaximumDataTimeout(Resolution.Minute));
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

        [TestCase(Resolution.Hour, true)]
        [TestCase(Resolution.Minute, true)]
        [TestCase(Resolution.Second, true)]
        [TestCase(Resolution.Hour, false)]
        [TestCase(Resolution.Minute, false)]
        [TestCase(Resolution.Second, false)]
        public void TakesIntoAccountTimeOut(Resolution resolution, bool dataArrivedLate)
        {
            var timeProvider = new ManualTimeProvider(new DateTime(2020, 5, 21, 10, 0, 0, 0), TimeZones.NewYork);

            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, resolution, out var enqueueableEnumerator, dailyStrictEndTimeEnabled: false);
            var openingBar = new TradeBar
            {
                Open = 0.01m,
                High = 0.01m,
                Low = 0.01m,
                Close = 0.01m,
                Volume = 1,
                EndTime = new DateTime(2020, 5, 21, 10, 0, 0),
                Symbol = Symbols.AAPL
            };
            var secondBar = new TradeBar
            {
                Open = 1m,
                High = 2m,
                Low = 1m,
                Close = 2m,
                Volume = 100,
                EndTime = openingBar.EndTime + resolution.ToTimeSpan(),
                Symbol = Symbols.AAPL
            };

            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // Advance the time, we don't expect a fill-forward bar because the timeout amount has not passed yet
            timeProvider.Advance(resolution.ToTimeSpan());
            if (dataArrivedLate)
            {
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.IsNull(fillForwardEnumerator.Current);

                // Advance the time, including the expected timout, we expect a fill-forward bar.
                timeProvider.Advance(LiveFillForwardEnumerator.GetMaximumDataTimeout(resolution));
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
                Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
                Assert.AreEqual(openingBar.EndTime.Add(resolution.ToTimeSpan()), fillForwardEnumerator.Current.EndTime);
            }

            // Now we expect data. The secondBar should be fill-forwarded from here on out after the MoveNext
            enqueueableEnumerator.Enqueue(secondBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void TakesIntoAccountTimeOutDaily(bool dailyStrictEndTimeEnabled, bool dataArrivedLate)
        {
            var resolution = Resolution.Daily;
            var referenceOpenTime = new DateTime(2020, 5, 21, 9, 30, 0);
            var referenceTime = new DateTime(2020, 5, 21, 16, 0, 0);
            if (!dailyStrictEndTimeEnabled)
            {
                referenceOpenTime = referenceOpenTime.Date;
                referenceTime = referenceTime.Date.AddDays(1);
            }
            var timeProvider = new ManualTimeProvider(referenceTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, resolution, out var enqueueableEnumerator, dailyStrictEndTimeEnabled);
            var openingBar = new TradeBar
            {
                Open = 0.01m,
                High = 0.01m,
                Low = 0.01m,
                Close = 0.01m,
                Volume = 1,
                Time = referenceOpenTime,
                EndTime = referenceTime,
                Symbol = Symbols.AAPL
            };
            var secondBar = new TradeBar
            {
                Open = 1m,
                High = 2m,
                Low = 1m,
                Close = 2m,
                Volume = 100,
                Time = referenceOpenTime.AddDays(1),
                EndTime = referenceTime.AddDays(1),
                Symbol = Symbols.AAPL
            };

            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // Advance the time, we don't expect a fill-forward bar because the timeout amount has not passed yet
            timeProvider.SetCurrentTime(secondBar.EndTime);

            if (dataArrivedLate)
            {
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.IsNull(fillForwardEnumerator.Current);

                // Advance the time, including the expected timout, we expect a fill-forward bar.
                timeProvider.Advance(LiveFillForwardEnumerator.GetMaximumDataTimeout(resolution));
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
                Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
                Assert.AreEqual(secondBar.EndTime, fillForwardEnumerator.Current.EndTime);
            }

            // Now we expect data. The secondBar should be fill-forwarded from here on out after the MoveNext
            enqueueableEnumerator.Enqueue(secondBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(secondBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(secondBar.EndTime, fillForwardEnumerator.Current.EndTime);
        }

        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        public void MultiResolutionSmallerFillForwardResolution(Resolution resolution)
        {
            var ffResolution = Resolution.Second;
            var referenceOpenTime = new DateTime(2020, 5, 21, 14, 0, 0);
            if (resolution == Resolution.Minute)
            {
                referenceOpenTime = new DateTime(2020, 5, 21, 15, 59, 0);
            }
            var referenceTime = new DateTime(2020, 5, 21, 15, 0, 0);

            var timeProvider = new ManualTimeProvider(referenceTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, resolution, out var enqueueableEnumerator, true, ffResolution);
            var openingBar = new TradeBar
            {
                Open = 0.01m,
                High = 0.01m,
                Low = 0.01m,
                Close = 0.01m,
                Volume = 1,
                Time = referenceOpenTime,
                EndTime = referenceTime,
                Symbol = Symbols.AAPL
            };

            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // Advance the time, we expect a fill-forward bar

            for (var i = 0; i < 60; i++)
            {
                timeProvider.Advance(Time.OneSecond);

                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.NotNull(fillForwardEnumerator.Current);
                Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
                Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork), fillForwardEnumerator.Current.EndTime);
            }
        }

        [TestCase(Resolution.Daily, Resolution.Minute)]
        [TestCase(Resolution.Hour, Resolution.Minute)]
        [TestCase(Resolution.Daily, Resolution.Second)]
        [TestCase(Resolution.Hour, Resolution.Second)]
        public void MultiResolutionMarketClose(Resolution resolution, Resolution ffResolution)
        {
            var referenceOpenTime = new DateTime(2020, 5, 21, 9, 30, 0);
            if (resolution == Resolution.Hour)
            {
                referenceOpenTime = new DateTime(2020, 5, 21, 15, 0, 0);
            }
            var referenceTime = new DateTime(2020, 5, 21, 16, 0, 0);
            var timeProvider = new ManualTimeProvider(referenceTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, resolution, out var enqueueableEnumerator, true, ffResolution);
            var openingBar = new TradeBar
            {
                Open = 0.01m,
                High = 0.01m,
                Low = 0.01m,
                Close = 0.01m,
                Volume = 1,
                Time = referenceOpenTime,
                EndTime = referenceTime,
                Symbol = Symbols.AAPL
            };

            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            for (var i = 0; i < 600; i++)
            {
                // Advance the time, we don't expect a fill-forward bar, we've emitted our daily bar already and market is closed
                timeProvider.Advance(ffResolution.ToTimeSpan());
                Assert.IsTrue(fillForwardEnumerator.MoveNext());
                Assert.IsNull(fillForwardEnumerator.Current);
            }
        }

        private static LiveFillForwardEnumerator GetLiveFillForwardEnumerator(ITimeProvider timeProvider, Resolution resolution,
            out EnqueueableEnumerator<BaseData> enqueueableEnumerator, bool dailyStrictEndTimeEnabled, Resolution? ffResolution = null)
        {
            enqueueableEnumerator = new EnqueueableEnumerator<BaseData>();
            var fillForwardEnumerator = new LiveFillForwardEnumerator(
                timeProvider,
                enqueueableEnumerator,
                new SecurityExchange(MarketHoursDatabase.FromDataFolder()
                    .ExchangeHoursListing
                    .First(kvp => kvp.Key.Market == Market.USA && kvp.Key.SecurityType == SecurityType.Equity)
                    .Value
                    .ExchangeHours),
                Ref.CreateReadOnly(() => (ffResolution ?? resolution).ToTimeSpan()),
                false,
                Time.EndOfTime,
                resolution,
                TimeZones.NewYork,
                dailyStrictEndTimeEnabled: dailyStrictEndTimeEnabled
            );

            return fillForwardEnumerator;
        }
    }
}
