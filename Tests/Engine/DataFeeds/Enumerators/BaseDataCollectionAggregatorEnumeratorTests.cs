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
*/

using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class BaseDataCollectionAggregatorEnumeratorTests
    {
        [Test]
        public void AggregatesUntilNull()
        {
            var time = new DateTime(2015, 10, 20);
            var underlying = Enumerable.Range(0, 5).Select(x => new Tick { Time = time }).ToList();
            underlying.AddRange(new Tick[] { null, null, null });

            var aggregator = new BaseDataCollectionAggregatorEnumerator(underlying.GetEnumerator(), Symbols.SPY);

            Assert.IsTrue(aggregator.MoveNext());
            Assert.IsNotNull(aggregator.Current);
            Assert.AreEqual(5, aggregator.Current.Data.Count);

            aggregator.Dispose();
        }
        [Test]
        public void AggregatesUntilTimeChange()
        {
            var time = new DateTime(2015, 10, 20);
            var underlying = Enumerable.Range(0, 5).Select(x => new Tick { Time = time }).ToList();
            underlying.AddRange(Enumerable.Range(0, 5).Select(x => new Tick {Time = time.AddSeconds(1)}));

            var aggregator = new BaseDataCollectionAggregatorEnumerator(underlying.GetEnumerator(), Symbols.SPY);

            Assert.IsTrue(aggregator.MoveNext());
            Assert.IsNotNull(aggregator.Current);
            Assert.AreEqual(5, aggregator.Current.Data.Count);

            aggregator.Dispose();
        }
    }
}
