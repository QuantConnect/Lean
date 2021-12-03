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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Packets;
using QuantConnect.Queues;
using System;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class CompositeDataQueueHandlerTests
    {
        [TestCase("ZerodhaBrokerage")]
        [TestCase("TradierBrokerage")]
        [TestCase("QuantConnect.Brokerages.InteractiveBrokers.InteractiveBrokersBrokerage")]
        [TestCase("OandaBrokerage")]
        [TestCase("GDAXDataQueueHandler")]
        [TestCase("BitfinexBrokerage")] 
        [TestCase("BinanceBrokerage")]
        public void GetFactoryFromDataQueueHandler(string dataQueueHandler)
        {
            var factory = JobQueue.GetFactoryFromDataQueueHandler(dataQueueHandler);
            Assert.NotNull(factory);
        }

        [Test]
        public void SetJob()
        {
            //Array IDQH
            var dataHandlers = Newtonsoft.Json.JsonConvert.SerializeObject(new[] { "FakeDataQueue" });
            var jobWithArrayIDQH = new LiveNodePacket
            {
                Brokerage = "ZerodhaBrokerage",
                DataQueueHandler = dataHandlers
            };
            var compositeDataQueueHandler = new CompositeDataQueueHandler();
            compositeDataQueueHandler.SetJob(jobWithArrayIDQH);
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void SubscribeReturnsNull()
        {
            var dataConfig = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var compositeDataQueueHandler = new CompositeDataQueueHandler();
            var enumerator = compositeDataQueueHandler.Subscribe(dataConfig, (_, _) => {});
            Assert.Null(enumerator);
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void SubscribeReturnsNotNull()
        {
            var dataHandlers = Newtonsoft.Json.JsonConvert.SerializeObject(new[] { "FakeDataQueue" });
            var job = new LiveNodePacket
            {
                Brokerage = "ZerodhaBrokerage",
                DataQueueHandler = dataHandlers
            };
            var compositeDataQueueHandler = new CompositeDataQueueHandler();
            compositeDataQueueHandler.SetJob(job);
            var dataConfig = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var enumerator = compositeDataQueueHandler.Subscribe(dataConfig, (_, _) => {});
            Assert.NotNull(enumerator);
            compositeDataQueueHandler.Dispose();
            enumerator.Dispose();
        }

        [Test]
        public void Unsubscribe()
        {
            var dataConfig = new SubscriptionDataConfig(typeof(TradeBar), Symbols.AAPL, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var compositeDataQueueHandler = new CompositeDataQueueHandler();
            compositeDataQueueHandler.Unsubscribe(dataConfig);
            compositeDataQueueHandler.Dispose();
        }
    }
}
