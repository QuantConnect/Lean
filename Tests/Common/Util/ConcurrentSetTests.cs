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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ConcurrentSetTests
    {
        [Test]
        public void UnionWith_Matches_HashSet()
        {
            var set = new ConcurrentSet<int> { 1, 2, 3, 4 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.UnionWith(other); return s; });
        }

        [Test]
        public void IntersectWith_Matches_HashSet()
        {
            var set = new ConcurrentSet<int> { 1, 2, 3, 4 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IntersectWith(other); return s; });
        }

        [Test]
        public void ExceptWith_Matches_HashSet()
        {
            var set = new ConcurrentSet<int> { 1, 2, 3, 4 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.ExceptWith(other); return s; });
        }

        [Test]
        public void SymmetricExceptWith_Matches_HashSet()
        {
            var set = new ConcurrentSet<int> { 1, 2, 3, 4 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.SymmetricExceptWith(other); return s; });
        }

        [Test]
        public void IsSubsetOf_Matches_HashSet_True_NonStrict()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsSubsetOf_Matches_HashSet_True_Strict()
        {
            var set = new ConcurrentSet<int> { 4, 5 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsSubsetOf_Matches_HashSet_False()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6, 7 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsSupersetOf_Matches_HashSet_True_NonStrict()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsSupersetOf_Matches_HashSet_True_Strict()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6, 7 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsSupersetOf_Matches_HashSet_False()
        {
            var set = new ConcurrentSet<int> { 4, 5 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsProperSupersetOf_Matches_HashSet_True()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6, 7 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsProperSupersetOf_Matches_HashSet_False()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsProperSubsetOf_Matches_HashSet_True()
        {
            var set = new ConcurrentSet<int> { 4, 5 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void IsProperSubsetOf_Matches_HashSet_False()
        {
            var set = new ConcurrentSet<int> { 4, 5, 6 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void Overlaps_Matches_HashSet_True()
        {
            var set = new ConcurrentSet<int> { 4, 5 };
            var other = new HashSet<int> { 4, 5, 6 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void Overlaps_Matches_HashSet_False()
        {
            var set = new ConcurrentSet<int> { 4, 5 };
            var other = new HashSet<int> { 6, 7 };

            CompareWithHashSet(set, s => { s.IsSubsetOf(other); return s; });
        }

        [Test]
        public void KeepsInsertionOrder_UnionWith()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5 };
            var other = new HashSet<int> { 6, 7, 1, 3 };

            set.UnionWith(other);
            CollectionAssert.AreEqual(set, new[] { 0, 4, 2, 5, 6, 7, 1, 3 });
        }

        [Test]
        public void KeepsInsertionOrder_Add()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5 };

            set.Add(-1);
            set.Add(11);
            CollectionAssert.AreEqual(set, new[] { 0, 4, 2, 5, -1, 11 });
        }

        [Test]
        public void IgnoresDuplicates()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5 };

            set.Add(-1);
            set.Add(-1);
            set.Add(-1);
            CollectionAssert.AreEqual(set, new[] { 0, 4, 2, 5, -1 });
        }

        [Test]
        public void RemoveItem()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5 };

            set.Remove(4);
            set.Remove(4);
            CollectionAssert.AreEqual(set, new[] { 0, 2, 5 });
        }

        [Test]
        public void ClearCollection()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5 };

            set.Clear();
            CollectionAssert.AreEqual(set, new int[] { });
        }

        [Test]
        public void KeepsInsertionOrder_Remove()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5 };

            set.Remove(2);
            CollectionAssert.AreEqual(set, new[] { 0, 4, 5 });
        }

        [Test]
        public void KeepsInsertionOrder()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5, 6, 7, 1, 3 };

            CollectionAssert.AreEqual(set, new[] { 0, 4, 2, 5, 6, 7, 1, 3 });
        }

        [Test]
        public void EnumerableIsThreadSafe()
        {
            var set = new ConcurrentSet<int> { 0, 4, 2, 5, 6, 7, 1, 3 };

            foreach (var value in set)
            {
                set.Remove(value);
            }
        }

        [Test]
        public void ConsolidatorsEnumeratedInOrder()
        {
            var set = new ConcurrentSet<IDataConsolidator>();
            for (var i = 0; i < 500; i++)
            {
                set.Add(new TestConsolidator(i));
            }

            var j = 0;
            foreach (var value in set)
            {
                Assert.AreEqual(j, (value as TestConsolidator).Id);
                j++;
            }
        }

        private void CompareWithHashSet<T>(ConcurrentSet<T> set, Func<ISet<T>, ISet<T>> func)
        {
            var asHashSet = set.ToHashSet();
            var expected = func(asHashSet);
            var actual = func(set);
            CollectionAssert.AreEquivalent(expected, actual);
        }

        private class TestConsolidator : PeriodCountConsolidatorBase<QuoteBar, QuoteBar>
        {
            public int Id { get; }

            public TestConsolidator(int maxCount) : base(maxCount)
            {
                Id = maxCount;
            }

            protected override void AggregateBar(ref QuoteBar workingBar, QuoteBar data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
