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
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class SynchronizingEnumeratorTests
    {
        [Test]
        public void SynchronizesData()
        {
            var time = new DateTime(2016, 03, 03, 12, 05, 00);
            var stream1 = Enumerable.Range(0, 10).Select(x => new Tick {Time = time.AddSeconds(x * 1)}).GetEnumerator();
            var stream2 = Enumerable.Range(0, 5).Select(x => new Tick {Time = time.AddSeconds(x * 2)}).GetEnumerator();
            var stream3 = Enumerable.Range(0, 20).Select(x => new Tick {Time = time.AddSeconds(x * 0.5)}).GetEnumerator();

            var previous = DateTime.MinValue;
            var synchronizer = new SynchronizingEnumerator(stream1, stream2, stream3);
            while (synchronizer.MoveNext())
            {
                Assert.That(synchronizer.Current.EndTime, Is.GreaterThanOrEqualTo(previous));
                previous = synchronizer.Current.EndTime;
            }
        }
    }
}
