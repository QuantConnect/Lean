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
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    [TestFixture]
    public class BacktestingTransactionHandlerTests
    {
        private const string Ticker = "EURUSD";
        private BrokerageTransactionHandlerTests.TestAlgorithm _algorithm;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new BrokerageTransactionHandlerTests.TestAlgorithm
            {
                HistoryProvider = new BrokerageTransactionHandlerTests.EmptyHistoryProvider()
            };
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            _algorithm.SetCash(100000);
            _algorithm.AddSecurity(SecurityType.Forex, Ticker);
            _algorithm.SetFinishedWarmingUp();
            TestPartialFilledModel.FilledOrders = new Dictionary<int, Order>();
        }

        [Test]
        public void InvalidOrderRequestWontSetTicketAsProcessed()
        {
            //Initializes the transaction handler
            var transactionHandler = new BacktestingTransactionHandler();
            transactionHandler.Initialize(_algorithm, new BacktestingBrokerage(_algorithm), new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 600, 0, 0, DateTime.Now, "");

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.IsAny<int>())).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var ticket = transactionHandler.AddOrder(orderRequest);

            var ticket2 = transactionHandler.AddOrder(orderRequest);

            // 600 after round off becomes 0 -> order is not placed
            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsError);
            Assert.IsTrue(orderRequest.Response.ErrorMessage
                .Contains("Cannot process submit request because order with id {0} already exists"));
        }

        [Test]
        public void SendingNewOrderFromOnOrderEvent()
        {
            //Initializes the transaction handler
            var transactionHandler = new BacktestingTransactionHandler();
            var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a market order
            var security = _algorithm.Securities[Ticker];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.UtcNow.AddDays(-1), security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, DateTime.UtcNow, "");
            var orderRequest2 = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1000, 0, 0, DateTime.UtcNow, "");
            orderRequest.SetOrderId(1);
            orderRequest2.SetOrderId(2);

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 1))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 2))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest2));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderEventCalls = 0;
            brokerage.OrderStatusChanged += (sender, orderEvent) =>
            {
                orderEventCalls++;
                switch (orderEventCalls)
                {
                    case 1:
                        Assert.AreEqual(1, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Submitted, orderEvent.Status);

                        // we send a new order request
                        var ticket2 = transactionHandler.Process(orderRequest2);
                        break;
                    case 2:
                        Assert.AreEqual(2, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Submitted, orderEvent.Status);
                        break;
                    case 3:
                        Assert.AreEqual(1, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Filled, orderEvent.Status);
                        break;
                    case 4:
                        Assert.AreEqual(2, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Filled, orderEvent.Status);
                        break;
                }
                Log.Trace($"{orderEvent}");
            };

            var ticket = transactionHandler.Process(orderRequest);

            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(OrderRequestStatus.Processed, orderRequest.Status);
            Assert.IsTrue(orderRequest2.Response.IsProcessed);
            Assert.IsTrue(orderRequest2.Response.IsSuccess);
            Assert.AreEqual(OrderRequestStatus.Processed, orderRequest2.Status);

            var order1 = transactionHandler.GetOrderById(1);
            Assert.AreEqual(OrderStatus.Filled, order1.Status);
            var order2 = transactionHandler.GetOrderById(2);
            Assert.AreEqual(OrderStatus.Filled, order2.Status);

            // 2 submitted and 2 filled
            Assert.AreEqual(4, orderEventCalls);
        }

        [Test]
        public void SendingNewOrderFromPartiallyFilledOnOrderEvent()
        {
            //Initializes the transaction handler
            var transactionHandler = new BacktestingTransactionHandler();
            var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a market order
            var security = _algorithm.Securities[Ticker];
            security.FillModel = new TestPartialFilledModel();

            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.UtcNow.AddDays(-1), security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, DateTime.UtcNow, "");
            var orderRequest2 = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1000, 0, 0, DateTime.UtcNow, "");
            orderRequest.SetOrderId(1);
            orderRequest2.SetOrderId(2);

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 1))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 2))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest2));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderEventCalls = 0;
            brokerage.OrderStatusChanged += (sender, orderEvent) =>
            {
                orderEventCalls++;
                switch (orderEventCalls)
                {
                    case 1:
                        Assert.AreEqual(1, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Submitted, orderEvent.Status);

                        // we send a new order request
                        var ticket2 = transactionHandler.Process(orderRequest2);
                        break;
                    case 2:
                        Assert.AreEqual(2, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Submitted, orderEvent.Status);
                        break;
                    case 3:
                        Assert.AreEqual(1, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.PartiallyFilled, orderEvent.Status);
                        break;
                    case 4:
                        Assert.AreEqual(2, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.PartiallyFilled, orderEvent.Status);
                        break;
                    case 5:
                        Assert.AreEqual(1, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Filled, orderEvent.Status);
                        break;
                    case 6:
                        Assert.AreEqual(2, orderEvent.OrderId);
                        Assert.AreEqual(OrderStatus.Filled, orderEvent.Status);
                        break;
                }
                Log.Trace($"{orderEvent}");
            };

            var ticket = transactionHandler.Process(orderRequest);

            Assert.IsTrue(orderRequest.Response.IsProcessed);
            Assert.IsTrue(orderRequest.Response.IsSuccess);
            Assert.AreEqual(OrderRequestStatus.Processed, orderRequest.Status);
            Assert.IsTrue(orderRequest2.Response.IsProcessed);
            Assert.IsTrue(orderRequest2.Response.IsSuccess);
            Assert.AreEqual(OrderRequestStatus.Processed, orderRequest2.Status);

            var order1 = transactionHandler.GetOrderById(1);
            Assert.AreEqual(OrderStatus.Filled, order1.Status);
            var order2 = transactionHandler.GetOrderById(2);
            Assert.AreEqual(OrderStatus.Filled, order2.Status);

            // 2 submitted and 2 PartiallyFilled and 2 Filled
            Assert.AreEqual(6, orderEventCalls);
        }

        internal class TestPartialFilledModel : IFillModel
        {
            public static Dictionary<int, Order> FilledOrders;

            public Fill Fill(FillModelParameters parameters)
            {
                var order = parameters.Order;
                var status = OrderStatus.PartiallyFilled;
                if (FilledOrders.ContainsKey(order.Id))
                {
                    status = OrderStatus.Filled;
                }
                FilledOrders[order.Id] = order;
                return new Fill(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                {
                    FillPrice = parameters.Security.Price,
                    FillQuantity = order.Quantity / 2,
                    Status = status
                });
            }
        }
    }
}
