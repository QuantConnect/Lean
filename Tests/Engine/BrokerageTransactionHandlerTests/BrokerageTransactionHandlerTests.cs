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
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;


namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    [TestFixture]
    class BrokerageTransactionHandlerTests
    {
        private const string Ticker = "EURUSD";
        private QCAlgorithm _algorithm;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new QCAlgorithm();
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
    }
}
