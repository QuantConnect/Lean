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

using NUnit.Framework;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OrderRequestProcessingPoolTests
    {
        [Test]
        public void DisposeDrainsQueuedAndParkedRequestsThroughTheProcessingLoop()
        {
            using var gate = new ManualResetEventSlim(false);
            var dispatched = new ConcurrentQueue<OrderRequest>();
            var processed = new ConcurrentQueue<OrderRequest>();
            Exception processingError = null;
            var pool = new OrderRequestProcessingPool(concurrencyEnabled: true, minimumThreads: 1, maximumThreads: 2,
                request =>
                {
                    processed.Enqueue(request);
                    gate.Wait();
                },
                exception => processingError = exception);
            // shrink the shutdown budget so a regression doesn't wait out the production timeout
            pool.ShutdownTimeout = TimeSpan.FromSeconds(5);

            try
            {
                var symbol = Symbols.SPY;
                var reference = new DateTime(2025, 07, 03, 10, 0, 0);

                // the order we track, its submit claims a worker and blocks on the gate
                var submit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                submit.SetOrderId(1);
                var order = Order.CreateOrder(submit);
                dispatched.Enqueue(submit);
                pool.Dispatch(submit, order);

                // keep feeding unrelated orders until both workers are blocked inside the handler
                var fillerId = 1000;
                var saturated = SpinWait.SpinUntil(() =>
                {
                    var filler = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                    filler.SetOrderId(++fillerId);
                    dispatched.Enqueue(filler);
                    pool.Dispatch(filler, Order.CreateOrder(filler));
                    return processed.Count >= 2;
                }, 10000);
                Assert.IsTrue(saturated, "the workers never got busy");

                // left behind when Dispose starts: the submit waits in the ready queue, the update and cancel parked
                var queuedSubmit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                queuedSubmit.SetOrderId(2);
                dispatched.Enqueue(queuedSubmit);
                pool.Dispatch(queuedSubmit, Order.CreateOrder(queuedSubmit));
                var update = new UpdateOrderRequest(reference, order.Id, new UpdateOrderFields());
                var cancel = new CancelOrderRequest(reference, order.Id, "");
                dispatched.Enqueue(update);
                dispatched.Enqueue(cancel);
                pool.Dispatch(update, order);
                pool.Dispatch(cancel, order);

                // free the workers shortly after Dispose has re-queued the parked backlog, so they chew it all
                var release = new Thread(() => { Thread.Sleep(300); gate.Set(); }) { IsBackground = true };
                release.Start();
                var stopwatch = Stopwatch.StartNew();
                pool.Dispose();
                stopwatch.Stop();

                // every dispatched request reached the handler exactly once, none lost and none duplicated
                CollectionAssert.AreEquivalent(dispatched, processed);
                Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(10), "Dispose did not return within the shutdown budget");
                Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
            }
            finally
            {
                gate.Set();
                pool.DisposeSafely();
            }
        }

        [Test]
        public void DisposeInterruptsStuckWorkersAndDrainsTheBacklogOnANewThread()
        {
            using var gate = new ManualResetEventSlim(false);
            var processed = new ConcurrentQueue<(OrderRequest Request, string Thread)>();
            var interruptedWorkers = 0;
            Exception processingError = null;
            var pool = new OrderRequestProcessingPool(concurrencyEnabled: true, minimumThreads: 2, maximumThreads: 2,
                request =>
                {
                    processed.Enqueue((request, Thread.CurrentThread.Name));
                    if (request.Tag == "poison")
                    {
                        try
                        {
                            // simulates a brokerage call pinned in a wait that only a thread interrupt can free
                            gate.Wait();
                        }
                        catch (ThreadInterruptedException)
                        {
                            Interlocked.Increment(ref interruptedWorkers);
                            throw;
                        }
                    }
                },
                exception => processingError = exception);
            pool.ShutdownTimeout = TimeSpan.FromMilliseconds(500);

            try
            {
                var symbol = Symbols.SPY;
                var reference = new DateTime(2025, 07, 03, 10, 0, 0);
                SubmitOrderRequest CreateSubmit(int orderId, string tag)
                {
                    var request = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, tag);
                    request.SetOrderId(orderId);
                    return request;
                }

                // both workers swallow a poison pill and get stuck inside the handler
                var poison = CreateSubmit(1, "poison");
                var poisonOrder = Order.CreateOrder(poison);
                pool.Dispatch(poison, poisonOrder);
                var otherPoison = CreateSubmit(2, "poison");
                pool.Dispatch(otherPoison, Order.CreateOrder(otherPoison));
                Assert.IsTrue(SpinWait.SpinUntil(() => processed.Count >= 2, 10000), "the workers never got stuck");

                // stranded until the drainer takes over: two queued submits and an update parked behind a poison
                var queued1 = CreateSubmit(3, "");
                pool.Dispatch(queued1, Order.CreateOrder(queued1));
                var queued2 = CreateSubmit(4, "");
                pool.Dispatch(queued2, Order.CreateOrder(queued2));
                var update = new UpdateOrderRequest(reference, poisonOrder.Id, new UpdateOrderFields());
                pool.Dispatch(update, poisonOrder);

                var stopwatch = Stopwatch.StartNew();
                pool.Dispose();
                stopwatch.Stop();

                Assert.AreEqual(2, interruptedWorkers, "the stuck workers were not interrupted");
                foreach (var request in new OrderRequest[] { queued1, queued2, update })
                {
                    var passes = processed.Where(pair => pair.Request == request).ToList();
                    Assert.AreEqual(1, passes.Count, $"the stranded request was processed {passes.Count} times");
                    Assert.AreEqual("Transaction Thread Drainer", passes[0].Thread, "the backlog was not drained on a new thread");
                }
                Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(10), "Dispose did not return within the shutdown budget");
                Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
            }
            finally
            {
                gate.Set();
                pool.DisposeSafely();
            }
        }

        [Test]
        public void DisposeOfAnIdlePoolIsFastAndQuiet()
        {
            var processedCount = 0;
            Exception processingError = null;
            var pool = new OrderRequestProcessingPool(concurrencyEnabled: true, minimumThreads: 2, maximumThreads: 10,
                _ => Interlocked.Increment(ref processedCount),
                exception => processingError = exception);

            // keeps the production shutdown timeout: idle workers must exit once adding completes, not wait it out
            var stopwatch = Stopwatch.StartNew();
            pool.Dispose();
            stopwatch.Stop();

            Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(10));
            Assert.AreEqual(0, processedCount);
            Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
            Assert.IsFalse(pool.IsActive);
        }

        [Test]
        public void SynchronousPoolDisposeProcessesPendingRequests()
        {
            var processed = new ConcurrentQueue<OrderRequest>();
            Exception processingError = null;
            var pool = OrderRequestProcessingPool.Synchronous(
                processed.Enqueue,
                exception => processingError = exception);

            var symbol = Symbols.SPY;
            var reference = new DateTime(2025, 07, 03, 10, 0, 0);
            var submit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
            submit.SetOrderId(1);
            var order = Order.CreateOrder(submit);
            pool.Dispatch(submit, order);
            pool.ProcessPending();
            CollectionAssert.AreEqual(new OrderRequest[] { submit }, processed);

            // queued after the last drain: Dispose pumps it through the processing loop on the caller thread
            var update = new UpdateOrderRequest(reference, order.Id, new UpdateOrderFields());
            pool.Dispatch(update, order);
            pool.Dispose();

            CollectionAssert.AreEqual(new OrderRequest[] { submit, update }, processed);
            Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
            Assert.IsFalse(pool.IsActive);
        }
    }
}
