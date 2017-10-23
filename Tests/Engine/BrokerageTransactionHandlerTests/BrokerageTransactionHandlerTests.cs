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
using Moq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    internal class EmptyHistoryProvider : IHistoryProvider
    {
        public int DataPointCount
        {
            get { return 0; }
        }

        public void Initialize(AlgorithmNodePacket job, IDataProvider dataProvider, IDataCacheProvider dataCacheProvider, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
        {
        }

        public IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            return Enumerable.Empty<Slice>();
        }
    }

    [TestFixture]
    class BrokerageTransactionHandlerTests
    {
        private const string Ticker = "EURUSD";
        private QCAlgorithm _algorithm;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new QCAlgorithm { HistoryProvider = new EmptyHistoryProvider() };
            _algorithm.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            _algorithm.SetCash(100000);
            _algorithm.AddSecurity(SecurityType.Forex, Ticker);
            _algorithm.SetFinishedWarmingUp();
        }

        [Test]
        public void OrderQuantityIsFlooredToNearestMultipleOfLotSizeWhenLongOrderIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
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
        public void OrderQuantityIsCeiledToNearestMultipleOfLotSizeWhenShortOrderIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
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
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
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
        public void LimitOrderPriceIsRounded()
        {
            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
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
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
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
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates a limit order
            var security = _algorithm.Securities[Ticker];
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

            transactionHandler.HandleOrderRequest(cancelRequest);
            Assert.IsTrue(cancelRequest.Response.IsProcessed);
            Assert.IsTrue(cancelRequest.Response.IsSuccess);
            Assert.IsTrue(cancelRequest.Status == OrderRequestStatus.Processed);
            Assert.IsTrue(orderTicket.Status == OrderStatus.Canceled);
        }

        [Test]
        public void RoundOff_Long_Fractional_Orders()
        {
            var algo = new QCAlgorithm();
            algo.SetBrokerageModel(BrokerageName.Default);
            algo.SetCash(100000);

            // Sets the Security

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.GDAX, false, 3.3m, true);

            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
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
            algo.SetBrokerageModel(BrokerageName.Default);
            algo.SetCash(100000);

            // Sets the Security

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.GDAX, false, 3.3m, true);

            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
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
            algo.SetBrokerageModel(BrokerageName.Default);
            algo.SetCash(100000);

            // Sets the Security

            var security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Hour, Market.GDAX, false, 3.3m, true);

            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 0.000000009m, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);

            Assert.AreEqual(0, actual);
        }
    }
}