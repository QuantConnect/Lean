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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class FastForwardEnumeratorTests
    {
        [Test]
        public void FastForwardsOldData()
        {
            var start = new DateTime(2015, 10, 10, 13, 0, 0);
            var data = new List<Tick>
            {
                new Tick {Time = start.AddMinutes(-1)},
                new Tick {Time = start.AddSeconds(-1)},
                new Tick {Time = start.AddSeconds(0)},
                new Tick {Time = start.AddSeconds(1)},
            };

            var timeProvider = new ManualTimeProvider(start, TimeZones.Utc);
            var fastForward = new FastForwardEnumerator(data.GetEnumerator(), timeProvider, TimeZones.Utc, TimeSpan.FromSeconds(0.5));

            Assert.IsTrue(fastForward.MoveNext());
            Assert.AreEqual(start, fastForward.Current.Time);
            fastForward.Dispose();
        }
        [Test]
        public void FastForwardsOldDataAllowsEquals()
        {
            var start = new DateTime(2015, 10, 10, 13, 0, 0);
            var data = new List<Tick>
            {
                new Tick {Time = start.AddMinutes(-1)},
                new Tick {Time = start.AddSeconds(-1)},
                new Tick {Time = start.AddSeconds(0)},
                new Tick {Time = start.AddSeconds(1)},
            };

            var timeProvider = new ManualTimeProvider(start, TimeZones.Utc);
            var fastForward = new FastForwardEnumerator(data.GetEnumerator(), timeProvider, TimeZones.Utc, TimeSpan.FromSeconds(1));

            Assert.IsTrue(fastForward.MoveNext());
            Assert.AreEqual(start.AddSeconds(-1), fastForward.Current.Time);
            fastForward.Dispose();
        }
        [Test]
        public void FiltersOutPastData()
        {
            var start = new DateTime(2015, 10, 10, 13, 0, 0);
            var data = new List<Tick>
            {
                new Tick {Time = start.AddMinutes(-1)},
                new Tick {Time = start.AddSeconds(-1)},
                new Tick {Time = start.AddSeconds(1)},
                new Tick {Time = start.AddSeconds(0)},
                new Tick {Time = start.AddSeconds(2)}
            };

            var timeProvider = new ManualTimeProvider(start, TimeZones.Utc);
            var fastForward = new FastForwardEnumerator(data.GetEnumerator(), timeProvider, TimeZones.Utc, TimeSpan.FromSeconds(0.5));

            Assert.IsTrue(fastForward.MoveNext());
            Assert.AreEqual(start.AddSeconds(1), fastForward.Current.Time);

            Assert.IsTrue(fastForward.MoveNext());
            Assert.AreEqual(start.AddSeconds(2), fastForward.Current.Time);
            fastForward.Dispose();
        }

        [Test]
        public void CurrentIsNullWhenEnumeratorReturnsFalse()
        {
            var start = new DateTime(2015, 10, 10, 13, 0, 0);
            var data = new List<Tick>
            {
                new Tick {Time = start.AddSeconds(0)}
            };

            var timeProvider = new ManualTimeProvider(start, TimeZones.Utc);
            var fastForward = new FastForwardEnumerator(data.GetEnumerator(), timeProvider, TimeZones.Utc, TimeSpan.FromSeconds(0.5));

            Assert.IsTrue(fastForward.MoveNext());
            Assert.IsNotNull(fastForward.Current);

            Assert.IsFalse(fastForward.MoveNext());
            Assert.IsNull(fastForward.Current);
            fastForward.Dispose();
        }
    }
}
