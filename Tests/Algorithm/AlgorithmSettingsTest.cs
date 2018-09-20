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
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmSettingsTest
    {
        [Test]
        public void DefaultTrueValueOfLiquidateWorksCorrectly()
        {
            var algo = new QCAlgorithm();
            var fakeOrderProcessor = InitializeAndGetFakeOrderProcessor(algo);

            algo.Liquidate();

            // It should send a order to set us flat
            Assert.IsFalse(fakeOrderProcessor.ProcessedOrdersRequests.IsNullOrEmpty());
        }

        [Test]
        public void DisablingLiquidateWorksCorrectly()
        {
            var algo = new QCAlgorithm();
            algo.Settings.LiquidateEnabled = false;
            var fakeOrderProcessor = InitializeAndGetFakeOrderProcessor(algo);

            algo.Liquidate();

            // It should NOT send a order to set us flat
            Assert.IsTrue(fakeOrderProcessor.ProcessedOrdersRequests.IsNullOrEmpty());
        }

        [Test]
        public void SettingDataSubscriptionLimitWorksCorrectly()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.Settings.DataSubscriptionLimit = 1;

            var tickers = new[] { "SPY", "AAPL" };

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    algo.AddEquity(tickers[i]);
                }
                Assert.Fail();
            }
            catch (Exception e)
            {
                // We are expecting it to throw an exception due to DataSubscriptionLimit
                Assert.IsTrue(e.Message.Contains("DataSubscriptionLimit"));
                Assert.AreEqual(algo.Securities.Count, 1);
            }
        }

        [Test]
        public void DefaultValueOfDataSubscriptionLimitWorksCorrectly()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));

            var tickers = new[] { "SPY", "AAPL" };

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    algo.AddEquity(tickers[i]);
                }
                Assert.AreEqual(algo.Securities.Count, 2);
            }
            catch (Exception e)
            {
                Assert.Fail();
                // We are NOT expecting it to throw an exception due to DataSubscriptionLimit
            }
        }

        [Test]
        public void SettingSetHoldingsBufferWorksCorrectly()
        {
            var algo = new QCAlgorithm();
            algo.Settings.FreePortfolioValuePercentage = 0;
            InitializeAndGetFakeOrderProcessor(algo);

            var actual = algo.CalculateOrderQuantity(Symbols.SPY, 1m);
            // 100000 / 20 - 2 due to fee =
            Assert.AreEqual(4998m, actual);
        }

        [Test]
        public void DefaultValueOfSetHoldingsBufferWorksCorrectly()
        {
            var algo = new QCAlgorithm();

            InitializeAndGetFakeOrderProcessor(algo);

            var actual = algo.CalculateOrderQuantity(Symbols.SPY, 1m);
            // 100000 / 20 - 1 due to fee - effect of the target being reduced because of FreePortfolioValuePercentage
            Assert.AreEqual(4986m, actual);
        }

        private FakeOrderProcessor InitializeAndGetFakeOrderProcessor(QCAlgorithm algo)
        {
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetFinishedWarmingUp();
            algo.SetCash(100000);
            algo.AddEquity("SPY");
            var fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(fakeOrderProcessor);
            algo.Portfolio["SPY"].SetHoldings(1, 10);
            var security = algo.Securities["SPY"];
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = 20,
                High = 20,
                Low = 20,
                Close = 20
            });

            Assert.IsTrue(fakeOrderProcessor.ProcessedOrdersRequests.IsNullOrEmpty());
            return fakeOrderProcessor;
        }
    }
}
