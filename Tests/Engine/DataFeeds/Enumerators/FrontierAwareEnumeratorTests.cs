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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class FrontierAwareEnumeratorTests
    {
        [Test]
        public void ReturnsTrueWhenNextDataIsAheadOfFrontier()
        {
            var currentTime = new DateTime(2015, 10, 13);
            var timeProvider = new ManualTimeProvider(currentTime);
            var underlying = new List<Tick>
            {
                new Tick {Time = currentTime.AddSeconds(1)}
            };

            var offsetProvider = new TimeZoneOffsetProvider(DateTimeZone.Utc, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1));
            var frontierAware = new FrontierAwareEnumerator(underlying.GetEnumerator(), timeProvider, offsetProvider);

            Assert.IsTrue(frontierAware.MoveNext());
            Assert.IsNull(frontierAware.Current);
            frontierAware.Dispose();
        }

        [Test]
        public void YieldsDataWhenFrontierPasses()
        {
            var currentTime = new DateTime(2015, 10, 13);
            var timeProvider = new ManualTimeProvider(currentTime);
            var underlying = new List<Tick>
            {
                new Tick {Time = currentTime.AddSeconds(1)}
            };

            var offsetProvider = new TimeZoneOffsetProvider(DateTimeZone.Utc, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1));
            var frontierAware = new FrontierAwareEnumerator(underlying.GetEnumerator(), timeProvider, offsetProvider);

            timeProvider.AdvanceSeconds(1);

            Assert.IsTrue(frontierAware.MoveNext());
            Assert.IsNotNull(frontierAware.Current);
            Assert.AreEqual(underlying[0], frontierAware.Current);
            frontierAware.Dispose();
        }

        [Test]
        public void YieldsFutureDataAtCorrectTime()
        {
            var currentTime = new DateTime(2015, 10, 13);
            var timeProvider = new ManualTimeProvider(currentTime);
            var underlying = new List<Tick>
            {
                new Tick {Time = currentTime.AddSeconds(10)}
            };

            var offsetProvider = new TimeZoneOffsetProvider(DateTimeZone.Utc, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1));
            var frontierAware = new FrontierAwareEnumerator(underlying.GetEnumerator(), timeProvider, offsetProvider);

            for (int i = 0; i < 10; i++)
            {
                timeProvider.AdvanceSeconds(1);
                Assert.IsTrue(frontierAware.MoveNext());
                if (i < 9)
                {
                    Assert.IsNull(frontierAware.Current);
                }
                else
                {
                    Assert.IsNotNull(frontierAware.Current);
                    Assert.AreEqual(underlying[0], frontierAware.Current);
                }
            }
            frontierAware.Dispose();
        }
    }
}
