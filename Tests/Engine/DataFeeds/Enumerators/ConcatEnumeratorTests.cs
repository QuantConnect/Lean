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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class ConcatEnumeratorTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void SkipsBasedOnEndTime(bool skipsBasedOnEndTime)
        {
            var time = new DateTime(2020, 1, 1);
            var enumerator1 = new List<BaseData>
            {
                new Tick(time, Symbols.SPY, 10, 10)
            }.GetEnumerator();
            var enumerator2 = new List<BaseData>
            {
                new Tick(time.AddSeconds(-1), Symbols.SPY, 20, 20), //should be skipped because end time is before previous tick
                new Tick(time.AddSeconds(1), Symbols.SPY, 30, 30)
            }.GetEnumerator();

            var concat = new ConcatEnumerator(skipsBasedOnEndTime, enumerator1, enumerator2);

            Assert.IsTrue(concat.MoveNext());
            Assert.AreEqual(10, (concat.Current as Tick).AskPrice);

            if (!skipsBasedOnEndTime)
            {
                Assert.IsTrue(concat.MoveNext());
                Assert.AreEqual(20, (concat.Current as Tick).AskPrice);
            }

            Assert.IsTrue(concat.MoveNext());
            Assert.AreEqual(30, (concat.Current as Tick).AskPrice);

            Assert.IsFalse(concat.MoveNext());
            Assert.IsNull(concat.Current);

            concat.Dispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EmptyNullEnumerators(bool skipsBasedOnEndTime)
        {
            var time = new DateTime(2020, 1, 1);
            // empty enumerators
            var enumerator1 = new List<BaseData>().GetEnumerator();
            var enumerator2 = new List<BaseData>().GetEnumerator();

            var enumerator3 = new List<BaseData>
            {
                new Tick(time, Symbols.SPY, 10, 10),
                new Tick(time.AddSeconds(-1), Symbols.SPY, 20, 20),
                new Tick(time.AddSeconds(1), Symbols.SPY, 30, 30)
            }.GetEnumerator();

            var concat = new ConcatEnumerator(
                skipsBasedOnEndTime,
                enumerator1,
                null,
                enumerator2,
                enumerator3
            );

            Assert.IsTrue(concat.MoveNext());
            Assert.AreEqual(10, (concat.Current as Tick).AskPrice);

            Assert.IsTrue(concat.MoveNext());
            Assert.AreEqual(20, (concat.Current as Tick).AskPrice);

            Assert.IsTrue(concat.MoveNext());
            Assert.AreEqual(30, (concat.Current as Tick).AskPrice);

            Assert.IsFalse(concat.MoveNext());
            Assert.IsNull(concat.Current);

            concat.Dispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DropsEnumeratorsReturningNullAndTrue(bool skipsBasedOnEndTime)
        {
            var enumerator1 = new TestEnumerator();
            var enumerator2 = new TestEnumerator();

            var concat = new ConcatEnumerator(skipsBasedOnEndTime, enumerator1, null, enumerator2);

            Assert.IsTrue(concat.MoveNext());

            Assert.IsNull(concat.Current);
            Assert.IsTrue(enumerator1.Disposed);
            Assert.IsFalse(enumerator2.Disposed);
            Assert.AreEqual(1, enumerator2.MoveNextCount);

            Assert.IsTrue(concat.MoveNext());

            // we assert it just keeps the last enumerator and drops the rest
            Assert.IsTrue(enumerator1.Disposed);
            Assert.IsFalse(enumerator2.Disposed);
            Assert.IsNull(concat.Current);
            Assert.AreEqual(2, enumerator2.MoveNextCount);

            concat.Dispose();
        }

        private class TestEnumerator : IEnumerator<BaseData>
        {
            public bool Disposed { get; private set; }
            public int MoveNextCount { get; private set; }
            public BaseData Current => null;

            object IEnumerator.Current => null;

            public void Dispose()
            {
                Disposed = true;
            }

            public bool MoveNext()
            {
                MoveNextCount++;
                return true;
            }

            public void Reset() { }
        }
    }
}
