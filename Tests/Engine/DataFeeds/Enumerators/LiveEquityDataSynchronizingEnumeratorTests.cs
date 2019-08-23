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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveEquityDataSynchronizingEnumeratorTests
    {
        // this test case generates data points in the past, will complete very quickly
        [TestCase(-15, 1)]
        // this test case generates data points in the future, will require at least 10 seconds to complete
        [TestCase(0, 11)]
        public void SynchronizesData(int timeOffsetSeconds, int testTimeSeconds)
        {
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(testTimeSeconds);

            var time = start;
            var tickList1 = Enumerable.Range(0, 10).Select(x => new Tick { Time = time.AddSeconds(x * 1 + timeOffsetSeconds), Value = x }).ToList();
            var tickList2 = Enumerable.Range(0, 5).Select(x => new Tick { Time = time.AddSeconds(x * 2 + timeOffsetSeconds), Value = x + 100 }).ToList();
            var stream1 = tickList1.GetEnumerator();
            var stream2 = tickList2.GetEnumerator();

            var count1 = 0;
            var count2 = 0;
            var previous = DateTime.MinValue;
            var synchronizer = new LiveEquityDataSynchronizingEnumerator(new RealTimeProvider(), DateTimeZone.Utc, stream1, stream2);
            while (synchronizer.MoveNext() && DateTime.UtcNow < end)
            {
                if (synchronizer.Current != null)
                {
                    if (synchronizer.Current.Value < 100)
                    {
                        Assert.AreEqual(count1, synchronizer.Current.Value);
                        count1++;
                    }
                    else
                    {
                        Assert.AreEqual(count2 + 100, synchronizer.Current.Value);
                        count2++;
                    }

                    Assert.That(synchronizer.Current.EndTime, Is.GreaterThanOrEqualTo(previous));
                    previous = synchronizer.Current.EndTime;

                    Log.Trace($"Data point emitted: {synchronizer.Current.EndTime:O} - {synchronizer.Current}");
                }
            }

            Log.Trace($"Total point count: {count1 + count2}");

            Assert.AreEqual(tickList1.Count, count1);
            Assert.AreEqual(tickList2.Count, count2);
            synchronizer.Dispose();
        }
    }
}
