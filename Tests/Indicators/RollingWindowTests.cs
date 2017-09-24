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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class RollingWindowTests
    {
        [Test]
        public void NewWindowIsEmpty()
        {
            var window = new RollingWindow<int>(1);
            Assert.AreEqual(1, window.Size);
            Assert.AreEqual(0, window.Count);
            Assert.AreEqual(0, window.Samples);
            Assert.IsFalse(window.IsReady);
        }

        [Test]
        public void AddsData()
        {
            var window = new RollingWindow<int>(2);
            Assert.AreEqual(0, window.Count);
            Assert.AreEqual(0, window.Samples);
            Assert.AreEqual(2, window.Size);
            Assert.IsFalse(window.IsReady);

            window.Add(1);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(1, window.Samples);
            Assert.AreEqual(2, window.Size);
            Assert.IsFalse(window.IsReady);

            // add one more and the window is ready
            window.Add(2);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(2, window.Samples);
            Assert.AreEqual(2, window.Size);
            Assert.IsTrue(window.IsReady);
        }

        [Test]
        public void OldDataFallsOffBackOfWindow()
        {
            var window = new RollingWindow<int>(1);
            Assert.IsFalse(window.IsReady);

            // add one and the window is ready, but MostRecentlyRemoved throws

            window.Add(0);
            Assert.Throws<InvalidOperationException>(() => { var x = window.MostRecentlyRemoved; });
            Assert.AreEqual(1, window.Count);
            Assert.IsTrue(window.IsReady);

            // add another one and MostRecentlyRemoved is available

            window.Add(1);
            Assert.AreEqual(0, window.MostRecentlyRemoved);
            Assert.AreEqual(1, window.Count);
            Assert.IsTrue(window.IsReady);
        }

        [Test]
        public void IndexingBasedOnReverseInsertedOrder()
        {
            var window = new RollingWindow<int>(3);
            Assert.AreEqual(3, window.Size);

            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);

            window.Add(1);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(0, window[1]);
            Assert.AreEqual(1, window[0]);

            window.Add(2);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(0, window[2]);
            Assert.AreEqual(1, window[1]);
            Assert.AreEqual(2, window[0]);

            window.Add(3);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(1, window[2]);
            Assert.AreEqual(2, window[1]);
            Assert.AreEqual(3, window[0]);
        }

        [Test]
        public void EnumeratesAsExpected()
        {
            var window = new RollingWindow<int>(3) { 0, 1, 2 };
            var inOrder = window.ToList();
            Assert.AreEqual(2, inOrder[0]);
            Assert.AreEqual(1, inOrder[1]);
            Assert.AreEqual(0, inOrder[2]);
        }

        [Test]
        public void ResetsProperly()
        {
            var window = new RollingWindow<int>(3) { 0, 1, 2 };
            window.Reset();
            Assert.AreEqual(0, window.Samples);
        }

        [Test]
        public void RetrievesNonZeroIndexProperlyAfterReset()
        {
            var window = new RollingWindow<int>(3);
            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);

            window.Add(1);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(0, window[1]);
            Assert.AreEqual(1, window[0]);

            window.Add(2);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(0, window[2]);
            Assert.AreEqual(1, window[1]);
            Assert.AreEqual(2, window[0]);

            window.Add(3);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(1, window[2]);
            Assert.AreEqual(2, window[1]);
            Assert.AreEqual(3, window[0]);

            window.Reset();
            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);

            window.Add(1);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(0, window[1]);
            Assert.AreEqual(1, window[0]);

            window.Add(2);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(0, window[2]);
            Assert.AreEqual(1, window[1]);
            Assert.AreEqual(2, window[0]);

            window.Add(3);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(1, window[2]);
            Assert.AreEqual(2, window[1]);
            Assert.AreEqual(3, window[0]);
        }

        [Test]
        public void ThrowsWhenIndexingOutOfRange()
        {
            var window = new RollingWindow<int>(1);
            Assert.IsFalse(window.IsReady);

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = window[0]; });

            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);
            Assert.IsTrue(window.IsReady);

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = window[1]; });
        }
    }
}
