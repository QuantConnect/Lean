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
using System.Reflection;
using System.Threading;
using Moq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Tests.Engine.Setup;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class BrokerageTransactionHandlerTests
    {
        private const string Ticker = "EURUSD";
        private MethodInfo _handleOptionNotification;
        private TestAlgorithm _algorithm;
        private Symbol _symbol;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new TestAlgorithm { HistoryProvider = new EmptyHistoryProvider() };
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            _algorithm.SetCash(100000);
            _symbol = _algorithm.AddSecurity(SecurityType.Forex, Ticker).Symbol;
            _algorithm.SetFinishedWarmingUp();

            _handleOptionNotification = typeof(BrokerageTransactionHandler).GetMethod("HandleOptionNotification", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(_handleOptionNotification);
        }

        private static SubmitOrderRequest MakeOrderRequest(Security security, OrderType orderType, DateTime date)
        {
            var groupOrderManager = new GroupOrderManager(1, 1, 100, orderType == OrderType.ComboLimit ? 100 : 0);

            return orderType switch
            {
                OrderType.Market => new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1, 0, 0, date, ""),
                OrderType.Limit => new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1, 0, 290, date, ""),
                OrderType.StopMarket => new SubmitOrderRequest(OrderType.StopMarket, security.Type, security.Symbol, 1, 305, 0, date, ""),
                OrderType.StopLimit => new SubmitOrderRequest(OrderType.StopLimit, security.Type, security.Symbol, 1, 305, 295, date, ""),
                OrderType.MarketOnOpen => new SubmitOrderRequest(OrderType.MarketOnOpen, security.Type, security.Symbol, 1, 0, 0, date, ""),
                OrderType.MarketOnClose => new SubmitOrderRequest(OrderType.MarketOnClose, security.Type, security.Symbol, 1, 0, 0, date, ""),
                OrderType.LimitIfTouched => new SubmitOrderRequest(OrderType.LimitIfTouched, security.Type, security.Symbol, 1, 0, 300, 305, date, ""),
                OrderType.OptionExercise => new SubmitOrderRequest(OrderType.OptionExercise, security.Type, security.Symbol, 1, 0, 0, date, ""),
                OrderType.ComboMarket => new SubmitOrderRequest(OrderType.ComboMarket, security.Type, security.Symbol, 1, 0, 0, date, "", groupOrderManager: groupOrderManager),
                OrderType.ComboLimit => new SubmitOrderRequest(OrderType.ComboLimit, security.Type, security.Symbol, 1, 295, 0, date, "", groupOrderManager: groupOrderManager),
                OrderType.ComboLegLimit => new SubmitOrderRequest(OrderType.ComboLegLimit, security.Type, security.Symbol, 1, 295, 0, date, "", groupOrderManager: groupOrderManager),
                OrderType.TrailingStop => new SubmitOrderRequest(OrderType.TrailingStop, security.Type, security.Symbol, 1, 305, 0, 305, date, ""),
                _ => throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null)
            };
        }

        [Test]
        public void OrderTagIsSetToTheDefaultOne([Values] OrderType orderType)
        {
            var reference = new DateTime(2024, 01, 25, 10, 0, 0);

            // Initialize the algorithm
            var algorithm = new TestAlgorithm { HistoryProvider = new EmptyHistoryProvider() };
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetCash(100000);
            var security = (Security)algorithm.AddEquity("SPY");
            algorithm.SetFinishedWarmingUp();

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(algorithm);
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());

            // Set up security
            security.SetMarketPrice(new Tick(reference, security.Symbol, 300, 300));
            if (orderType == OrderType.OptionExercise)
            {
                algorithm.AddOption(security.Symbol);
                security = algorithm.AddOptionContract(Symbol.CreateOption(security.Symbol, Market.USA, OptionStyle.American, OptionRight.Call,
                    300, reference.AddDays(4).Date));
                security.SetMarketPrice(new Tick(reference, security.Symbol, 10, 10));
            }

            // Creates the order
            var orderRequest = MakeOrderRequest(security, orderType, reference);

            // Mock the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(algorithm.Transactions, orderRequest));
            algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(OrderStatus.Submitted, orderTicket.Status);

            // Assert the order tag is set to the default one
            var order = transactionHandler.GetOpenOrders().Single();
            Assert.AreEqual(orderType, order.Type);
            Assert.AreEqual(order.GetDefaultTag(), order.Tag);
        }

        [Test]
        public void OrderQuantityIsFlooredToNearestMultipleOfLotSizeWhenLongOrderIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1600, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            // 1600 after round off becomes 1000
            Assert.AreEqual(1000, orderTicket.Quantity);
        }

        [Test]
        public void BrokerageOrderIdChanged()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var testBrokerage = new TestBroker(_algorithm, true);
            transactionHandler.Initialize(_algorithm, testBrokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1600, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);

            var originalBrokerageOrderId = transactionHandler.GetOrderById(orderTicket.OrderId).BrokerId;
            var orderIdChanged = new BrokerageOrderIdChangedEvent { OrderId = orderTicket.OrderId, BrokerId = new List<string> { "asd" } };
            testBrokerage.OnOrderIdChangedEventPublic(orderIdChanged);

            var newBrokerageOrderId = transactionHandler.GetOrderById(orderTicket.OrderId).BrokerId;
            Assert.AreNotEqual(originalBrokerageOrderId, newBrokerageOrderId);
            Assert.AreEqual(1, newBrokerageOrderId.Count);
            Assert.AreEqual("asd", newBrokerageOrderId[0]);
        }

        [Test]
        public void OrderQuantityIsCeiledToNearestMultipleOfLotSizeWhenShortOrderIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1600, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            // -1600 after round off becomes -1000
            Assert.AreEqual(-1000, orderTicket.Quantity);
        }

        [Test]
        public void OrderIsNotPlacedWhenOrderIsLowerThanLotSize()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 600, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // 600 after round off becomes 0 -> order is not placed
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsError);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Invalid);
        }

        [Test]
        public void GetOpenOrderTicketsDoesWorksCorrectly()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            var newTicket = transactionHandler.GetOpenOrderTickets(ticket => ticket.Symbol == security.Symbol).Single();
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            Assert.AreEqual(newTicket, orderTicket);

            transactionHandler.HandleOrderRequest(orderRequest);

            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsFalse(orderRequest.Response.IsError);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);

            var processedTicket = transactionHandler.GetOpenOrderTickets(ticket => ticket.Symbol == security.Symbol).ToList();
            Assert.IsNotEmpty(processedTicket);
        }

        [Test]
        public void GetOpenOrderTicketsDoesNotReturnInvalidatedOrder()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 600, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            var newTicket = transactionHandler.GetOpenOrderTickets(ticket => ticket.Symbol == security.Symbol).Single();
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            Assert.AreEqual(newTicket, orderTicket);

            transactionHandler.HandleOrderRequest(orderRequest);

            // 600 after round off becomes 0 -> order is not placed
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsError);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Invalid);

            var processedTicket = transactionHandler.GetOpenOrderTickets(ticket => ticket.Symbol == security.Symbol).ToList();
            Assert.IsEmpty(processedTicket);
        }

        [TestCase("NDX", "1.14", "1.15")]
        [TestCase("NDX", "1.16", "1.15")]
        [TestCase("NDX", "4.14", "4.10")]
        [TestCase("NDX", "4.16", "4.20")]
        [TestCase("VIX", "1.14", "1.14")]
        [TestCase("VIX", "1.16", "1.16")]
        [TestCase("VIX", "4.14", "4.15")]
        [TestCase("VIX", "4.18", "4.20")]
        [TestCase("VIXW", "1.14", "1.14")]
        [TestCase("VIXW", "1.16", "1.16")]
        [TestCase("VIXW", "4.14", "4.14")]
        [TestCase("VIXW", "4.16", "4.16")]
        public void DynamicIndexOptionPriceRoundeding(string indexOption, string orderPriceStr, string expectedPriceStr)
        {
            var orderPrice = decimal.Parse(orderPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
            var expectedPrice = decimal.Parse(expectedPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);

            //Initializes the transaction handler
            _algorithm.SetBrokerageModel(new DefaultBrokerageModel());
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.AddIndexOption(indexOption);
            var price = 1.12129m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 10, orderPrice, orderPrice, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            Assert.AreEqual(10, orderTicket.Quantity);
            // 1.16 after round becomes 1.10
            Assert.AreEqual(expectedPrice, orderTicket.Get(OrderField.LimitPrice));
        }

        [Test]
        public void LimitOrderPriceIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12129m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1600, 1.12121212m, 1.12121212m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            // 1600 after round off becomes 1000
            Assert.AreEqual(1000, orderTicket.Quantity);
            // 1.12121212 after round becomes 1.12121
            Assert.AreEqual(1.12121m, orderTicket.Get(OrderField.LimitPrice));
        }

        [Test]
        public void StopMarketOrderPriceIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12129m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.StopMarket, security.Type, security.Symbol, 1600, 1.12131212m, 1.12131212m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            // 1600 after round off becomes 1000
            Assert.AreEqual(1000, orderTicket.Quantity);
            // 1.12131212 after round becomes 1.12131
            Assert.AreEqual(1.12131m, orderTicket.Get(OrderField.StopPrice));
        }

        [Test]
        public void TrailingStopOrderPriceIsRounded([Values] bool trailingAsPercentage)
        {
            //Initialize the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Create the order
            _algorithm.SetBrokerageModel(new DefaultBrokerageModel());
            var security = _algorithm.AddEquity("SPY");
            security.PriceVariationModel = new EquityPriceVariationModel();
            var price = 330.12129m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.TrailingStop, security.Type, security.Symbol, 100, stopPrice: 300.12121212m, 0, 0,
                trailingAmount: 20.12121212m, trailingAsPercentage, DateTime.Now, "");

            // Mock the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.New);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            // 300.12121212 after round becomes 300.12
            Assert.AreEqual(300.12m, orderTicket.Get(OrderField.StopPrice));
            // If trailing amount is not a price, it's not rounded
            Assert.AreEqual(trailingAsPercentage ? 20.12121212m : 20.12, orderTicket.Get(OrderField.TrailingAmount));
        }

        // 331.12121212m after round becomes 331.12m, the smallest price variation is 0.01 - index. For index options it is 0.1
        [TestCase(OrderType.ComboLimit, 300.12121212, 0, 0, 300.12, 300.12)]
        [TestCase(OrderType.ComboLegLimit, 0, 1.12121212, 300.13131313, 1.12, 300.1)]
        [TestCase(OrderType.ComboLegLimit, 0, 1.12121212, 300.15151515, 1.12, 300.2)]
        public void ComboLimitOrderPriceIsRounded(OrderType orderType, decimal groupOrderLimitPrice, decimal leg1LimitPrice, decimal leg2LimitPrice,
            decimal expectedLeg1LimitPrice, decimal expectedLeg2LimitPrice)
        {
            var algorithm = new TestAlgorithm { HistoryProvider = new EmptyHistoryProvider() };
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetBrokerageModel(BrokerageName.Default);
            algorithm.SetCash(1000000);
            var symbol1 = algorithm.AddIndex("SPX").Symbol;
            var symbol2 = Symbol.CreateOption(symbol1, Market.USA, OptionStyle.European, OptionRight.Put, 300m, new DateTime(2024, 05, 16));
            algorithm.AddIndexOptionContract(symbol2);
            algorithm.SetFinishedWarmingUp();

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(algorithm);
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());

            var expectedGroupOrderLimitPrice = 0m;
            if (orderType == OrderType.ComboLimit)
            {
                // legs have the same global limit price
                leg1LimitPrice = groupOrderLimitPrice;
                leg2LimitPrice = groupOrderLimitPrice;
                expectedGroupOrderLimitPrice = expectedLeg2LimitPrice = expectedLeg1LimitPrice;
            }
            else if (orderType == OrderType.ComboLegLimit)
            {
                // Each leg has its own limit price
                groupOrderLimitPrice = 0m;
                expectedGroupOrderLimitPrice = 0m;
            }

            // Creates the orders
            var dateTime = new DateTime(2024, 05, 14, 12, 0, 0);
            var groupOrderManager = new GroupOrderManager(1, 2, 10, groupOrderLimitPrice);

            var security1 = algorithm.Securities[symbol1];
            var price1 = 1.12129m;
            security1.SetMarketPrice(new Tick(dateTime, security1.Symbol, price1, price1, price1));
            var orderRequest1 = new SubmitOrderRequest(orderType, security1.Type, security1.Symbol, 20, leg1LimitPrice, leg1LimitPrice,
                dateTime, "", groupOrderManager: groupOrderManager);

            var security2 = algorithm.Securities[symbol2];
            var price2 = 330.12129m;
            security2.SetMarketPrice(new Tick(dateTime, security2.Symbol, price2, price2, price2));
            var orderRequest2 = new SubmitOrderRequest(orderType, security2.Type, security2.Symbol, 10, leg2LimitPrice, leg2LimitPrice,
                dateTime, "", groupOrderManager: groupOrderManager);

            orderRequest1.SetOrderId(1);
            orderRequest2.SetOrderId(2);
            groupOrderManager.OrderIds.Add(1);
            groupOrderManager.OrderIds.Add(2);

            // Mock the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(1)).Returns(new OrderTicket(algorithm.Transactions, orderRequest1));
            orderProcessorMock.Setup(m => m.GetOrderTicket(2)).Returns(new OrderTicket(algorithm.Transactions, orderRequest2));
            algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket1 = transactionHandler.Process(orderRequest1);
            Assert.AreEqual(OrderStatus.New, orderTicket1.Status);
            transactionHandler.HandleOrderRequest(orderRequest1);

            var orderTicket2 = transactionHandler.Process(orderRequest2);
            Assert.AreEqual(OrderStatus.New, orderTicket2.Status);
            transactionHandler.HandleOrderRequest(orderRequest2);

            // Assert
            Assert.IsTrue(orderRequest1.Response.IsProcessed);
            Assert.IsTrue(orderRequest1.Response.IsSuccess);
            Assert.AreEqual(OrderStatus.Submitted, orderTicket1.Status);
            Assert.AreEqual(expectedLeg1LimitPrice, orderTicket1.Get(OrderField.LimitPrice));

            Assert.IsTrue(orderRequest2.Response.IsProcessed);
            Assert.IsTrue(orderRequest2.Response.IsSuccess);
            Assert.AreEqual(OrderStatus.Submitted, orderTicket2.Status);
            Assert.AreEqual(expectedLeg2LimitPrice, orderTicket2.Get(OrderField.LimitPrice));

            Assert.AreEqual(expectedGroupOrderLimitPrice, groupOrderManager.LimitPrice);
        }

        [Test]
        public void OrderCancellationTransitionsThroughCancelPendingStatus()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);

            // Cancel the order
            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            transactionHandler.Process(cancelRequest);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.IsTrue(cancelRequest.Status == OrderRequestStatus.Processing);
            Assert.IsTrue(orderTicket.Status == OrderStatus.CancelPending);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 1);

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.IsTrue(cancelRequest.Status == OrderRequestStatus.Processed);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Canceled);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);

            // Check CancelPending was sent
            Assert.AreEqual(_algorithm.OrderEvents.Count, 3);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.CancelPending), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Canceled), 1);
        }

        [TestCase(0.9, 1.123456789, 1.12)]
        [TestCase(0.9, 0.987654321, 0.9877)]
        [TestCase(0.9, 0.999999999, 1)]
        [TestCase(0.9, 1, 1)]
        [TestCase(0.9, 1.000000001, 1)]
        [TestCase(1.1, 1.123456789, 1.12)]
        [TestCase(1.1, 0.987654321, 0.9877)]
        [TestCase(1.1, 0.999999999, 1)]
        [TestCase(1.1, 1, 1)]
        [TestCase(1.1, 1.000000001, 1)]
        public void RoundsEquityLimitOrderPricesCorrectly(decimal securityPrice, decimal orderPrice, decimal expected)
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetLiveMode(true);

            var security = algo.AddEquity("YGTY");
            security.SetMarketPrice(new Tick { Value = securityPrice });

            var transactionHandler = new TestBrokerageTransactionHandler();
            var brokerage = new Mock<IBrokerage>();
            transactionHandler.Initialize(algo, brokerage.Object, null);

            var order = new LimitOrder(security.Symbol, 1000, orderPrice, DateTime.UtcNow);
            transactionHandler.RoundOrderPrices(order, security);

            Assert.AreEqual(expected, order.LimitPrice);
        }

        [Test]
        public void RoundOff_Long_Fractional_Orders()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetBrokerageModel(BrokerageName.Default);
            algo.SetCash(100000);

            // Sets the Security

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.Coinbase, false, 1m, true);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(algo);
            transactionHandler.Initialize(algo, brokerage, new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 123.123456789m, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);

            Assert.AreEqual(123.12345678m, actual);
        }

        [Test]
        public void RoundOff_Short_Fractional_Orders()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetBrokerageModel(BrokerageName.Default);
            algo.SetCash(100000);

            // Sets the Security

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.Coinbase, false, 1m, true);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(algo);
            transactionHandler.Initialize(algo, brokerage, new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -123.123456789m, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);

            Assert.AreEqual(-123.12345678m, actual);
        }

        [Test]
        public void RoundOff_LessThanLotSize_Fractional_Orders()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetBrokerageModel(BrokerageName.Default);
            algo.SetCash(100000);

            // Sets the Security

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.Coinbase, false, 1m, true);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(algo);
            transactionHandler.Initialize(algo, brokerage, new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 0.000000009m, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);

            Assert.AreEqual(0, actual);
        }

        [Test]
        public void InvalidUpdateOrderRequestShouldNotInvalidateCanceledOrder()
        {
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new NoSubmitTestBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            _algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);

            brokerage.PublishOrderEvent(new OrderEvent(_algorithm.Transactions.GetOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Submitted });
            Assert.AreEqual(OrderStatus.Submitted, orderTicket.Status);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields() { Quantity = 10000 });
            var updateTicket = transactionHandler.Process(updateRequest);
            transactionHandler.HandleOrderRequest(updateRequest);
            Assert.AreEqual(OrderRequestStatus.Processed, updateRequest.Status);
            Assert.IsTrue(updateRequest.Response.IsSuccess);

            // Canceled!
            brokerage.PublishOrderEvent(new OrderEvent(_algorithm.Transactions.GetOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Canceled });

            Assert.AreEqual(OrderStatus.Canceled, orderTicket.Status);

            // update failed!
            brokerage.PublishOrderEvent(new OrderEvent(_algorithm.Transactions.GetOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Invalid });

            // nothing should change
            Assert.AreEqual(OrderStatus.Canceled, orderTicket.Status);
        }

        [Test]
        public void InvalidUpdateOrderRequestShouldNotInvalidateFilledOrder()
        {
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new NoSubmitTestBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            _algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);

            brokerage.PublishOrderEvent(new OrderEvent(_algorithm.Transactions.GetOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Submitted });
            Assert.AreEqual(OrderStatus.Submitted, orderTicket.Status);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields() { Quantity = 10000 });
            var updateTicket = transactionHandler.Process(updateRequest);
            transactionHandler.HandleOrderRequest(updateRequest);
            Assert.AreEqual(OrderRequestStatus.Processed, updateRequest.Status);
            Assert.IsTrue(updateRequest.Response.IsSuccess);

            // filled!
            brokerage.PublishOrderEvent(new OrderEvent(_algorithm.Transactions.GetOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Filled, FillQuantity = 1000, FillPrice = price });

            Assert.AreEqual(1000, security.Holdings.Quantity);
            Assert.AreEqual(price, security.Holdings.AveragePrice);
            Assert.AreEqual(OrderStatus.Filled, orderTicket.Status);

            // update failed!
            brokerage.PublishOrderEvent(new OrderEvent(_algorithm.Transactions.GetOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Invalid });

            // nothing should change
            Assert.AreEqual(1000, security.Holdings.Quantity);
            Assert.AreEqual(price, security.Holdings.AveragePrice);
            Assert.AreEqual(OrderStatus.Filled, orderTicket.Status);
        }

        [Test]
        public void InvalidUpdateOrderRequestShouldNotInvalidateOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());
            _algorithm.SetBrokerageModel(new TestBrokerageModel());
            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields());
            transactionHandler.Process(updateRequest);
            Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(updateRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            transactionHandler.HandleOrderRequest(updateRequest);
            Assert.IsFalse(updateRequest.Response.ErrorMessage.IsNullOrEmpty());
            Assert.IsTrue(updateRequest.Response.IsError);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 2);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Message.Contains("unable to update order")), 1);
            Assert.IsTrue(_algorithm.OrderEvents.TrueForAll(orderEvent => orderEvent.Status == OrderStatus.Submitted));
        }

        [Test]
        public void UpdateOrderRequestShouldWork()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields());
            transactionHandler.Process(updateRequest);
            Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(updateRequest.Response.IsSuccess);
            Assert.AreEqual(OrderStatus.Submitted, orderTicket.Status);

            transactionHandler.HandleOrderRequest(updateRequest);
            Assert.IsTrue(updateRequest.Response.IsSuccess);
            Assert.AreEqual(OrderStatus.UpdateSubmitted, orderTicket.Status);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 2);
            Assert.IsTrue(_algorithm.OrderEvents[0].Status == OrderStatus.Submitted);
            Assert.IsTrue(_algorithm.OrderEvents[1].Status == OrderStatus.UpdateSubmitted);
        }

        [Test]
        public void UpdatePartiallyFilledOrderRequestShouldWork()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using (var broker = new BacktestingBrokerage(_algorithm))
            {
                transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

                // Creates a limit order
                var security = _algorithm.Securities[_symbol];
                var originalFillModel = security.FillModel;
                security.SetFillModel(new PartialFillModel(_algorithm, 0.5m));
                var price = 1.12m;
                security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
                var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0,
                    1.11m, DateTime.Now, "");

                // Mock the the order processor
                var orderProcessorMock = new Mock<IOrderProcessor>();
                orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>()))
                    .Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
                _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

                // Submit and process a limit order
                var orderTicket = transactionHandler.Process(orderRequest);
                transactionHandler.HandleOrderRequest(orderRequest);
                Assert.IsTrue(orderRequest.Response.IsProcessed);
                Assert.IsTrue(orderRequest.Response.IsSuccess);
                Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

                broker.Scan();
                Assert.AreEqual(orderTicket.Status, OrderStatus.PartiallyFilled);

                var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields());
                transactionHandler.Process(updateRequest);
                Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Processing);
                Assert.IsTrue(updateRequest.Response.IsSuccess);
                Assert.AreEqual(OrderStatus.PartiallyFilled, orderTicket.Status);

                transactionHandler.HandleOrderRequest(updateRequest);
                Assert.IsTrue(updateRequest.Response.IsSuccess);
                Assert.AreEqual(OrderStatus.UpdateSubmitted, orderTicket.Status);

                Assert.AreEqual(_algorithm.OrderEvents.Count, 3);
                Assert.IsTrue(_algorithm.OrderEvents[0].Status == OrderStatus.Submitted);
                Assert.IsTrue(_algorithm.OrderEvents[1].Status == OrderStatus.PartiallyFilled);
                Assert.IsTrue(_algorithm.OrderEvents[2].Status == OrderStatus.UpdateSubmitted);
                security.SetFillModel(originalFillModel);
            }
        }

        [Test]
        public void UpdateOrderRequestShouldFailForInvalidOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            _algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var orderTicket = _algorithm.MarketOrder(security.Symbol, 1);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Invalid);

            orderTicket.UpdateQuantity(10);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Invalid);
        }

        [Test]
        public void CancelOrderRequestShouldFailForInvalidOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            _algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var orderTicket = _algorithm.MarketOrder(security.Symbol, 1);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Invalid);

            orderTicket.Cancel();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Invalid);
        }

        [Test]
        public void UpdateOrderRequestShouldFailForFilledOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields() { Quantity = 100 });
            transactionHandler.Process(updateRequest);
            Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(updateRequest.Response.IsError);
            Assert.AreEqual(updateRequest.Response.ErrorCode, OrderResponseErrorCode.InvalidOrderStatus);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 2);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void TagUpdateOrderRequestShouldSucceedForFilledOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields() { Tag = "New tag" });
            transactionHandler.Process(updateRequest);
            Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(updateRequest.Response.IsSuccess);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 2);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void UpdateOrderRequestShouldFailForNewOrderStatus()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new NoSubmitTestBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderTicket.OrderId, new UpdateOrderFields());
            transactionHandler.Process(updateRequest);
            Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(updateRequest.Response.IsError);
            Assert.AreEqual(updateRequest.Response.ErrorCode, OrderResponseErrorCode.InvalidNewOrderStatus);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 0);
        }

        [Test]
        public void CancelOrderRequestShouldFailForNewOrderStatus()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new NoSubmitTestBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            transactionHandler.Process(cancelRequest);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(cancelRequest.Response.IsError);
            Assert.AreEqual(cancelRequest.Response.ErrorCode, OrderResponseErrorCode.InvalidNewOrderStatus);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 0);
        }

        [Test]
        public void CancelOrderTicket()
        {
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new NoSubmitTestBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            _algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            // will fail because order status is new
            var response = orderTicket.Cancel();
            Assert.IsTrue(response.IsProcessed);
            Assert.IsTrue(response.IsError);
            Assert.AreEqual(response.ErrorCode, OrderResponseErrorCode.InvalidNewOrderStatus);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 0);

            var submitted = new OrderEvent(_algorithm.Transactions.GetOpenOrders().Single(), _algorithm.UtcTime, OrderFee.Zero)
            { Status = OrderStatus.Submitted };
            brokerage.PublishOrderEvent(submitted);

            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var response2 = orderTicket.Cancel();
            Assert.IsTrue(response2.IsProcessed);
            Assert.IsFalse(response2.IsError);
            Assert.AreEqual(orderTicket.Status, OrderStatus.CancelPending);
        }

        [Test]
        public void CancelOrderRequestShouldFailForFilledOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            transactionHandler.Process(cancelRequest);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(cancelRequest.Response.IsError);
            Assert.AreEqual(cancelRequest.Response.ErrorCode, OrderResponseErrorCode.InvalidOrderStatus);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 2);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void SyncFailedCancelOrderRequestShouldUpdateOrderStatusCorrectly()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new TestBroker(_algorithm, false);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            transactionHandler.Process(cancelRequest);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 1);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.CancelPending);

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsTrue(cancelRequest.Response.IsError);
            Assert.IsTrue(cancelRequest.Response.ErrorMessage.Contains("Brokerage failed to cancel order"));

            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            Assert.AreEqual(_algorithm.OrderEvents.Count, 2);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.CancelPending), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
        }

        [Test]
        public void AsyncFailedCancelOrderRequestShouldUpdateOrderStatusCorrectly()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new TestBroker(_algorithm, true);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            transactionHandler.Process(cancelRequest);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 1);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.CancelPending);

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.CancelPending);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Processed);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsFalse(cancelRequest.Response.IsError);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 1);

            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 3);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.CancelPending), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void AsyncFailedCancelOrderRequestShouldUpdateOrderStatusCorrectlyWithIntermediateUpdate()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new TestBroker(_algorithm, true);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            transactionHandler.Process(cancelRequest);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 1);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.CancelPending);

            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsTrue(cancelRequest.Response.IsError);
            Assert.AreEqual(cancelRequest.Response.ErrorCode, OrderResponseErrorCode.InvalidOrderStatus);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 3);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.CancelPending), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void SyncFailedCancelOrderRequestShouldUpdateOrderStatusCorrectlyWithIntermediateUpdate()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new TestBroker(_algorithm, false);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);

            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            transactionHandler.Process(cancelRequest);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 1);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Processing);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.CancelPending);

            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.AreEqual(transactionHandler.CancelPendingOrdersSize, 0);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);
            Assert.AreEqual(cancelRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsTrue(cancelRequest.Response.IsError);
            Assert.AreEqual(cancelRequest.Response.ErrorCode, OrderResponseErrorCode.InvalidOrderStatus);

            Assert.AreEqual(_algorithm.OrderEvents.Count, 3);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.CancelPending), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Submitted), 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Filled), 1);
        }

        [Test]
        public void UpdateOrderRequestShouldFailForInvalidOrderId()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            var updateRequest = new UpdateOrderRequest(DateTime.Now, -10, new UpdateOrderFields());
            transactionHandler.Process(updateRequest);
            Assert.AreEqual(updateRequest.Status, OrderRequestStatus.Error);
            Assert.IsTrue(updateRequest.Response.IsError);

            Assert.IsTrue(_algorithm.OrderEvents.IsNullOrEmpty());
        }

        [Test]
        public void GetOpenOrdersWorksForSubmittedFilledStatus()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            Assert.AreEqual(transactionHandler.GetOpenOrders().Count, 0);

            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Submitted);
            var openOrders = transactionHandler.GetOpenOrders();
            Assert.AreEqual(openOrders.Count, 1);
            Assert.AreEqual(openOrders[0].Id, orderTicket.OrderId);
            broker.Scan();
            Assert.AreEqual(orderTicket.Status, OrderStatus.Filled);
            Assert.AreEqual(transactionHandler.GetOpenOrders().Count, 0);
        }

        [Test]
        public void GetOpenOrdersWorksForCancelPendingCanceledStatus()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, security.Type, security.Symbol, 1000, 0, 1.11m, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            Assert.AreEqual(transactionHandler.GetOpenOrders().Count, 0);
            // Submit and process a limit order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);
            var openOrders = transactionHandler.GetOpenOrders();
            Assert.AreEqual(openOrders.Count, 1);
            Assert.AreEqual(openOrders[0].Id, orderTicket.OrderId);

            // Cancel the order
            var cancelRequest = new CancelOrderRequest(DateTime.Now, orderTicket.OrderId, "");
            transactionHandler.Process(cancelRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.CancelPending);
            openOrders = transactionHandler.GetOpenOrders();
            Assert.AreEqual(openOrders.Count, 1);
            Assert.AreEqual(openOrders[0].Id, orderTicket.OrderId);

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Canceled);
            Assert.AreEqual(transactionHandler.GetOpenOrders().Count, 0);
        }

        [Test]
        public void ProcessSynchronousEventsShouldPerformCashSyncOnce()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new TestBrokerage();

            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());
            _algorithm.SetLiveMode(true);

            var lastSyncDateBefore = transactionHandler.GetLastSyncDate();

            // Advance current time UTC so cash sync is performed
            transactionHandler.TestCurrentTimeUtc = transactionHandler.TestCurrentTimeUtc.AddDays(2);

            transactionHandler.ProcessSynchronousEvents();
            var lastSyncDateAfter = transactionHandler.GetLastSyncDate();

            Assert.AreNotEqual(lastSyncDateAfter, lastSyncDateBefore);

            transactionHandler.ProcessSynchronousEvents();
            var lastSyncDateAfterAgain = transactionHandler.GetLastSyncDate();
            Assert.AreEqual(lastSyncDateAfter, lastSyncDateAfterAgain);

            Assert.AreEqual(1, brokerage.GetCashBalanceCallCount);
        }

        [Test]
        public void OrderFillShouldTriggerRePerformingCashSync()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new TestBrokerage();

            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());
            _algorithm.SetLiveMode(true);

            var lastSyncDateBefore = transactionHandler.GetLastSyncDate();

            // Advance current time UTC so cash sync is performed
            transactionHandler.TestCurrentTimeUtc = transactionHandler.TestCurrentTimeUtc.AddDays(2);

            // update last fill time
            transactionHandler.TestTimeSinceLastFill = TimeSpan.FromSeconds(15);

            transactionHandler.ProcessSynchronousEvents();
            var lastSyncDateAfter = transactionHandler.GetLastSyncDate();

            // cash sync happened
            Assert.AreNotEqual(lastSyncDateAfter, lastSyncDateBefore);

            var count = 0;
            while (!brokerage.ShouldPerformCashSync(transactionHandler.TestCurrentTimeUtc))
            {
                count++;
                if (count > 40)
                {
                    Assert.Fail("Timeout waiting for ShouldPerformCashSync");
                }
                // delayed task should take ~10 seconds to set the perform cash sync flag up, due to TimeSinceLastFill
                Thread.Sleep(1000);
            }
            transactionHandler.ProcessSynchronousEvents();

            Assert.AreEqual(2, brokerage.GetCashBalanceCallCount);
        }

        [Test]
        public void ProcessSynchronousEventsShouldPerformCashSyncOnlyAtExpectedTime()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new TestBrokerage();

            // This is 2 am New York
            transactionHandler.TestCurrentTimeUtc = new DateTime(1, 1, 1, 7, 0, 0);

            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());
            _algorithm.SetLiveMode(true);

            var lastSyncDateBefore = transactionHandler.GetLastSyncDate();

            // Advance current time UTC
            transactionHandler.TestCurrentTimeUtc = transactionHandler.TestCurrentTimeUtc.AddDays(2);

            transactionHandler.ProcessSynchronousEvents();
            var lastSyncDateAfter = transactionHandler.GetLastSyncDate();

            Assert.AreEqual(lastSyncDateAfter, lastSyncDateBefore);

            Assert.AreEqual(0, brokerage.GetCashBalanceCallCount);
        }

        [Test]
        public void EmptyCashBalanceIsValid()
        {
            var mock = new Mock<TestBrokerage>
            {
                CallBase = true
            };
            var cashBalance = mock.Setup(m => m.GetCashBalance()).Returns(new List<CashAmount>());
            mock.Setup(m => m.IsConnected).Returns(true);
            mock.Setup(m => m.ShouldPerformCashSync(It.IsAny<DateTime>())).Returns(true);

            var brokerage = mock.Object;
            Assert.IsTrue(brokerage.IsConnected);

            var algorithm = new QCAlgorithm();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algorithm.Portfolio));
            algorithm.Securities.SetSecurityService(securityService);
            algorithm.SetLiveMode(true);
            algorithm.SetFinishedWarmingUp();

            var transactionHandler = new TestBrokerageTransactionHandler();
            var resultHandler = new TestResultHandler();
            transactionHandler.Initialize(algorithm, brokerage, resultHandler);

            // Advance current time UTC so cash sync is performed
            transactionHandler.TestCurrentTimeUtc = transactionHandler.TestCurrentTimeUtc.AddDays(2);

            transactionHandler.ProcessSynchronousEvents();

            resultHandler.Exit();

            mock.VerifyAll();
        }

        [Test]
        public void DoesNotLoopEndlesslyIfGetCashBalanceAlwaysThrows()
        {
            // simulate connect failure
            var ib = new Mock<IBrokerage>();
            ib.Setup(m => m.GetCashBalance()).Callback(() => { throw new Exception("Connection error in CashBalance"); });
            ib.Setup(m => m.IsConnected).Returns(false);
            ib.Setup(m => m.ShouldPerformCashSync(It.IsAny<DateTime>())).Returns(true);
            ib.Setup(m => m.PerformCashSync(It.IsAny<IAlgorithm>(), It.IsAny<DateTime>(), It.IsAny<Func<TimeSpan>>()))
                .Returns(
                    () =>
                    {
                        try
                        {
                            ib.Object.GetCashBalance();
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                        return true;
                    });

            var brokerage = ib.Object;
            Assert.IsFalse(brokerage.IsConnected);

            var algorithm = new QCAlgorithm();
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
            var securityService = new SecurityService(algorithm.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDataBase, algorithm, RegisteredSecurityDataTypesProvider.Null, new SecurityCacheProvider(algorithm.Portfolio));
            algorithm.Securities.SetSecurityService(securityService);
            algorithm.SetLiveMode(true);
            algorithm.SetFinishedWarmingUp();

            var transactionHandler = new TestBrokerageTransactionHandler();
            var resultHandler = new TestResultHandler();
            transactionHandler.Initialize(algorithm, brokerage, resultHandler);

            // Advance current time UTC so cash sync is performed
            transactionHandler.TestCurrentTimeUtc = transactionHandler.TestCurrentTimeUtc.AddDays(2);

            try
            {
                while (true)
                {
                    transactionHandler.ProcessSynchronousEvents();

                    Assert.IsFalse(brokerage.IsConnected);

                    Thread.Sleep(1000);
                }
            }
            catch (Exception exception)
            {
                // expect exception from ProcessSynchronousEvents when max attempts reached
                Assert.That(exception.Message.Contains("maximum number of attempts"));
            }

            resultHandler.Exit();
        }

        [Test]
        public void AddOrderWaitsForOrderToBeProcessed()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var security = algorithm.AddSecurity(SecurityType.Equity, "SPY");
            security.SetMarketPrice(new Tick { Value = 150 });
            algorithm.SetFinishedWarmingUp();

            var transactionHandler = new BrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            // lets wait until the transactionHandler starts running
            Thread.Sleep(250);

            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var ticket = algorithm.LimitOrder(security.Symbol, 1, 100);

            var openOrders = algorithm.Transactions.GetOpenOrders();

            transactionHandler.Exit();

            Assert.AreEqual(1, openOrders.Count);
            Assert.IsTrue(ticket.HasOrder);
        }

        [Test]
        public void FillMessageIsAddedToOrderTag()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a market order
            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.UtcNow.AddDays(-1), security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, DateTime.UtcNow, "TestTag");

            // Mock the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            Assert.AreEqual(transactionHandler.GetOpenOrders().Count, 0);
            // Submit and process the market order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);

            brokerage.Scan();
            Assert.IsTrue(orderTicket.Status == OrderStatus.Filled);

            var order = transactionHandler.GetOrderById(orderTicket.OrderId);
            Assert.IsTrue(order.Tag.Contains("TestTag"));
            Assert.IsTrue(order.Tag.Contains("Warning: fill at stale price"));
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void IncrementalOrderId()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(TestIncrementalOrderIdAlgorithm),
                new Dictionary<string, string>(),
                Language.CSharp,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                setupHandler: "TestIncrementalOrderIdSetupHandler");

            Assert.AreEqual(12, TestIncrementalOrderIdAlgorithm.OrderEventIds.Count);
        }

        [Test]
        public void InvalidOrderEventDueToNonShortableAsset()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var broker = new TestBroker(_algorithm, false);
            _algorithm.SetBrokerageModel(new TestShortableBrokerageModel());
            transactionHandler.Initialize(_algorithm, broker, new BacktestingResultHandler());

            var security = _algorithm.Securities[_symbol];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.Now, security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1000, 0, 1.11m, DateTime.UtcNow, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.AreEqual(orderTicket.Status, OrderStatus.Invalid);
            Assert.AreEqual(_algorithm.OrderEvents.Count, 1);
            Assert.AreEqual(_algorithm.OrderEvents.Count(orderEvent => orderEvent.Status == OrderStatus.Invalid), 1);
        }

        // Short Call --> OTM (expired worthless)
        [TestCase(-1, OptionRight.Call, 455, 100, 450, 1, 0, 100, "OTM")]
        // Short Put --> OTM (expired worthless)
        [TestCase(-1, OptionRight.Put, 455, 100, 460, 1, 0, 100, "OTM")]
        // Long Call --> OTM (expired worthless)
        [TestCase(1, OptionRight.Call, 455, 100, 450, 1, 0, 100, "OTM")]
        // Long Put --> OTM (expired worthless)
        [TestCase(1, OptionRight.Put, 455, 100, 460, 1, 0, 100, "OTM")]
        // Short Call --> ITM (assigned)
        [TestCase(-1, OptionRight.Call, 450, 100, 455, 2, 0, 0, "Automatic Assignment")]
        // Short Put --> ITM (assigned)
        [TestCase(-1, OptionRight.Put, 455, 100, 450, 2, 0, 200, "Automatic Assignment")]
        // Long Call --> ITM (auto-exercised)
        [TestCase(1, OptionRight.Call, 450, 100, 455, 2, 0, 200, "Automatic Exercise")]
        // Long Put --> ITM (auto-exercised)
        [TestCase(1, OptionRight.Put, 455, 100, 450, 2, 0, 0, "Automatic Exercise")]
        public void OptionExpirationEmitsOrderEvents(
            int initialOptionPosition,
            OptionRight optionRight,
            decimal strikePrice,
            int initialUnderlyingPosition,
            decimal underlyingPrice,
            int expectedOrderEvents,
            int expectedOptionPosition,
            int expectedUnderlyingPosition,
            string expectedMessage
            )
        {
            var algorithm = new TestAlgorithm();
            var equity = algorithm.AddEquity("SPY");
            var optionSymbol = Symbol.CreateOption(equity.Symbol, equity.Symbol.ID.Market, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2021, 9, 8));
            var option = algorithm.AddOptionContract(optionSymbol);

            algorithm.Portfolio[equity.Symbol].SetHoldings(underlyingPrice, initialUnderlyingPosition);
            algorithm.Portfolio[option.Symbol].SetHoldings(0.01m, initialOptionPosition);

            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            using var brokerage = new NoSubmitTestBrokerage(algorithm);
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            // 9 PM ET
            transactionHandler.TestCurrentTimeUtc = new DateTime(2021, 9, 9, 1, 0, 0);

            var parameters = new object[] { new OptionNotificationEventArgs(optionSymbol, 0) };
            _handleOptionNotification.Invoke(transactionHandler, parameters);

            var tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);

            var ticket = tickets.First();
            Assert.IsTrue(ticket.HasOrder);

            Assert.AreEqual(expectedOrderEvents, ticket.OrderEvents.Count);
            Assert.AreEqual(1, ticket.OrderEvents.Count(x => x.Message.Contains(expectedMessage, StringComparison.InvariantCulture)));

            Assert.AreEqual(expectedUnderlyingPosition, algorithm.Portfolio[equity.Symbol].Quantity);
            Assert.AreEqual(expectedOptionPosition, algorithm.Portfolio[optionSymbol].Quantity);

            // let's push the same event again
            _handleOptionNotification.Invoke(transactionHandler, parameters);
            transactionHandler.Exit();

            // we should not see any new orders or events come through
            tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);
        }

        // Long Call --> ITM (exercised early - full)
        [TestCase(1, OptionRight.Call, 450, 100, 455, 2, 0, 200, "Automatic Exercise")]
        // Long Put --> ITM (exercised early - full)
        [TestCase(1, OptionRight.Put, 455, 100, 450, 2, 0, 0, "Automatic Exercise")]
        // Long Call --> ITM (exercised early - partial)
        [TestCase(3, OptionRight.Call, 450, 100, 455, 2, 1, 300, "Automatic Exercise")]
        // Long Put --> ITM (exercised early - partial)
        [TestCase(3, OptionRight.Put, 455, 300, 450, 2, 1, 100, "Automatic Exercise")]
        public void EarlyExerciseEmitsOrderEvents(
            int initialOptionPosition,
            OptionRight optionRight,
            decimal strikePrice,
            int initialUnderlyingPosition,
            decimal underlyingPrice,
            int expectedOrderEvents,
            int expectedOptionPosition,
            int expectedUnderlyingPosition,
            string expectedMessage
            )
        {
            var algorithm = new TestAlgorithm();
            var equity = algorithm.AddEquity("SPY");
            var optionSymbol = Symbol.CreateOption(equity.Symbol, equity.Symbol.ID.Market, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2021, 9, 8));
            var option = algorithm.AddOptionContract(optionSymbol);

            algorithm.Portfolio[equity.Symbol].SetHoldings(underlyingPrice, initialUnderlyingPosition);
            algorithm.Portfolio[option.Symbol].SetHoldings(0.01m, initialOptionPosition);

            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            using var brokerage = new NoSubmitTestBrokerage(algorithm);
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            // 10 AM ET
            transactionHandler.TestCurrentTimeUtc = new DateTime(2021, 9, 8, 14, 0, 0);

            // Creates an exercise order
            var exerciseQuantity = initialOptionPosition - expectedOptionPosition;
            var orderRequest = new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, -exerciseQuantity, 0, 0, transactionHandler.TestCurrentTimeUtc, "");

            // Submit and process the exercise order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            var parameters = new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) };
            _handleOptionNotification.Invoke(transactionHandler, parameters);

            var tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);

            var ticket = tickets.First();
            Assert.IsTrue(ticket.HasOrder);

            Assert.AreEqual(expectedOrderEvents, ticket.OrderEvents.Count);
            Assert.AreEqual(1, ticket.OrderEvents.Count(x => x.Message.Contains(expectedMessage, StringComparison.InvariantCulture)));

            Assert.AreEqual(expectedUnderlyingPosition, algorithm.Portfolio[equity.Symbol].Quantity);
            Assert.AreEqual(expectedOptionPosition, algorithm.Portfolio[optionSymbol].Quantity);

            // let's push the same event again
            _handleOptionNotification.Invoke(transactionHandler, parameters);
            transactionHandler.Exit();

            // we should not see any new orders or events come through
            tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);
        }

        // Long Call --> ITM (exercised early - full)
        [TestCase(1, OptionRight.Call, 450, 100, 455, 1, 0, "Automatic Exercise")]
        // Long Put --> ITM (exercised early - full)
        [TestCase(1, OptionRight.Put, 455, 100, 450, 1, 0, "Automatic Exercise")]
        // Long Call --> ITM (exercised early - partial)
        [TestCase(3, OptionRight.Call, 450, 100, 455, 1, 1, "Automatic Exercise")]
        // Long Put --> ITM (exercised early - partial)
        [TestCase(3, OptionRight.Put, 455, 300, 450, 1, 1, "Automatic Exercise")]
        public void EarlyExerciseDoesNotEmitsOrderEvents(
            int initialOptionPosition,
            OptionRight optionRight,
            decimal strikePrice,
            int initialUnderlyingPosition,
            decimal underlyingPrice,
            int expectedOrderEvents,
            int expectedOptionPosition,
            string expectedMessage
            )
        {
            var algorithm = new TestAlgorithm();
            var equity = algorithm.AddEquity("SPY");
            var optionSymbol = Symbol.CreateOption(equity.Symbol, equity.Symbol.ID.Market, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2021, 9, 8));
            var option = algorithm.AddOptionContract(optionSymbol);

            algorithm.Portfolio[equity.Symbol].SetHoldings(underlyingPrice, initialUnderlyingPosition);
            algorithm.Portfolio[option.Symbol].SetHoldings(0.01m, initialOptionPosition);

            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            using var brokerage = new NoSubmitTestBrokerage(algorithm);
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            // 10 AM ET
            transactionHandler.TestCurrentTimeUtc = new DateTime(2021, 9, 8, 14, 0, 0);

            // Creates an exercise order
            var exerciseQuantity = initialOptionPosition - expectedOptionPosition;
            var orderRequest = new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, -exerciseQuantity, 0, 0, transactionHandler.TestCurrentTimeUtc, "");

            // Submit and process the exercise order
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(orderTicket.Status, OrderStatus.New);

            // Fill the exercise order
            brokerage.PublishOrderEvent(new OrderEvent(orderTicket.OrderId, option.Symbol, transactionHandler.TestCurrentTimeUtc,
                OrderStatus.Filled, OrderDirection.Sell, 0, orderRequest.Quantity, OrderFee.Zero));
            Assert.IsTrue(orderTicket.Status.IsClosed());

            var tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);

            _handleOptionNotification.Invoke(transactionHandler, new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) });

            // assert nothing happens!
            tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);
        }

        // Short Call --> ITM (assigned early - full)
        [TestCase(-1, OptionRight.Call, 450, 100, 455, 2, 0, 0, "Automatic Assignment")]
        // Short Put --> ITM (assigned early - full)
        [TestCase(-1, OptionRight.Put, 455, 100, 450, 2, 0, 200, "Automatic Assignment")]
        // Short Call --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Call, 450, 300, 455, 2, -1, 100, "Automatic Assignment")]
        // Short Put --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Put, 455, 100, 450, 2, -1, 300, "Automatic Assignment")]
        public void EarlyAssignmentEmitsOrderEvents(
            int initialOptionPosition,
            OptionRight optionRight,
            decimal strikePrice,
            int initialUnderlyingPosition,
            decimal underlyingPrice,
            int expectedOrderEvents,
            int expectedOptionPosition,
            int expectedUnderlyingPosition,
            string expectedMessage
            )
        {
            var algorithm = new TestAlgorithm();
            var equity = algorithm.AddEquity("SPY");
            var optionSymbol = Symbol.CreateOption(equity.Symbol, equity.Symbol.ID.Market, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2021, 9, 8));
            var option = algorithm.AddOptionContract(optionSymbol);

            algorithm.Portfolio[equity.Symbol].SetHoldings(underlyingPrice, initialUnderlyingPosition);
            algorithm.Portfolio[option.Symbol].SetHoldings(0.01m, initialOptionPosition);

            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            using var brokerage = new NoSubmitTestBrokerage(algorithm);
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            // 10 AM ET
            transactionHandler.TestCurrentTimeUtc = new DateTime(2021, 9, 8, 14, 0, 0);

            var parameters = new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) };
            _handleOptionNotification.Invoke(transactionHandler, parameters);

            var tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);

            var ticket = tickets.First();
            Assert.IsTrue(ticket.HasOrder);

            Assert.AreEqual(expectedOrderEvents, ticket.OrderEvents.Count);
            Assert.AreEqual(1, ticket.OrderEvents.Count(x => x.Message.Contains(expectedMessage, StringComparison.InvariantCulture)));

            Assert.AreEqual(expectedUnderlyingPosition, algorithm.Portfolio[equity.Symbol].Quantity);
            Assert.AreEqual(expectedOptionPosition, algorithm.Portfolio[optionSymbol].Quantity);

            // let's push the same event again
            _handleOptionNotification.Invoke(transactionHandler, parameters);
            transactionHandler.Exit();

            // we should not see any new orders or events come through
            tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(1, tickets.Count);
        }

        // Short Call --> ITM (assigned early - full)
        [TestCase(-2, OptionRight.Call, 450, 100, 455, 2, 0, 0, "Automatic Assignment")]
        // Short Put --> ITM (assigned early - full)
        [TestCase(-2, OptionRight.Put, 455, 100, 450, 2, 0, 200, "Automatic Assignment")]
        // Short Call --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Call, 450, 300, 455, 2, -1, 200, "Automatic Assignment")]
        // Short Put --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Put, 455, 100, 450, 2, -1, 200, "Automatic Assignment")]
        public void EarlyAssignmentEmitsOrderEventsEvenIfOldBuyOrderPresent(
            int initialOptionPosition,
            OptionRight optionRight,
            decimal strikePrice,
            int initialUnderlyingPosition,
            decimal underlyingPrice,
            int expectedOrderEvents,
            int expectedOptionPosition,
            int expectedUnderlyingPosition,
            string expectedMessage
            )
        {
            var algorithm = new TestAlgorithm();
            var equity = algorithm.AddEquity("SPY");
            var optionSymbol = Symbol.CreateOption(equity.Symbol, equity.Symbol.ID.Market, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2021, 9, 8));
            var option = algorithm.AddOptionContract(optionSymbol);

            algorithm.Portfolio[equity.Symbol].SetHoldings(underlyingPrice, initialUnderlyingPosition);
            algorithm.Portfolio[option.Symbol].SetHoldings(0.01m, initialOptionPosition);

            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            using var brokerage = new NoSubmitTestBrokerage(algorithm);
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            // 10 AM ET
            transactionHandler.TestCurrentTimeUtc = new DateTime(2021, 9, 8, 14, 0, 0);

            // Creates a market order
            var orderTime = transactionHandler.TestCurrentTimeUtc.AddMinutes(-10);
            var orderRequest = new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, 1, 0, 0, orderTime, "");
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);

            Assert.AreEqual(1,  algorithm.Transactions.GetOrderTickets().Count());

            // Fill the order, 1 second later, but ~10 minutes away form current time
            brokerage.PublishOrderEvent(new OrderEvent(orderTicket.OrderId, option.Symbol, orderTime.AddSeconds(1),
                OrderStatus.Filled, OrderDirection.Buy, 10, orderRequest.Quantity, OrderFee.Zero));

            Assert.IsTrue(orderTicket.Status.IsClosed());
            Assert.AreEqual(1,  algorithm.Transactions.GetOrderTickets().Count());

            var parameters = new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) };
            _handleOptionNotification.Invoke(transactionHandler, parameters);

            var tickets = algorithm.Transactions.GetOrderTickets().ToList();
            Assert.AreEqual(2, tickets.Count);

            var ticket = tickets[0];
            Assert.IsTrue(ticket.HasOrder);

            Assert.AreEqual(expectedOrderEvents, ticket.OrderEvents.Count);
            Assert.AreEqual(1, ticket.OrderEvents.Count(x => x.Message.Contains(expectedMessage, StringComparison.InvariantCulture)));

            Assert.AreEqual(expectedUnderlyingPosition, algorithm.Portfolio[equity.Symbol].Quantity);
            Assert.AreEqual(expectedOptionPosition, algorithm.Portfolio[optionSymbol].Quantity);
        }

        // Short Call --> ITM (assigned early - full)
        [TestCase(-2, OptionRight.Call, 450, 100, 455, 0, OrderDirection.Buy)]
        // Short Put --> ITM (assigned early - full)
        [TestCase(-2, OptionRight.Put, 455, 100, 450, 0, OrderDirection.Buy)]
        // Short Call --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Call, 450, 300, 455, -1, OrderDirection.Buy)]
        // Short Put --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Put, 455, 100, 450, -1, OrderDirection.Buy)]

        // Short Call --> ITM (assigned early - full)
        [TestCase(-2, OptionRight.Call, 450, 100, 455, 0, OrderDirection.Sell)]
        // Short Put --> ITM (assigned early - full)
        [TestCase(-2, OptionRight.Put, 455, 100, 450, 0, OrderDirection.Sell)]
        // Short Call --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Call, 450, 300, 455, -1, OrderDirection.Sell)]
        // Short Put --> ITM (assigned early - partial)
        [TestCase(-3, OptionRight.Put, 455, 100, 450, -1, OrderDirection.Sell)]
        public void EarlyAssignmentDoesNotEmitsOrderEventsInLive(
            int initialOptionPosition,
            OptionRight optionRight,
            decimal strikePrice,
            int initialUnderlyingPosition,
            decimal underlyingPrice,
            int expectedOptionPosition,
            OrderDirection orderDirection
            )
        {
            var algorithm = new TestAlgorithm();
            var equity = algorithm.AddEquity("SPY");
            var optionSymbol = Symbol.CreateOption(equity.Symbol, equity.Symbol.ID.Market, OptionStyle.American, optionRight, strikePrice,
                new DateTime(2021, 9, 8));
            var option = algorithm.AddOptionContract(optionSymbol);

            algorithm.Portfolio[equity.Symbol].SetHoldings(underlyingPrice, initialUnderlyingPosition);
            algorithm.Portfolio[option.Symbol].SetHoldings(0.01m, initialOptionPosition);

            equity.SetMarketPrice(new Tick { Value = underlyingPrice });

            using var brokerage = new NoSubmitTestBrokerage(algorithm);
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algorithm, brokerage, new BacktestingResultHandler());
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            // 10 AM ET
            transactionHandler.TestCurrentTimeUtc = new DateTime(2021, 9, 8, 14, 0, 0);

            // Creates a market order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, orderDirection == OrderDirection.Buy ? 1 : -1, 0, 0, transactionHandler.TestCurrentTimeUtc, "");
            var orderTicket = transactionHandler.Process(orderRequest);
            transactionHandler.HandleOrderRequest(orderRequest);

            Assert.AreEqual(1,  algorithm.Transactions.GetOrderTickets().Count());

            _handleOptionNotification.Invoke(transactionHandler, new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) });

            // we expect no difference because there is an open market order!
            Assert.AreEqual(1, algorithm.Transactions.GetOrderTickets().Count());

            // Fill the order
            brokerage.PublishOrderEvent(new OrderEvent(orderTicket.OrderId, option.Symbol, transactionHandler.TestCurrentTimeUtc,
                OrderStatus.Filled, orderDirection, 10, orderRequest.Quantity, OrderFee.Zero));
            Assert.IsTrue(orderTicket.Status.IsClosed());

            _handleOptionNotification.Invoke(transactionHandler, new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) });

            // we expect no difference because there is a closed market order!
            Assert.AreEqual(1, algorithm.Transactions.GetOrderTickets().Count());

            // Timeout the order effect
            transactionHandler.TestCurrentTimeUtc = transactionHandler.TestCurrentTimeUtc.AddMinutes(1);

            _handleOptionNotification.Invoke(transactionHandler, new object[] { new OptionNotificationEventArgs(optionSymbol, expectedOptionPosition) });

            // we expect difference because market order is old!
            Assert.AreEqual(2, algorithm.Transactions.GetOrderTickets().Count());
        }

        private static TestCaseData[] PriceAdjustmentModeTestCases => Enum.GetValues(typeof(DataNormalizationMode))
            .Cast<DataNormalizationMode>()
            .SelectMany(x => new[] { new TestCaseData(x, false), new TestCaseData(x, true) })
            .ToArray();

        [TestCaseSource(nameof(PriceAdjustmentModeTestCases))]
        public void OrderPriceAdjustmentModeIsSetAfterPlacingOrder(DataNormalizationMode dataNormalizationMode, bool liveMode)
        {
            _algorithm.SetLiveMode(liveMode);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Add the security
            var security = _algorithm.AddSecurity(SecurityType.Forex, "CADUSD", dataNormalizationMode: dataNormalizationMode);
            var securityNormalizationMode = _algorithm.SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(security.Symbol)[0]
                .DataNormalizationMode;

            Assert.AreEqual(dataNormalizationMode, securityNormalizationMode);

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1600, 0, 0, DateTime.Now, "");

            // Mock the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            // Act
            var orderTicket = transactionHandler.Process(orderRequest);
            Assert.AreEqual(OrderStatus.New, orderTicket.Status);
            transactionHandler.HandleOrderRequest(orderRequest);

            // Assert
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Submitted);

            var expectedNormalizationMode = liveMode ? DataNormalizationMode.Raw : dataNormalizationMode;
            Assert.AreEqual(expectedNormalizationMode, transactionHandler.GetOrderById(orderTicket.OrderId).PriceAdjustmentMode);
        }

        [TestCaseSource(nameof(PriceAdjustmentModeTestCases))]
        public void OrderPriceAdjustmentModeIsSetWhenAddingOpenOrder(DataNormalizationMode dataNormalizationMode, bool liveMode)
        {
            _algorithm.SetLiveMode(liveMode);

            // The engine might fetch brokerage open orders before even initializing the transaction handler,
            // so let's not initialize it here to simulate that scenario
            var transactionHandler = new TestBrokerageTransactionHandler();

            // Add the security
            var security = _algorithm.AddSecurity(SecurityType.Forex, "CADUSD", dataNormalizationMode: dataNormalizationMode);
            var securityNormalizationMode = _algorithm.SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(security.Symbol)[0]
                .DataNormalizationMode;

            Assert.AreEqual(dataNormalizationMode, securityNormalizationMode);

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1600, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);

            // Act
            transactionHandler.AddOpenOrder(order, _algorithm);

            // Assert
            Assert.Greater(order.Id, 0);

            var expectedNormalizationMode = liveMode ? DataNormalizationMode.Raw : dataNormalizationMode;
            Assert.AreEqual(expectedNormalizationMode, transactionHandler.GetOrderById(order.Id).PriceAdjustmentMode);
        }

        private static TestCaseData[] BrokerageSideOrdersTestCases => new[]
        {
            new TestCaseData(OrderType.Limit, false),
            new TestCaseData(OrderType.StopMarket, false),
            new TestCaseData(OrderType.StopLimit, false),
            new TestCaseData(OrderType.MarketOnOpen, false),
            new TestCaseData(OrderType.MarketOnClose, false),
            new TestCaseData(OrderType.LimitIfTouched, false),
            new TestCaseData(OrderType.ComboMarket, false),
            new TestCaseData(OrderType.ComboLimit, false),
            new TestCaseData(OrderType.ComboLegLimit, false),
            new TestCaseData(OrderType.TrailingStop, false),
            // Only market orders are supported for this test
            new TestCaseData(OrderType.Market, true),
        };

        private static Order GetOrder(OrderType type, Symbol symbol)
        {
            switch (type)
            {
                case OrderType.Market:
                    return new MarketOrder(symbol, 100, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.Limit:
                    return new LimitOrder(symbol, 100, 100m, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, 100, 100m, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.StopLimit:
                    return new StopLimitOrder(symbol, 100, 100m, 100m, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, 100, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, 100, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.LimitIfTouched:
                    return new LimitIfTouchedOrder(symbol, 100, 100m, 100m, new DateTime(2024, 01, 19, 12, 0, 0));
                case OrderType.ComboMarket:
                    return new ComboMarketOrder(symbol, 100, new DateTime(2024, 01, 19, 12, 0, 0), new GroupOrderManager(1, 1, 10));
                case OrderType.ComboLimit:
                    return new ComboLimitOrder(symbol, 100, 100m, new DateTime(2024, 01, 19, 12, 0, 0), new GroupOrderManager(1, 1, 10, 100));
                case OrderType.ComboLegLimit:
                    return new ComboLegLimitOrder(symbol, 100, 100m, new DateTime(2024, 01, 19, 12, 0, 0), new GroupOrderManager(1, 1, 10));
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, 100, 100m, 100m, false, new DateTime(2024, 01, 19, 12, 0, 0));
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        [TestCaseSource(nameof(BrokerageSideOrdersTestCases))]
        public void NewBrokerageOrdersAreFiltered(OrderType orderType, bool accepted)
        {
            //Initialize the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new TestingBrokerage();
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            _algorithm.SetBrokerageModel(new DefaultBrokerageModel());
            var brokerageMessageHandler = new TestBrokerageMessageHandler();
            _algorithm.SetBrokerageMessageHandler(brokerageMessageHandler);

            var symbol = _algorithm.AddEquity("SPY").Symbol;

            var order = GetOrder(orderType, symbol);
            Assert.AreEqual(orderType, order.Type);
            brokerage.OnNewBrokerageOrder(new NewBrokerageOrderNotificationEventArgs(order));
            Assert.AreEqual(accepted, brokerageMessageHandler.LastHandleOrderResult);
            Assert.AreEqual(accepted ? 1 : 0, transactionHandler.OrdersCount);
        }

        [Test]
        public void UnrequestedSecuritiesAreAddedForNewBrokerageSideOrders()
        {
            //Initialize the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            using var brokerage = new TestingBrokerage();
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            _algorithm.SetBrokerageModel(new DefaultBrokerageModel());
            var brokerageMessageHandler = new TestBrokerageMessageHandler();
            _algorithm.SetBrokerageMessageHandler(brokerageMessageHandler);

            var symbol = Symbols.SPY;
            Assert.IsFalse(_algorithm.Securities.ContainsKey(symbol));

            var order = GetOrder(OrderType.Market, symbol);
            brokerage.OnNewBrokerageOrder(new NewBrokerageOrderNotificationEventArgs(order));
            Assert.IsTrue(brokerageMessageHandler.LastHandleOrderResult);
            Assert.AreEqual(1, transactionHandler.OrdersCount);

            Assert.IsTrue(_algorithm.Securities.TryGetValue(symbol, out var security));
            Assert.AreEqual(symbol, security.Symbol);
        }

        internal class TestIncrementalOrderIdAlgorithm : OrderTicketDemoAlgorithm
        {
            public static readonly Dictionary<int, int> OrderEventIds = new Dictionary<int, int>();

            public override void OnOrderEvent(OrderEvent orderEvent)
            {
                if (!OrderEventIds.ContainsKey(orderEvent.OrderId))
                {
                    OrderEventIds[orderEvent.OrderId] = orderEvent.Id;
                    if (orderEvent.Id != 1)
                    {
                        throw new Exception("Expected first order event to have id 1");
                    }
                }
                else
                {
                    var previous = OrderEventIds[orderEvent.OrderId];
                    if (orderEvent.Id != (previous + 1))
                    {
                        throw new Exception("Expected incremental order event ids");
                    }

                    OrderEventIds[orderEvent.OrderId] = orderEvent.Id;
                }
            }
        }

        internal class TestIncrementalOrderIdSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                return Algorithm = new TestIncrementalOrderIdAlgorithm();
            }
        }

        internal class EmptyHistoryProvider : HistoryProviderBase
        {
            public override int DataPointCount => 0;

            public override void Initialize(HistoryProviderInitializeParameters parameters)
            {
            }

            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                return Enumerable.Empty<Slice>();
            }
        }

        internal class TestBrokerageModel : DefaultBrokerageModel
        {
            public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
            {
                message = new BrokerageMessageEvent(0, 0, "");
                return false;
            }
        }

        internal class TestAlgorithm : QCAlgorithm
        {
            public List<OrderEvent> OrderEvents = new List<OrderEvent>();
            public TestAlgorithm()
            {
                SubscriptionManager.SetDataManager(new DataManagerStub(this));
                SetFinishedWarmingUp();
            }
            public override void OnOrderEvent(OrderEvent orderEvent)
            {
                OrderEvents.Add(orderEvent);
            }
        }

        // Implemented through an underlying BactestingBrokerage instead of directly inheriting from it for easy implementation
        // and for tests that require simulating using a live brokerage, not derived from BacktestingBrokerage.
        internal class NoSubmitTestBrokerage : Brokerage
        {
            private BacktestingBrokerage _underlyingBrokerage;

            public override bool IsConnected => _underlyingBrokerage.IsConnected;

            public NoSubmitTestBrokerage(IAlgorithm algorithm) : base("NoSubmitTestBrokerage")
            {
                _underlyingBrokerage = new BacktestingBrokerage(algorithm);
            }
            public override bool PlaceOrder(Order order)
            {
                return true;
            }
            public override bool UpdateOrder(Order order)
            {
                return true;
            }
            public void PublishOrderEvent(OrderEvent orderEvent)
            {
                OnOrderEvent(orderEvent);
            }

            public override bool CancelOrder(Order order)
            {
                return _underlyingBrokerage.CancelOrder(order);
            }

            public override void Connect()
            {
                _underlyingBrokerage.Connect();
            }

            public override void Disconnect()
            {
                _underlyingBrokerage.Disconnect();
            }

            public override List<Order> GetOpenOrders()
            {
                return _underlyingBrokerage.GetOpenOrders();
            }

            public override List<Holding> GetAccountHoldings()
            {
                return _underlyingBrokerage.GetAccountHoldings();
            }

            public override List<CashAmount> GetCashBalance()
            {
                return _underlyingBrokerage.GetCashBalance();
            }
        }

        internal class TestBroker : BacktestingBrokerage
        {
            private readonly bool _cancelOrderResult;
            public TestBroker(IAlgorithm algorithm, bool cancelOrderResult) : base(algorithm)
            {
                _cancelOrderResult = cancelOrderResult;
            }
            public override bool CancelOrder(Order order)
            {
                return _cancelOrderResult;
            }
            public void OnOrderIdChangedEventPublic(BrokerageOrderIdChangedEvent e)
            {
                base.OnOrderIdChangedEvent(e);
            }
        }

        public class TestBrokerageTransactionHandler : BrokerageTransactionHandler
        {
            private IBrokerageCashSynchronizer _brokerage;

            public int CancelPendingOrdersSize => _cancelPendingOrders.GetCancelPendingOrdersSize;

            public TimeSpan TestTimeSinceLastFill = TimeSpan.FromDays(1);
            public DateTime TestCurrentTimeUtc = new DateTime(13, 1, 13, 13, 13, 13);

            // modifying current time so cash sync is triggered
            protected override DateTime CurrentTimeUtc => TestCurrentTimeUtc;

            protected override TimeSpan TimeSinceLastFill => TestTimeSinceLastFill;

            public override void Initialize(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler resultHandler)
            {
                _brokerage = brokerage;

                base.Initialize(algorithm, brokerage, resultHandler);
            }

            public DateTime GetLastSyncDate()
            {
                return _brokerage.LastSyncDateTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            }

            protected override void InitializeTransactionThread()
            {
                // nop
            }

            public new void RoundOrderPrices(Order order, Security security)
            {
                base.RoundOrderPrices(order, security);
            }
        }

        private class TestNonShortableProvider : IShortableProvider
        {
            public Dictionary<Symbol, long> AllShortableSymbols(DateTime localTime)
            {
                return new Dictionary<Symbol, long>();
            }

            public decimal FeeRate(Symbol symbol, DateTime localTime)
            {
                return 0;
            }

            public decimal RebateRate(Symbol symbol, DateTime localTime)
            {
                return 0;
            }

            public long? ShortableQuantity(Symbol symbol, DateTime localTime)
            {
                return 0;
            }
        }

        private class TestShortableBrokerageModel : DefaultBrokerageModel
        {
            public override IShortableProvider GetShortableProvider(Security security)
            {
                return new TestNonShortableProvider();
            }
        }

        private class TestBrokerageMessageHandler : IBrokerageMessageHandler
        {
            public bool LastHandleOrderResult { get; private set; }

            public void HandleMessage(BrokerageMessageEvent messageEvent)
            {
            }

            public bool HandleOrder(NewBrokerageOrderNotificationEventArgs eventArgs)
            {
                // For testing purposes, only market orders are handled
                return LastHandleOrderResult = eventArgs.Order.Type == OrderType.Market;

            }
        }

        private class TestingBrokerage : TestBrokerage
        {
            public void OnNewBrokerageOrder(NewBrokerageOrderNotificationEventArgs e)
            {
                OnNewBrokerageOrderNotification(e);
            }
        }
    }
}
