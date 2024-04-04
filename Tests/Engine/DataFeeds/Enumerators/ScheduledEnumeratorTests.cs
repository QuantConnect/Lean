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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ScheduledEnumeratorTests
    {
        private readonly DateTime _referenceTime = new DateTime(2019, 1, 1);

        [TestCase(true)]
        [TestCase(false)]
        public void RespectsPredicateTimeProvider(bool newDataArrivedInTime)
        {
            var scheduledDate = _referenceTime.AddDays(1);
            using var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(scheduledDate.AddDays(-1), Symbols.SPY, 1, 1)
                })
            };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { scheduledDate },
                new PredicateTimeProvider(timeProvider, (currentDateTime) => {
                    // will only let time advance after it's passed the 7/8 hour frontier
                    return currentDateTime.TimeOfDay > TimeSpan.FromMinutes(7 * 60 + DateTime.UtcNow.Second);
                }),
                TimeZones.Utc,
                DateTime.MinValue);

            // still null since frontier is still behind schedule
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            timeProvider.SetCurrentTimeUtc(scheduledDate);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            timeProvider.SetCurrentTimeUtc(scheduledDate.AddHours(2));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            if (newDataArrivedInTime)
            {
                // New data comes in!
                underlyingEnumerator.MoveNextNewValues.Enqueue(new Tick(scheduledDate, Symbols.SPY, 10, 10));
            }

            timeProvider.SetCurrentTimeUtc(scheduledDate.AddHours(8));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(scheduledDate, enumerator.Current.Time);
            Assert.AreEqual(newDataArrivedInTime ? 10 : 1, (enumerator.Current as Tick).BidPrice);

            // schedule ended so enumerator will end too
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void ScheduleSkipsOldDates()
        {
            using var enumerator = new ScheduledEnumerator(
                new TestEnumerator(),
                new List<DateTime> { _referenceTime },
                new ManualTimeProvider(_referenceTime),
                TimeZones.Utc,
                _referenceTime.AddDays(1));

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void EmptyScheduleThrowsNoException()
        {
            ScheduledEnumerator enumerator = null;
            Assert.DoesNotThrow(() => enumerator = new ScheduledEnumerator(
                new TestEnumerator(),
                new List<DateTime>(),
                new ManualTimeProvider(_referenceTime),
                TimeZones.Utc,
                DateTime.MinValue));

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void ReturnsTrueEvenIfUnderlyingIsNullButReturnsTrue()
        {
            using var underlyingEnumerator = new TestEnumerator { MoveNextReturn = true };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { _referenceTime.AddDays(1) },
                timeProvider,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void ReturnsFalseWhenUnderlyingReturnsFalse()
        {
            using var underlyingEnumerator = new TestEnumerator { MoveNextReturn = false };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { _referenceTime.AddDays(1) },
                timeProvider,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void ForwardsDataToFitSchedule()
        {
            var scheduledDate = _referenceTime.AddDays(1);
            using var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(scheduledDate, Symbols.SPY, 1, 1),
                    // way in the future compared with the schedule
                    new Tick(scheduledDate.AddYears(1), Symbols.SPY, 10, 10)
                })
            };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { scheduledDate, scheduledDate.AddDays(1) },
                timeProvider,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            timeProvider.SetCurrentTimeUtc(scheduledDate);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(scheduledDate, enumerator.Current.Time);
            Assert.AreEqual(1, (enumerator.Current as Tick).BidPrice);

            // it will forward previous available value to fit the schedule
            timeProvider.SetCurrentTimeUtc(scheduledDate.AddDays(1));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(scheduledDate.AddDays(1), enumerator.Current.Time);
            Assert.AreEqual(1, (enumerator.Current as Tick).BidPrice);

            // schedule ended so enumerator will end too
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void UpdatesCurrentBasedOnSchedule()
        {
            var scheduledDate = _referenceTime.AddDays(1);
            using var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(scheduledDate, Symbols.SPY, 1, 1)
                })
            };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { scheduledDate },
                timeProvider,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            timeProvider.SetCurrentTimeUtc(scheduledDate);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(scheduledDate, enumerator.Current.Time);
            Assert.AreEqual(1, (enumerator.Current as Tick).BidPrice);

            // schedule ended so enumerator will end too
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void WillUseLatestDataPoint()
        {
            using var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(new DateTime(2019, 1, 15), Symbols.SPY, 1, 1),
                    new Tick(new DateTime(2019, 1, 20), Symbols.SPY, 2, 1),
                    new Tick(new DateTime(2019, 1, 25), Symbols.SPY, 3, 1)
                })
            };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1) },
                timeProvider,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            // frontier is now a month after the scheduled time!
            timeProvider.SetCurrentTimeUtc(new DateTime(2019, 3, 1));

            Assert.IsTrue(enumerator.MoveNext());
            // it uses the last available data point in the enumerator
            Assert.AreEqual(new DateTime(2019, 2, 1), enumerator.Current.Time);
            Assert.AreEqual(3, (enumerator.Current as Tick).BidPrice);

            Assert.IsNull(underlyingEnumerator.Current);
        }

        [Test]
        public void WillUseLatestDataPointOnlyIfBeforeOrAtSchedule()
        {
            using var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(new DateTime(2019, 1, 20), Symbols.SPY, 2, 1),
                    new Tick(new DateTime(2019, 1, 25), Symbols.SPY, 3, 1),
                    // this guys is in 2020
                    new Tick(new DateTime(2020, 1, 1), Symbols.SPY, 4, 1)
                })
            };
            var timeProvider = new ManualTimeProvider(_referenceTime);

            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1), new DateTime(2020, 2, 1) },
                timeProvider,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            // frontier is now a month after the scheduled time!
            timeProvider.SetCurrentTimeUtc(new DateTime(2019, 3, 1));

            Assert.IsTrue(enumerator.MoveNext());
            // it uses the last available data point in the enumerator that is before the schedule
            Assert.AreEqual(new DateTime(2019, 2, 1), enumerator.Current.Time);
            Assert.AreEqual(3, (enumerator.Current as Tick).BidPrice);

            // the underlying enumerator hold the next data point
            Assert.AreEqual(new DateTime(2020, 1, 1), underlyingEnumerator.Current.Time);

            // now lets test fetching the last data point
            timeProvider.SetCurrentTimeUtc(new DateTime(2021, 3, 1));

            // the underlying will end but should still emit the data point it has
            underlyingEnumerator.MoveNextReturn = false;

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new DateTime(2020, 2, 1), enumerator.Current.Time);
            Assert.AreEqual(4, (enumerator.Current as Tick).BidPrice);

            Assert.IsNull(underlyingEnumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void NoTimeProvider()
        {
            using var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(new DateTime(2019, 1, 20), Symbols.SPY, 2, 1),
                    new Tick(new DateTime(2019, 1, 25), Symbols.SPY, 3, 1),

                    new Tick(new DateTime(2020, 1, 1), Symbols.SPY, 4, 1)
                })
            };
            using var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1), new DateTime(2020, 2, 1) },
                null,
                TimeZones.Utc,
                DateTime.MinValue);

            Assert.IsTrue(enumerator.MoveNext());
            // it uses the last available data point in the enumerator that is before the schedule
            Assert.AreEqual(new DateTime(2019, 2, 1), enumerator.Current.Time);
            Assert.AreEqual(3, (enumerator.Current as Tick).BidPrice);

            // the underlying enumerator hold the next data point
            Assert.AreEqual(new DateTime(2020, 1, 1), underlyingEnumerator.Current.Time);

            // the underlying will end but should still emit the data point it has
            underlyingEnumerator.MoveNextReturn = false;

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new DateTime(2020, 2, 1), enumerator.Current.Time);
            Assert.AreEqual(4, (enumerator.Current as Tick).BidPrice);

            Assert.IsNull(underlyingEnumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        private class TestEnumerator : IEnumerator<BaseData>
        {
            public Queue<BaseData> MoveNextNewValues { get; set; }
            public BaseData Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNextReturn { get; set; }

            public bool MoveNext()
            {
                if (MoveNextNewValues != null && MoveNextNewValues.Count > 0)
                {
                    Current = MoveNextNewValues.Dequeue();
                }
                else
                {
                    Current = null;
                }
                return MoveNextReturn;
            }

            public void Reset()
            {}
            public void Dispose()
            {}
        }
    }
}
