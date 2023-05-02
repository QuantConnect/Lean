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

        [Test]
        public void OrderQuantityIsFlooredToNearestMultipleOfLotSizeWhenLongOrderIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            var orderPrice = decimal.Parse(orderPriceStr, System.Globalization.NumberStyles.Any);
            var expectedPrice = decimal.Parse(expectedPriceStr, System.Globalization.NumberStyles.Any);

            //Initializes the transaction handler
            _algorithm.SetBrokerageModel(new DefaultBrokerageModel());
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
        public void OrderCancellationTransitionsThroughCancelPendingStatus()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.GDAX, false, 1m, true);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

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

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.GDAX, false, 1m, true);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

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

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.GDAX, false, 1m, true);

            //Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 0.000000009m, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);

            Assert.AreEqual(0, actual);
        }

        [Test]
        public void InvalidUpdateOrderRequestShouldNotInvalidateOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());
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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
        public void UpdateOrderRequestShouldFailForFilledOrder()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            var broker = new BacktestingBrokerage(_algorithm);
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
            var broker = new BacktestingBrokerage(_algorithm);
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
            var broker = new BacktestingBrokerage(_algorithm);
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
            var broker = new TestBroker(_algorithm, false);
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
            var broker = new TestBroker(_algorithm, true);
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
            var broker = new TestBroker(_algorithm, true);
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
            var broker = new TestBroker(_algorithm, false);
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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            var broker = new BacktestingBrokerage(_algorithm);
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
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

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
            var brokerage = new TestBrokerage();

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
            var brokerage = new TestBrokerage();

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
            var brokerage = new TestBrokerage();

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
            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), new BacktestingResultHandler());
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
            var brokerage = new BacktestingBrokerage(_algorithm);
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

            Assert.AreEqual(10, TestIncrementalOrderIdAlgorithm.OrderEventIds.Count);
        }

        [Test]
        public void InvalidOrderEventDueToNonShortableAsset()
        {
            // Initializes the transaction handler
            var transactionHandler = new TestBrokerageTransactionHandler();
            var broker = new TestBroker(_algorithm, false);
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
        public void EarlyAssignmentDoesNotEmitsOrderEvents(
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

        internal class NoSubmitTestBrokerage : BacktestingBrokerage
        {
            public NoSubmitTestBrokerage(IAlgorithm algorithm) : base(algorithm)
            {
            }
            public override bool PlaceOrder(Order order)
            {
                return true;
            }
            public void PublishOrderEvent(OrderEvent orderEvent)
            {
                OnOrderEvent(orderEvent);
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
            public long? ShortableQuantity(Symbol symbol, DateTime localTime)
            {
                return 0;
            }
        }

        private class TestShortableBrokerageModel : DefaultBrokerageModel
        {
            public TestShortableBrokerageModel()
            {
                ShortableProvider = new TestNonShortableProvider();
            }
        }
    }
}
