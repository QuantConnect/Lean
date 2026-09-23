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
            var fillForward = new LiveFillForwardEnumerator(timeProvider, underlying.GetEnumerator(), exchange, Ref.Create(Time.OneSecond), false, reference, Time.EndOfTime, Resolution.Second, exchange.TimeZone, false);

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

            // step ahead into null data territory
            timeProvider.SetCurrentTime(reference.AddSeconds(4));

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork).RoundDown(Time.OneSecond), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            Assert.IsTrue(fillForward.MoveNext());
            Assert.IsNull(fillForward.Current);

            timeProvider.SetCurrentTime(reference.AddSeconds(5));

            Assert.IsTrue(fillForward.MoveNext());
            Assert.AreEqual(underlying[2].Value, fillForward.Current.Value);
            Assert.AreEqual(timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork).RoundDown(Time.OneSecond), fillForward.Current.EndTime);
            Assert.IsTrue(fillForward.Current.IsFillForward);
            Assert.AreEqual(0, ((TradeBar)fillForward.Current).Volume);

            timeProvider.SetCurrentTime(reference.AddSeconds(6));

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
                reference,
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
            var reference = new DateTime(2020, 5, 21, 9, 40, 0, 100);
            var timeProvider = new ManualTimeProvider(reference, TimeZones.NewYork);

            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, reference.Date, Resolution.Minute, out var enqueueableEnumerator, false);
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
            enqueueableEnumerator.Dispose();
        }

        private static IEnumerable<TestCaseData> TimeOutTestCases
        {
            get
            {
                // Hour resolution, fill forward to market open
                yield return new(Resolution.Hour, new DateTime(2020, 5, 21, 16, 0, 0), new DateTime(2020, 5, 22, 10, 0, 0), true, true);
                yield return new(Resolution.Hour, new DateTime(2020, 5, 21, 16, 0, 0), new DateTime(2020, 5, 22, 10, 0, 0), false, false);
                yield return new(Resolution.Hour, new DateTime(2020, 5, 21, 10, 0, 0), null, true, false);
                yield return new(Resolution.Hour, new DateTime(2020, 5, 21, 10, 0, 0), null, false, false);
                // over a weekend (22th is a Friday and 25th Monday is a holiday)
                yield return new(Resolution.Hour, new DateTime(2020, 5, 22, 16, 0, 0), new DateTime(2020, 5, 26, 10, 0, 0), true, true);
                yield return new(Resolution.Hour, new DateTime(2020, 5, 22, 16, 0, 0), new DateTime(2020, 5, 26, 10, 0, 0), false, false);
                // market close
                yield return new(Resolution.Hour, new DateTime(2020, 5, 21, 15, 0, 0), null, true, true);
                yield return new(Resolution.Hour, new DateTime(2020, 5, 21, 15, 0, 0), null, false, false);

                // Minute resolution, fill forward to market open
                yield return new(Resolution.Minute, new DateTime(2020, 5, 21, 16, 0, 0), new DateTime(2020, 5, 22, 9, 31, 0), true, true);
                yield return new(Resolution.Minute, new DateTime(2020, 5, 21, 16, 0, 0), new DateTime(2020, 5, 22, 9, 31, 0), false, false);
                yield return new(Resolution.Minute, new DateTime(2020, 5, 21, 9, 31, 0), null, true, false);
                yield return new(Resolution.Minute, new DateTime(2020, 5, 21, 9, 31, 0), null, false, false);
                // over a weekend
                yield return new(Resolution.Minute, new DateTime(2020, 5, 22, 16, 0, 0), new DateTime(2020, 5, 26, 9, 31, 0), true, true);
                yield return new(Resolution.Minute, new DateTime(2020, 5, 22, 16, 0, 0), new DateTime(2020, 5, 26, 9, 31, 0), false, false);
                // market close
                yield return new(Resolution.Minute, new DateTime(2020, 5, 21, 15, 59, 0), null, true, true);
                yield return new(Resolution.Minute, new DateTime(2020, 5, 21, 15, 59, 0), null, false, false);

                // Second resolution, fill forward to market open
                yield return new(Resolution.Second, new DateTime(2020, 5, 21, 16, 0, 0), new DateTime(2020, 5, 22, 9, 30, 1), true, true);
                yield return new(Resolution.Second, new DateTime(2020, 5, 21, 16, 0, 0), new DateTime(2020, 5, 22, 9, 30, 1), false, false);
                yield return new(Resolution.Second, new DateTime(2020, 5, 21, 9, 30, 1), null, true, false);
                yield return new(Resolution.Second, new DateTime(2020, 5, 21, 9, 30, 1), null, false, false);
                // over a weekend
                yield return new(Resolution.Second, new DateTime(2020, 5, 22, 16, 0, 0), new DateTime(2020, 5, 26, 9, 30, 1), true, true);
                yield return new(Resolution.Second, new DateTime(2020, 5, 22, 16, 0, 0), new DateTime(2020, 5, 26, 9, 30, 1), false, false);
                // market close
                yield return new(Resolution.Second, new DateTime(2020, 5, 21, 15, 59, 59), null, true, true);
                yield return new(Resolution.Second, new DateTime(2020, 5, 21, 15, 59, 59), null, false, false);
            }
        }

        [TestCaseSource(nameof(TimeOutTestCases))]
        public void TakesIntoAccountTimeOut(Resolution resolution, DateTime previousDataEndTime, DateTime? expectedNextBarEndTime, bool dataArrivedLate, bool shouldHaveTimeout)
        {
            var timeProvider = new ManualTimeProvider(previousDataEndTime, TimeZones.NewYork);

            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, previousDataEndTime.Date, resolution, out var enqueueableEnumerator, dailyStrictEndTimeEnabled: false);
            var period = resolution.ToTimeSpan();
            var openingBar = new TradeBar(previousDataEndTime.Subtract(period), Symbols.AAPL, 0.01m, 0.01m, 0.01m, 0.01m, 1, period);

            expectedNextBarEndTime ??= previousDataEndTime.Add(resolution.ToTimeSpan());
            var secondBar = new TradeBar(openingBar.EndTime, Symbols.AAPL, 1m, 2m, 1m, 2m, 100, period);

            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // Advance the time, we don't expect a fill-forward bar because the timeout amount has not passed yet
            timeProvider.SetCurrentTime(expectedNextBarEndTime.Value);
            if (dataArrivedLate)
            {
                Assert.IsTrue(fillForwardEnumerator.MoveNext());

                if (shouldHaveTimeout)
                {
                    Assert.IsNull(fillForwardEnumerator.Current);

                    // Advance the time, including the expected timout, we expect a fill-forward bar.
                    timeProvider.Advance(LiveFillForwardEnumerator.GetMaximumDataTimeout(resolution));

                    Assert.IsTrue(fillForwardEnumerator.MoveNext());
                }

                Assert.IsNotNull(fillForwardEnumerator.Current);
                Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
                Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
                Assert.AreEqual(expectedNextBarEndTime, fillForwardEnumerator.Current.EndTime);

                Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
                Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
                Assert.AreEqual(expectedNextBarEndTime, fillForwardEnumerator.Current.EndTime);

            }

            // Now we expect data. The secondBar should be fill-forwarded from here on out after the MoveNext
            enqueueableEnumerator.Enqueue(secondBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            enqueueableEnumerator.Dispose();
        }

        [Test]
        public void TakesIntoAccountTimeOutWhenThereAreBigGaps([Values(Resolution.Second, Resolution.Minute, Resolution.Hour)] Resolution resolution)
        {
            var start = new DateTime(2020, 5, 20, 12, 0, 0);
            var end = new DateTime(2020, 5, 22, 16, 0, 0);

            var timeProvider = new ManualTimeProvider(start, TimeZones.NewYork);

            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, start, resolution, out var enqueueableEnumerator, dailyStrictEndTimeEnabled: false);
            var period = resolution.ToTimeSpan();
            var openingBar = new TradeBar(start.Subtract(period), Symbols.AAPL, 0.01m, 0.01m, 0.01m, 0.01m, 1, period);

            // Enqueue the first point, which will be emitted ASAP.
            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.NotNull(fillForwardEnumerator.Current);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            timeProvider.Advance(period);

            var previous = (TradeBar)fillForwardEnumerator.Current;
            var currentIsMarketOpen = false;
            var currentIsMarketClose = false;
            while (previous.EndTime < end)
            {
                var currentTime = timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork);

                Assert.IsTrue(fillForwardEnumerator.MoveNext(), $"Previous: {previous.EndTime}");

                if (currentIsMarketOpen || currentIsMarketClose)
                {
                    Assert.IsNull(fillForwardEnumerator.Current, $"Previous: {previous.EndTime}");

                    // Advance the time, including the expected timout, we expect a fill-forward bar.
                    timeProvider.Advance(LiveFillForwardEnumerator.GetMaximumDataTimeout(resolution));
                    Assert.IsTrue(fillForwardEnumerator.MoveNext(), $"Previous: {previous.EndTime}");
                }

                Assert.IsNotNull(fillForwardEnumerator.Current, $"Previous: {previous.EndTime}");

                var current = (TradeBar)fillForwardEnumerator.Current;
                Assert.IsTrue(current.IsFillForward, $"Current: {previous.EndTime}");
                Assert.AreEqual(previous.Open, current.Open, $"Current: {previous.EndTime}");
                Assert.AreEqual(previous.Price, current.Price, $"Current: {previous.EndTime}");

                var expectedEndTime = previous.EndTime.Add(period);
                if (currentIsMarketOpen)
                {
                    expectedEndTime = expectedEndTime.Date.AddDays(1).AddHours(9).AddMinutes(30).Add(period);
                    if (resolution == Resolution.Hour)
                    {
                        expectedEndTime = expectedEndTime.RoundDown(period);
                    }
                }
                Assert.AreEqual(expectedEndTime, current.EndTime, $"Current: {previous.EndTime}");

                currentIsMarketOpen = current.EndTime.TimeOfDay == TimeSpan.FromHours(16);
                currentIsMarketClose = current.EndTime.TimeOfDay == TimeSpan.FromHours(16) - period;
                previous = current;

                // Advance the time provider
                var nextTime = current.EndTime.Add(period);
                if (nextTime.TimeOfDay > TimeSpan.FromHours(16))
                {
                    // If the next time is after market close, we need to advance to the next day
                    nextTime = nextTime.Date.AddDays(1).AddHours(9).AddMinutes(30).Add(period);
                    if (resolution == Resolution.Hour)
                    {
                        nextTime = nextTime.RoundDown(period);
                    }
                }
                timeProvider.SetCurrentTime(nextTime);
            }

            enqueueableEnumerator.Dispose();
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
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, referenceTime.Date, resolution, out var enqueueableEnumerator, dailyStrictEndTimeEnabled);
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
            enqueueableEnumerator.Dispose();
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
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, referenceTime.Date, resolution, out var enqueueableEnumerator, true, ffResolution);
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

            enqueueableEnumerator.Dispose();
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
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, referenceTime.Date, resolution, out var enqueueableEnumerator, true, ffResolution);
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

            enqueueableEnumerator.Dispose();
        }

        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        public void SkipsStaleFillForwardBarsAfterTimeJump(Resolution fillForwardResolution)
        {
            var previousBarEndTime = new DateTime(2020, 5, 21, 10, 0, 0);
            var timeProvider = new ManualTimeProvider(previousBarEndTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, previousBarEndTime.Date, Resolution.Minute,
                out var enqueueableEnumerator, dailyStrictEndTimeEnabled: false, fillForwardResolution);
            var openingBar = new TradeBar(previousBarEndTime.AddMinutes(-1), Symbols.AAPL, 0.01m, 0.01m, 0.01m, 0.01m, 1, Time.OneMinute);

            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // the clock jumps 30 minutes ahead at once, like the warmup to live handover does
            var expectedBarEndTime = previousBarEndTime.AddMinutes(30);
            timeProvider.SetCurrentTime(expectedBarEndTime.AddMilliseconds(100));

            // a single fill forward bar is emitted, the latest expected one, instead of back filling the whole gap
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNotNull(fillForwardEnumerator.Current);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(expectedBarEndTime, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);

            // and nothing else to emit until time moves forward again
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNull(fillForwardEnumerator.Current);

            enqueueableEnumerator.Dispose();
        }

        [TestCase(Resolution.Minute, Resolution.Minute, false)]
        [TestCase(Resolution.Minute, Resolution.Second, false)]
        [TestCase(Resolution.Hour, Resolution.Minute, false)]
        [TestCase(Resolution.Hour, Resolution.Second, false)]
        [TestCase(Resolution.Second, Resolution.Second, false)]
        [TestCase(Resolution.Daily, Resolution.Minute, false)]
        [TestCase(Resolution.Daily, Resolution.Second, false)]
        [TestCase(Resolution.Daily, Resolution.Minute, true)]
        [TestCase(Resolution.Daily, Resolution.Second, true)]
        public void SkipsStaleFillForwardBarsAfterTimeJumpCrossResolution(Resolution dataResolution, Resolution fillForwardResolution, bool dailyStrictEndTime)
        {
            DateTime previousBarTime;
            DateTime previousBarEndTime;
            if (dataResolution == Resolution.Daily)
            {
                if (dailyStrictEndTime)
                {
                    // Wednesday session
                    previousBarTime = new DateTime(2020, 5, 20, 9, 30, 0);
                    previousBarEndTime = new DateTime(2020, 5, 20, 16, 0, 0);
                }
                else
                {
                    previousBarTime = new DateTime(2020, 5, 20);
                    previousBarEndTime = new DateTime(2020, 5, 21);
                }
            }
            else
            {
                previousBarEndTime = new DateTime(2020, 5, 21, 10, 0, 0);
                previousBarTime = previousBarEndTime - dataResolution.ToTimeSpan();
            }

            var timeProvider = new ManualTimeProvider(previousBarEndTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, previousBarEndTime.Date, dataResolution,
                out var enqueueableEnumerator, dailyStrictEndTime, fillForwardResolution);
            var openingBar = new TradeBar(previousBarTime, Symbols.AAPL, 0.01m, 0.01m, 0.01m, 0.01m, 1, previousBarEndTime - previousBarTime);

            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // the clock jumps ahead at once into Thursday's session, like the warmup to live handover does
            var expectedBarEndTime = new DateTime(2020, 5, 21, 10, 30, 0);
            timeProvider.SetCurrentTime(expectedBarEndTime.AddMilliseconds(100));

            // a single fill forward bar is emitted, the latest expected one, instead of back filling the whole gap
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNotNull(fillForwardEnumerator.Current);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(expectedBarEndTime, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);
            // emitted bars keep the data resolution as their period, the fill forward resolution is only the emission grid
            if (dataResolution != Resolution.Daily)
            {
                Assert.AreEqual(dataResolution.ToTimeSpan(), fillForwardEnumerator.Current.EndTime - fillForwardEnumerator.Current.Time);
            }

            // and nothing else to emit until time moves forward again
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNull(fillForwardEnumerator.Current);

            enqueueableEnumerator.Dispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SkipRoundsDownOnTheDataTimeZoneGrid(bool halfHourOffsetDataTimeZone)
        {
            // the emission grid follows the data time zone: with a +5:30 offset data time zone, hour bars
            // end at half past the hour in exchange (New York) time, with a whole hour offset they end on the hour
            var dataTimeZone = halfHourOffsetDataTimeZone ? TimeZones.Kolkata : TimeZones.Utc;
            var previousBarEndTime = halfHourOffsetDataTimeZone
                ? new DateTime(2020, 5, 21, 10, 30, 0)
                : new DateTime(2020, 5, 21, 10, 0, 0);
            var expectedBarEndTime = halfHourOffsetDataTimeZone
                ? new DateTime(2020, 5, 21, 13, 30, 0)
                : new DateTime(2020, 5, 21, 13, 0, 0);

            var exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            var timeProvider = new ManualTimeProvider(previousBarEndTime, TimeZones.NewYork);
            var enqueueableEnumerator = new EnqueueableEnumerator<BaseData>();
            using var fillForwardEnumerator = new LiveFillForwardEnumerator(timeProvider, enqueueableEnumerator, exchange,
                Ref.Create(Time.OneHour), false, previousBarEndTime.Date, Time.EndOfTime, Resolution.Hour, dataTimeZone, false);
            var openingBar = new TradeBar(previousBarEndTime.AddHours(-1), Symbols.EURUSD, 0.01m, 0.01m, 0.01m, 0.01m, 1, Time.OneHour);

            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // the clock jumps over 3 hours ahead at once
            timeProvider.SetCurrentTime(new DateTime(2020, 5, 21, 13, 45, 0));

            // a single fill forward bar is emitted, ending on the data time zone hour grid
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNotNull(fillForwardEnumerator.Current);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(expectedBarEndTime, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);

            // and nothing else to emit until time moves forward again
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNull(fillForwardEnumerator.Current);

            enqueueableEnumerator.Dispose();
        }

        [Test]
        public void SkipClampsToPreviousSessionCloseWhenMarketIsClosed()
        {
            // Thursday, mid session
            var previousBarEndTime = new DateTime(2020, 5, 21, 15, 30, 0);
            var timeProvider = new ManualTimeProvider(previousBarEndTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, previousBarEndTime.Date, Resolution.Minute,
                out var enqueueableEnumerator, dailyStrictEndTimeEnabled: false);
            var openingBar = new TradeBar(previousBarEndTime.AddMinutes(-1), Symbols.AAPL, 0.01m, 0.01m, 0.01m, 0.01m, 1, Time.OneMinute);

            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // the clock jumps to Saturday noon: the latest expected bar is Friday's last, never a future bar of the next session
            timeProvider.SetCurrentTime(new DateTime(2020, 5, 23, 12, 0, 0));

            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNotNull(fillForwardEnumerator.Current);
            Assert.IsTrue(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(new DateTime(2020, 5, 22, 16, 0, 0), fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(openingBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);

            // next expected bar is the following session open, in the future, nothing to emit
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.IsNull(fillForwardEnumerator.Current);

            enqueueableEnumerator.Dispose();
        }

        [Test]
        public void SkipDoesNotJumpPastNextRealDataPoint()
        {
            var previousBarEndTime = new DateTime(2020, 5, 21, 10, 0, 0);
            var timeProvider = new ManualTimeProvider(previousBarEndTime, TimeZones.NewYork);
            using var fillForwardEnumerator = GetLiveFillForwardEnumerator(timeProvider, previousBarEndTime.Date, Resolution.Minute,
                out var enqueueableEnumerator, dailyStrictEndTimeEnabled: false);
            var openingBar = new TradeBar(previousBarEndTime.AddMinutes(-1), Symbols.AAPL, 0.01m, 0.01m, 0.01m, 0.01m, 1, Time.OneMinute);
            var nextBar = new TradeBar(previousBarEndTime.AddMinutes(4), Symbols.AAPL, 1m, 2m, 1m, 2m, 100, Time.OneMinute);

            enqueueableEnumerator.Enqueue(openingBar);
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            Assert.AreEqual(openingBar.EndTime, fillForwardEnumerator.Current.EndTime);

            // real data is available behind a stale gap, the clock jumped 30 minutes
            enqueueableEnumerator.Enqueue(nextBar);
            timeProvider.SetCurrentTime(previousBarEndTime.AddMinutes(30));

            // fill forward bars never reach past the next real point, which is emitted intact right after
            var fillForwardCount = 0;
            while (fillForwardEnumerator.MoveNext() && fillForwardEnumerator.Current != null && fillForwardEnumerator.Current.IsFillForward)
            {
                Assert.LessOrEqual(fillForwardEnumerator.Current.EndTime, nextBar.Time.AddMinutes(1));
                fillForwardCount++;
                Assert.LessOrEqual(fillForwardCount, 4);
            }
            Assert.IsNotNull(fillForwardEnumerator.Current);
            Assert.IsFalse(fillForwardEnumerator.Current.IsFillForward);
            Assert.AreEqual(nextBar.EndTime, fillForwardEnumerator.Current.EndTime);
            Assert.AreEqual(nextBar.Open, ((TradeBar)fillForwardEnumerator.Current).Open);

            enqueueableEnumerator.Dispose();
        }

        private static LiveFillForwardEnumerator GetLiveFillForwardEnumerator(ITimeProvider timeProvider, DateTime startTime, Resolution resolution,
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
                startTime,
                Time.EndOfTime,
                resolution,
                TimeZones.NewYork,
                dailyStrictEndTimeEnabled: dailyStrictEndTimeEnabled
            );

            return fillForwardEnumerator;
        }
    }
}
