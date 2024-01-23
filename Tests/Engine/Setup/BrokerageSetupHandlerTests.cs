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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Option.StrategyMatcher;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;
using Bitcoin = QuantConnect.Algorithm.CSharp.LiveTradingFeaturesAlgorithm.Bitcoin;
using System.Collections;

namespace QuantConnect.Tests.Engine.Setup
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class BrokerageSetupHandlerTests
    {
        private IAlgorithm _algorithm;
        private ITransactionHandler _transactionHandler;
        private NonDequeingTestResultsHandler _resultHandler;
        private IBrokerage _brokerage;
        private DataManager _dataManager;

        private TestableBrokerageSetupHandler _brokerageSetupHandler;

        [OneTimeSetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _dataManager = new DataManagerStub(_algorithm);
            _algorithm.SubscriptionManager.SetDataManager(_dataManager);
            _transactionHandler = new BrokerageTransactionHandler();
            _resultHandler = new NonDequeingTestResultsHandler();
            _brokerage = new TestBrokerage();

            _brokerageSetupHandler = new TestableBrokerageSetupHandler();
            _transactionHandler.Initialize(_algorithm, _brokerage, _resultHandler);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _dataManager.RemoveAllSubscriptions();
            _brokerage.DisposeSafely();
            _transactionHandler.Exit();
            _resultHandler.Exit();
        }

        [Test]
        public void CanGetOpenOrders()
        {
            _brokerageSetupHandler.PublicGetOpenOrders(_algorithm, _resultHandler, _transactionHandler, _brokerage);

            Assert.AreEqual(_transactionHandler.Orders.Count, 4);

            Assert.AreEqual(_transactionHandler.OrderTickets.Count, 4);

            // Check Price Currency is not null
            foreach (var order in _transactionHandler.Orders.Values)
            {
                Assert.IsFalse(string.IsNullOrEmpty(order.PriceCurrency));
                Assert.AreEqual(OrderStatus.Submitted, order.Status);
            }

            // Warn the user about each open order
            Assert.AreEqual(_resultHandler.PersistentMessages.Count, 4);

            // Market order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Market).Value.Quantity, 100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Market).Value.SubmitRequest.LimitPrice, 1.2345m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Market).Value.SubmitRequest.StopPrice, 1.2345m);

            // Limit Order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Limit).Value.Quantity, -100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Limit).Value.SubmitRequest.LimitPrice, 2.2345m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Limit).Value.SubmitRequest.StopPrice, 0m);

            // Stop market order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopMarket).Value.Quantity, 100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopMarket).Value.SubmitRequest.LimitPrice, 0m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopMarket).Value.SubmitRequest.StopPrice, 2.2345m);

            // Stop Limit order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopLimit).Value.Quantity, 100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopLimit).Value.SubmitRequest.LimitPrice, 0.2345m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopLimit).Value.SubmitRequest.StopPrice, 2.2345m);

            // SPY security should be added to the algorithm
            Assert.Contains(Symbols.SPY, _algorithm.Securities.Select(x => x.Key).ToList());
        }

        [TestCaseSource(typeof(ExistingHoldingAndOrdersDataClass), nameof(ExistingHoldingAndOrdersDataClass.GetExistingHoldingsAndOrdersTestCaseData))]
        public void SecondExistingHoldingsAndOrdersResolution(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            ExistingHoldingsAndOrdersResolution(getHoldings, getOrders, expected, Resolution.Second);
        }

        [TestCaseSource(typeof(ExistingHoldingAndOrdersDataClass), nameof(ExistingHoldingAndOrdersDataClass.GetExistingHoldingsAndOrdersTestCaseData))]
        public void MinuteExistingHoldingsAndOrdersResolution(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            ExistingHoldingsAndOrdersResolution(getHoldings, getOrders, expected, Resolution.Minute);
        }

        [TestCaseSource(typeof(ExistingHoldingAndOrdersDataClass), nameof(ExistingHoldingAndOrdersDataClass.GetExistingHoldingsAndOrdersTestCaseData))]
        public void TickExistingHoldingsAndOrdersResolution(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            ExistingHoldingsAndOrdersResolution(getHoldings, getOrders, expected, Resolution.Tick);
        }

        public void ExistingHoldingsAndOrdersResolution(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected, Resolution resolution)
        {
            var algorithm = new TestAlgorithm { UniverseSettings = { Resolution = resolution } };
            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();
            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var objectStore = new Mock<IObjectStore>();
            var brokerage = new Mock<IBrokerage>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(getHoldings);
            brokerage.Setup(x => x.GetOpenOrders()).Returns(getOrders);

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            var result = setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider));

            Assert.AreEqual(expected, result);

            foreach (var symbol in algorithm.Securities.Keys)
            {
                var configs = algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol);
                Assert.AreEqual(algorithm.UniverseSettings.Resolution, configs.First().Resolution);
            }
        }

        [TestCaseSource(typeof(ExistingHoldingAndOrdersDataClass), nameof(ExistingHoldingAndOrdersDataClass.GetExistingHoldingsAndOrdersTestCaseData))]
        public void ExistingHoldingsAndOrdersUniverseSettings(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            // Set our universe settings
            var hasCrypto = false;
            try
            {
                hasCrypto = getHoldings().Any(x => x.Symbol.Value == "BTCUSD");
            }
            catch
            {
            }
            var algorithm = new TestAlgorithm { UniverseSettings = { Resolution = Resolution.Daily, Leverage = (hasCrypto ? 1 : 20), FillForward = false, ExtendedMarketHours = true} };
            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();
            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var objectStore = new Mock<IObjectStore>();
            var brokerage = new Mock<IBrokerage>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(getHoldings);
            brokerage.Setup(x => x.GetOpenOrders()).Returns(getOrders);

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            var result = setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider));

            if (result != expected)
            {
                Assert.Fail("SetupHandler result did not match expected value");
            }

            foreach (var symbol in algorithm.Securities.Keys)
            {
                var config = algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol).First();

                // Assert Resolution and FillForward settings persisted
                Assert.AreEqual(algorithm.UniverseSettings.Resolution, config.Resolution);
                Assert.AreEqual(algorithm.UniverseSettings.FillForward, config.FillDataForward);

                // Assert ExtendedHours setting persisted for equities
                if (config.SecurityType == SecurityType.Equity)
                {
                    Assert.AreEqual(algorithm.UniverseSettings.ExtendedMarketHours, config.ExtendedMarketHours);
                }

                // Assert Leverage setting persisted for non options or futures (Blocked from setting leverage in Security.SetLeverage())
                if (!symbol.SecurityType.IsOption() && symbol.SecurityType != SecurityType.Future)
                {
                    var security = algorithm.Securities[symbol];
                    Assert.AreEqual(algorithm.UniverseSettings.Leverage, security.Leverage);
                }
            }
        }

        [TestCaseSource(typeof(ExistingHoldingAndOrdersDataClass),nameof(ExistingHoldingAndOrdersDataClass.GetExistingHoldingsAndOrdersTestCaseData))]
        public void LoadsExistingHoldingsAndOrders(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            var algorithm = new TestAlgorithm();
            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            TestLoadExistingHoldingsAndOrders(algorithm, getHoldings, getOrders, expected);

            foreach (var security in algorithm.Securities.Values)
            {
                if (security.Symbol.SecurityType == SecurityType.Option)
                {
                    Assert.AreEqual(DataNormalizationMode.Raw, security.DataNormalizationMode);

                    var underlyingSecurity = algorithm.Securities[security.Symbol.Underlying];
                    Assert.AreEqual(DataNormalizationMode.Raw, underlyingSecurity.DataNormalizationMode);
                }
            }
        }

        [TestCaseSource(nameof(GetExistingHoldingsAndOrdersWithCustomDataTestCase))]
        public void LoadsExistingHoldingsAndOrdersWithCustomData(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders)
        {
            var algorithm = new TestAlgorithm();
            algorithm.AddData<Bitcoin>("BTC");
            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            TestLoadExistingHoldingsAndOrders(algorithm, getHoldings, getOrders, true);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EnforcesTotalPortfolioValue(bool fails)
        {
            var algorithm = new TestAlgorithm();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);

            algorithm.SetHistoryProvider(new TestHistoryProvider());
            var job = GetJob();
            job.BrokerageData[BrokerageSetupHandler.MaxAllocationLimitConfig] = fails ? "1" : "1000000000";

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount> { new CashAmount(10000, Currencies.USD), new CashAmount(11, Currencies.GBP)});
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>());
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.AreEqual(!fails, setupHandler.Setup(new SetupHandlerParameters(algorithm.DataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            Assert.AreEqual(10000, algorithm.Portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(11, algorithm.Portfolio.CashBook[Currencies.GBP].Amount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EnforcesAccountCurrency(bool enforceAccountCurrency)
        {
            var algorithm = new TestAlgorithm();
            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();
            if (enforceAccountCurrency)
            {
                job.BrokerageData[BrokerageSetupHandler.MaxAllocationLimitConfig] = "200000";
            }

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>());
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.EUR);

            var setupHandler = new BrokerageSetupHandler();
            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            Assert.AreEqual(enforceAccountCurrency ? Currencies.USD : Currencies.EUR, algorithm.AccountCurrency);
        }

        [Test]
        public void HandlesErrorOnInitializeCorrectly()
        {
            var algorithm = new BacktestingSetupHandlerTests.TestAlgorithmThrowsOnInitialize();

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>());
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsFalse(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            setupHandler.DisposeSafely();
            Assert.AreEqual(1, setupHandler.Errors.Count);
            Assert.IsTrue(setupHandler.Errors[0].InnerException.Message.Equals("Some failure"));
        }

        [Test]
        public void HoldingsPositionGroupResolved()
        {
            var algorithm = new TestAlgorithm();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>
            {
                // covered call
                new Holding { Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = -1 },
                new Holding { Symbol = Symbols.SPY, Quantity = 100 }
            });
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            using var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            // let's assert be detect the covered call option strategy for existing position correctly
            if (algorithm.Portfolio.Positions.Groups.Where(group => group.BuyingPowerModel is OptionStrategyPositionGroupBuyingPowerModel)
                .Count(group => ((OptionStrategyPositionGroupBuyingPowerModel)@group.BuyingPowerModel).ToString() == OptionStrategyDefinitions.CoveredCall.Name
                    && (Math.Abs(group.Quantity) == 1)) != 1)
            {
                throw new Exception($"Option strategy: '{OptionStrategyDefinitions.CoveredCall.Name}' was not found!");
            }
        }

        [Test]
        public void LoadsHoldingsForExpectedMarket()
        {
            var symbol = Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda);

            var algorithm = new TestAlgorithm();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>
            {
                new Holding { Symbol = symbol, Quantity = 100 }
            });
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            Security security;
            Assert.IsTrue(algorithm.Portfolio.Securities.TryGetValue(symbol, out security));
            Assert.AreEqual(symbol, security.Symbol);
        }

        [Test]
        public void SeedsSecurityCorrectly()
        {
            var symbol = Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda);

            var algorithm = new TestAlgorithm();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>
            {
                new Holding { Symbol = symbol, Quantity = 100, MarketPrice = 99}
            });
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            Security security;
            Assert.IsTrue(algorithm.Portfolio.Securities.TryGetValue(symbol, out security));
            Assert.AreEqual(symbol, security.Symbol);
            Assert.AreEqual(99, security.Price);

            var last = security.GetLastData();
            Assert.IsTrue((DateTime.UtcNow.ConvertFromUtc(security.Exchange.TimeZone) - last.Time) < TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AlgorithmTimeIsSetToUtcNowBeforePostInitialize()
        {
            var time = DateTime.UtcNow;
            TestAlgorithm algorithm = null;

            algorithm = new TestAlgorithm(() =>
            {
                Assert.That(algorithm.UtcTime > time);
            });

            Assert.AreEqual(new DateTime(1998, 1, 1), algorithm.UtcTime);

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>());
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            Assert.Greater(algorithm.UtcTime, time);
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void HasErrorWithZeroTotalPortfolioValue(bool hasCashBalance, bool hasHoldings)
        {
            var algorithm = new TestAlgorithm();

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = GetJob();
            job.Brokerage = "TestBrokerage";

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();
            var objectStore = new Mock<IObjectStore>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(
                hasCashBalance
                    ? new List<CashAmount>
                    {
                        new CashAmount(1000, "USD")
                    }
                    : new List<CashAmount>()
                );
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(
                hasHoldings
                    ? new List<Holding>
                    {
                        new Holding { Symbol = Symbols.SPY, Quantity = 1, AveragePrice = 100, MarketPrice = 100 }
                    }
                    : new List<Holding>());
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            var dataManager = new DataManagerStub(algorithm, new MockDataFeed(), true);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider)));

            if (!hasCashBalance && !hasHoldings)
            {
                Assert.IsFalse(algorithm.DebugMessages.IsEmpty);

                Assert.That(algorithm.DebugMessages.Any(x => x.Contains("No cash balances or holdings were found in the brokerage account.")));
            }
        }

        private void TestLoadExistingHoldingsAndOrders(IAlgorithm algorithm, Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            var job = GetJob();

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var objectStore = new Mock<IObjectStore>();
            var brokerage = new Mock<IBrokerage>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.AccountBaseCurrency).Returns(Currencies.USD);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(getHoldings);
            brokerage.Setup(x => x.GetOpenOrders()).Returns(getOrders);

            var setupHandler = new TestableBrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            var parameters = new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider);
            var result = setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider));
            Assert.AreEqual(expected, result);
        }

        private static object[] GetExistingHoldingsAndOrdersWithCustomDataTestCase =
        {
            new object[] {
                new Func<List<Holding>>(() => new List<Holding> { new Holding { Symbol = new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), Quantity = 1 }}),
                new Func<List<Order>>(() => new List<Order>())},
            new object[] {
                new Func<List<Holding>>(() => new List<Holding> { new Holding { Symbol = Symbols.SPY, Quantity = 1 }}),
                new Func<List<Order>>(() => new List<Order>())},
            new object[] {
                new Func<List<Holding>>(() => new List<Holding>()),
                new Func<List<Order>>(() => new List<Order>() { new LimitOrder(new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), 1, 1, DateTime.UtcNow)})},
            new object[] {
                new Func<List<Holding>>(() => new List<Holding> { new Holding { Symbol = new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), Quantity = 1 }}),
                new Func<List<Order>>(() => new List<Order>() { new LimitOrder(new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), 1, 1, DateTime.UtcNow)})},
            new object[] {
                new Func<List<Holding>>(() => new List<Holding> { new Holding { Symbol = new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), Quantity = 1 },
                    new Holding { Symbol = Symbols.SPY, Quantity = 1 }}),
                new Func<List<Order>>(() => new List<Order>() { new LimitOrder(new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), 1, 1, DateTime.UtcNow)})},
            new object[] {
                new Func<List<Holding>>(() => new List<Holding> { new Holding { Symbol = new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), Quantity = 1 },
                    new Holding { Symbol = Symbols.SPY, Quantity = 1 }}),
                new Func<List<Order>>(() => new List<Order>() { new LimitOrder(new Symbol(
                    SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), 1, 1, DateTime.UtcNow),
                    new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow)})}
        };

        private class ExistingHoldingAndOrdersDataClass
        {
            public static IEnumerable GetExistingHoldingsAndOrdersTestCaseData
            {
                get
                {
                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>()),
                        new Func<List<Order>>(() => new List<Order>()), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.SPY, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY_C_192_Feb19_2016, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.SPY, Quantity = 1 },
                            new Holding { Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow),
                            new LimitOrder(Symbols.SPY_C_192_Feb19_2016, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 },
                            new Holding { Symbol = Symbols.SPY, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY_C_192_Feb19_2016, 1, 1, DateTime.UtcNow),
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow),
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.EURUSD, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.EURUSD, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.BTCUSD, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.BTCUSD, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbols.Fut_SPY_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.Fut_SPY_Feb19_2016, 1, 1, DateTime.UtcNow)
                        }), true);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = Symbol.Create("XYZ", SecurityType.Base, Market.USA), Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder("XYZ", 1, 1, DateTime.UtcNow)
                        }), false);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Symbol = new Symbol(SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(new Symbol(SecurityIdentifier.GenerateBase(typeof(Bitcoin), "BTC", Market.USA, false), "BTC"), 1, 1, DateTime.UtcNow)
                        }), false);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => { throw new Exception(); }),
                        new Func<List<Order>>(() => new List<Order>()), false);

                    yield return new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>()),
                        new Func<List<Order>>(() => { throw new Exception(); }), false);
                }
            }
        }

        internal class TestAlgorithm : QCAlgorithm
        {
            private readonly Action _beforePostInitializeAction;

            public DataManager DataManager { get; set; }

            public TestAlgorithm(Action beforePostInitializeAction = null)
            {
                _beforePostInitializeAction = beforePostInitializeAction;
                DataManager = new DataManagerStub(this, new MockDataFeed(), liveMode: true);
                SubscriptionManager.SetDataManager(DataManager);
            }

            public override void Initialize() { }

            public override void PostInitialize()
            {
                _beforePostInitializeAction?.Invoke();
                base.PostInitialize();
            }
        }

        internal static LiveNodePacket GetJob()
        {
            var job = new LiveNodePacket
            {
                UserId = 1,
                ProjectId = 1,
                DeployId = "1",
                Brokerage = "PaperBrokerage",
                DataQueueHandler = "none"
            };
            // Increasing RAM limit, else the tests fail. This is happening in master, when running all the tests together, locally (not travis).
            job.Controls.RamAllocation = 1024 * 1024 * 1024;
            return job;
        }

        private class NonDequeingTestResultsHandler : TestResultHandler
        {
            private readonly AlgorithmNodePacket _job = new BacktestNodePacket();
            public readonly ConcurrentQueue<Packet> PersistentMessages = new ConcurrentQueue<Packet>();

            public override void DebugMessage(string message)
            {
                PersistentMessages.Enqueue(new DebugPacket(_job.ProjectId, _job.AlgorithmId, _job.CompileId, message));
            }
        }

        private class TestableBrokerageSetupHandler : BrokerageSetupHandler
        {
            public void PublicGetOpenOrders(IAlgorithm algorithm, IResultHandler resultHandler, ITransactionHandler transactionHandler, IBrokerage brokerage)
            {
                GetOpenOrders(algorithm, resultHandler, transactionHandler, brokerage);
            }

            public bool TestLoadExistingHoldingsAndOrders(IBrokerage brokerage, IAlgorithm algorithm, SetupHandlerParameters parameters)
            {
                return LoadExistingHoldingsAndOrders(brokerage, algorithm, parameters);
            }
        }
    }

    internal class TestBrokerageFactory : BrokerageFactory
    {
        public TestBrokerageFactory() : base(typeof(TestBrokerage))
        {
        }

        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>();
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.TestBrokerageModel();
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm) => new TestBrokerage();
        public override void Dispose() { }
    }

    /// <summary>
    /// Public so that mock can access it
    /// </summary>
    public class TestBrokerage : Brokerage
    {
        public override bool IsConnected { get; } = true;
        public int GetCashBalanceCallCount;

        public TestBrokerage() : base("Test")
        {
        }

        public TestBrokerage(string name) : base(name)
        {
        }

        public override List<Order> GetOpenOrders()
        {
            const decimal delta = 1m;
            const decimal price = 1.2345m;
            const int quantity = 100;
            const decimal pricePlusDelta = price + delta;
            const decimal priceMinusDelta = price - delta;
            var tz = TimeZones.NewYork;

            var time = new DateTime(2016, 2, 4, 16, 0, 0).ConvertToUtc(tz);
            var marketOrderWithPrice = new MarketOrder(Symbols.SPY, quantity, time)
            {
                Price = price
            };

            return new List<Order>
            {
                marketOrderWithPrice,
                new LimitOrder(Symbols.SPY, -quantity, pricePlusDelta, time),
                new StopMarketOrder(Symbols.SPY, quantity, pricePlusDelta, time),
                new StopLimitOrder(Symbols.SPY, quantity, pricePlusDelta, priceMinusDelta, time)
            };
        }

        public override List<CashAmount> GetCashBalance()
        {
            GetCashBalanceCallCount++;

            return new List<CashAmount> { new CashAmount(10, Currencies.USD) };
        }

        #region UnusedMethods

        public override List<Holding> GetAccountHoldings()
        {
            throw new NotImplementedException();
        }

        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
