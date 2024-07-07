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
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Queues;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class FakeDataQueueTests
    {
        [Test]
        public void GeneratesDataCorrectly()
        {
            using var aggregator = new AggregationManager();
            var algorithm = new AlgorithmStub();
            var dataQueue = new FakeDataQueue(aggregator, dataPointsPerSecondPerSymbol: 10);
            algorithm.AddEquity("SPY", Resolution.Second);
            List<IEnumerator<BaseData>> enumerators = new();
            foreach (var config in algorithm.SubscriptionManager.Subscriptions)
            {
                using var newDataEvent = new ManualResetEvent(false);
                enumerators.Add(
                    dataQueue.Subscribe(
                        config,
                        (_, _) =>
                        {
                            try
                            {
                                newDataEvent.Set();
                            }
                            catch (ObjectDisposedException) { }
                        }
                    )
                );

                Assert.IsTrue(newDataEvent.WaitOne(15000));

                // let's just generate a single point
                dataQueue.Unsubscribe(config);
            }
            dataQueue.Dispose();

            foreach (var enumerator in enumerators)
            {
                // assert each data type generate data correctly
                Assert.IsTrue(enumerator.MoveNext());
                Assert.IsNotNull(enumerator.Current);

                Log.Debug($"FakeDataQueueTests.GeneratesDataCorrectly(): {enumerator.Current}");
            }
        }
    }
}
