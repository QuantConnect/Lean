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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BrokerageConcurrentMessageHandlerTests
    {
        [Test]
        public void MessagesHandledCorrectly()
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

            for (var i = 0; i < expectedCount; )
            {
                handler.WithLockedStream(() =>
                {
                    // place order
                    i++;
                });

                if (i % 50 == 0)
                {
                    Thread.Sleep(1);
                }
            }

            if (!task.Wait(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail(
                    "BrokerageConcurrentMessageHandlerTests.MessagesHandledCorrectly(): timeout waiting for task to finish"
                );
            }

            // all processed
            Assert.AreEqual(expectedCount, numbers.Count);

            for (var i = 0; i < numbers.Count; i++)
            {
                // all in order
                Assert.AreEqual($"{i + 1}", numbers[i]);
            }
        }
    }
}
