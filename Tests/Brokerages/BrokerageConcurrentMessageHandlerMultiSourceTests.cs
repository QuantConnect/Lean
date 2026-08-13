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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Tests for the non-generic, multi-source <see cref="BrokerageConcurrentMessageHandler"/>.
    /// </summary>
    [TestFixture]
    public class BrokerageConcurrentMessageHandlerMultiSourceTests
    {
        [Test]
        public void TwoRegisteredMessageTypesShareOneLock()
        {
            var processed = new List<string>();
            using var handler = new BrokerageConcurrentMessageHandler(concurrencyEnabled: true);
            handler.Register<StreamMessage>(stream => processed.Add($"stream:{stream.Value}"));
            handler.Register<PolledMessage>(polled => processed.Add($"poll:{polled.Value}"));

            using var lockedEvent = new ManualResetEventSlim(false);
            using var releaseEvent = new ManualResetEventSlim(false);

            // Simulates an order request holding WithLockedStream while a place/update/cancel call is in flight.
            var writer = Task.Run(() =>
            {
                handler.WithLockedStream(() =>
                {
                    lockedEvent.Set();
                    releaseEvent.Wait();
                });
            });

            lockedEvent.Wait();

            // A "stream" message and a "poll" message both queue behind the write lock instead of running concurrently with it.
            handler.HandleNewMessage(new StreamMessage("s1"));
            handler.HandleNewMessage(new PolledMessage("p1"));

            Assert.AreEqual(0, processed.Count);

            releaseEvent.Set();
            writer.Wait();

            CollectionAssert.AreEqual(new[] { "stream:s1", "poll:p1" }, processed);
        }

        [Test]
        public void MessageWithNoRegisteredListenerIsSkippedWithoutStoppingLaterMessages()
        {
            var processed = new List<string>();
            using var handler = new BrokerageConcurrentMessageHandler();
            handler.Register<StreamMessage>(stream => processed.Add(stream.Value));

            // Nobody listens for PolledMessage: must not throw, and must not block s1 behind it.
            Assert.DoesNotThrow(() => handler.HandleNewMessage(new PolledMessage("ignored")));
            handler.HandleNewMessage(new StreamMessage("s1"));

            CollectionAssert.AreEqual(new[] { "s1" }, processed);
        }

        [Test]
        public void ListenerRegisteredWhileMessagesFlowReceivesLaterMessages()
        {
            var processed = new List<string>();
            using var handler = new BrokerageConcurrentMessageHandler();
            handler.Register<StreamMessage>(stream => processed.Add($"stream:{stream.Value}"));

            handler.HandleNewMessage(new StreamMessage("s1"));

            // The order polling service registers its own type mid-run, when a stream dies.
            handler.Register<PolledMessage>(polled => processed.Add($"poll:{polled.Value}"));
            handler.HandleNewMessage(new PolledMessage("p1"));

            CollectionAssert.AreEqual(new[] { "stream:s1", "poll:p1" }, processed);
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
