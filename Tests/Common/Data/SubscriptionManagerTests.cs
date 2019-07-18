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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SubscriptionManagerTests
    {
        [Test]
        [TestCase(SecurityType.Base, Resolution.Minute, typeof(TradeBar), TickType.Trade)]
        [TestCase(SecurityType.Base, Resolution.Tick, typeof(Tick), TickType.Trade)]
        [TestCase(SecurityType.Equity, Resolution.Minute, typeof(TradeBar), TickType.Trade)]
        [TestCase(SecurityType.Equity, Resolution.Tick, typeof(Tick), TickType.Trade)]
        [TestCase(SecurityType.Forex, Resolution.Minute, typeof(QuoteBar), TickType.Quote)]
        [TestCase(SecurityType.Forex, Resolution.Tick, typeof(Tick), TickType.Quote)]
        [TestCase(SecurityType.Cfd, Resolution.Minute, typeof(QuoteBar), TickType.Quote)]
        [TestCase(SecurityType.Cfd, Resolution.Tick, typeof(Tick), TickType.Quote)]
        public void GetsSubscriptionDataTypesSingle(SecurityType securityType, Resolution resolution, Type expectedDataType, TickType expectedTickType)
        {
            var types = GetSubscriptionDataTypes(securityType, resolution);

            Assert.AreEqual(1, types.Count);
            Assert.AreEqual(expectedDataType, types[0].Item1);
            Assert.AreEqual(expectedTickType, types[0].Item2);
        }

        [Test]
        [TestCase(SecurityType.Future, Resolution.Minute, typeof(ZipEntryName), TickType.Quote)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(ZipEntryName), TickType.Quote)]
        [TestCase(SecurityType.Option, Resolution.Minute, typeof(ZipEntryName), TickType.Quote)]
        [TestCase(SecurityType.Option, Resolution.Tick, typeof(ZipEntryName), TickType.Quote)]
        public void GetsSubscriptionDataTypesCanonical(SecurityType securityType, Resolution resolution, Type expectedDataType, TickType expectedTickType)
        {
            var types = GetSubscriptionDataTypes(securityType, resolution, true);

            Assert.AreEqual(1, types.Count);
            Assert.AreEqual(expectedDataType, types[0].Item1);
            Assert.AreEqual(expectedTickType, types[0].Item2);
        }

        [Test]
        [TestCase(SecurityType.Future, Resolution.Minute)]
        [TestCase(SecurityType.Option, Resolution.Minute)]
        public void GetsSubscriptionDataTypesFuturesOptionsMinute(SecurityType securityType, Resolution resolution)
        {
            var types = GetSubscriptionDataTypes(securityType, resolution);

            Assert.AreEqual(3, types.Count);
            Assert.AreEqual(typeof(QuoteBar), types[0].Item1);
            Assert.AreEqual(TickType.Quote, types[0].Item2);
            Assert.AreEqual(typeof(TradeBar), types[1].Item1);
            Assert.AreEqual(TickType.Trade, types[1].Item2);
            Assert.AreEqual(typeof(OpenInterest), types[2].Item1);
            Assert.AreEqual(TickType.OpenInterest, types[2].Item2);
        }

        [Test]
        [TestCase(SecurityType.Future, Resolution.Tick)]
        [TestCase(SecurityType.Option, Resolution.Tick)]
        public void GetsSubscriptionDataTypesFuturesOptionsTick(SecurityType securityType, Resolution resolution)
        {
            var types = GetSubscriptionDataTypes(securityType, resolution);

            Assert.AreEqual(3, types.Count);
            Assert.AreEqual(typeof(Tick), types[0].Item1);
            Assert.AreEqual(TickType.Quote, types[0].Item2);
            Assert.AreEqual(typeof(Tick), types[1].Item1);
            Assert.AreEqual(TickType.Trade, types[1].Item2);
            Assert.AreEqual(typeof(Tick), types[2].Item1);
            Assert.AreEqual(TickType.OpenInterest, types[2].Item2);
        }

        [Test]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Tick)]
        public void GetsSubscriptionDataTypesCrypto(Resolution resolution)
        {
            var types = GetSubscriptionDataTypes(SecurityType.Crypto, resolution);

            Assert.AreEqual(2, types.Count);

            if (resolution == Resolution.Tick)
            {
                Assert.AreEqual(typeof(Tick), types[0].Item1);
                Assert.AreEqual(typeof(Tick), types[1].Item1);
            }
            else
            {
                Assert.AreEqual(typeof(TradeBar), types[0].Item1);
                Assert.AreEqual(typeof(QuoteBar), types[1].Item1);
            }

            Assert.AreEqual(TickType.Trade, types[0].Item2);
            Assert.AreEqual(TickType.Quote, types[1].Item2);
        }

        [Test]
        public void SubscriptionsMemberIsThreadSafe()
        {
            var subscriptionManager = new SubscriptionManager();
            subscriptionManager.SetDataManager(new DataManagerStub());
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(5);
            var tickers = QuantConnect.Algorithm.CSharp.StressSymbols.StockSymbols.ToList();
            var symbols = tickers.Select(ticker => Symbol.Create(ticker, SecurityType.Equity, QuantConnect.Market.USA)).ToList();

            var readTask = new TaskFactory().StartNew(() =>
            {
                Console.WriteLine("Read task started");
                while (DateTime.UtcNow < end)
                {
                    subscriptionManager.Subscriptions.Select(x => x.Resolution).DefaultIfEmpty(Resolution.Minute).Min();
                    Thread.Sleep(1);
                }
                Console.WriteLine("Read task ended");
            });

            while (readTask.Status != TaskStatus.Running) Thread.Sleep(1);

            var addTask = new TaskFactory().StartNew(() =>
            {
                Console.WriteLine("Add task started");
                foreach (var symbol in symbols)
                {
                    subscriptionManager.Add(symbol, Resolution.Minute, DateTimeZone.Utc, DateTimeZone.Utc, true, false);
                }
                Console.WriteLine("Add task ended");
            });

            Task.WaitAll(addTask, readTask);
        }

        [Test]
        public void GetsCustomSubscriptionDataTypes()
        {
            var subscriptionManager = new SubscriptionManager();
            subscriptionManager.SetDataManager(new DataManagerStub());
            subscriptionManager.AvailableDataTypes[SecurityType.Commodity] = new List<TickType> { TickType.OpenInterest, TickType.Quote, TickType.Trade };
            var types = subscriptionManager.LookupSubscriptionConfigDataTypes(SecurityType.Commodity, Resolution.Daily, false);

            Assert.AreEqual(3, types.Count);

            Assert.AreEqual(typeof(OpenInterest), types[0].Item1);
            Assert.AreEqual(typeof(QuoteBar), types[1].Item1);
            Assert.AreEqual(typeof(TradeBar), types[2].Item1);

            Assert.AreEqual(TickType.OpenInterest, types[0].Item2);
            Assert.AreEqual(TickType.Quote, types[1].Item2);
            Assert.AreEqual(TickType.Trade, types[2].Item2);
        }

        [Test]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.Trade, typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.Trade, typeof(QuoteBar), false)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.Trade, typeof(OpenInterest), false)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.Quote, typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.Quote, typeof(QuoteBar), true)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.Quote, typeof(OpenInterest), false)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.OpenInterest, typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.OpenInterest, typeof(QuoteBar), false)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(Tick), TickType.OpenInterest, typeof(OpenInterest), true)]
        public void ValidatesSubscriptionTickTypesForConsolidators(
            SecurityType securityType,
            Resolution subscriptionResolution,
            Type subscriptionDataType,
            TickType? subscriptionTickType,
            Type consolidatorOutputType,
            bool expected)
        {
            var subscription = new SubscriptionDataConfig(
                subscriptionDataType,
                Symbol.Create("XYZ", securityType, QuantConnect.Market.USA),
                subscriptionResolution,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                true,
                false,
                false,
                false,
                subscriptionTickType);

            var consolidator = new TestConsolidator(subscriptionDataType, consolidatorOutputType);

            Assert.AreEqual(expected, SubscriptionManager.IsSubscriptionValidForConsolidator(subscription, consolidator));
        }

        private class TestConsolidator : IDataConsolidator
        {
            public event DataConsolidatedHandler DataConsolidated;
            public IBaseData Consolidated { get; }
            public IBaseData WorkingData { get; }
            public Type InputType { get; }
            public Type OutputType { get; }
            public void Update(IBaseData data) { }
            public void Scan(DateTime currentLocalTime) { }
            public void Dispose() { }

            public TestConsolidator(Type inputType, Type outputType)
            {
                InputType = inputType;
                OutputType = outputType;
            }
        }

        private static List<Tuple<Type, TickType>> GetSubscriptionDataTypes(SecurityType securityType, Resolution resolution, bool isCanonical = false)
        {
            var subscriptionManager = new SubscriptionManager();
            subscriptionManager.SetDataManager(new DataManagerStub());
            return subscriptionManager.LookupSubscriptionConfigDataTypes(securityType, resolution, isCanonical);
        }
    }
}
