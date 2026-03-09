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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class DataQueueHandlerManagerTests
    {
        [TestCase("BacktestingBrokerage")]
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
                DataQueueHandler = dataHandlers
            };
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(jobWithArrayIDQH);
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void SubscribeReturnsNull()
        {
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            var enumerator = compositeDataQueueHandler.Subscribe(GetConfig(), (_, _) => {});
            Assert.Null(enumerator);
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void SubscribeReturnsNotNull()
        {
            var dataHandlers = Newtonsoft.Json.JsonConvert.SerializeObject(new[] { "FakeDataQueue" });
            var job = new LiveNodePacket
            {
                DataQueueHandler = dataHandlers
            };
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(job);
            var enumerator = compositeDataQueueHandler.Subscribe(GetConfig(), (_, _) => {});
            Assert.NotNull(enumerator);
            compositeDataQueueHandler.Dispose();
            enumerator.Dispose();
        }

        [Test]
        public void Unsubscribe()
        {
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.Unsubscribe(GetConfig());
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void IsNotUniverseProvider()
        {
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            Assert.IsFalse(compositeDataQueueHandler.HasUniverseProvider);
            Assert.Throws<NotSupportedException>(() => compositeDataQueueHandler.LookupSymbols(Symbols.ES_Future_Chain, false));
            Assert.Throws<NotSupportedException>(() => compositeDataQueueHandler.CanPerformSelection());
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void DoubleSubscribe()
        {
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(new LiveNodePacket { DataQueueHandler = "[ \"TestDataHandler\" ]" });

            var dataConfig = GetConfig();
            var enumerator = compositeDataQueueHandler.Subscribe(dataConfig, (_, _) => {});

            Assert.DoesNotThrow(() => compositeDataQueueHandler.Subscribe(dataConfig, (_, _) => { }));
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void SingleSubscribe()
        {
            TestDataHandler.UnsubscribeCounter = 0;
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(new LiveNodePacket { DataQueueHandler = "[ \"TestDataHandler\" ]" });

            var dataConfig = GetConfig();
            var enumerator = compositeDataQueueHandler.Subscribe(dataConfig, (_, _) => {});

            compositeDataQueueHandler.Unsubscribe(dataConfig);
            compositeDataQueueHandler.Unsubscribe(dataConfig);
            compositeDataQueueHandler.Unsubscribe(dataConfig);

            Assert.AreEqual(1, TestDataHandler.UnsubscribeCounter);

            compositeDataQueueHandler.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MappedConfig(bool canonicalUnsubscribeFirst)
        {
            TestDataHandler.UnsubscribeCounter = 0;
            TestDataHandler.SubscribeCounter = 0;
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(new LiveNodePacket { DataQueueHandler = "[ \"TestDataHandler\" ]" });

            var canonicalSymbol = Symbols.ES_Future_Chain.UpdateMappedSymbol(Symbols.Future_ESZ18_Dec2018.ID.ToString());
            var canonicalConfig = GetConfig(canonicalSymbol);
            var contractConfig = GetConfig(Symbols.Future_ESZ18_Dec2018);

            var enumerator = new LiveSubscriptionEnumerator(canonicalConfig, compositeDataQueueHandler, (_, _) => {}, (_) => false);
            var enumerator2 = new LiveSubscriptionEnumerator(contractConfig, compositeDataQueueHandler, (_, _) => {}, (_) => false);

            var firstUnsubscribe = canonicalUnsubscribeFirst ? canonicalConfig : contractConfig;
            var secondUnsubscribe = canonicalUnsubscribeFirst ? contractConfig : canonicalConfig;

            Assert.AreEqual(2, TestDataHandler.SubscribeCounter);

            compositeDataQueueHandler.UnsubscribeWithMapping(firstUnsubscribe);
            Assert.AreEqual(1, TestDataHandler.UnsubscribeCounter);

            compositeDataQueueHandler.UnsubscribeWithMapping(secondUnsubscribe);
            Assert.AreEqual(2, TestDataHandler.UnsubscribeCounter);

            enumerator.Dispose();
            enumerator2.Dispose();
            compositeDataQueueHandler.Dispose();
        }

        [Test]
        public void HandleExplodingDataQueueHandler()
        {
            using var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            // first exploding
            compositeDataQueueHandler.SetJob(new LiveNodePacket { DataQueueHandler = "[ \"ExplodingDataHandler\", \"TestDataHandler\" ]" });
            IEnumerator<BaseData> enumerator = null;
            Assert.DoesNotThrow(() =>
            {
                enumerator = compositeDataQueueHandler.Subscribe(GetConfig(), (_, _) => { });
            });
            Assert.IsNotNull(enumerator);
            enumerator.Dispose();
        }

        [Test]
        public void ExplodingDataQueueHandler()
        {
            using var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(new LiveNodePacket { DataQueueHandler = "[ \"ExplodingDataHandler\" ]" });
            Assert.Throws<Exception>(() =>
            {
                using var enumerator = compositeDataQueueHandler.Subscribe(GetConfig(), (_, _) => { });
            });
        }

        [Test]
        public void HandlesCustomData()
        {
            var customSymbol = Symbol.CreateBase(typeof(AlgorithmSettings), Symbols.SPY);
            var config = new SubscriptionDataConfig(typeof(TradeBar), customSymbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork,
                false, false, false, false, TickType.Trade, false);
            var dataHandlers = Newtonsoft.Json.JsonConvert.SerializeObject(new[] { "FakeDataQueue" });
            var job = new LiveNodePacket
            {
                DataQueueHandler = dataHandlers
            };
            var compositeDataQueueHandler = new DataQueueHandlerManager(new AlgorithmSettings());
            compositeDataQueueHandler.SetJob(job);
            var enumerator = compositeDataQueueHandler.Subscribe(config, (_, _) => { });
            Assert.NotNull(enumerator);
            compositeDataQueueHandler.Dispose();
            enumerator.Dispose();
        }

        private static SubscriptionDataConfig GetConfig(Symbol symbol = null)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol ?? Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork,
                false, false, false, false, TickType.Trade, false);
        }


        private class TestDataHandler : IDataQueueHandler
        {
            public static int SubscribeCounter { get; set; }

            public static int UnsubscribeCounter { get; set; }
            public void Dispose()
            {
            }

            public virtual IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
            {
                SubscribeCounter++;
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            public void Unsubscribe(SubscriptionDataConfig dataConfig)
            {
                UnsubscribeCounter++;
            }

            public void SetJob(LiveNodePacket job)
            {
            }

            public bool IsConnected { get; }
        }

        private class ExplodingDataHandler : TestDataHandler
        {
            public override IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
            {
                throw new Exception("ExplodingDataHandler exception!");
            }
        }
    }
}
