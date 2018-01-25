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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class MarginCallModelTests
    {
        // Test class to enable calling protected methods
        public class TestSecurityMarginBuyingPowerModel : SecurityMarginBuyingPowerModel
        {
            public TestSecurityMarginBuyingPowerModel(decimal leverage) : base(leverage) {}

            public new decimal GetInitialMarginRequiredForOrder(Security security, Order order)
            {
                return base.GetInitialMarginRequiredForOrder(security, order);
            }
        }

        [Test]
        public void InitializationTest()
        {
            const decimal actual = 2;
            var security = GetSecurity(Symbols.AAPL);
            security.BuyingPowerModel = new SecurityMarginBuyingPowerModel(actual);
            var expected = security.Leverage;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SetAndGetLeverageTest()
        {
            var security = GetSecurity(Symbols.AAPL);
            security.BuyingPowerModel = new SecurityMarginBuyingPowerModel(2);

            const decimal actual = 50;
            security.SetLeverage(actual);
            var expected = security.Leverage;

            Assert.AreEqual(expected, actual);

            expected = security.BuyingPowerModel.GetLeverage(security);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetInitialMarginRequiredForOrderTest()
        {
            var security = GetSecurity(Symbols.AAPL);
            var buyingPowerModel = new TestSecurityMarginBuyingPowerModel(2);
            security.BuyingPowerModel = buyingPowerModel;
            var order = new MarketOrder(security.Symbol, 100, DateTime.Now);
            var actual = buyingPowerModel.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(0, actual);
        }

        [Test]
        public void GetMaintenanceMarginTest()
        {
            const int quantity = 1000;
            const decimal leverage = 2;
            var expected = quantity / leverage;

            var security = GetSecurity(Symbols.AAPL);
            security.BuyingPowerModel = new SecurityMarginBuyingPowerModel(leverage);
            security.Holdings.SetHoldings(1m, quantity);
            var actual = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(security);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetMarginRemainingTests()
        {
            const int quantity = 1000;
            const decimal leverage = 2;
            var orderProcessor = new OrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity);
            portfolio.MarginCallModel = MarginCallModel.Null;

            var security = GetSecurity(Symbols.AAPL);
            var buyingPowerModel = new TestSecurityMarginBuyingPowerModel(leverage);
            security.BuyingPowerModel = buyingPowerModel;
            portfolio.Securities.Add(security);

            security.Holdings.SetHoldings(1m, quantity);
            var actual1 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Buy);
            Assert.AreEqual(quantity / leverage, actual1);

            var actual2 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Sell);
            Assert.AreEqual(quantity, actual2);

            security.Holdings.SetHoldings(1m, -quantity);
            var actual3 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Sell);
            Assert.AreEqual(quantity / leverage, actual3);

            var actual4 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Buy);
            Assert.AreEqual(quantity, actual4);
        }

        /// <summary>
        /// Test GenerateMarginCallOrder with SecurityPortfolioManager.ScanForMarginCall
        /// to comprehensively test margin call dynamics
        /// </summary>
        [Test]
        public void GenerateMarginCallOrderTests()
        {
            const int quantity = 1000;
            const decimal leverage = 1m;
            var orderProcessor = new OrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity);
            portfolio.MarginCallModel = MarginCallModel.Null;

            var security = GetSecurity(Symbols.AAPL);
            portfolio.Securities.Add(security);

            var time = DateTime.Now;
            const decimal buyPrice = 1m;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, buyPrice, buyPrice));

            var order = new MarketOrder(Symbols.AAPL, quantity, time) {Price = buyPrice};
            var fill = new OrderEvent(order, DateTime.UtcNow, 0) { FillPrice = buyPrice, FillQuantity = quantity };
            orderProcessor.AddOrder(order);
            var request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            Assert.AreEqual(portfolio.Cash, fill.FillPrice*fill.FillQuantity);

            portfolio.ProcessFill(fill);

            Assert.AreEqual(0, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity, portfolio.TotalPortfolioValue);

            // we shouldn't be able to place a trader
            var newOrder = new MarketOrder(Symbols.AAPL, 1, time.AddSeconds(1)) {Price = buyPrice};
            var hasSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(portfolio, security, newOrder);
            Assert.IsFalse(hasSufficientBuyingPower);

            // now the stock doubles, so we should have margin remaining
            time = time.AddDays(1);
            const decimal highPrice = buyPrice * 2;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, highPrice, highPrice));

            Assert.AreEqual(quantity, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity * 2, portfolio.TotalPortfolioValue);

            // we shouldn't be able to place a trader
            var anotherOrder = new MarketOrder(Symbols.AAPL, 1, time.AddSeconds(1)) { Price = highPrice };
            hasSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(portfolio, security, anotherOrder);
            Assert.IsTrue(hasSufficientBuyingPower);

            // now the stock plummets, so we should have negative margin remaining
            time = time.AddDays(1);
            const decimal lowPrice = buyPrice/2;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, lowPrice, lowPrice));

            Assert.AreEqual(-quantity/2m, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity/2m, portfolio.TotalPortfolioValue);

            // this would not cause a margin call due to leverage = 1
            bool issueMarginCallWarning;
            var marginCallOrders = portfolio.ScanForMarginCall(out issueMarginCallWarning);
            Assert.IsFalse(issueMarginCallWarning);
            Assert.AreEqual(0, marginCallOrders.Count);

            // now change the leverage to test margin call warning and margin call logic
            security.SetLeverage(leverage * 2);

            // Stock price increase by minimum variation
            const decimal newPrice = lowPrice + 0.01m;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, newPrice, newPrice));

            // this would not cause a margin call, only a margin call warning
            marginCallOrders = portfolio.ScanForMarginCall(out issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(0, marginCallOrders.Count);

            // Price drops again to previous low, margin call orders will be issued
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, lowPrice, lowPrice));

            order = new MarketOrder(Symbols.AAPL, quantity, time) { Price = buyPrice };
            fill = new OrderEvent(order, DateTime.UtcNow, 0) { FillPrice = buyPrice, FillQuantity = quantity };
            portfolio.ProcessFill(fill);

            Assert.AreEqual(0, portfolio.TotalPortfolioValue);

            // Even with TotalPortfolioValue == 0, do not issue warning or orders
            marginCallOrders = portfolio.ScanForMarginCall(out issueMarginCallWarning);
            Assert.IsFalse(issueMarginCallWarning);
            Assert.AreEqual(0, marginCallOrders.Count);
        }

        private SecurityPortfolioManager GetPortfolio(IOrderProcessor orderProcessor, int quantity)
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }));
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(orderProcessor);

            var portfolio = new SecurityPortfolioManager(securities, transactions);
            portfolio.SetCash(quantity);

            return portfolio;
        }

        private Security GetSecurity(Symbol symbol)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
        }

        public class OrderProcessor : IOrderProcessor
        {
            private readonly ConcurrentDictionary<int, Order> _orders = new ConcurrentDictionary<int, Order>();
            private readonly ConcurrentDictionary<int, OrderTicket> _tickets = new ConcurrentDictionary<int, OrderTicket>();
            public void AddOrder(Order order)
            {
                _orders[order.Id] = order;
            }

            public void AddTicket(OrderTicket ticket)
            {
                _tickets[ticket.OrderId] = ticket;
            }
            public int OrdersCount { get; private set; }
            public Order GetOrderById(int orderId)
            {
                Order order;
                _orders.TryGetValue(orderId, out order);
                return order;
            }

            public Order GetOrderByBrokerageId(string brokerageId)
            {
                return _orders.Values.FirstOrDefault(x => x.BrokerId.Contains(brokerageId));
            }

            public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
            {
                return _tickets.Values.Where(filter ?? (x => true));
            }

            public OrderTicket GetOrderTicket(int orderId)
            {
                OrderTicket ticket;
                _tickets.TryGetValue(orderId, out ticket);
                return ticket;
            }

            public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null)
            {
                return _orders.Values.Where(filter ?? (x => true));
            }

            public OrderTicket Process(OrderRequest request)
            {
                throw new NotImplementedException();
            }
        }
    }
}
