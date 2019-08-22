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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class EnqueableEnumeratorTests
    {
        [Test]
        public void PassesTicksStraightThrough()
        {
            var enumerator = new EnqueueableEnumerator<Tick>();

            // add some ticks
            var currentTime = new DateTime(2015, 10, 08);

            // returns true even if no data present until stop is called
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick1 = new Tick(currentTime, Symbols.SPY, 199.55m, 199, 200) {Quantity = 10};
            enumerator.Enqueue(tick1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(tick1, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick2 = new Tick(currentTime, Symbols.SPY, 199.56m, 199.21m, 200.02m) {Quantity = 5};
            enumerator.Enqueue(tick2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(tick2, enumerator.Current);

            enumerator.Stop();

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            enumerator.Dispose();
        }

        [Test]
        public void RecordsInternalQueueCount()
        {
            var enumerator = new EnqueueableEnumerator<Tick>();

            var currentTime = new DateTime(2015, 12, 01);
            var tick = new Tick(currentTime, Symbols.SPY, 100, 101);
            enumerator.Enqueue(tick);
            Assert.AreEqual(1, enumerator.Count);

            tick = new Tick(currentTime, Symbols.SPY, 100, 101);
            enumerator.Enqueue(tick);
            Assert.AreEqual(2, enumerator.Count);

            enumerator.MoveNext();
            Assert.AreEqual(1, enumerator.Count);

            enumerator.MoveNext();
            Assert.AreEqual(0, enumerator.Count);

            enumerator.Dispose();
        }

        [Test]
        public void RecordsMostRecentlyEnqueuedItem()
        {
            var enumerator = new EnqueueableEnumerator<Tick>();

            var currentTime = new DateTime(2015, 12, 01);
            var tick1 = new Tick(currentTime, Symbols.SPY, 100, 101);
            enumerator.Enqueue(tick1);
            Assert.AreEqual(null, enumerator.Current);
            Assert.AreEqual(tick1, enumerator.LastEnqueued);

            var tick2 = new Tick(currentTime, Symbols.SPY, 100, 101);
            enumerator.Enqueue(tick2);
            Assert.AreEqual(tick2, enumerator.LastEnqueued);

            enumerator.MoveNext();
            Assert.AreEqual(tick1, enumerator.Current);

            enumerator.MoveNext();
            Assert.AreEqual(tick2, enumerator.Current);

            enumerator.Dispose();
        }

        [Test, Category("TravisExclude")]
        public void MoveNextBlocks()
        {
            var finished = new ManualResetEvent(false);
            var enumerator = new EnqueueableEnumerator<Tick>(true);

            // producer
            int count = 0;
            Task.Run(() =>
            {
                while (!finished.WaitOne(TimeSpan.FromMilliseconds(50)))
                {
                    enumerator.Enqueue(new Tick(DateTime.Now, Symbols.SPY, 100, 101));
                    count++;

                    // 5 data points is plenty
                    if (count > 5)
                    {
                        finished.Set();
                        enumerator.Stop();
                    }
                }
            });

            // consumer
            int dequeuedCount = 0;
            bool encounteredError = false;
            var consumerTaskFinished = new ManualResetEvent(false);
            Task.Run(() =>
            {
                while (enumerator.MoveNext())
                {
                    dequeuedCount++;
                    if (enumerator.Current == null)
                    {
                        encounteredError = true;
                    }
                }
                consumerTaskFinished.Set();
            });

            finished.WaitOne(Timeout.Infinite);
            consumerTaskFinished.WaitOne(Timeout.Infinite);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(encounteredError);
            Assert.AreEqual(count, dequeuedCount);

            enumerator.Dispose();
        }
    }
}