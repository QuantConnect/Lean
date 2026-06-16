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
using System.Collections.Generic;
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
            Assert.AreEqual(10, pool.PartitionCount);
        }

        [Test]
        public void ClampsMinAndMaxWorkers()
        {
            // min is clamped to at least 1, and to at most max
            using var pool = new DynamicWorkerPool<int>(_ => { }, minWorkers: 0, maxWorkers: 1);
            pool.Start();

            Assert.AreEqual(1, pool.WorkerCount);
            Assert.AreEqual(1, pool.PartitionCount);
        }

        [Test]
        public void ThrowsOnNullHandler()
        {
            Assert.Throws<ArgumentNullException>(() => new DynamicWorkerPool<int>(null, 1, 2));
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
                // keep feeding work while the workers stay busy on the gate, so the starving pool grows
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
            // workers process instantly, so there is never a starving backlog and the pool stays minimal
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
            const int maxWorkers = 10;
            const int keysCount = maxWorkers;       // one logical key per partition
            const int itemsPerKey = 50;
            using var gate = new ManualResetEventSlim(false);
            var sequence = new ConcurrentQueue<(int Key, int Value)>();

            using var pool = new DynamicWorkerPool<(int Key, int Value)>(item =>
            {
                gate.Wait();
                sequence.Enqueue(item);
            }, minWorkers: 2, maxWorkers: maxWorkers);
            pool.Start();

            // interleave items across keys; items with the same key must keep their relative order
            for (var n = 0; n < itemsPerKey; n++)
            {
                for (var key = 0; key < keysCount; key++)
                {
                    pool.Enqueue(key, (key, n));
                }
            }

            gate.Set();
            Assert.IsTrue(pool.WaitForIdle(TimeSpan.FromSeconds(10)));

            foreach (var group in sequence.ToList().GroupBy(x => x.Key))
            {
                var values = group.Select(x => x.Value).ToList();
                CollectionAssert.AreEqual(Enumerable.Range(0, itemsPerKey).ToList(), values,
                    $"key {group.Key} was processed out of order");
            }
        }

        [Test]
        public void NeverProcessesSamePartitionConcurrently()
        {
            const int maxWorkers = 10;
            var active = new ConcurrentDictionary<long, int>();
            var overlapDetected = 0;
            const int count = 2000;
            using var done = new CountdownEvent(count);

            using var pool = new DynamicWorkerPool<long>(item =>
            {
                // items sharing item % maxWorkers land on the same partition and must never overlap
                var partition = item % maxWorkers;
                if (active.AddOrUpdate(partition, 1, (_, c) => c + 1) > 1)
                {
                    Interlocked.Exchange(ref overlapDetected, 1);
                }
                Thread.SpinWait(50);
                active.AddOrUpdate(partition, 0, (_, c) => c - 1);
                done.Signal();
            }, minWorkers: 4, maxWorkers: maxWorkers);
            pool.Start();

            for (var i = 0; i < count; i++)
            {
                // many distinct keys colliding on the same partitions (key % maxWorkers)
                pool.Enqueue(i, i);
            }

            Assert.IsTrue(done.Wait(15000));
            Assert.AreEqual(0, overlapDetected, "the same partition was processed by two workers at once");
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

        [Test]
        public void EnqueueRoutesNegativeKeysToValidPartition()
        {
            var processed = new ConcurrentBag<int>();
            using var done = new CountdownEvent(4);
            using var pool = new DynamicWorkerPool<int>(i => { processed.Add(i); done.Signal(); }, 1, 4);
            pool.Start();

            // negative keys must still map to a valid partition without throwing
            pool.Enqueue(-1, 10);
            pool.Enqueue(-7, 20);
            pool.Enqueue(-13, 30);
            pool.Enqueue(-100, 40);

            Assert.IsTrue(done.Wait(5000));
            CollectionAssert.AreEquivalent(new[] { 10, 20, 30, 40 }, processed);
        }

        [Test]
        public void DisposeStopsWorkers()
        {
            var pool = new DynamicWorkerPool<int>(_ => { }, 2, 4);
            pool.Start();
            pool.Enqueue(0, 0);
            Assert.IsTrue(pool.WaitForIdle(TimeSpan.FromSeconds(5)));

            Assert.DoesNotThrow(() => pool.Dispose());
            // disposing again is safe
            Assert.DoesNotThrow(() => pool.Dispose());
        }

        [Test]
        public void EnqueueBeforeStartIsProcessedOnStart()
        {
            var processed = new ConcurrentBag<int>();
            using var done = new CountdownEvent(3);
            using var pool = new DynamicWorkerPool<int>(i => { processed.Add(i); done.Signal(); }, 2, 4);

            // enqueue before Start: items wait in their partitions until workers come up
            pool.Enqueue(0, 1);
            pool.Enqueue(1, 2);
            pool.Enqueue(2, 3);

            pool.Start();

            Assert.IsTrue(done.Wait(5000));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, processed);
        }
    }
}
