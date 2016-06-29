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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.MarginInterest;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class MarginInterestTests
    {
        [Test]
        public void ComputeMarginInterestPaymentAsSecurityPriceFluctuates()
        {
            var Noon = new DateTime(2016, 06, 20, 12, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var TimeKeeper = new TimeKeeper(Noon, TimeZones.NewYork);

            // Define a portfolio with one security
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(securities);
            var orderProcessor = new OrderProcessor();
            transactions.SetOrderProcessor(orderProcessor);
            var portfolio = new SecurityPortfolioManager(securities, transactions);

            var config = CreateTradeBarDataConfig(SecurityType.Equity, Symbols.AAPL);
            securities.Add(new Security(CreateUsEquitySecurityExchangeHours(), config, new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            var security = securities[Symbols.AAPL];
            var time = security.LocalTime;

            const decimal marginInterest = 0.08m;
            const decimal leverage = 2m;
            const decimal buyPrice = 50m;
            const decimal sellPrice = 70m;
            const int quantity = 200;
            const decimal interestPaid = buyPrice * quantity / leverage * marginInterest;
            const decimal amount = buyPrice * quantity + interestPaid;

            portfolio.MarginInterestModel = new ConstantMarginInterestModel(marginInterest);
            portfolio.CashBook[CashBook.AccountCurrency].SetAmount(amount);
            security.SetLeverage(leverage);
            security.SetMarketPrice(new TradeBar(time, Symbols.AAPL, buyPrice, buyPrice, buyPrice, buyPrice, 1));

            // Buy
            var order = new MarketOrder(Symbols.AAPL, quantity, time) { Price = buyPrice };
            var fill = new OrderEvent(order, DateTime.UtcNow, 0) { FillPrice = buyPrice, FillQuantity = quantity };
            orderProcessor.AddOrder(order);
            var request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            Assert.AreEqual(portfolio.CashBook["USD"].Amount, amount);

            portfolio.ProcessFill(fill);

            // Hold stock for one year
            for (var i = 0; i < 365; i++)
            {
                time = time.AddDays(1);
                portfolio.PayMarginInterest(time);
            }

            Assert.AreEqual(buyPrice * quantity / leverage, portfolio.TotalMarginUsed);
            Assert.AreEqual((double)interestPaid, (double)portfolio.TotalInterestPaid, 1e-6);
            Assert.AreEqual((double)portfolio.Cash, 0, 1e-6);

            // Short
            order = new MarketOrder(Symbols.AAPL, -quantity, time) { Price = sellPrice };
            fill = new OrderEvent(order, DateTime.UtcNow, 0) { FillPrice = sellPrice, FillQuantity = -quantity };
            orderProcessor.AddOrder(order);
            request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            portfolio.ProcessFill(fill);

            // Loop another year (we do not hold any stock, so no more interest should be paid
            for (var i = 0; i < 365; i++)
            {
                time = time.AddDays(1);
                portfolio.PayMarginInterest(time);
            }

            Assert.AreEqual((double)interestPaid, (double)portfolio.TotalInterestPaid, 1e-6);

            // Sell
            order = new MarketOrder(Symbols.AAPL, -quantity, time) { Price = sellPrice };
            fill = new OrderEvent(order, DateTime.UtcNow, 0) { FillPrice = sellPrice, FillQuantity = -quantity };
            orderProcessor.AddOrder(order);
            request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            portfolio.ProcessFill(fill);

            // Loop another year (short sell, so no more interest should be paid)
            for (var i = 0; i < 365; i++)
            {
                time = time.AddDays(1);
                portfolio.PayMarginInterest(time);
            }

            Assert.AreEqual((double)interestPaid, (double)portfolio.TotalInterestPaid, 1e-6);

            // Cover
            order = new MarketOrder(Symbols.AAPL, quantity, time) { Price = buyPrice };
            fill = new OrderEvent(order, DateTime.UtcNow, 0) { FillPrice = buyPrice, FillQuantity = quantity };
            orderProcessor.AddOrder(order);
            request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            portfolio.ProcessFill(fill);

            // Loop another year (we do not hold any stock, so no more interest should be paid
            for (var i = 0; i < 365; i++)
            {
                time = time.AddDays(1);
                portfolio.PayMarginInterest(time);
            }

            Assert.AreEqual((double)interestPaid, (double)portfolio.TotalInterestPaid, 1e-6);
        }
        
        private static SecurityExchangeHours CreateUsEquitySecurityExchangeHours()
        {
            var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            return new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek));
        }

        private static SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork,
                TimeZones.NewYork, true, true, false);
        }

        private SubscriptionDataConfig CreateTradeBarDataConfig(SecurityType type, Symbol symbol)
        {
            if (type == SecurityType.Equity)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Forex)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            throw new NotImplementedException(type.ToString());
        }

        class OrderProcessor : IOrderProcessor
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