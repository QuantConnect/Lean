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
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class BrokerageConcurrentMessageHandlerTests
    {
        [Test]
        public void MessagesHandledCorrectly([Values] bool parallel)
        {
            const int expectedCount = 10000;
            var numbers = new List<string>();
            void Action(string number) => numbers.Add(number);
            var handler = new BrokerageConcurrentMessageHandler<string>(Action);

            var task = Task.Factory.StartNew(() =>
            {
                var counter = 0;
                for (var i = 0; i < expectedCount; i++)
                {
                    handler.HandleNewMessage($"{Interlocked.Increment(ref counter)}");

                    if (i % 50 == 0)
                    {
                        Thread.Sleep(1);
                    }
                }
            });

            Action<int> placeOrder = i =>
            {
                handler.WithLockedStream(() =>
                {
                    // place order
                });

                if (i % 50 == 0)
                {
                    Thread.Sleep(1);
                }
            };

            if (parallel)
            {
                Parallel.ForEach(Enumerable.Range(0, expectedCount), placeOrder);
            }
            else
            {
                for (var i = 0; i < expectedCount; i++)
                {
                    placeOrder(i);
                }
            }

            if(!task.Wait(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("BrokerageConcurrentMessageHandlerTests.MessagesHandledCorrectly(): timeout waiting for task to finish");
            }

            // all processed
            Assert.AreEqual(expectedCount, numbers.Count);

            for (var i = 0; i < numbers.Count; i++)
            {
                // all in order
                Assert.AreEqual($"{i + 1}", numbers[i]);
            }
        }

        [Test]
        public void ProducersWaitUntilBufferIsNotFull()
        {
            const int bufferSize = 10;
            Config.Set("brokerage-concurrent-message-handler-buffer-size", bufferSize);

            var numbers = new List<string>();
            void Action(string number) => numbers.Add(number);
            var handler = new BrokerageConcurrentMessageHandler<string>(Action, concurrencyEnabled: true);

            using var startEvent = new ManualResetEventSlim(false);
            using var waitingEvent = new ManualResetEventSlim(false);

            // Lock the buffer
            Task.Run(() =>
            {
                handler.WithLockedStream(() =>
                {
                    waitingEvent.Set();
                    startEvent.Wait();
                });
            });

            // Wait until the buffer is locked
            waitingEvent.Wait();

            // Enqueue messages
            for (var i = 0; i < bufferSize; i++)
            {
                handler.HandleNewMessage($"{i + 1}");
            }

            // No messages should have been processed yet
            Assert.AreEqual(0, numbers.Count);

            // Start producers, they should wait until the buffer is not full
            var producers = Task.Run(() =>
            {
                Parallel.ForEach(Enumerable.Range(0, bufferSize), (int _) =>
                {
                    handler.WithLockedStream(() => { });
                });
            });

            Assert.IsFalse(producers.Wait(1000));

            startEvent.Set();

            Assert.IsTrue(producers.Wait(1000));

            // All messages should have been processed
            Assert.AreEqual(bufferSize, numbers.Count);
            for (var i = 0; i < numbers.Count; i++)
            {
                // all in order
                Assert.AreEqual($"{i + 1}", numbers[i]);
            }
        }

        [Test]
        public void MessagesAreProcessedOnlyByLastConcurrentThread()
        {
            const int bufferSize = 20;
            Config.Set("brokerage-concurrent-message-handler-buffer-size", bufferSize);

            const int expectedCount = bufferSize / 2;
            var numbers = new List<string>();

            var processingThreadIds = new HashSet<int>();

            void Action(string number)
            {
                // Store the thread ID that processed the message
                processingThreadIds.Add(Environment.CurrentManagedThreadId);
                numbers.Add(number);
            }

            var handler = new BrokerageConcurrentMessageHandler<string>(Action, concurrencyEnabled: true);

            using var firstProducerUnlockEvent = new ManualResetEventSlim(false);
            using var waitingEvent = new ManualResetEventSlim(false);
            var firstProducerThreadId = -1;

            // Lock the buffer
            var firstProducer = Task.Run(() =>
            {
                handler.WithLockedStream(() =>
                {
                    // Store the thread ID of the first producer
                    firstProducerThreadId = Environment.CurrentManagedThreadId;
                    waitingEvent.Set();
                    firstProducerUnlockEvent.Wait();
                });
            });

            // Wait until the buffer is locked
            waitingEvent.Wait();

            // Enqueue messages
            for (var i = 0; i < expectedCount; i++)
            {
                handler.HandleNewMessage($"{i + 1}");
            }

            // No messages should have been processed yet
            Assert.AreEqual(0, numbers.Count);

            // None of this
            Parallel.ForEach(Enumerable.Range(0, 100), (int _) =>
            {
                handler.WithLockedStream(() => { });
            });

            // No messages should have been processed yet
            Assert.AreEqual(0, numbers.Count);

            // Unlock the first producer
            firstProducerUnlockEvent.Set();
            firstProducer.Wait();

            Assert.AreEqual(expectedCount, numbers.Count);
            Assert.AreEqual(1, processingThreadIds.Count, "All messages should be processed by the same thread");
            Assert.IsTrue(processingThreadIds.Contains(firstProducerThreadId), "Messages should be processed by the first producer thread");
        }

        [Test]
        public void StateIsMaintainedAfterExceptions([Values] bool exceptionInConsumer, [Values] bool exceptionInProducer)
        {
            var expectedCount = 1000;
            var numbers = new List<string>();
            void Action(string number)
            {
                numbers.Add(number);
                if (exceptionInConsumer && number.ToInt32() % 2 == 0)
                {
                    throw new Exception("Test exception in consumer");
                }
            }
            var handler = new BrokerageConcurrentMessageHandler<string>(Action, concurrencyEnabled: true);

            var producersCallCount = 0;

            var task = Task.Run(() =>
            {
                for (var i = 0; i < expectedCount; i++)
                {
                    handler.HandleNewMessage($"{i + 1}");
                }
            });

            // Start producers
            var producersCount = 100;
            Parallel.ForEach(Enumerable.Range(0, producersCount), (int i) =>
            {
                try
                {
                    handler.WithLockedStream(() =>
                    {
                        Interlocked.Increment(ref producersCallCount);
                        if (exceptionInProducer && i % 2 == 0)
                        {
                            throw new Exception("Exception in producer");
                        }
                    });
                }
                catch
                {
                    // nop
                }
            });

            Assert.AreEqual(producersCount, producersCallCount);

            task.Wait();

            for (var i = numbers.Count; i < expectedCount; i++)
            {
                handler.WithLockedStream(() => { });
            }

            // All processed
            Assert.AreEqual(expectedCount, numbers.Count);
            for (var i = 0; i < numbers.Count; i++)
            {
                // all in order
                Assert.AreEqual($"{i + 1}", numbers[i]);
            }
        }
    }
}
