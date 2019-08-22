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
using Moq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class RefreshEnumeratorTests
    {
        [Test]
        public void RefreshesEnumeratorOnFirstMoveNext()
        {
            var refreshed = false;
            var refresher = new RefreshEnumerator<int?>(() =>
            {
                refreshed = true;
                return new List<int?>().GetEnumerator();
            });

            refresher.MoveNext();
            Assert.IsTrue(refreshed);

            refresher.Dispose();
        }

        [Test]
        public void MoveNextReturnsTrueWhenUnderlyingEnumeratorReturnsFalse()
        {
            var refresher = new RefreshEnumerator<int?>(() => new List<int?>().GetEnumerator());
            Assert.IsTrue(refresher.MoveNext());

            refresher.Dispose();
        }

        [Test]
        public void CurrentIsDefault_T_WhenUnderlyingEnumeratorReturnsFalse()
        {
            var refresher = new RefreshEnumerator<int?>(() => new List<int?>().GetEnumerator());
            refresher.MoveNext();
            Assert.AreEqual(default(int?), refresher.Current);

            refresher.Dispose();
        }

        [Test]
        public void UnderlyingEnumeratorDisposed_WhenUnderlyingEnumeratorReturnsFalse()
        {
            var fakeEnumerator = new Mock<IEnumerator<int?>>();
            fakeEnumerator.Setup(e => e.MoveNext()).Returns(false);
            fakeEnumerator.Setup(e => e.Dispose()).Verifiable();
            var refresher = new RefreshEnumerator<int?>(() => fakeEnumerator.Object);
            refresher.MoveNext();

            fakeEnumerator.Verify(enumerator => enumerator.Dispose(), Times.Once);

            refresher.Dispose();
        }

        [Test]
        public void DisposeCallsUnderlyingDispose()
        {
            var fakeEnumerator = new Mock<IEnumerator<int?>>();
            fakeEnumerator.Setup(e => e.MoveNext()).Returns(true);
            fakeEnumerator.Setup(e => e.Dispose()).Verifiable();
            var refresher = new RefreshEnumerator<int?>(() => fakeEnumerator.Object);
            refresher.MoveNext();
            refresher.Dispose();

            fakeEnumerator.Verify(enumerator => enumerator.Dispose(), Times.Once);

            refresher.Dispose();
        }

        [Test]
        public void ResetCallsUnderlyingReset()
        {
            var fakeEnumerator = new Mock<IEnumerator<int?>>();
            fakeEnumerator.Setup(e => e.MoveNext()).Returns(true);
            fakeEnumerator.Setup(e => e.Reset()).Verifiable();
            var refresher = new RefreshEnumerator<int?>(() => fakeEnumerator.Object);
            refresher.MoveNext();
            refresher.Reset();

            fakeEnumerator.Verify(enumerator => enumerator.Reset(), Times.Once);

            refresher.Dispose();
        }

        [Test]
        public void RefreshesAfterMoveNextReturnsFalse()
        {
            var refreshCount = 0;
            var list = new List<int?> {1, 2};
            var refresher = new RefreshEnumerator<int?>(() =>
            {
                refreshCount++;
                return list.GetEnumerator();
            });

            Assert.IsTrue(refresher.MoveNext());
            Assert.AreEqual(1, refreshCount);
            Assert.AreEqual(1, refresher.Current);

            Assert.IsTrue(refresher.MoveNext());
            Assert.AreEqual(1, refreshCount);
            Assert.AreEqual(2, refresher.Current);

            Assert.IsTrue(refresher.MoveNext());
            Assert.AreEqual(1, refreshCount);
            Assert.AreEqual(default(int?), refresher.Current);

            Assert.IsTrue(refresher.MoveNext());
            Assert.AreEqual(2, refreshCount);
            Assert.AreEqual(1, refresher.Current);

            Assert.IsTrue(refresher.MoveNext());
            Assert.AreEqual(2, refreshCount);
            Assert.AreEqual(2, refresher.Current);

            refresher.Dispose();
        }
    }
}
