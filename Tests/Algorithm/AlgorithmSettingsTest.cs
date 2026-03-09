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
using QuantConnect.Util;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Lean.Engine.Setup;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
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
        public void SettingSetHoldingsBufferWorksCorrectly()
        {
            var algo = new QCAlgorithm();
            algo.Settings.FreePortfolioValue = 0;
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            var actual = algo.CalculateOrderQuantity(Symbols.SPY, 1m);
            // 100000 / 20 - 1 due to fee - effect of the target being reduced because of FreePortfolioValuePercentage
            Assert.AreEqual(4986m, actual);
        }

        [TestCase(BrokerageName.FTX, 365)]
        [TestCase(BrokerageName.RBI, 252)]
        [TestCase(BrokerageName.Eze, 252)]
        [TestCase(BrokerageName.Axos, 252)]
        [TestCase(BrokerageName.Samco, 252)]
        [TestCase(BrokerageName.FTXUS, 365)]
        [TestCase(BrokerageName.Bybit, 365)]
        [TestCase(BrokerageName.Kraken, 365)]
        [TestCase(BrokerageName.Exante, 252)]
        [TestCase(BrokerageName.Binance, 365)]
        [TestCase(BrokerageName.Default, 252)]
        [TestCase(BrokerageName.Zerodha, 252)]
        [TestCase(BrokerageName.Bitfinex, 365)]
        [TestCase(BrokerageName.Wolverine, 252)]
        [TestCase(BrokerageName.TDAmeritrade, 252)]
        [TestCase(BrokerageName.FxcmBrokerage, 252)]
        [TestCase(BrokerageName.OandaBrokerage, 252)]
        [TestCase(BrokerageName.BinanceFutures, 365)]
        [TestCase(BrokerageName.TradierBrokerage, 252)]
        [TestCase(BrokerageName.BinanceCoinFutures, 365)]
        [TestCase(BrokerageName.TradingTechnologies, 252)]
        [TestCase(BrokerageName.QuantConnectBrokerage, 252)]
        [TestCase(BrokerageName.Coinbase, 365, AccountType.Cash)]
        [TestCase(BrokerageName.BinanceUS, 365, AccountType.Cash)]
        [TestCase(BrokerageName.InteractiveBrokersBrokerage, 252)]
        public void ReturnUniqueTradingDayPerYearDependOnBrokerageName(BrokerageName brokerageName, int expectedTradingDayPerYear, AccountType accountType = AccountType.Margin)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetBrokerageModel(brokerageName, accountType);

            BaseSetupHandler.SetBrokerageTradingDayPerYear(algorithm);

            Assert.AreEqual(expectedTradingDayPerYear, algorithm.Settings.TradingDaysPerYear.Value);
        }

        [TestCase(BrokerageName.Bybit, 202, 365)]
        [TestCase(BrokerageName.InteractiveBrokersBrokerage, 404, 252)]
        public void ReturnCustomTradingDayPerYearIndependentlyFromBrokerageName(BrokerageName brokerageName, int customTradingDayPerYear, int expectedDefaultTradingDayPerYearForBrokerage)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetBrokerageModel(brokerageName);
            algorithm.Settings.TradingDaysPerYear = customTradingDayPerYear;

            // duplicate: make sure that custom value is assigned
            BaseSetupHandler.SetBrokerageTradingDayPerYear(algorithm);

            Assert.AreNotEqual(expectedDefaultTradingDayPerYearForBrokerage, algorithm.Settings.TradingDaysPerYear);
        }

        [TestCase(252, null)]
        [TestCase(404, 404)]
        public void ReturnTradingDayPerYearWithoutSetBrokerage(int expectedTradingDayPerYear, int? customTradingDayPerYear = null)
        {
            var algorithm = new QCAlgorithm();

            if (customTradingDayPerYear.HasValue)
            {
                algorithm.Settings.TradingDaysPerYear = customTradingDayPerYear.Value;
            }

            BaseSetupHandler.SetBrokerageTradingDayPerYear(algorithm);

            Assert.AreEqual(expectedTradingDayPerYear, algorithm.Settings.TradingDaysPerYear);
        }

        private FakeOrderProcessor InitializeAndGetFakeOrderProcessor(QCAlgorithm algo)
        {
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetFinishedWarmingUp();
            algo.SetCash(100000);
            var symbol = algo.AddEquity("SPY").Symbol;
            var fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(fakeOrderProcessor);
            algo.Portfolio[symbol].SetHoldings(1, 10);
            var security = algo.Securities[symbol];
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
