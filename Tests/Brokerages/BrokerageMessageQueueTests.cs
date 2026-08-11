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

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class BrokerageMessageQueueTests
    {
        [Test]
        public void TwoSubscribedMessageTypesShareOneLock()
        {
            var processed = new List<string>();
            var queue = new BrokerageMessageQueue(concurrencyEnabled: true);
            queue.MessageReceived += message =>
            {
                switch (message)
                {
                    case StreamMessage stream:
                        processed.Add($"stream:{stream.Value}");
                        break;
                    case PolledMessage polled:
                        processed.Add($"poll:{polled.Value}");
                        break;
                }
            };

            using var lockedEvent = new ManualResetEventSlim(false);
            using var releaseEvent = new ManualResetEventSlim(false);

            // Simulates an order request holding WithLockedStream while a place/update/cancel call is in flight.
            var writer = Task.Run(() =>
            {
                queue.WithLockedStream(() =>
                {
                    lockedEvent.Set();
                    releaseEvent.Wait();
                });
            });

            lockedEvent.Wait();

            // A "stream" message and a "poll" message both queue behind the write lock instead of running concurrently with it.
            queue.Enqueue(new StreamMessage("s1"));
            queue.Enqueue(new PolledMessage("p1"));

            Assert.AreEqual(0, processed.Count);

            releaseEvent.Set();
            writer.Wait();

            CollectionAssert.AreEqual(new[] { "stream:s1", "poll:p1" }, processed);
        }

        [Test]
        public void MessageWithNoInterestedSubscriberIsSkippedWithoutStoppingLaterMessages()
        {
            var processed = new List<string>();
            var queue = new BrokerageMessageQueue();
            queue.MessageReceived += message =>
            {
                if (message is StreamMessage stream)
                {
                    processed.Add(stream.Value);
                }
            };

            // Nobody filters for PolledMessage: must not throw, and must not block s1 behind it.
            Assert.DoesNotThrow(() => queue.Enqueue(new PolledMessage("ignored")));
            queue.Enqueue(new StreamMessage("s1"));

            CollectionAssert.AreEqual(new[] { "s1" }, processed);
        }

        [Test]
        public void ConcurrentMessageHandlerHandlesARegisteredSecondMessageType()
        {
            var processed = new List<string>();
            var handler = new BrokerageConcurrentMessageHandler<string>(m => processed.Add($"stream:{m}"));

            // A second, unrelated message type registered on the same handler - standing in for the order
            // polling service registering BrokerOrderState alongside the brokerage's own stream type.
            handler.RegisterMessageType<PolledMessage>(m => processed.Add($"poll:{m.Value}"));

            // Same method name, either type - the generic argument is inferred from what is passed in.
            handler.HandleNewMessage("s1");
            handler.HandleNewMessage(new PolledMessage("p1"));

            CollectionAssert.AreEqual(new[] { "stream:s1", "poll:p1" }, processed);
        }

        [Test]
        public void ConcurrentMessageHandlerStillBlocksOnWithLockedStream()
        {
            var processed = new List<string>();
            var handler = new BrokerageConcurrentMessageHandler<string>(processed.Add, concurrencyEnabled: true);

            using var lockedEvent = new ManualResetEventSlim(false);
            using var releaseEvent = new ManualResetEventSlim(false);

            var writer = Task.Run(() =>
            {
                handler.WithLockedStream(() =>
                {
                    lockedEvent.Set();
                    releaseEvent.Wait();
                });
            });

            lockedEvent.Wait();

            handler.HandleNewMessage("s1");
            Assert.AreEqual(0, processed.Count);

            releaseEvent.Set();
            writer.Wait();

            CollectionAssert.AreEqual(new[] { "s1" }, processed);
        }

        private sealed class StreamMessage
        {
            public string Value { get; }
            public StreamMessage(string value) => Value = value;
        }

        private sealed class PolledMessage
        {
            public string Value { get; }
            public PolledMessage(string value) => Value = value;
        }
    }
}
