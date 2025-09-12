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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Brokerages.Paper;
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
            using var backtestingBrokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, backtestingBrokerage, new BacktestingResultHandler());

            // Creates the order
            var security = _algorithm.Securities[Ticker];
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 600, 0, 0, 0, DateTime.Now, "");

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
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a market order
            var security = _algorithm.Securities[Ticker];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.UtcNow.AddDays(-1), security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, 0, DateTime.UtcNow, "");
            var orderRequest2 = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1000, 0, 0, 0, DateTime.UtcNow, "");
            orderRequest.SetOrderId(1);
            orderRequest2.SetOrderId(2);

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 1))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 2))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest2));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderEventCalls = 0;
            brokerage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                orderEventCalls++;
                var orderEvent = orderEvents[0];
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
            using var brokerage = new BacktestingBrokerage(_algorithm);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a market order
            var security = _algorithm.Securities[Ticker];
            security.FillModel = new TestPartialFilledModel();

            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.UtcNow.AddDays(-1), security.Symbol, price, price, price));
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 2000, 0, 0, 9, DateTime.UtcNow, "");
            var orderRequest2 = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -2000, 0, 0, 9, DateTime.UtcNow, "");
            orderRequest.SetOrderId(1);
            orderRequest2.SetOrderId(2);

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 1))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 2))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest2));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var orderEventCalls = 0;
            brokerage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                orderEventCalls++;
                var orderEvent = orderEvents[0];
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

        [Test]
        public void ProcessesOrdersInLivePaperTrading()
        {
            //Initializes the transaction handler
            var transactionHandler = new BacktestingTransactionHandler();
            using var brokerage = new PaperBrokerage(_algorithm, null);
            _algorithm.SetLiveMode(true);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());

            // Creates a market order
            var security = _algorithm.Securities[Ticker];
            var price = 1.12m;
            security.SetMarketPrice(new Tick(DateTime.UtcNow.AddDays(-1), security.Symbol, price, price, price));
            var reference = new DateTime(2025, 07, 03, 10, 0, 0);
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, 0, reference, "");
            var orderRequest2 = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1000, 0, 0, 0, reference.AddSeconds(1), "");
            orderRequest.SetOrderId(1);
            orderRequest2.SetOrderId(2);

            // Mock the the order processor
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 1))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest));
            orderProcessorMock.Setup(m => m.GetOrderTicket(It.Is<int>(i => i == 2))).Returns(new OrderTicket(_algorithm.Transactions, orderRequest2));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            var allOrderEvents = new List<OrderEvent>();
            using var eventsReceived = new AutoResetEvent(false);

            brokerage.OrdersStatusChanged += (sender, orderEvents) =>
            {
                var orderEvent = orderEvents[0];
                lock (allOrderEvents)
                {
                    allOrderEvents.Add(orderEvent);
                    if (allOrderEvents.Count == 4)
                    {
                        eventsReceived.Set();
                    }
                }

                // Let's place another order before this one is filled
                if (orderEvent.OrderId == 1 && orderEvent.Status == OrderStatus.Submitted)
                {
                    var ticket2 = transactionHandler.Process(orderRequest2);
                }

                Log.Debug($"{orderEvent}");
            };

            var ticket = transactionHandler.Process(orderRequest);

            if (!eventsReceived.WaitOne(10000))
            {
                Assert.Fail($"Did not receive all order events, received {allOrderEvents.Count} order events: {string.Join(", ", allOrderEvents)}");
            }

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
            Assert.AreEqual(4, allOrderEvents.Count);

            var firstOrderSubmittedEvent = allOrderEvents.FirstOrDefault(x => x.OrderId == 1 && x.Status == OrderStatus.Submitted);
            Assert.IsNotNull(firstOrderSubmittedEvent);
            var firstOrderFilledEvent = allOrderEvents.FirstOrDefault(x => x.OrderId == 1 && x.Status == OrderStatus.Filled);
            Assert.IsNotNull(firstOrderFilledEvent);

            var secondOrderSubmittedEvent = allOrderEvents.FirstOrDefault(x => x.OrderId == 2 && x.Status == OrderStatus.Submitted);
            Assert.IsNotNull(secondOrderSubmittedEvent);
            var secondOrderFilledEvent = allOrderEvents.FirstOrDefault(x => x.OrderId == 2 && x.Status == OrderStatus.Filled);
            Assert.IsNotNull(secondOrderFilledEvent);

            transactionHandler.Exit();
        }

        [Test]
        public void ProcessesOrdersConcurrentlyInLivePaperTrading()
        {
            _algorithm.SetLiveMode(true);
            using var brokerage = new PaperBrokerage(_algorithm, null);

            const int expectedOrdersCount = 20;
            using var finishedEvent = new ManualResetEventSlim(false);
            var transactionHandler = new TestablePaperBrokerageTransactionHandler(expectedOrdersCount, finishedEvent);
            transactionHandler.Initialize(_algorithm, brokerage, new BacktestingResultHandler());
            _algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var security = (Security)_algorithm.AddEquity("SPY");
            _algorithm.SetFinishedWarmingUp();

            // Set up security
            var reference = new DateTime(2025, 07, 03, 10, 0, 0);
            security.SetMarketPrice(new Tick(reference, security.Symbol, 300, 300));

            // Creates the order
            var orderRequests = Enumerable.Range(0, expectedOrdersCount)
                .Select(_ => new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1000, 0, 0, 0, reference, ""))
                .ToList();

            // Act
            for (var i = 0; i < orderRequests.Count; i++)
            {
                var orderRequest = orderRequests[i];
                orderRequest.SetOrderId(i + 1);
                transactionHandler.Process(orderRequest);
            }

            // Wait for all orders to be processed
            Assert.IsTrue(finishedEvent.Wait(10000));
            Assert.Greater(transactionHandler.ProcessingThreadNames.Count, 1);
            CollectionAssert.AreEquivalent(orderRequests.Select(x => x.ToString()), transactionHandler.ProcessedRequests.Select(x => x.ToString()));

            transactionHandler.Exit();
        }

        private class TestablePaperBrokerageTransactionHandler : BacktestingTransactionHandler
        {
            private readonly int _expectedOrdersCount;
            private readonly ManualResetEventSlim _finishedEvent;
            private int _currentOrdersCount;

            public HashSet<string> ProcessingThreadNames = new();

            public ConcurrentBag<OrderRequest> ProcessedRequests = new();

            public TestablePaperBrokerageTransactionHandler(int expectedOrdersCount, ManualResetEventSlim finishedEvent)
            {
                _expectedOrdersCount = expectedOrdersCount;
                _finishedEvent = finishedEvent;
            }

            public override void HandleOrderRequest(OrderRequest request)
            {
                base.HandleOrderRequest(request);

                // Capture the thread name for debugging purposes
                lock (ProcessingThreadNames)
                {
                    ProcessingThreadNames.Add(Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToStringInvariant());
                }

                ProcessedRequests.Add(request);

                if (Interlocked.Increment(ref _currentOrdersCount) >= _expectedOrdersCount)
                {
                    // Signal that we have processed the expected number of orders
                    _finishedEvent.Set();
                }
            }
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
