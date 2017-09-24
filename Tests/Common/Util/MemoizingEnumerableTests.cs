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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class MemoizingEnumerableTests
    {
        [Test]
        public void EnumeratesList()
        {
            var list = new List<int> {1, 2, 3, 4, 5};
            var memoized = new MemoizingEnumerable<int>(list);
            CollectionAssert.AreEqual(list, memoized);
        }

        [Test]
        public void ChainedMemoizingEnumerables()
        {
            var list = new int [] { 1, 2, 3, 4, 5 };
            var memoized = new MemoizingEnumerable<int>(list);
            var memoized2 = new MemoizingEnumerable<int>(memoized);
            var memoized3 = new MemoizingEnumerable<int>(memoized2);
            CollectionAssert.AreEqual(list, memoized3);
        }

        [Test]
        public void EnumeratesOnce()
        {
            int i = 0;
            var enumerable = Enumerable.Range(0, 10).Select(x => i++);
            var memoized = new MemoizingEnumerable<int>(enumerable);
            // enumerating memoized twice shouldn't matter
            CollectionAssert.AreEqual(memoized.ToList(), memoized.ToList());
        }
    }
}
