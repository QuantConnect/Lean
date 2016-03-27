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

using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class BusyBlockingCollectionTests
    {
        [Test]
        public void IsNotBusyWithZeroItemsWaiting()
        {
            var collection = new BusyBlockingCollection<int>();
            Assert.IsTrue(collection.WaitHandle.WaitOne(0));
        }

        [Test]
        public void IsBusyWithItemsWaiting()
        {
            var collection = new BusyBlockingCollection<int>();
            collection.Add(1);
            Assert.IsFalse(collection.WaitHandle.WaitOne(0));
        }

        [Test]
        public void GetConsumingEnumerableReturnsItemsInOrder()
        {
            var collection = new BusyBlockingCollection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.CompleteAdding();
            CollectionAssert.AreEquivalent(new[]{1,2,3}, collection.GetConsumingEnumerable());
        }

        [Test]
        public void WaitForProcessingCompletedDuringGetConsumingEnumerable()
        {
            var collection = new BusyBlockingCollection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.CompleteAdding();
            Assert.IsFalse(collection.WaitHandle.WaitOne(0));
            foreach (var item in collection.GetConsumingEnumerable())
            {
                Assert.IsFalse(collection.WaitHandle.WaitOne(0));
            }
            Assert.IsTrue(collection.WaitHandle.WaitOne(0));
        }
    }
}
