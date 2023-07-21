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

using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fills;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using Fasterflect;
using QuantConnect.Lean.Engine.TransactionHandlers;
using System;
using QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Tests.Engine;
using System.Linq;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class DefaultBrokerageModelTests
    {
        private readonly DefaultBrokerageModel _defaultBrokerageModel = new DefaultBrokerageModel();

        [Test]
        public void CanSubmitOrder_WhenMarketOnOpenOrderForFutures()
        {
            var order = GetMarketOnOpenOrder();
            var future = TestsHelpers.GetSecurity(securityType: SecurityType.Future, symbol: Futures.Indices.SP500EMini, market: Market.CME);
            var futureOption = TestsHelpers.GetSecurity(securityType: SecurityType.FutureOption, symbol: Futures.Indices.SP500EMini, market: Market.CME);
            Assert.IsFalse(_defaultBrokerageModel.CanSubmitOrder(future, order, out _));
            Assert.IsFalse(_defaultBrokerageModel.CanSubmitOrder(futureOption, order, out _));
        }

        [TestCase(SecurityType.Base)]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Index)]
        [TestCase(SecurityType.IndexOption)]
        public void CanSubmitOrder_WhenMarketOnOpenOrderForOtherSecurityTypes(SecurityType securityType)
        {
            var order = GetMarketOnOpenOrder();
            var security = TestsHelpers.GetSecurity(securityType: securityType, market: Market.USA);
            Assert.IsTrue(_defaultBrokerageModel.CanSubmitOrder(security, order, out _));
        }

        [TestCase(SecurityType.Base, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.Equity, nameof(EquityFillModel))]
        [TestCase(SecurityType.Option, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.Forex, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.Cfd, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.Crypto, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.Index, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.IndexOption, nameof(ImmediateFillModel))]
        [TestCase(SecurityType.Future, nameof(FutureFillModel))]
        [TestCase(SecurityType.FutureOption, nameof(FutureOptionFillModel))]
        public void GetsCorrectFillModel(SecurityType securityType, string expectedFillModel)
        {
            var security = TestsHelpers.GetSecurity(securityType: securityType, market: Market.USA);
            var fillModel = _defaultBrokerageModel.GetFillModel(security);
            Assert.AreEqual(expectedFillModel, fillModel.GetType().Name);
        }

        [Test]
        public void ApplySplitWorksAsExpected()
        {
            var orderTypes = new List<OrderType>()
            {
                OrderType.Limit,
                OrderType.StopLimit,
                OrderType.LimitIfTouched,
                OrderType.TrailingStop
            };

            var algorithm = new BrokerageTransactionHandlerTests.TestAlgorithm
            {
                HistoryProvider = new BrokerageTransactionHandlerTests.EmptyHistoryProvider()
            };
            var transactionHandler = new BacktestingTransactionHandler();
            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), new TestResultHandler(Console.WriteLine));

            algorithm.Transactions.SetOrderProcessor(transactionHandler);
            algorithm.AddEquity("IBM");
            var tickets = new List<OrderTicket>();
            foreach (var type in orderTypes)
            {
                SubmitOrderRequest orderRequest = null;
                switch (type)
                {
                    case OrderType.Limit:
                        orderRequest = new SubmitOrderRequest(OrderType.Limit, SecurityType.Equity, Symbols.IBM, 100, 0, limitPrice: 8, 0,
                            DateTime.UtcNow, "");
                        break;
                    case OrderType.StopLimit:
                        orderRequest = new SubmitOrderRequest(OrderType.StopLimit, SecurityType.Equity, Symbols.IBM, 100, stopPrice: 10, 0, 0,
                            DateTime.UtcNow, "");
                        break;
                    case OrderType.LimitIfTouched:
                        orderRequest = new SubmitOrderRequest(OrderType.LimitIfTouched, SecurityType.Equity, Symbols.IBM, 100, 0, limitPrice: 14,
                            triggerPrice: 12, DateTime.UtcNow, "");
                        break;
                    case OrderType.TrailingStop:
                        orderRequest = new SubmitOrderRequest(OrderType.TrailingStop, SecurityType.Equity, Symbols.IBM, 100, stopPrice: 10, 0, 0,
                            trailingAmount: 0.5m, trailingAsPercentage: false, DateTime.UtcNow, "");
                        break;
                }
                algorithm.Transactions.AddOrder(orderRequest);
                var ticket = new OrderTicket(algorithm.Transactions, orderRequest);
                tickets.Add(ticket);
            }

            var split = new Split(Symbols.IBM, DateTime.UtcNow, 1, 0.5m, SplitType.SplitOccurred);
            _defaultBrokerageModel.ApplySplit(tickets, split);
            transactionHandler.ProcessSynchronousEvents();
            foreach (var order in algorithm.Transactions.GetOrders())
            {
                Assert.AreEqual(200, order.Quantity);
                var orderType = order.Type;
                switch (orderType)
                {
                    case OrderType.Limit:
                        Assert.AreEqual(4, order.GetPropertyValue("LimitPrice"));
                        break;
                    case OrderType.StopLimit:
                        Assert.AreEqual(5, order.GetPropertyValue("StopPrice"));
                        break;
                    case OrderType.LimitIfTouched:
                        Assert.AreEqual(6, order.GetPropertyValue("TriggerPrice"));
                        Assert.AreEqual(7, order.GetPropertyValue("LimitPrice"));
                        break;
                    case OrderType.TrailingStop:
                        Assert.AreEqual(5, order.GetPropertyValue("StopPrice"));
                        Assert.AreEqual(0.25m, order.GetPropertyValue("TrailingAmount"));
                        break;
                }
            }
        }


        [Test]
        public void AppliesSplitOnlyWhenTrailingStopOrderTrailingAmountIsNotPercentage([Values] bool trailingAsPercentage)
        {
            var algorithm = new BrokerageTransactionHandlerTests.TestAlgorithm
            {
                HistoryProvider = new BrokerageTransactionHandlerTests.EmptyHistoryProvider()
            };
            var transactionHandler = new BacktestingTransactionHandler();
            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), new TestResultHandler(Console.WriteLine));

            algorithm.Transactions.SetOrderProcessor(transactionHandler);
            algorithm.AddEquity("IBM");

            var tickets = new List<OrderTicket>();
            var orderTime = new DateTime(2023, 07, 21, 12, 0, 0);
            var orderRequest = new SubmitOrderRequest(OrderType.TrailingStop, SecurityType.Equity, Symbols.IBM, 100, stopPrice: 10, 0, 0,
                trailingAmount: 0.1m, trailingAsPercentage, orderTime, "");
            algorithm.Transactions.AddOrder(orderRequest);
            var ticket = new OrderTicket(algorithm.Transactions, orderRequest);
            tickets.Add(ticket);

            var split = new Split(Symbols.IBM, orderTime, 1, 0.5m, SplitType.SplitOccurred);
            _defaultBrokerageModel.ApplySplit(tickets, split);
            transactionHandler.ProcessSynchronousEvents();

            var order = algorithm.Transactions.GetOrders().Single();

            Assert.AreEqual(5, order.GetPropertyValue("StopPrice", Flags.Instance | Flags.Public));
            Assert.AreEqual(trailingAsPercentage ? 0.1m : 0.05m, order.GetPropertyValue("TrailingAmount"));
        }


        private static Order GetMarketOnOpenOrder()
        {
            var order = new Mock<Order>();
            order.Setup(o => o.Type).Returns(OrderType.MarketOnOpen);
            return order.Object;
        }
    }
}
