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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Logging;
using QuantConnect.Statistics;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class SubscriptionManagerTests
    {
        [TestCase(SecurityType.Forex, Resolution.Daily, 1, TickType.Quote)]
        [TestCase(SecurityType.Forex, Resolution.Hour, 1, TickType.Quote)]
        [TestCase(SecurityType.Cfd, Resolution.Daily, 1, TickType.Quote)]
        [TestCase(SecurityType.Cfd, Resolution.Hour, 1, TickType.Quote)]
        [TestCase(SecurityType.Crypto, Resolution.Daily, 2, TickType.Trade, TickType.Quote)]
        [TestCase(SecurityType.Crypto, Resolution.Hour, 2, TickType.Trade, TickType.Quote)]
        [TestCase(SecurityType.Equity, Resolution.Daily, 1, TickType.Trade)]
        [TestCase(SecurityType.Equity, Resolution.Hour, 1, TickType.Trade)]
        public void GetsSubscriptionDataTypesLowResolution(SecurityType securityType, Resolution resolution, int count, params TickType[] expectedTickTypes)
        {
            var types = GetSubscriptionDataTypes(securityType, resolution);

            Assert.AreEqual(count, types.Count);
            for (var i = 0; i < expectedTickTypes.Length; i++)
            {
                Assert.IsTrue(types[i].Item2 == expectedTickTypes[i]);
            }
        }

        [Test]
        [TestCase(SecurityType.Base, Resolution.Minute, typeof(TradeBar), TickType.Trade)]
        [TestCase(SecurityType.Base, Resolution.Tick, typeof(Tick), TickType.Trade)]
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
        [TestCase(SecurityType.Future, Resolution.Minute, typeof(FutureUniverse), TickType.Quote)]
        [TestCase(SecurityType.Future, Resolution.Tick, typeof(FutureUniverse), TickType.Quote)]
        [TestCase(SecurityType.FutureOption, Resolution.Minute, typeof(OptionUniverse), TickType.Quote)]
        [TestCase(SecurityType.FutureOption, Resolution.Tick, typeof(OptionUniverse), TickType.Quote)]
        [TestCase(SecurityType.Option, Resolution.Minute, typeof(OptionUniverse), TickType.Quote)]
        [TestCase(SecurityType.Option, Resolution.Tick, typeof(OptionUniverse), TickType.Quote)]
        [TestCase(SecurityType.IndexOption, Resolution.Minute, typeof(OptionUniverse), TickType.Quote)]
        [TestCase(SecurityType.IndexOption, Resolution.Tick, typeof(OptionUniverse), TickType.Quote)]
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
        [TestCase(SecurityType.Equity, Resolution.Minute)]
        [TestCase(SecurityType.Equity, Resolution.Second)]
        [TestCase(SecurityType.Equity, Resolution.Tick)]
        [TestCase(SecurityType.Crypto, Resolution.Minute)]
        [TestCase(SecurityType.Crypto, Resolution.Second)]
        [TestCase(SecurityType.Crypto, Resolution.Tick)]
        public void GetsSubscriptionDataTypes(SecurityType securityType, Resolution resolution)
        {
            var types = GetSubscriptionDataTypes(securityType, resolution);

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
            var subscriptionManager = new SubscriptionManager(NullTimeKeeper.Instance);
            subscriptionManager.SetDataManager(new DataManagerStub());
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(5);
            var tickers = QuantConnect.Algorithm.CSharp.StressSymbols.StockSymbols.ToList();
            var symbols = tickers.Select(ticker => Symbol.Create(ticker, SecurityType.Equity, QuantConnect.Market.USA)).ToList();

            var readTask = new TaskFactory().StartNew(() =>
            {
                Log.Trace("Read task started");
                while (DateTime.UtcNow < end)
                {
                    subscriptionManager.Subscriptions.Select(x => x.Resolution).DefaultIfEmpty(Resolution.Minute).Min();
                    Thread.Sleep(1);
                }
                Log.Trace("Read task ended");
            });

            while (readTask.Status != TaskStatus.Running) Thread.Sleep(1);

            var addTask = new TaskFactory().StartNew(() =>
            {
                Log.Trace("Add task started");
                foreach (var symbol in symbols)
                {
                    subscriptionManager.Add(symbol, Resolution.Minute, DateTimeZone.Utc, DateTimeZone.Utc, true, false);
                }
                Log.Trace("Add task ended");
            });

            Task.WaitAll(addTask, readTask);
        }

        [Test]
        public void ScanPastConsolidatorsIsThreadSafe()
        {
            var subscriptionManager = new SubscriptionManager(new TimeKeeper(DateTime.UtcNow));
            var algorithm = new AlgorithmStub();
            subscriptionManager.SetDataManager(new DataManagerStub());
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(5);
            var tickers = QuantConnect.Algorithm.CSharp.StressSymbols.StockSymbols.Take(100).ToList();
            var symbols = tickers.Select(ticker => Symbol.Create(ticker, SecurityType.Equity, QuantConnect.Market.USA)).ToList();
            var consolidators = new Queue<Tuple<Symbol, IDataConsolidator>>();
            foreach (var symbol in symbols)
            {
                subscriptionManager.Add(symbol, Resolution.Minute, DateTimeZone.Utc, DateTimeZone.Utc, true, false);
            }

            var scanTask = Task.Factory.StartNew(() =>
            {
                Log.Debug("ScanPastConsolidators started");
                while (DateTime.UtcNow < end)
                {
                    subscriptionManager.ScanPastConsolidators(end.AddDays(1), algorithm);
                }
                Log.Debug("ScanPastConsolidators finished");
            });

            var addTask = Task.Factory.StartNew(() =>
            {
                while (scanTask.Status == TaskStatus.Running)
                {
                    Log.Debug("AddConsolidators started");
                    foreach (var symbol in symbols)
                    {
                        var consolidator = new IdentityDataConsolidator<BaseData>();
                        subscriptionManager.AddConsolidator(symbol, consolidator);
                        consolidators.Enqueue(new Tuple<Symbol, IDataConsolidator>(symbol, consolidator));
                    }
                    Log.Debug("AddConsolidators finished");
                    Assert.AreEqual(100, consolidators.Count);
                    Log.Debug("RemoveConsolidators started");
                    while (consolidators.TryDequeue(out var pair))
                    {
                        subscriptionManager.RemoveConsolidator(pair.Item1, pair.Item2);
                    }
                    Log.Debug("RemoveConsolidators finished");
                }
            });

            Task.WaitAll(scanTask, addTask);
            Assert.AreEqual(100, subscriptionManager.Count);
            Assert.AreEqual(0, consolidators.Count);
        }

        [Test]
        public void GetsCustomSubscriptionDataTypes()
        {
            var subscriptionManager = new SubscriptionManager(NullTimeKeeper.Instance);
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

        [TestCase(SecurityType.Future, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, TickType.Trade, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, TickType.Trade, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Future, TickType.Trade, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Future, TickType.Trade, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Future, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, TickType.Quote, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, TickType.Quote, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Future, TickType.Quote, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, TickType.Quote, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Future, TickType.Quote, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Future, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, TickType.OpenInterest, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, TickType.OpenInterest, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Future, TickType.OpenInterest, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, TickType.OpenInterest, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Future, TickType.OpenInterest, typeof(RenkoBar), typeof(Tick), false)]

        [TestCase(SecurityType.Equity, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, TickType.Trade, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, TickType.Trade, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Equity, TickType.Trade, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Equity, TickType.Trade, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, TickType.Quote, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, TickType.Quote, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Equity, TickType.Quote, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, TickType.Quote, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, TickType.Quote, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, TickType.OpenInterest, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, TickType.OpenInterest, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Equity, TickType.OpenInterest, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, TickType.OpenInterest, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, TickType.OpenInterest, typeof(RenkoBar), typeof(Tick), false)]

        [TestCase(SecurityType.Cfd, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, TickType.Trade, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, TickType.Trade, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, TickType.Trade, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Cfd, TickType.Trade, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, TickType.Quote, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, TickType.Quote, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Cfd, TickType.Quote, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, TickType.Quote, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Cfd, TickType.Quote, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, TickType.OpenInterest, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, TickType.OpenInterest, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Cfd, TickType.OpenInterest, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, TickType.OpenInterest, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, TickType.OpenInterest, typeof(RenkoBar), typeof(Tick), false)]

        [TestCase(SecurityType.Forex, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, TickType.Trade, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, TickType.Trade, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, TickType.Trade, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Forex, TickType.Trade, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, TickType.Quote, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, TickType.Quote, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Forex, TickType.Quote, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, TickType.Quote, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Forex, TickType.Quote, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, TickType.OpenInterest, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, TickType.OpenInterest, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Forex, TickType.OpenInterest, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, TickType.OpenInterest, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, TickType.OpenInterest, typeof(RenkoBar), typeof(Tick), false)]

        [TestCase(SecurityType.Crypto, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, TickType.Trade, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, TickType.Trade, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, TickType.Trade, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Crypto, TickType.Trade, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, TickType.Quote, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, TickType.Quote, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Crypto, TickType.Quote, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, TickType.Quote, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Crypto, TickType.Quote, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, TickType.OpenInterest, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, TickType.OpenInterest, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Crypto, TickType.OpenInterest, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, TickType.OpenInterest, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, TickType.OpenInterest, typeof(RenkoBar), typeof(Tick), false)]
        public void ValidatesSubscriptionTickTypesForConsolidators(
            SecurityType securityType,
            TickType? subscriptionTickType,
            Type consolidatorInputType,
            Type consolidatorOutputType,
            bool expected)
        {
            var subscription = new SubscriptionDataConfig(
                typeof(Tick),
                Symbol.Create("XYZ", securityType, QuantConnect.Market.USA),
                Resolution.Tick,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                true,
                false,
                false,
                false,
                subscriptionTickType);
            using var consolidator = new TestConsolidator(consolidatorInputType, consolidatorOutputType);
            Assert.AreEqual(expected, SubscriptionManager.IsSubscriptionValidForConsolidator(subscription, consolidator));
        }

        [TestCase(TickType.Trade, TickType.Trade, true)]
        [TestCase(TickType.Trade, TickType.Quote, false)]
        [TestCase(TickType.Trade, TickType.OpenInterest, false)]

        [TestCase(TickType.Quote, TickType.Quote, true)]
        [TestCase(TickType.Quote, TickType.Trade, false)]
        [TestCase(TickType.Quote, TickType.OpenInterest, false)]

        [TestCase(TickType.OpenInterest, TickType.OpenInterest, true)]
        [TestCase(TickType.OpenInterest, TickType.Quote, false)]
        [TestCase(TickType.OpenInterest, TickType.Trade, false)]
        public void ValidatesSubscriptionTickTypesForClassicRenkoConsolidators(TickType subscriptionTickType, TickType desiredTickType, bool expected)
        {
            var subscription = new SubscriptionDataConfig(
                  typeof(Tick),
                  Symbol.Create("XYZ", SecurityType.Future, QuantConnect.Market.USA),
                  Resolution.Tick,
                  DateTimeZone.Utc,
                  DateTimeZone.Utc,
                  true,
                  false,
                  false,
                  false,
                  subscriptionTickType);
            Func<IBaseData, decimal> selector = data =>
            {
                var tick = data as Tick;
                return tick.Quantity * tick.Price;
            };
            using var consolidator = new ClassicRenkoConsolidator(2000m, selector);
            Assert.AreEqual(expected, SubscriptionManager.IsSubscriptionValidForConsolidator(subscription, consolidator, desiredTickType));
        }

        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(IBaseData), TickType.Trade, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Future, typeof(OpenInterest), TickType.Trade, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, typeof(OpenInterest), TickType.Trade, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Trade, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(IBaseData), TickType.Quote, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Future, typeof(OpenInterest), TickType.Quote, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, typeof(OpenInterest), TickType.Quote, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, TickType.Trade, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.Quote, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.OpenInterest, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Future, typeof(IBaseData), TickType.OpenInterest, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Future, typeof(Tick), TickType.OpenInterest, null, typeof(IBaseData), typeof(Tick), false)]

        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(IBaseData), TickType.Trade, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Equity, typeof(OpenInterest), TickType.Trade, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, typeof(OpenInterest), TickType.Trade, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Trade, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(IBaseData), TickType.Quote, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Equity, typeof(OpenInterest), TickType.Quote, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, typeof(OpenInterest), TickType.Quote, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, TickType.Trade, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.Quote, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.OpenInterest, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Equity, typeof(IBaseData), TickType.OpenInterest, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Equity, typeof(Tick), TickType.OpenInterest, null, typeof(IBaseData), typeof(Tick), false)]

        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(IBaseData), TickType.Trade, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Cfd, typeof(OpenInterest), TickType.Trade, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(OpenInterest), TickType.Trade, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Trade, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(IBaseData), TickType.Quote, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, null, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, typeof(OpenInterest), TickType.Quote, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(OpenInterest), TickType.Quote, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, TickType.Trade, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.Quote, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(IBaseData), TickType.OpenInterest, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Cfd, typeof(OpenInterest), TickType.OpenInterest, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(OpenInterest), TickType.OpenInterest, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Cfd, typeof(Tick), TickType.OpenInterest, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]

        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(IBaseData), TickType.Trade, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Forex, typeof(OpenInterest), TickType.Trade, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(OpenInterest), TickType.Trade, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Trade, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(IBaseData), TickType.Quote, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, null, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, typeof(OpenInterest), TickType.Quote, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(OpenInterest), TickType.Quote, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, TickType.Trade, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.Quote, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(IBaseData), TickType.OpenInterest, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Forex, typeof(OpenInterest), TickType.OpenInterest, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(OpenInterest), TickType.OpenInterest, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Forex, typeof(Tick), TickType.OpenInterest, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]

        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(IBaseData), TickType.Trade, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Crypto, typeof(OpenInterest), TickType.Trade, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(OpenInterest), TickType.Trade, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Trade, TickType.Trade, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(QuoteBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(IBaseData), TickType.Quote, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, null, typeof(IBaseData), typeof(Tick), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, typeof(OpenInterest), TickType.Quote, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(OpenInterest), TickType.Quote, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, TickType.Trade, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.Quote, TickType.Quote, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, null, typeof(Tick), typeof(QuoteBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, null, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(IBaseData), TickType.OpenInterest, null, typeof(IBaseData), typeof(RenkoBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, null, typeof(IBaseData), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, null, typeof(RenkoBar), typeof(Tick), false)]
        [TestCase(SecurityType.Crypto, typeof(OpenInterest), TickType.OpenInterest, null, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(OpenInterest), TickType.OpenInterest, TickType.Trade, typeof(OpenInterest), typeof(TradeBar), true)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, TickType.Trade, typeof(TradeBar), typeof(TradeBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, TickType.Quote, typeof(Tick), typeof(RenkoBar), false)]
        [TestCase(SecurityType.Crypto, typeof(Tick), TickType.OpenInterest, TickType.OpenInterest, typeof(Tick), typeof(RenkoBar), true)]
        public void GetsExpectedSubscriptionsGivenATickType(SecurityType securityType,
            Type subscriptionType,
            TickType? subscriptionTickType,
            TickType? desiredTickType,
            Type consolidatorInputType,
            Type consolidatorOutputType,
            bool expected)
        {
            var subscription = new SubscriptionDataConfig(
                subscriptionType,
                Symbol.Create("XYZ", securityType, QuantConnect.Market.USA),
                Resolution.Tick,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                true,
                false,
                false,
                false,
                subscriptionTickType);
            using var consolidator = new TestConsolidator(consolidatorInputType, consolidatorOutputType);
            Assert.AreEqual(expected, SubscriptionManager.IsSubscriptionValidForConsolidator(subscription, consolidator, desiredTickType));
        }

        [Test]
        public void CanAddAndRemoveCSharpConsolidatorFromPython()
        {
            // NOTE: we use the IdentityDataConsolidator here because it's a generic class, which reproduces the bug.
            // pyConsolidator.TryConvert(out IDataConsolidator consolidator) will return false, because the python type name
            // and the C# type name don't match for generic types (e.g. IdentityDataConsolidator[TradeBar] != IdentityDataConsolidator`1)

            using var _ = Py.GIL();
            var module = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def get_consolidator():
    return IdentityDataConsolidator[TradeBar]()
");

            var algorithm = new AlgorithmStub();
            var symbol = algorithm.AddEquity("SPY").Symbol;

            var consolidator = module.GetAttr("get_consolidator").Invoke();

            algorithm.SubscriptionManager.AddConsolidator(symbol, consolidator);
            Assert.AreEqual(1, algorithm.SubscriptionManager.Subscriptions.Sum(x => x.Consolidators.Count));

            algorithm.SubscriptionManager.RemoveConsolidator(symbol, consolidator);
            Assert.AreEqual(0, algorithm.SubscriptionManager.Subscriptions.Sum(x => x.Consolidators.Count));
        }

        [Test]
        public void CanAddAndRemoveCSharpConsolidatorFromPythonWithWrapper()
        {
            using var _ = Py.GIL();
            var module = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def get_consolidator():
    return IdentityDataConsolidator[TradeBar]()
");

            var algorithm = new AlgorithmStub();
            var symbol = algorithm.AddEquity("SPY").Symbol;

            var pyConsolidator = module.GetAttr("get_consolidator").Invoke();

            algorithm.SubscriptionManager.AddConsolidator(Symbols.SPY, pyConsolidator);
            Assert.AreEqual(1, algorithm.SubscriptionManager.Subscriptions.Sum(x => x.Consolidators.Count));

            algorithm.SubscriptionManager.RemoveConsolidator(Symbols.SPY, pyConsolidator);
            Assert.AreEqual(0, algorithm.SubscriptionManager.Subscriptions.Sum(x => x.Consolidators.Count));
        }

        [Test]
        public void CanAddAndRemovePythonConsolidator()
        {
            using var _ = Py.GIL();
            var module = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class CustomQuoteBarConsolidator(PythonConsolidator):

    def __init__(self):

        #IDataConsolidator required vars for all consolidators
        self.consolidated = None
        self.working_data = None
        self.input_type = QuoteBar
        self.output_type = QuoteBar

    def update(self, data):
        pass

    def scan(self, time):
        pass

def get_consolidator():
    return CustomQuoteBarConsolidator()
");

            var algorithm = new AlgorithmStub();
            var symbol = algorithm.AddEquity("SPY").Symbol;

            var pyConsolidator = module.GetAttr("get_consolidator").Invoke();

            algorithm.SubscriptionManager.AddConsolidator(Symbols.SPY, pyConsolidator);
            Assert.AreEqual(1, algorithm.SubscriptionManager.Subscriptions.Sum(x => x.Consolidators.Count));

            algorithm.SubscriptionManager.RemoveConsolidator(Symbols.SPY, pyConsolidator);
            Assert.AreEqual(0, algorithm.SubscriptionManager.Subscriptions.Sum(x => x.Consolidators.Count));
        }

        [Test]
        public void RunRemoveConsolidatorsRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("ManuallyRemovedConsolidatorsAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "0"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "0%"},
                    {"Drawdown", "0%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "0%"},
                    {"Sharpe Ratio", "0"},
                    {"Probabilistic Sharpe Ratio", "0%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0"},
                    {"Annual Standard Deviation", "0"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "-8.91"},
                    {"Tracking Error", "0.223"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$0.00"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        private class TestConsolidator : IDataConsolidator
        {
#pragma warning disable 0067 // TestConsolidator never uses this event; just ignore the warning
            public event DataConsolidatedHandler DataConsolidated;
#pragma warning restore 0067

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

            public TestConsolidator(Type inputType)
            {
                InputType = inputType;
            }

            public void Reset()
            {
            }
        }

        private static List<Tuple<Type, TickType>> GetSubscriptionDataTypes(SecurityType securityType, Resolution resolution, bool isCanonical = false)
        {
            var subscriptionManager = new SubscriptionManager(NullTimeKeeper.Instance);
            subscriptionManager.SetDataManager(new DataManagerStub());
            return subscriptionManager.LookupSubscriptionConfigDataTypes(securityType, resolution, isCanonical);
        }
    }
}
