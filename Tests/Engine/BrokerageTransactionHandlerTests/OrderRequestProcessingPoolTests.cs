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
        public void DisposeDropsQueuedAndParkedRequestsBeforeStopping()
        {
            using var gate = new ManualResetEventSlim(false);
            var processed = new ConcurrentQueue<OrderRequest>();
            var dropped = new ConcurrentQueue<OrderRequest>();
            Exception processingError = null;
            var pool = new OrderRequestProcessingPool(concurrencyEnabled: true, minimumThreads: 1, maximumThreads: 2,
                request =>
                {
                    processed.Enqueue(request);
                    gate.Wait();
                },
                exception => processingError = exception,
                dropped.Enqueue);
            // shrink the shutdown budget so the test doesn't wait out the production timeout on the blocked workers
            pool.ShutdownTimeout = TimeSpan.FromMilliseconds(500);

            try
            {
                var symbol = Symbols.SPY;
                var reference = new DateTime(2025, 07, 03, 10, 0, 0);

                // the order we track, its submit claims a worker and blocks on the gate
                var submit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                submit.SetOrderId(1);
                var order = Order.CreateOrder(submit);
                pool.Dispatch(submit, order);

                // keep feeding unrelated orders until both workers are blocked inside the handler
                var fillerId = 1000;
                var saturated = SpinWait.SpinUntil(() =>
                {
                    var filler = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                    filler.SetOrderId(++fillerId);
                    pool.Dispatch(filler, Order.CreateOrder(filler));
                    return processed.Count >= 2;
                }, 10000);
                Assert.IsTrue(saturated, "the workers never got busy");

                // these can never be processed: the submit waits in the ready queue, the update and cancel wait parked
                var queuedSubmit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                queuedSubmit.SetOrderId(2);
                pool.Dispatch(queuedSubmit, Order.CreateOrder(queuedSubmit));
                var update = new UpdateOrderRequest(reference, order.Id, new UpdateOrderFields());
                var cancel = new CancelOrderRequest(reference, order.Id, "");
                pool.Dispatch(update, order);
                pool.Dispatch(cancel, order);

                pool.Dispose();

                var droppedRequests = dropped.ToList();
                Assert.Contains(queuedSubmit, droppedRequests);
                Assert.Contains(update, droppedRequests);
                Assert.Contains(cancel, droppedRequests);
                // whoever takes a request owns it: nothing is both processed and dropped
                CollectionAssert.IsEmpty(droppedRequests.Intersect(processed));
                Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
            }
            finally
            {
                gate.Set();
                pool.DisposeSafely();
            }
        }

        [Test]
        public void DisposeInterruptsWorkersStuckInTheRequestHandler()
        {
            using var gate = new ManualResetEventSlim(false);
            using var workerBlocked = new ManualResetEventSlim(false);
            using var workerInterrupted = new ManualResetEventSlim(false);
            var dropped = new ConcurrentQueue<OrderRequest>();
            Exception processingError = null;
            var pool = new OrderRequestProcessingPool(concurrencyEnabled: true, minimumThreads: 1, maximumThreads: 2,
                request =>
                {
                    workerBlocked.Set();
                    try
                    {
                        // simulates a brokerage call pinned in a wait that only a thread interrupt can free
                        gate.Wait();
                    }
                    catch (ThreadInterruptedException)
                    {
                        workerInterrupted.Set();
                        throw;
                    }
                },
                exception => processingError = exception,
                dropped.Enqueue);
            pool.ShutdownTimeout = TimeSpan.FromMilliseconds(500);

            try
            {
                var symbol = Symbols.SPY;
                var reference = new DateTime(2025, 07, 03, 10, 0, 0);
                var submit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
                submit.SetOrderId(1);
                pool.Dispatch(submit, Order.CreateOrder(submit));
                Assert.IsTrue(workerBlocked.Wait(10000), "the worker never picked up the request");

                var stopwatch = Stopwatch.StartNew();
                pool.Dispose();
                stopwatch.Stop();

                Assert.IsTrue(workerInterrupted.IsSet, "the stuck worker was not interrupted");
                Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(10), "Dispose did not return within the shutdown budget");
                CollectionAssert.IsEmpty(dropped);
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
            var dropped = new ConcurrentQueue<OrderRequest>();
            Exception processingError = null;
            var pool = new OrderRequestProcessingPool(concurrencyEnabled: true, minimumThreads: 2, maximumThreads: 10,
                _ => { },
                exception => processingError = exception,
                dropped.Enqueue);

            // keeps the production shutdown timeout: idle workers must exit on cancellation, not wait it out
            var stopwatch = Stopwatch.StartNew();
            pool.Dispose();
            stopwatch.Stop();

            Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(10));
            CollectionAssert.IsEmpty(dropped);
            Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
            Assert.IsFalse(pool.IsActive);
        }

        [Test]
        public void SynchronousPoolDisposeDropsPendingRequests()
        {
            var dropped = new ConcurrentQueue<OrderRequest>();
            var processedCount = 0;
            Exception processingError = null;
            var pool = OrderRequestProcessingPool.Synchronous(
                _ => processedCount++,
                exception => processingError = exception,
                dropped.Enqueue);

            var symbol = Symbols.SPY;
            var reference = new DateTime(2025, 07, 03, 10, 0, 0);
            var submit = new SubmitOrderRequest(OrderType.Market, symbol.SecurityType, symbol, 1, 0, 0, reference, "");
            submit.SetOrderId(1);
            var order = Order.CreateOrder(submit);
            pool.Dispatch(submit, order);
            pool.ProcessPending();
            Assert.AreEqual(1, processedCount);

            // queued after the last drain, so it is never processed and must be dropped at dispose
            var update = new UpdateOrderRequest(reference, order.Id, new UpdateOrderFields());
            pool.Dispatch(update, order);
            pool.Dispose();

            Assert.AreEqual(1, processedCount);
            CollectionAssert.AreEquivalent(new OrderRequest[] { update }, dropped);
            Assert.IsNull(processingError, $"the pool reported an error: {processingError}");
        }
    }
}
