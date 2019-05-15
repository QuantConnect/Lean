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
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
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
    }
}
