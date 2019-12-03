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
    [TestFixture]
    public class ScheduledEnumeratorTests
    {
        [Test]
        public void EmptyScheduleThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new ScheduledEnumerator(
                new TestEnumerator(),
                new List<DateTime>(),
                new ManualTimeProvider(new DateTime(2019, 1, 1)),
                TimeZones.Utc));
        }

        [Test]
        public void ReturnsTrueEvenIfUnderlyingIsNullButReturnsTrue()
        {
            var underlyingEnumerator = new TestEnumerator { MoveNextReturn = true };
            var timeProvider = new ManualTimeProvider(new DateTime(2019, 1, 1));

            var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1) },
                timeProvider,
                TimeZones.Utc);

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
            var underlyingEnumerator = new TestEnumerator { MoveNextReturn = false };
            var timeProvider = new ManualTimeProvider(new DateTime(2019, 1, 1));

            var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1) },
                timeProvider,
                TimeZones.Utc);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void UpdatesCurrentBasedOnSchedule()
        {
            var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(new DateTime(2019, 1, 15), Symbols.SPY, 1, 1)
                })
            };
            var timeProvider = new ManualTimeProvider(new DateTime(2019, 1, 1));

            var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1) },
                timeProvider,
                TimeZones.Utc);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            timeProvider.SetCurrentTimeUtc(new DateTime(2019, 2, 1));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(enumerator.Current.Time, new DateTime(2019, 1, 15));
            Assert.AreEqual((enumerator.Current as Tick).BidPrice, 1);

            // schedule ended so enumerator will end to
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void WillUseLatestDataPoint()
        {
            var underlyingEnumerator = new TestEnumerator
            {
                MoveNextReturn = true,
                MoveNextNewValues = new Queue<BaseData>(new List<BaseData>
                {
                    new Tick(new DateTime(2019, 1, 15), Symbols.SPY, 1, 1),
                    new Tick(new DateTime(2019, 1, 20), Symbols.SPY, 2, 1),
                    new Tick(new DateTime(2019, 1, 25), Symbols.SPY, 3, 1)
                })
            };
            var timeProvider = new ManualTimeProvider(new DateTime(2019, 1, 1));

            var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1) },
                timeProvider,
                TimeZones.Utc);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            // frontier is now a month after the scheduled time!
            timeProvider.SetCurrentTimeUtc(new DateTime(2019, 3, 1));

            Assert.IsTrue(enumerator.MoveNext());
            // it uses the last available data point in the enumerator
            Assert.AreEqual(enumerator.Current.Time, new DateTime(2019, 1, 25));
            Assert.AreEqual((enumerator.Current as Tick).BidPrice, 3);

            Assert.IsNull(underlyingEnumerator.Current);
        }

        [Test]
        public void WillUseLatestDataPointOnlyIfBeforeOrAtSchedule()
        {
            var underlyingEnumerator = new TestEnumerator
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
            var timeProvider = new ManualTimeProvider(new DateTime(2019, 1, 1));

            var enumerator = new ScheduledEnumerator(
                underlyingEnumerator,
                new List<DateTime> { new DateTime(2019, 2, 1), new DateTime(2020, 2, 1) },
                timeProvider,
                TimeZones.Utc);

            Assert.IsTrue(enumerator.MoveNext());
            // still null since frontier is still behind schedule
            Assert.IsNull(enumerator.Current);

            // frontier is now a month after the scheduled time!
            timeProvider.SetCurrentTimeUtc(new DateTime(2019, 3, 1));

            Assert.IsTrue(enumerator.MoveNext());
            // it uses the last available data point in the enumerator that is before the schedule
            Assert.AreEqual(enumerator.Current.Time, new DateTime(2019, 1, 25));
            Assert.AreEqual((enumerator.Current as Tick).BidPrice, 3);

            // the underlying enumerator hold the next data point
            Assert.AreEqual(new DateTime(2020, 1, 1), underlyingEnumerator.Current.Time);

            // now lets test fetching the last data point
            timeProvider.SetCurrentTimeUtc(new DateTime(2021, 3, 1));

            // the underlying will end but should still emit the data point it has
            underlyingEnumerator.MoveNextReturn = false;

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(enumerator.Current.Time, new DateTime(2020, 1, 1));
            Assert.AreEqual((enumerator.Current as Tick).BidPrice, 4);

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
            {
            }
            public void Dispose()
            {
            }
        }
    }
}
