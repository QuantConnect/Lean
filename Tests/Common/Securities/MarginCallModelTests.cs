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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class MarginCallModelTests
    {
        // Test class to enable calling protected methods
        public class TestSecurityMarginModel : SecurityMarginModel
        {
            public TestSecurityMarginModel(decimal leverage) : base(leverage) {}

            public new decimal GetInitialMarginRequiredForOrder(
                InitialMarginRequiredForOrderParameters parameters)
            {
                return base.GetInitialMarginRequiredForOrder(parameters).Value;
            }

            public new decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
            {
                return base.GetMarginRemaining(portfolio, security, direction);
            }
        }

        [Test]
        public void InitializationTest()
        {
            const decimal actual = 2;
            var security = GetSecurity(Symbols.AAPL);
            security.BuyingPowerModel = new SecurityMarginModel(actual);
            var expected = security.Leverage;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SetAndGetLeverageTest()
        {
            var security = GetSecurity(Symbols.AAPL);
            security.BuyingPowerModel = new SecurityMarginModel(2);

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
            var buyingPowerModel = new TestSecurityMarginModel(2);
            security.BuyingPowerModel = buyingPowerModel;
            var order = new MarketOrder(security.Symbol, 100, DateTime.Now);
            var actual = buyingPowerModel.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order));

            Assert.AreEqual(0, actual);
        }

        [Test]
        public void GetMaintenanceMarginTest()
        {
            const int quantity = 1000;
            const decimal leverage = 2;
            var expected = quantity / leverage;

            var security = GetSecurity(Symbols.AAPL);
            security.BuyingPowerModel = new SecurityMarginModel(leverage);
            security.Holdings.SetHoldings(1m, quantity);
            // current value is used to determine reserved buying power
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = 1,
                High = 1,
                Low = 1,
                Close = 1
            });
            var actual = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(security);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetMarginRemainingTests()
        {
            const int quantity = 1000;
            const decimal leverage = 2;
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, cash:1000);

            var security = GetSecurity(Symbols.AAPL);
            var buyingPowerModel = new TestSecurityMarginModel(leverage);
            security.BuyingPowerModel = buyingPowerModel;
            portfolio.Securities.Add(security);

            // we buy $1000 worth of shares
            security.Holdings.SetHoldings(1m, quantity);
            portfolio.SetCash(0);

            // current value is used to determine reserved buying power
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = 1,
                High = 1,
                Low = 1,
                Close = 1
            });
            var actual1 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Buy);
            Assert.AreEqual(quantity / leverage, actual1);

            var actual2 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Sell);
            Assert.AreEqual(quantity + quantity / leverage, actual2);

            security.Holdings.SetHoldings(1m, -quantity);
            var actual3 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Sell);
            Assert.AreEqual(quantity / leverage, actual3);

            var actual4 = buyingPowerModel.GetMarginRemaining(portfolio, security, OrderDirection.Buy);
            Assert.AreEqual(quantity + quantity / leverage, actual4);
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
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);

            var security = GetSecurity(Symbols.AAPL);
            portfolio.Securities.Add(security);

            var time = DateTime.Now;
            const decimal buyPrice = 1m;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, buyPrice, buyPrice));

            var order = new MarketOrder(Symbols.AAPL, quantity, time) {Price = buyPrice};
            var fill = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                { FillPrice = buyPrice, FillQuantity = quantity, Status = OrderStatus.Filled};
            orderProcessor.AddOrder(order);
            var request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            Assert.AreEqual(portfolio.Cash, fill.FillPrice*fill.FillQuantity);

            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(0, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity, portfolio.TotalPortfolioValue);

            // we shouldn't be able to place a trader
            var newOrder = new MarketOrder(Symbols.AAPL, 1, time.AddSeconds(1)) {Price = buyPrice};
            var hasSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(portfolio, security, newOrder).IsSufficient;
            Assert.IsFalse(hasSufficientBuyingPower);

            // now the stock doubles, so we should have margin remaining
            time = time.AddDays(1);
            const decimal highPrice = buyPrice * 2;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, highPrice, highPrice));
            portfolio.InvalidateTotalPortfolioValue();

            // leverage is 1 we shouldn't have more margin remaining
            Assert.AreEqual(0, portfolio.MarginRemaining);
            Assert.AreEqual(quantity * 2, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity * 2, portfolio.TotalPortfolioValue);

            // we shouldn't be able to place a trader
            var anotherOrder = new MarketOrder(Symbols.AAPL, 1, time.AddSeconds(1)) { Price = highPrice };
            hasSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(portfolio, security, anotherOrder).IsSufficient;
            Assert.IsFalse(hasSufficientBuyingPower);

            // now the stock plummets, leverage is 1 we shouldn't have more margin remaining
            time = time.AddDays(1);
            const decimal lowPrice = buyPrice/2;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, lowPrice, lowPrice));
            portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(0, portfolio.MarginRemaining);
            Assert.AreEqual(quantity/2m, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity/2m, portfolio.TotalPortfolioValue);

            // this would not cause a margin call due to leverage = 1
            bool issueMarginCallWarning;
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out issueMarginCallWarning);
            Assert.IsFalse(issueMarginCallWarning);
            Assert.AreEqual(0, marginCallOrders.Count);

            // now change the leverage to test margin call warning and margin call logic
            security.SetLeverage(leverage * 2);
            // simulate a loan - when we fill using leverage it will set a negative cash amount
            portfolio.CashBook[Currencies.USD].SetAmount(-250);

            // Stock price increase by minimum variation
            const decimal newPrice = lowPrice + 0.01m;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, newPrice, newPrice));
            portfolio.InvalidateTotalPortfolioValue();

            // this would not cause a margin call, only a margin call warning
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(0, marginCallOrders.Count);

            // Price drops again to previous low, margin call orders will be issued
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, lowPrice, lowPrice));
            portfolio.InvalidateTotalPortfolioValue();

            order = new MarketOrder(Symbols.AAPL, quantity, time) { Price = buyPrice };
            fill = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                { FillPrice = buyPrice, FillQuantity = quantity };
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(-250, portfolio.TotalPortfolioValue);

            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(1, marginCallOrders.Count);
        }

        [Test]
        public void GenerateMarginCallOrdersForPositionGroup()
        {
            const int cash = 22000;
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, cash);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null, 0m);

            var underlying = GetSecurity(Symbols.SPY);
            var callOption = GetOption(Symbols.SPY_C_192_Feb19_2016);
            var putOption = GetOption(Symbols.SPY_P_192_Feb19_2016);
            callOption.Underlying = underlying;
            putOption.Underlying = underlying;

            portfolio.Securities.Add(underlying);
            portfolio.Securities.Add(callOption);
            portfolio.Securities.Add(putOption);

            var time = DateTime.Now;
            const decimal underlyingPrice = 100m;
            const decimal callOptionPrice = 1m;
            const decimal putOptionPrice = 1m;
            underlying.SetMarketPrice(new Tick(time, underlying.Symbol, underlyingPrice, underlyingPrice));
            callOption.SetMarketPrice(new Tick(time, callOption.Symbol, callOptionPrice, callOptionPrice));
            putOption.SetMarketPrice(new Tick(time, putOption.Symbol, putOptionPrice, putOptionPrice));

            var groupOrderManager = new GroupOrderManager(1, 2, -10);
            var callOptionOrder = new ComboMarketOrder(callOption.Symbol, -10, time, groupOrderManager) { Price = callOptionPrice };
            var putOptionOrder = new ComboMarketOrder(putOption.Symbol, -10, time, groupOrderManager) { Price = putOptionPrice };

            var callOptionOrderFill = new OrderEvent(callOptionOrder, DateTime.UtcNow, OrderFee.Zero)
            {
                FillPrice = callOptionOrder.Price,
                FillQuantity = callOptionOrder.Quantity,
                Status = OrderStatus.Filled
            };
            var putOptionOrderFill = new OrderEvent(putOptionOrder, DateTime.UtcNow, OrderFee.Zero)
            {
                FillPrice = putOptionOrder.Price,
                FillQuantity = putOptionOrder.Quantity,
                Status = OrderStatus.Filled
            };

            orderProcessor.AddOrder(callOptionOrder);
            orderProcessor.AddOrder(putOptionOrder);

            var callOptionRequest = new SubmitOrderRequest(
                OrderType.ComboMarket,
                callOption.Type,
                callOption.Symbol,
                callOptionOrder.Quantity,
                0,
                0,
                callOptionOrder.Time,
                "",
                groupOrderManager: groupOrderManager);
            var putOptionRequest = new SubmitOrderRequest(
                OrderType.ComboMarket,
                putOption.Type,
                putOption.Symbol,
                putOptionOrder.Quantity,
                0,
                0,
                putOptionOrder.Time,
                "",
                groupOrderManager: groupOrderManager);

            callOptionRequest.SetOrderId(1);
            putOptionRequest.SetOrderId(2);

            groupOrderManager.OrderIds.Add(1);
            groupOrderManager.OrderIds.Add(2);

            orderProcessor.AddTicket(new OrderTicket(null, callOptionRequest));
            orderProcessor.AddTicket(new OrderTicket(null, putOptionRequest));

            portfolio.ProcessFills(new List<OrderEvent> { callOptionOrderFill, putOptionOrderFill });

            // Simulate options price increase so the remaining margin goes below zero
            callOption.SetMarketPrice(new Tick(time.AddMinutes(1), callOption.Symbol, callOptionPrice * 1.2m, callOptionPrice * 1.2m));
            putOption.SetMarketPrice(new Tick(time.AddMinutes(1), putOption.Symbol, putOptionPrice * 1.2m, putOptionPrice * 1.2m));

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out var issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(2, marginCallOrders.Count);
        }

        private SecurityPortfolioManager GetPortfolio(IOrderProcessor orderProcessor, int cash)
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }));
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(orderProcessor);

            var portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());
            portfolio.SetCash(cash);

            return portfolio;
        }

        private Security GetSecurity(Symbol symbol)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private Option GetOption(Symbol symbol)
        {
            return new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties("", Currencies.USD, 100, 0.01m, 1),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
        }
    }
}
