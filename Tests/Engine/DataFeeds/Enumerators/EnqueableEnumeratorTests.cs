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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class EnqueableEnumeratorTests
    {
        [Test]
        public void PassesTicksStraightThrough()
        {
            var enumerator = new EnqueableEnumerator<Tick>();

            // add some ticks
            var currentTime = new DateTime(2015, 10, 08);

            // returns true even if no data present until stop is called
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick1 = new Tick(currentTime, "SPY", 199.55m, 199, 200) {Quantity = 10};
            enumerator.Enqueue(tick1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(tick1, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick2 = new Tick(currentTime, "SPY", 199.56m, 199.21m, 200.02m) {Quantity = 5};
            enumerator.Enqueue(tick2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(tick2, enumerator.Current);

            enumerator.Stop();

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }
    }
}