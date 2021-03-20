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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Packets;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class LiveTradingResultHandlerTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void DailySampleValueBasedOnMarketHour(bool extendedMarketHoursEnabled)
        {
            var referenceDate = new DateTime(2020, 11, 25);
            var resultHandler = new TestLiveTradingResultHandler
            {
                // market is open
                InitialSampleTime = referenceDate.AddHours(10)
            };
            resultHandler.Initialize(new LiveNodePacket(),
                new QuantConnect.Messaging.Messaging(),
                new Api.Api(), 
                new BacktestingTransactionHandler());

            var algo = new AlgorithmStub(createDataManager:false);
            algo.SetFinishedWarmingUp();
            var dataManager = new DataManagerStub(new TestDataFeed(), algo);
            algo.SubscriptionManager.SetDataManager(dataManager);
            var aapl = algo.AddEquity("AAPL", extendedMarketHours: extendedMarketHoursEnabled);
            algo.PostInitialize();
            resultHandler.SetAlgorithm(algo, 100000);
            resultHandler.OnSecuritiesChanged(SecurityChanges.Added(aapl));

            algo.Portfolio.CashBook["USD"].AddAmount(1000);
            algo.Portfolio.InvalidateTotalPortfolioValue();
            resultHandler.Sample(referenceDate.AddHours(15));

            Assert.IsFalse(resultHandler.Charts.ContainsKey("Strategy Equity"), "Should not sample on the same start date");

            // will be ignored based on 'extendedMarketHoursEnabled'
            algo.Portfolio.CashBook["USD"].AddAmount(10000);
            algo.Portfolio.InvalidateTotalPortfolioValue();
            resultHandler.Sample(referenceDate.AddHours(22));

            Assert.IsFalse(resultHandler.Charts.ContainsKey("Strategy Equity"), "Should not sample on the same start date");

            resultHandler.Sample(referenceDate.AddHours(24));
            Assert.IsTrue(resultHandler.Charts.ContainsKey("Strategy Equity"), "Expect sample of date change");

            Assert.AreEqual(extendedMarketHoursEnabled ? 111000 : 101000,
                resultHandler.Charts["Strategy Equity"].Series["Equity"].Values.Single().y);

            resultHandler.Exit();
        }

        private class TestLiveTradingResultHandler : LiveTradingResultHandler
        {
            public DateTime InitialSampleTime { get; set; }
            public override void Initialize(AlgorithmNodePacket job,
                IMessagingHandler messagingHandler,
                IApi api,
                ITransactionHandler transactionHandler)
            {
                base.Initialize(job, messagingHandler, api, transactionHandler);
                PreviousUtcSampleTime = InitialSampleTime;
            }
        }

        private class TestDataFeed : IDataFeed
        {
            public bool IsActive { get; }

            public void Initialize(
                IAlgorithm algorithm,
                AlgorithmNodePacket job,
                IResultHandler resultHandler,
                IMapFileProvider mapFileProvider,
                IFactorFileProvider factorFileProvider,
                IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager,
                IDataFeedTimeProvider dataFeedTimeProvider,
                IDataChannelProvider dataChannelProvider
                )
            {
            }
            public Subscription CreateSubscription(SubscriptionRequest request)
            {
                return null;
            }
            public void RemoveSubscription(Subscription subscription)
            {
            }
            public void Exit()
            {
            }
        }
    }
}
