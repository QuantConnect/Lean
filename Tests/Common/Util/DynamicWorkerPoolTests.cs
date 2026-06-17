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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class DynamicWorkerPoolTests
    {
        [Test]
        public void StartsWithMinimumWorkers()
        {
            using var pool = new DynamicWorkerPool<int>(_ => { }, minWorkers: 2, maxWorkers: 10);
            pool.Start();

            Assert.AreEqual(2, pool.WorkerCount);
        }

        [Test]
        public void ProcessesAllEnqueuedItems()
        {
            const int count = 200;
            var processed = new ConcurrentBag<int>();
            using var done = new CountdownEvent(count);
            using var pool = new DynamicWorkerPool<int>(i =>
            {
                processed.Add(i);
                done.Signal();
            }, minWorkers: 2, maxWorkers: 10);
            pool.Start();

            for (var i = 0; i < count; i++)
            {
                pool.Enqueue(i, i);
            }

            Assert.IsTrue(done.Wait(10000));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, count), processed);
        }

        [Test]
        public void GrowsUnderBacklogUpToMaximum([Values(10, 3)] int maxWorkers)
        {
            using var gate = new ManualResetEventSlim(false);
            using var pool = new DynamicWorkerPool<int>(_ => gate.Wait(), minWorkers: 2, maxWorkers: maxWorkers);
            pool.Start();
            Assert.AreEqual(2, pool.WorkerCount);

            try
            {
                // workers block on the gate, so the queues pile up and the pool grows to the maximum
                var key = 0;
                var reachedMax = SpinWait.SpinUntil(() =>
                {
                    if (key < 1000)
                    {
                        pool.Enqueue(key, key);
                        key++;
                    }
                    return pool.WorkerCount >= maxWorkers;
                }, 10000);

                Assert.IsTrue(reachedMax, $"Pool did not grow to the maximum, current size: {pool.WorkerCount}");
                // never grows beyond the configured maximum
                Assert.AreEqual(maxWorkers, pool.WorkerCount);
            }
            finally
            {
                gate.Set();
            }
        }

        [Test]
        public void DoesNotGrowWhenWorkersKeepUp()
        {
            // workers process instantly, so the queues never pile up and the pool stays minimal
            using var pool = new DynamicWorkerPool<int>(_ => { }, minWorkers: 2, maxWorkers: 10);
            pool.Start();

            for (var i = 0; i < 50; i++)
            {
                pool.Enqueue(i, i);
                Thread.Sleep(1);
            }

            Assert.IsTrue(pool.WaitForIdle(TimeSpan.FromSeconds(5)));
            Assert.AreEqual(2, pool.WorkerCount);
        }

        [Test]
        public void PreservesOrderPerKey()
        {
            // with a stable pool size, items sharing a key go to the same queue and keep their order
            const int workers = 10;
            const int itemsPerKey = 50;
            var sequence = new ConcurrentQueue<(int Key, int Value)>();
            using var pool = new DynamicWorkerPool<(int Key, int Value)>(sequence.Enqueue,
                minWorkers: workers, maxWorkers: workers);
            pool.Start();

            for (var n = 0; n < itemsPerKey; n++)
            {
                for (var key = 0; key < workers; key++)
                {
                    pool.Enqueue(key, (key, n));
                }
            }

            Assert.IsTrue(pool.WaitForIdle(TimeSpan.FromSeconds(10)));

            foreach (var group in sequence.ToList().GroupBy(x => x.Key))
            {
                var values = group.Select(x => x.Value).ToList();
                CollectionAssert.AreEqual(Enumerable.Range(0, itemsPerKey).ToList(), values,
                    $"key {group.Key} was processed out of order");
            }
        }

        [Test]
        public void WaitForIdleReturnsFalseOnTimeoutAndTrueWhenDrained()
        {
            using var gate = new ManualResetEventSlim(false);
            using var pool = new DynamicWorkerPool<int>(_ => gate.Wait(), minWorkers: 2, maxWorkers: 4);
            pool.Start();

            pool.Enqueue(0, 0);
            // a worker is stuck on the gate, so the pool is busy
            Assert.IsFalse(pool.WaitForIdle(TimeSpan.FromMilliseconds(200)));

            gate.Set();
            Assert.IsTrue(pool.WaitForIdle(TimeSpan.FromSeconds(5)));
            Assert.IsFalse(pool.IsBusy);
        }

        [Test]
        public void InvokesOnErrorWhenHandlerThrows()
        {
            using var raised = new ManualResetEventSlim(false);
            Exception captured = null;
            using var pool = new DynamicWorkerPool<int>(
                _ => throw new InvalidOperationException("boom"),
                minWorkers: 1,
                maxWorkers: 1,
                onError: err => { captured = err; raised.Set(); });
            pool.Start();

            pool.Enqueue(0, 0);

            Assert.IsTrue(raised.Wait(5000));
            Assert.IsInstanceOf<InvalidOperationException>(captured);
        }
    }
}
