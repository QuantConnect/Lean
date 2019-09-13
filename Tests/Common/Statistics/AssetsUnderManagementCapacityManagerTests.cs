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

using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Linq;
using Moq;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Tests.Engine;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class AssetsUnderManagementCapacityManagerTests
    {
        [TestCase(Resolution.Tick)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Minute)]
        public void ComputeCapacityForOneSymbolInOneDay_HighResolution(Resolution resolution)
        {
            var algorithm = GetAlgorithm(resolution);
            var portfolio = algorithm.Portfolio;
            var subscriptionDataConfigProvider = algorithm.SubscriptionManager.SubscriptionDataConfigService;
            var orderEventProvider = new MockOrderEventProvider();

            var security = portfolio.Securities[Symbols.SPY];

            var manager = new AssetsUnderManagementCapacityManager(
                portfolio,
                subscriptionDataConfigProvider,
                orderEventProvider
            );

            // 50% of SPY. Volume of 50mi.
            var orderEvent = CreateOrderEvent(algorithm, Symbols.SPY, .5m, 50);
            orderEventProvider.OnNewOrderEvent(orderEvent);

            var config = subscriptionDataConfigProvider.GetSubscriptionDataConfigs(orderEvent.Symbol).First();
            Assert.AreEqual(1, config.Consolidators.Count);

            // Push data to the consolidator
            var data = security.GetLastData();
            data.Time = data.Time.AddMinutes(5);
            config.Consolidators.First().Update(data);

            const double expected = 50000000 * .025 / .5;
            Assert.AreEqual(expected * GetFactor(resolution), manager.AumCapacity);
            Assert.AreEqual(0, config.Consolidators.Count);
        }

        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void ComputeCapacityForOneSymbolInOneDay(Resolution resolution)
        {
            var algorithm = GetAlgorithm(resolution);
            var orderEventProvider = new MockOrderEventProvider();

            var manager = new AssetsUnderManagementCapacityManager(
                algorithm.Portfolio,
                algorithm.SubscriptionManager.SubscriptionDataConfigService,
                orderEventProvider
            );

            // 50% of SPY. Volume of 50mi.
            var orderEvent = CreateOrderEvent(algorithm, Symbols.SPY, .5m, 50);
            orderEventProvider.OnNewOrderEvent(orderEvent);

            const double expected = 50000000 * .025 / .5;
            Assert.AreEqual(expected * GetFactor(resolution), manager.AumCapacity);
        }

        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void ComputeCapacityForTwoSymbolsInOneDay(Resolution resolution)
        {
            var algorithm = GetAlgorithm(resolution);
            var orderEventProvider = new MockOrderEventProvider();

            var manager = new AssetsUnderManagementCapacityManager(
                algorithm.Portfolio,
                algorithm.SubscriptionManager.SubscriptionDataConfigService,
                orderEventProvider
            );

            // 50% of SPY. Volume of 50mi.
            var OrderEvent1 = CreateOrderEvent(algorithm, Symbols.SPY, .5m, 50);
            orderEventProvider.OnNewOrderEvent(OrderEvent1);

            // 50% of AAPL. Volume of 40mi.
            var orderEvent2 = CreateOrderEvent(algorithm, Symbols.AAPL, .5m, 40);
            orderEventProvider.OnNewOrderEvent(orderEvent2);

            const double expected = 50000000 * .025 / .5 + 40000000 * .025 / .5;
            Assert.AreEqual(expected * GetFactor(resolution), manager.AumCapacity);
        }

        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void ComputeCapacityForOneSymbolInTwoDays(Resolution resolution)
        {
            var algorithm = GetAlgorithm(resolution);
            var orderEventProvider = new MockOrderEventProvider();

            var manager = new AssetsUnderManagementCapacityManager(
                algorithm.Portfolio,
                algorithm.SubscriptionManager.SubscriptionDataConfigService,
                orderEventProvider
            );

            // 50% of SPY. Volume of 50mi.
            algorithm.SetDateTime(new DateTime(2018, 1, 9, 12, 05, 0));
            var OrderEvent1 = CreateOrderEvent(algorithm, Symbols.SPY, .5m, 50);
            orderEventProvider.OnNewOrderEvent(OrderEvent1);

            // 50% of SPY. Volume of 40mi.
            algorithm.SetDateTime(new DateTime(2018, 1, 10, 12, 05, 0));
            var orderEvent2 = CreateOrderEvent(algorithm, Symbols.SPY, .5m, 40);
            orderEventProvider.OnNewOrderEvent(orderEvent2);

            const double expected = (50000000 * .025 / .5 + 40000000 * .025 / .5) / 2;
            Assert.AreEqual(expected * GetFactor(resolution), manager.AumCapacity);
        }

        [Test]
        public void ComputeCapacityForComplexCase()
        {
            var algorithm = GetAlgorithm(Resolution.Hour);
            var orderEventProvider = new MockOrderEventProvider();
            var factor = GetFactor(Resolution.Hour);

            var manager = new AssetsUnderManagementCapacityManager(
                algorithm.Portfolio,
                algorithm.SubscriptionManager.SubscriptionDataConfigService,
                orderEventProvider
            );

            //// 2018-01-09 12:05
            algorithm.SetDateTime(new DateTime(2018, 1, 9, 12, 05, 0));

            // 50% of SPY. Volume of 50mi.
            orderEventProvider
                .OnNewOrderEvent(CreateOrderEvent(algorithm, Symbols.SPY, .5m, 50));

            var expected = factor * 2500000; // Capacity = 2500000 = 50000000 * .025 / .5
            Assert.AreEqual(expected, manager.AumCapacity);

            // 25% of AAPL. Volume of 45mi.
            orderEventProvider
                .OnNewOrderEvent(CreateOrderEvent(algorithm, Symbols.AAPL, .25m, 45));

            expected += factor * 45000000 * .025 / .25; // Capacity = 45000000 * .025 / .25 + previous value from SPY
            Assert.AreEqual(expected, manager.AumCapacity);

            //// 2018-01-09 13:05
            algorithm.SetDateTime(new DateTime(2018, 1, 9, 13, 05, 0));

            // 75% of SPY. Volume of 55mi.
            orderEventProvider
                .OnNewOrderEvent(CreateOrderEvent(algorithm, Symbols.SPY, .75m, 55));

            // Capacity = 55000000 * .025 / .75  + 45000000 * .025 / .25 from AAPL
            expected = factor * (55000000 * .025 / .75 + 45000000 * .025 / .25);
            Assert.AreEqual(Math.Round(expected, 2), Math.Round(manager.AumCapacity, 2));

            // 65% of AAPL. Volume of 47mi.
            orderEventProvider
                .OnNewOrderEvent(CreateOrderEvent(algorithm, Symbols.AAPL, .65m, 47));

            // Capacity = 55000000 * .025 / .75  + 47000000 * .025 / .65 from AAPL
            expected = factor * (55000000 * .025 / .75 + 47000000 * .025 / .65);
            Assert.AreEqual(Math.Round(expected, 2), Math.Round(manager.AumCapacity, 2));

            //// 2018-01-09 13:05
            algorithm.SetDateTime(new DateTime(2018, 2, 9, 10, 0, 0));

            // 30% of SPY. Volume of 58mi.
            orderEventProvider
                .OnNewOrderEvent(CreateOrderEvent(algorithm, Symbols.SPY, .30m, 58));

            // Capacity = (58000000 * .025 / .30 + value from day before) / 2
            var previousDay = expected;
            expected = (previousDay + factor * 58000000 * .025 / .30) / 2;
            Assert.AreEqual(Math.Round(expected, 2), Math.Round(manager.AumCapacity, 2));

            // 30% of AAPL. Volume of 31mi.
            orderEventProvider
                .OnNewOrderEvent(CreateOrderEvent(algorithm, Symbols.AAPL, .30m, 31));

            // Capacity = (31000000 * .025 / .30 + 483333.33 from SPY + value from day before) / 2
            expected = (previousDay + factor * (31000000 * .025 / .30 + 58000000 * .025 / .30)) / 2;
            Assert.AreEqual(Math.Round(expected, 2), Math.Round(manager.AumCapacity, 2));
        }

        private static QCAlgorithm GetAlgorithm(Resolution resolution)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.SetDateTime(DateTime.UtcNow);
            algorithm.AddEquity("SPY", resolution);
            algorithm.AddEquity("AAPL", resolution);
            algorithm.SetFinishedWarmingUp();

            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(
                algorithm,
                new BacktestingBrokerage(algorithm),
                new TestResultHandler(Console.WriteLine)
            );

            algorithm.Transactions.SetOrderProcessor(transactionHandler);
            return algorithm;
        }

        private static OrderEvent CreateOrderEvent(IAlgorithm algorithm, Symbol symbol, decimal turnover, decimal volume)
        {
            const decimal price = 100;
            const decimal quantity = 100;
            var size = volume * 1000000;

            var reference = algorithm.Time;
            var resolution = algorithm.SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(symbol).GetHighestResolution();

            // Sets Market Price and Holdings
            var security = algorithm.Securities[symbol];

            BaseData data = new Tick(reference, symbol, price, price) { TickType = TickType.Trade, Quantity = size };
            if (resolution > Resolution.Tick)
            {
                data = new TradeBar(reference, symbol, price, price, price, price, size, resolution.ToTimeSpan());
            }

            security.SetMarketPrice(data);
            security.Holdings.SetHoldings(price, quantity);
            var holdingsValue = security.Holdings.AbsoluteHoldingsValue;

            // Add amount to cash book to maintain the initial TotalPortfolioValue
            var cash = algorithm.Portfolio.CashBook["USD"];
            cash.AddAmount(holdingsValue - algorithm.Portfolio.TotalPortfolioValue);

            var orderQuantity = quantity * turnover;

            var ticket = algorithm.Transactions.AddOrder(
                new SubmitOrderRequest(
                    OrderType.Market,
                    security.Type,
                    security.Symbol,
                    orderQuantity,
                    0,
                    0,
                    DateTime.Now,
                    ""
                )
            );

            var orderEvent = new OrderEvent(
                ticket.OrderId,
                symbol,
                reference,
                OrderStatus.Filled,
                quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell,
                price,
                orderQuantity,
                OrderFee.Zero,
                ""
            );

            ticket.AddOrderEvent(orderEvent);

            return orderEvent;
        }

        private double GetFactor(Resolution resolution) => resolution == Resolution.Daily ? .025 : .05;

        private class MockOrderEventProvider : IOrderEventProvider
        {
            public event EventHandler<OrderEvent> NewOrderEvent;

            public void OnNewOrderEvent(OrderEvent e) => NewOrderEvent?.Invoke(this, e);
        }
    }
}