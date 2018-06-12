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

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveBaseDataSynchronizingEnumeratorTests
    {
        [Test]
        public void SynchronizesData()
        {
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(15);

            var time = start;
            var stream1 = Enumerable.Range(0, 10).Select(x => new Tick { Time = time.AddSeconds(1) }).GetEnumerator();
            var stream2 = Enumerable.Range(0, 5).Select(x => new Tick { Time = time.AddSeconds(2) }).GetEnumerator();
            var stream3 = Enumerable.Range(0, 20).Select(x => new Tick { Time = time.AddSeconds(0.5) }).GetEnumerator();

            var previous = DateTime.MinValue;
            var synchronizer = new LiveBaseDataSynchronizingEnumerator(new RealTimeProvider(), DateTimeZone.Utc, stream1, stream2, stream3);
            while (synchronizer.MoveNext() && DateTime.UtcNow < end)
            {
                if (synchronizer.Current != null)
                {
                    Assert.That(synchronizer.Current.EndTime, Is.GreaterThanOrEqualTo(previous));
                    previous = synchronizer.Current.EndTime;
                }
            }
        }
    }
}
