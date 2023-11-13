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
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class DataMonitorTests
    {
        [Test]
        public void StoresTradedSecuritiesSubscriptionDataConfigs()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var dataMonitor = new TestableDataMonitor();
            dataMonitor.SetSubscriptionManager(algorithm.SubscriptionManager);

            // Add a few securities
            var spy = algorithm.AddEquity("SPY", Resolution.Daily).Symbol;
            algorithm.AddEquity("SPY", Resolution.Hour);
            algorithm.AddEquity("SPY", Resolution.Minute);

            var es = algorithm.AddFuture("ES", Resolution.Minute).Symbol;
            algorithm.AddFuture("ES", Resolution.Second);
            algorithm.AddFuture("ES", Resolution.Tick);

            // This will not be traded
            algorithm.AddIndex("SPX");

            var orderEvents = new List<OrderEvent>()
            {
                new OrderEvent(1, spy, new DateTime(2023, 11, 3, 11, 0, 0), OrderStatus.Filled, OrderDirection.Buy, 300, 10, OrderFee.Zero),
                new OrderEvent(2, spy, new DateTime(2023, 11, 3, 12, 0, 0), OrderStatus.Canceled, OrderDirection.Sell, 310, 5, OrderFee.Zero),
                new OrderEvent(3, es, new DateTime(2023, 11, 3, 13, 0, 0), OrderStatus.Submitted, OrderDirection.Buy, 4300, 10, OrderFee.Zero),
                new OrderEvent(3, es, new DateTime(2023, 11, 3, 13, 0, 1), OrderStatus.Filled, OrderDirection.Buy, 4300, 10, OrderFee.Zero),
            };
            foreach (var orderEvent in orderEvents)
            {
                dataMonitor.OnOrderEvent(null, orderEvent);
            }

            // Make sure it stores the data even if the subscription has been removed
            algorithm.RemoveSecurity(spy);

            dataMonitor.StoreTradedSubscriptions();

            var filePath = dataMonitor.GetFilePath($"traded-securities-subscriptions.json");
            Assert.IsTrue(File.Exists(filePath));

            var resultJson = JArray.Parse(File.ReadAllText(filePath));

            Assert.AreEqual(2, resultJson.Count);

            var spyJsonSubscription = resultJson[0] as JObject;
            Assert.AreEqual(spy, spyJsonSubscription["symbol"].ToObject<Symbol>());
            CollectionAssert.AreEqual(
                new List<TickType>() { TickType.Trade, TickType.Quote },
                spyJsonSubscription["tick-types"].ToObject<List<TickType>>());

            var esJsonSubscription = resultJson[1] as JObject;
            Assert.AreEqual(es, esJsonSubscription["symbol"].ToObject<Symbol>());
            CollectionAssert.AreEqual(
                new List<TickType>() { TickType.Quote },
                esJsonSubscription["tick-types"].ToObject<List<TickType>>());
        }

        public class TestableDataMonitor : DataMonitor
        {
            public TestableDataMonitor()
            {
                TimeProvider = new ManualTimeProvider(new DateTime(2023, 11, 3, 10, 0, 0));
            }

            public new void StoreTradedSubscriptions()
            {
                base.StoreTradedSubscriptions();
            }

            public new string GetFilePath(string fileName)
            {
                return base.GetFilePath(fileName);
            }
        }
    }
}
