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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

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

        [TestCase(OpenOrderKind.MarginCall)]
        [TestCase(OpenOrderKind.MarginCallReplacedOnMarginCall)]
        [TestCase(OpenOrderKind.StopLoss)]
        public void DoesNotGenerateMarginCallOrdersWhileMarginCallOrdersAreOpen(OpenOrderKind openOrderKind)
        {
            const int quantity = 1000;
            const decimal leverage = 1m;
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);

            var security = GetSecurity(Symbols.AAPL, portfolio.CashBook);
            portfolio.Securities.Add(security);

            var time = DateTime.Now;
            const decimal buyPrice = 1m;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, buyPrice, buyPrice));

            var order = new MarketOrder(Symbols.AAPL, quantity, time) { Price = buyPrice };
            var fill = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                { FillPrice = buyPrice, FillQuantity = quantity, Status = OrderStatus.Filled };
            orderProcessor.AddOrder(order);
            var request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, order.Time, null);
            request.SetOrderId(0);
            orderProcessor.AddTicket(new OrderTicket(null, request));
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            // the stock doubles
            time = time.AddDays(1);
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, buyPrice * 2, buyPrice * 2));
            portfolio.InvalidateTotalPortfolioValue();

            // and then plummets
            time = time.AddDays(1);
            const decimal lowPrice = buyPrice / 2;
            security.SetMarketPrice(new Tick(time, Symbols.AAPL, lowPrice, lowPrice));
            portfolio.InvalidateTotalPortfolioValue();

            // leverage up and simulate a loan, buying more shares
            security.SetLeverage(leverage * 2);
            portfolio.CashBook[Currencies.USD].SetAmount(-250);

            order = new MarketOrder(Symbols.AAPL, quantity, time) { Price = buyPrice };
            fill = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) { FillPrice = buyPrice, FillQuantity = quantity };
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out var issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(1, marginCallOrders.Count);

            if (openOrderKind == OpenOrderKind.StopLoss)
            {
                // an open order the algorithm placed itself doesn't prevent the margin call
                orderProcessor.AddOrder(new StopMarketOrder(Symbols.AAPL, marginCallOrders[0].Quantity, lowPrice / 2, time, "Stop loss") { Id = 1 });
                Assert.AreEqual(1, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);
                return;
            }

            var submittedRequest = marginCallOrders[0];
            if (openOrderKind == OpenOrderKind.MarginCallReplacedOnMarginCall)
            {
                // like OnMarginCall replacing the generated request with a new one
                submittedRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol,
                    marginCallOrders[0].Quantity * 1.1m, 0, 0, time, "OnMarginCall");
                marginCallOrders.Clear();
                marginCallOrders.Add(submittedRequest);
            }
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
            Assert.AreEqual(1, tickets.Count);
            var openOrder = Order.CreateOrder(submittedRequest);
            orderProcessor.AddOrder(openOrder);
            tickets[0].SetOrder(openOrder);

            // the margin call order hasn't been filled yet, the position is still the same: scanning the portfolio
            // again must not duplicate the liquidation, which would flip the position
            Assert.AreEqual(0, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);

            // once the margin call order is closed without liquidating the position, the margin call is generated again
            openOrder.Status = OrderStatus.Canceled;
            Assert.AreEqual(1, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);
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

            callOptionOrderFill.Ticket = new OrderTicket(null, callOptionRequest);
            orderProcessor.AddTicket(callOptionOrderFill.Ticket);
            putOptionOrderFill.Ticket = new OrderTicket(null, putOptionRequest);
            orderProcessor.AddTicket(putOptionOrderFill.Ticket);

            portfolio.ProcessFills(new List<OrderEvent> { callOptionOrderFill, putOptionOrderFill });

            // Simulate options price increase so the remaining margin goes below zero
            callOption.SetMarketPrice(new Tick(time.AddMinutes(1), callOption.Symbol, callOptionPrice * 1.2m, callOptionPrice * 1.2m));
            putOption.SetMarketPrice(new Tick(time.AddMinutes(1), putOption.Symbol, putOptionPrice * 1.2m, putOptionPrice * 1.2m));

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out var issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(2, marginCallOrders.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LiquidatesPositionGroupsSharingALegForTheDeficitOpenMarginCallOrdersLeave(bool cancelComboLiquidation)
        {
            var orderProcessor = new FakeOrderProcessor();
            // a covered call group (+100 SPY / -1 call) and a separate +200 SPY group, both holding SPY
            var portfolio = GetSpyPortfolio(orderProcessor, -22400, -1);
            Assert.AreEqual(2, portfolio.Positions.Groups.Count);

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(2, marginCallOrders.Count(request => request.GroupOrderManager != null));
            Assert.AreEqual(1, marginCallOrders.Count(request => request.GroupOrderManager == null));
            var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(3, orders.Count);

            // the covered call liquidation (-100 SPY / +1 call) releases $5000 of the $7500 deficit: the stock group is liquidated for the rest
            Assert.AreEqual(-50, orders.Single(order => order.GroupOrderManager == null).Quantity);

            // one of the liquidations is canceled while the other one is still open
            foreach (var order in orders.Where(order => (order.GroupOrderManager != null) == cancelComboLiquidation))
            {
                order.Status = OrderStatus.Canceled;
            }
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            if (cancelComboLiquidation)
            {
                // the open stock liquidation releases $2500: the 250 SPY left, a covered call group and a +150 SPY group,
                // are each liquidated for the $5000 left
                Assert.AreEqual(3, marginCallOrders.Count);
                Assert.AreEqual(-100, marginCallOrders.Single(request => request.Symbol == Symbols.SPY && request.GroupOrderManager != null).Quantity);
                Assert.AreEqual(1, marginCallOrders.Single(request => request.Symbol == Symbols.SPY_C_192_Feb19_2016).Quantity);
                Assert.AreEqual(-100, marginCallOrders.Single(request => request.GroupOrderManager == null).Quantity);
            }
            else
            {
                // the open covered call liquidation releases $5000: the stock group is liquidated again for the rest
                Assert.AreEqual(1, marginCallOrders.Count);
                Assert.IsNull(marginCallOrders[0].GroupOrderManager);
                Assert.AreEqual(-51, marginCallOrders[0].Quantity);
            }

            // once every margin call order is closed, the margin call is generated again from the current holdings
            foreach (var order in orders)
            {
                order.Status = OrderStatus.Canceled;
            }
            Assert.AreEqual(3, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);
        }

        [Test]
        public void DoesNotLiquidateHoldingsAgainWhenTheyRegroupWhileMarginCallOrdersAreOpen()
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetSpyPortfolio(orderProcessor, -26000, 0);

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(-221, marginCallOrders[0].Quantity);
            ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);

            // a short call fills while the stock liquidation is open, regrouping the holdings into a covered call group
            // (+100 SPY / -1 call) and a +200 SPY group: the shares already being liquidated must not be liquidated again,
            // so the covered call group, whose 100 SPY with the 221 being sold would be more than the 300 held, isn't liquidated
            portfolio.Securities[Symbols.SPY_C_192_Feb19_2016].Holdings.SetHoldings(1m, -1);
            portfolio.InvalidateTotalPortfolioValue();
            Assert.AreEqual(2, portfolio.Positions.Groups.Count);
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.IsTrue(marginCallOrders.All(request => request.GroupOrderManager == null));
            Assert.LessOrEqual(221 - marginCallOrders.Sum(request => request.Quantity), 300);
        }

        [Test]
        public void GeneratesMarginCallOrdersForTheDeficitOpenMarginCallOrdersDoNotCover()
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            var time = DateTime.Now;
            var ibm = GetSecurity(Symbols.IBM, portfolio.CashBook);
            var msft = GetSecurity(Symbols.MSFT, portfolio.CashBook);
            foreach (var (security, price) in new[] { (ibm, 10m), (msft, 100m) })
            {
                portfolio.Securities.Add(security);
                security.SetLeverage(2);
                security.SetMarketPrice(new Tick(time, security.Symbol, price, price));
                security.Holdings.SetHoldings(price, 100);
            }
            portfolio.CashBook[Currencies.USD].SetAmount(-7000);
            portfolio.InvalidateTotalPortfolioValue();

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            var ibmOrder = orders.Single(order => order.Symbol == Symbols.IBM);
            var msftOrder = orders.Single(order => order.Symbol == Symbols.MSFT);
            Assert.AreEqual(-100, ibmOrder.Quantity);
            // MSFT is scaled down to the $1000 of the $1500 deficit that selling all of IBM leaves, at $50 of margin per share
            Assert.AreEqual(-20, msftOrder.Quantity);

            // the MSFT liquidation fills, the IBM one is still open and then MSFT falls further:
            // selling all of IBM doesn't cover the deficit, beyond the margin buffer, anymore
            msftOrder.Status = OrderStatus.Filled;
            msft.Holdings.SetHoldings(100m, 100 + msftOrder.Quantity);
            portfolio.CashBook[Currencies.USD].AddAmount(-msftOrder.Quantity * 100m);
            msft.SetMarketPrice(new Tick(time, Symbols.MSFT, 70m, 70m));
            portfolio.InvalidateTotalPortfolioValue();

            // MSFT is liquidated for the deficit the open IBM order leaves, $1200 at $35 of margin per share,
            // and IBM, which the open order already sells entirely, isn't liquidated again
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(Symbols.MSFT, marginCallOrders[0].Symbol);
            Assert.AreEqual(-35, marginCallOrders[0].Quantity);
        }

        [Test]
        public void StopsExecutingMarginCallOrdersOnceTheOpenOnesCoverTheDeficit()
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            // IBM, bought at $200, is the loser, so its margin call order is executed first
            var time = DateTime.Now;
            foreach (var (symbol, averagePrice, price) in new[] { (Symbols.IBM, 200m, 100m), (Symbols.MSFT, 10m, 10m) })
            {
                var security = GetSecurity(symbol, portfolio.CashBook);
                portfolio.Securities.Add(security);
                security.SetLeverage(2);
                security.SetMarketPrice(new Tick(time, symbol, price, price));
                security.Holdings.SetHoldings(averagePrice, 100);
            }
            portfolio.CashBook[Currencies.USD].SetAmount(-6500);
            portfolio.InvalidateTotalPortfolioValue();

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(2, marginCallOrders.Count);

            // the IBM order stays open, like a market order waiting for the next daily bar, and covers the deficit once filled
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
            Assert.AreEqual(1, tickets.Count);
            Assert.AreEqual(Symbols.IBM, tickets[0].Symbol);
        }

        [Test]
        public void SizesEachPositionGroupForTheDeficitAndHoldingsTheEarlierLiquidationsLeave()
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            // IBM, bought at $120, is the loser, so it's liquidated first
            var time = DateTime.Now;
            foreach (var (symbol, averagePrice) in new[] { (Symbols.IBM, 120m), (Symbols.MSFT, 100m) })
            {
                var security = GetSecurity(symbol, portfolio.CashBook);
                portfolio.Securities.Add(security);
                security.SetLeverage(2);
                security.SetMarketPrice(new Tick(time, symbol, 100m, 100m));
                security.Holdings.SetHoldings(averagePrice, 100);
            }
            portfolio.CashBook[Currencies.USD].SetAmount(-14000);
            portfolio.InvalidateTotalPortfolioValue();

            // each position group is sized for the $4000 deficit: IBM's order, executed first, releases $4050 and covers it
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(2, marginCallOrders.Count);
            Assert.IsTrue(marginCallOrders.All(request => request.Quantity == -81));
            var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(Symbols.IBM, orders.Single().Symbol);

            // while the IBM order is open MSFT falls, leaving a $2700 deficit: the 19 IBM shares the open order doesn't sell
            // are sized against themselves, and MSFT, now the loser, is liquidated entirely, releasing $2250
            var msft = portfolio.Securities[Symbols.MSFT];
            msft.SetMarketPrice(new Tick(time, Symbols.MSFT, 45m, 45m));
            portfolio.InvalidateTotalPortfolioValue();
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(2, marginCallOrders.Count);
            Assert.AreEqual(-100, marginCallOrders.Single(request => request.Symbol == Symbols.MSFT).Quantity);
            Assert.AreEqual(-19, marginCallOrders.Single(request => request.Symbol == Symbols.IBM).Quantity);

            // the IBM order is scaled down to the 9 shares that cover the $450 the MSFT order leaves
            orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(2, orders.Count);
            Assert.AreEqual(-100, orders[0].Quantity);
            Assert.AreEqual(-9, orders[1].Quantity);
        }

        [Test]
        public void LiquidatesTheStockOfACoveredCallWhoseCallIsBeingBoughtBack()
        {
            var orderProcessor = new FakeOrderProcessor();
            // 3 covered calls (+300 SPY / -3 calls)
            var portfolio = GetSpyPortfolio(orderProcessor, -26000, -3);

            // like OnMarginCall replacing the combo order with an order per leg
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _)
                .Select(request => new SubmitOrderRequest(OrderType.Market, request.SecurityType, request.Symbol, request.Quantity, 0, 0,
                    request.Time, "OnMarginCall"))
                .ToList();
            var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(2, orders.Count);

            // the stock order is canceled while the call buyback is still open: the shares left regroup into a stock group
            // that is liquidated for the deficit
            orders.Single(order => order.Symbol == Symbols.SPY).Status = OrderStatus.Canceled;
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(Symbols.SPY, marginCallOrders[0].Symbol);
            Assert.IsNull(marginCallOrders[0].GroupOrderManager);
            Assert.AreEqual(-300, marginCallOrders[0].Quantity);
        }

        [TestCase(-300)]
        [TestCase(-150)]
        public void LiquidatesPositionGroupsThatAreNotHeldForTheDeficit(int stockOrderQuantity)
        {
            var orderProcessor = new FakeOrderProcessor();
            // 3 covered calls (+300 SPY / -3 calls)
            var portfolio = GetSpyPortfolio(orderProcessor, -26000, -3);

            // like OnMarginCall replacing the combo order with a stock order only
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _)
                .Where(request => request.Symbol == Symbols.SPY)
                .Select(request => new SubmitOrderRequest(OrderType.Market, request.SecurityType, request.Symbol, stockOrderQuantity, 0, 0,
                    request.Time, "OnMarginCall"))
                .ToList();
            Assert.AreEqual(1, ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders).Count);

            // the stock order is still open and the account loses more: the holdings left regroup into naked calls,
            // and covered calls and shares for a partial stock order, groups that aren't held
            portfolio.CashBook[Currencies.USD].SetAmount(-29000);
            portfolio.InvalidateTotalPortfolioValue();
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            var calls = marginCallOrders.Where(request => request.Symbol == Symbols.SPY_C_192_Feb19_2016).ToList();
            var shares = marginCallOrders.Where(request => request.Symbol == Symbols.SPY).ToList();
            if (stockOrderQuantity == -300)
            {
                // $3300 of margin for the 3 naked calls, a $2600 deficit: all 3 are bought back, $1100 each
                Assert.AreEqual(1, marginCallOrders.Count);
                Assert.IsNull(calls.Single().GroupOrderManager);
                Assert.AreEqual(3, calls.Single().Quantity);
            }
            else
            {
                // a covered call, 50 shares and 2 naked calls, each liquidated entirely for the $9000 deficit
                Assert.AreEqual(4, marginCallOrders.Count);
                Assert.AreEqual(1, calls.Single(request => request.GroupOrderManager != null).Quantity);
                Assert.AreEqual(-100, shares.Single(request => request.GroupOrderManager != null).Quantity);
                Assert.AreEqual(2, calls.Single(request => request.GroupOrderManager == null).Quantity);
                Assert.AreEqual(-50, shares.Single(request => request.GroupOrderManager == null).Quantity);

                // the covered call releases $5000, the naked calls $2200, and the shares are scaled down to the 36 that cover the $1800 left
                var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
                Assert.AreEqual(4, orders.Count);
                Assert.AreEqual(2, orders.Single(order => order.Symbol == Symbols.SPY_C_192_Feb19_2016 && order.GroupOrderManager == null).Quantity);
                Assert.AreEqual(-36, orders.Single(order => order.Symbol == Symbols.SPY && order.GroupOrderManager == null).Quantity);
            }
        }

        [TestCase(OrderStatus.Invalid)]
        [TestCase(OrderStatus.Canceled)]
        public void LiquidatesTheNextPositionGroupForTheWholeDeficitWhenAMarginCallOrderFails(OrderStatus failedOrderStatus)
        {
            // the brokerage fails IBM's and MSFT's orders
            var orderProcessor = new FakeOrderProcessor
            {
                RejectedOrderStatus = failedOrderStatus,
                RejectsOrder = requests => requests.Any(request => request.Symbol == Symbols.IBM || request.Symbol == Symbols.MSFT)
            };
            // each position can cover the $4000 deficit on its own, losers first: IBM, MSFT and then SPY
            var portfolio = GetStocksPortfolio(orderProcessor, -19000, (Symbols.IBM, 120m, null), (Symbols.MSFT, 110m, null),
                (Symbols.SPY, 100m, null));

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(3, marginCallOrders.Count);
            Assert.IsTrue(marginCallOrders.All(request => request.Quantity == -81));

            // a failed order releases no margin: SPY is liquidated as generated for the whole deficit
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
            Assert.AreEqual(3, tickets.Count);
            Assert.AreEqual(Symbols.IBM, tickets[0].Symbol);
            Assert.AreEqual(failedOrderStatus, tickets[0].Status);
            Assert.AreEqual(Symbols.MSFT, tickets[1].Symbol);
            Assert.AreEqual(failedOrderStatus, tickets[1].Status);
            Assert.AreEqual(Symbols.SPY, tickets[2].Symbol);
            Assert.AreEqual(-81, tickets[2].Quantity);
            Assert.IsTrue(tickets[2].Status.IsOpen());
        }

        [Test]
        public void ExecutesTheRequestedOrderWhenTheScaledDownOneIsBelowTheMinimumOrderSize()
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;
            // the orders are checked for the minimum order size, as brokerage models do before submitting them
            orderProcessor.RejectsOrder = requests =>
                requests.Any(request => !DefaultBrokerageModel.IsValidOrderSize(portfolio.Securities[request.Symbol], request.Quantity, out _));

            // 100 IBM at $10, the loser, and 100 MSFT at $100, whose orders must be of at least 25 shares
            var time = DateTime.Now;
            var ibm = GetSecurity(Symbols.IBM, portfolio.CashBook);
            var msft = GetSecurity(Symbols.MSFT, portfolio.CashBook,
                symbolProperties: new SymbolProperties("", Currencies.USD, 1, 0.01m, 1, "", minimumOrderSize: 25));
            foreach (var (security, price, averagePrice) in new[] { (ibm, 10m, 20m), (msft, 100m, 100m) })
            {
                portfolio.Securities.Add(security);
                security.SetLeverage(2);
                security.SetMarketPrice(new Tick(time, security.Symbol, price, price));
                security.Holdings.SetHoldings(averagePrice, 100);
            }
            portfolio.CashBook[Currencies.USD].SetAmount(-7000);
            portfolio.InvalidateTotalPortfolioValue();

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            var requestedMsftQuantity = marginCallOrders.Single(request => request.Symbol == Symbols.MSFT).Quantity;
            Assert.LessOrEqual(requestedMsftQuantity, -25);

            // selling all of IBM leaves $1000 of the $1500 deficit: the 20 MSFT shares that cover it are fewer than the minimum,
            // so the requested order is executed instead
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
            Assert.AreEqual(2, tickets.Count);
            Assert.AreEqual(-100, tickets[0].Quantity);
            Assert.AreEqual(requestedMsftQuantity, tickets[1].Quantity);
            Assert.IsTrue(tickets.All(ticket => ticket.Status.IsOpen()));
        }

        [Test]
        public void ExecutesLaterMarginCallRequestsWithoutQuantityAsGiven()
        {
            var orderProcessor = new FakeOrderProcessor { RejectsOrder = requests => requests.Any(request => request.Symbol == Symbols.IBM) };
            var portfolio = GetStocksPortfolio(orderProcessor, -19000, (Symbols.IBM, 120m, null), (Symbols.MSFT, 110m, null));

            // like OnMarginCall replacing MSFT's order with one without quantity
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _)
                .Select(request => request.Symbol == Symbols.IBM ? request : new SubmitOrderRequest(OrderType.Market, request.SecurityType,
                    request.Symbol, 0, 0, 0, request.Time, "OnMarginCall"))
                .ToList();
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
            Assert.AreEqual(2, tickets.Count);
            Assert.AreEqual(0, tickets[1].Quantity);
        }

        [Test]
        public void ExecutesThePositionGroupsWhoseMarginCallOrdersFailedLast()
        {
            var orderProcessor = new FakeOrderProcessor { RejectsOrder = requests => requests.Any(request => request.Symbol == Symbols.IBM) };
            // each position can cover the $4000 deficit on its own, losers first: IBM, MSFT and then SPY
            var portfolio = GetStocksPortfolio(orderProcessor, -19000, (Symbols.IBM, 120m, null), (Symbols.MSFT, 110m, null),
                (Symbols.SPY, 100m, null));

            // IBM's order is rejected and MSFT's, which covers the deficit, stays open
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(portfolio.MarginCallModel.GetMarginCallOrders(out _));
            Assert.AreEqual(2, tickets.Count);
            Assert.AreEqual(OrderStatus.Invalid, tickets[0].Status);
            Assert.AreEqual(Symbols.MSFT, tickets[1].Symbol);
            var msftOrder = Order.CreateOrder(tickets[1].SubmitRequest);
            orderProcessor.AddOrder(msftOrder);
            tickets[1].SetOrder(msftOrder);
            Assert.AreEqual(0, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);

            // the brokerage cancels MSFT's order later: SPY, whose orders haven't failed, is liquidated first, then IBM and MSFT
            msftOrder.Status = OrderStatus.Canceled;
            orderProcessor.RejectsOrder = _ => true;
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(3, marginCallOrders.Count);
            CollectionAssert.AreEqual(new[] { Symbols.SPY, Symbols.IBM, Symbols.MSFT },
                portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders).Select(ticket => ticket.Symbol));

            // every order failed again: SPY's orders failed once, IBM's and MSFT's twice
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            CollectionAssert.AreEqual(new[] { Symbols.SPY, Symbols.IBM, Symbols.MSFT },
                portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders).Select(ticket => ticket.Symbol));

            // once the margin call ends the failures are forgotten: the next one liquidates the losers first again
            portfolio.CashBook[Currencies.USD].SetAmount(0);
            portfolio.InvalidateTotalPortfolioValue();
            Assert.AreEqual(0, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);
            portfolio.CashBook[Currencies.USD].SetAmount(-19000);
            portfolio.InvalidateTotalPortfolioValue();
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(Symbols.IBM, portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders)[0].Symbol);
        }

        [Test]
        public void LiquidatesAStockGroupSharingTheUnderlyingOfARejectedComboOrder()
        {
            // the brokerage rejects combo orders but accepts stock orders
            var orderProcessor = new FakeOrderProcessor { RejectsOrder = requests => requests[0].OrderType == OrderType.ComboMarket };
            // a covered call group (+100 SPY / -1 call) and a separate +200 SPY group, both holding SPY,
            // with the call sold at $0.50 so the covered call is the loser
            var portfolio = GetSpyPortfolio(orderProcessor, -19000, -1);
            portfolio.Securities[Symbols.SPY_C_192_Feb19_2016].Holdings.SetHoldings(0.5m, -1);
            portfolio.InvalidateTotalPortfolioValue();

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(3, marginCallOrders.Count);

            // the combo order is rejected: the stock group is liquidated as generated for the whole $4100 deficit
            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders);
            Assert.AreEqual(3, tickets.Count);
            Assert.IsTrue(tickets.Take(2).All(ticket => ticket.Status == OrderStatus.Invalid && ticket.SubmitRequest.GroupOrderManager != null));
            Assert.AreEqual(Symbols.SPY, tickets[2].Symbol);
            Assert.IsNull(tickets[2].SubmitRequest.GroupOrderManager);
            Assert.AreEqual(-83, tickets[2].Quantity);
            Assert.IsTrue(tickets[2].Status.IsOpen());

            // the fill and its $1 fee leave the account within its margin requirement
            var order = Order.CreateOrder(tickets[2].SubmitRequest);
            portfolio.ProcessFills(new List<OrderEvent> { new OrderEvent(order, DateTime.UtcNow, new OrderFee(new CashAmount(1, Currencies.USD)))
                { FillPrice = 100m, FillQuantity = order.Quantity, Status = OrderStatus.Filled } });
            Assert.GreaterOrEqual(portfolio.MarginRemaining, 0);
        }

        [Test]
        public void LiquidatesAllTheSharesOfAStockGroupSharingTheSecurityWithAnotherGroup()
        {
            var orderProcessor = new FakeOrderProcessor();
            // a covered call group (+100 SPY / -1 call) and a separate +200 SPY group, both holding SPY, with no equity left
            var portfolio = GetSpyPortfolio(orderProcessor, -29900, -1);
            Assert.AreEqual(0, portfolio.TotalPortfolioValue);

            // the stock group is sized from its own 200 shares, not the 300 SPY held
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(3, marginCallOrders.Count);
            Assert.AreEqual(-100, marginCallOrders.Single(request => request.Symbol == Symbols.SPY && request.GroupOrderManager != null).Quantity);
            Assert.AreEqual(1, marginCallOrders.Single(request => request.Symbol == Symbols.SPY_C_192_Feb19_2016).Quantity);
            Assert.AreEqual(-200, marginCallOrders.Single(request => request.GroupOrderManager == null).Quantity);
        }

        [Test]
        public void LiquidatesTheShortPositionsLeftAfterOpenMarginCallOrders()
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, 0);
            // without option strategies, the short calls are a position group of their own, with short option margin
            portfolio.SetPositions(SecurityPositionGroupModel.Null);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            var underlying = GetSecurity(Symbols.SPY, portfolio.CashBook);
            var callOption = GetOption(Symbols.SPY_C_192_Feb19_2016, portfolio.CashBook);
            callOption.Underlying = underlying;
            portfolio.Securities.Add(underlying);
            portfolio.Securities.Add(callOption);
            var time = DateTime.Now;
            underlying.SetMarketPrice(new Tick(time, underlying.Symbol, 100m, 100m));
            callOption.SetMarketPrice(new Tick(time, callOption.Symbol, 1m, 1m));
            callOption.Holdings.SetHoldings(1m, -3);
            portfolio.CashBook[Currencies.USD].SetAmount(3200);
            portfolio.InvalidateTotalPortfolioValue();

            // $3300 of margin for the 3 short calls against $2900 of equity: one is bought back, and the order stays open
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(1, marginCallOrders[0].Quantity);
            ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);

            // the equity falls to $500: the 2 short calls left, $2200 of margin, are both bought back
            portfolio.CashBook[Currencies.USD].SetAmount(800);
            portfolio.InvalidateTotalPortfolioValue();
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(2, marginCallOrders[0].Quantity);
        }

        [Test]
        public void DoesNotSellMoreThanHeldAcrossPositionGroupsSharingALeg()
        {
            var orderProcessor = new FakeOrderProcessor();
            // a covered call group (+100 SPY / -1 call) and a separate +200 SPY group, both holding SPY
            var portfolio = GetSpyPortfolio(orderProcessor, -22400, -1);
            var orders = ExecuteMarginCall(portfolio, orderProcessor, portfolio.MarginCallModel.GetMarginCallOrders(out _));
            Assert.AreEqual(3, orders.Count);

            // the combo is canceled, the stock order (-50 SPY) stays open and SPY falls: the 250 shares left regroup into
            // a covered call group and a +150 SPY group, which are liquidated again from those holdings
            foreach (var order in orders.Where(order => order.GroupOrderManager != null))
            {
                order.Status = OrderStatus.Canceled;
            }
            portfolio.Securities[Symbols.SPY].SetMarketPrice(new Tick(DateTime.Now, Symbols.SPY, 80m, 80m));
            portfolio.InvalidateTotalPortfolioValue();

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(3, marginCallOrders.Count);
            Assert.AreEqual(-100, marginCallOrders.Single(request => request.Symbol == Symbols.SPY && request.GroupOrderManager != null).Quantity);
            Assert.AreEqual(1, marginCallOrders.Single(request => request.Symbol == Symbols.SPY_C_192_Feb19_2016).Quantity);
            Assert.AreEqual(-150, marginCallOrders.Single(request => request.GroupOrderManager == null).Quantity);
            Assert.AreEqual(-50, orders.Single(order => order.GroupOrderManager == null).Quantity);

            // the stock group, whose orders haven't failed, is liquidated first, then the covered call whose combo was canceled:
            // 300 shares with the open order, all of those held
            var newOrders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(3, newOrders.Count);
            Assert.AreEqual(-150, newOrders[0].Quantity);
            Assert.AreEqual(-300, newOrders.Concat(orders.Where(order => order.GroupOrderManager == null))
                .Where(order => order.Symbol == Symbols.SPY).Sum(order => order.Quantity));
        }

        [Test]
        public void LiquidatesMultiLotPositionGroupsOnceWithTheirHeldQuantities()
        {
            var orderProcessor = new FakeOrderProcessor();
            // 3 covered calls (+300 SPY / -3 calls)
            var portfolio = GetSpyPortfolio(orderProcessor, -26000, -3);
            Assert.AreEqual(1, portfolio.Positions.Groups.Count);

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(2, marginCallOrders.Count);
            Assert.AreEqual(-300, marginCallOrders.Single(request => request.Symbol == Symbols.SPY).Quantity);
            Assert.AreEqual(3, marginCallOrders.Single(request => request.Symbol == Symbols.SPY_C_192_Feb19_2016).Quantity);
            Assert.AreEqual(2, ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders).Count);

            // the combo order is still open: the group isn't liquidated again
            Assert.AreEqual(0, portfolio.MarginCallModel.GetMarginCallOrders(out _).Count);
        }

        [TestCase(10)]
        [TestCase(-10)]
        public void GeneratesMarginCallOrdersForFutures(int quantity)
        {
            var orderProcessor = new FakeOrderProcessor();
            // $60000 maintenance margin against a $30000 portfolio: the $30000 the futures keep is a target of the whole
            // portfolio value, which holds the 4 contracts whose $6600 initial margin each fits in it
            var portfolio = GetFuturesPortfolio(orderProcessor, 30000, quantity);
            Assert.AreEqual(60000, portfolio.TotalMarginUsed);
            Assert.AreEqual(30000, portfolio.TotalPortfolioValue);

            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out var issueMarginCallWarning);
            Assert.IsTrue(issueMarginCallWarning);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(Symbols.Future_ESZ18_Dec2018, marginCallOrders[0].Symbol);
            Assert.AreEqual(-Math.Sign(quantity) * 6, marginCallOrders[0].Quantity);

            var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(-Math.Sign(quantity) * 6, orders.Single().Quantity);
        }

        [TestCase(10)]
        [TestCase(-10)]
        public void GeneratesMarginCallOrdersForFuturesAlongsideAnotherSecurity(int quantity)
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetFuturesPortfolio(orderProcessor, 30000, quantity);

            // 100 AAPL shares at $100 with 2x leverage, bought at $120 so they're the loser and liquidated first
            var stock = GetSecurity(Symbols.AAPL, portfolio.CashBook);
            portfolio.Securities.Add(stock);
            stock.SetLeverage(2);
            stock.SetMarketPrice(new Tick(DateTime.Now, Symbols.AAPL, 100m, 100m));
            stock.Holdings.SetHoldings(120m, 100);
            portfolio.InvalidateTotalPortfolioValue();
            Assert.AreEqual(65000, portfolio.TotalMarginUsed);
            Assert.AreEqual(40000, portfolio.TotalPortfolioValue);

            // the $25000 deficit: the futures keep $35000 of their $60000 margin, 5 contracts at $6600 initial margin each,
            // and the stock none of its $5000
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(2, marginCallOrders.Count);
            Assert.AreEqual(-Math.Sign(quantity) * 5, marginCallOrders.Single(request => request.Symbol == Symbols.Future_ESZ18_Dec2018).Quantity);
            Assert.AreEqual(-100, marginCallOrders.Single(request => request.Symbol == Symbols.AAPL).Quantity);

            // the stock order releases $5000, the futures one is scaled down to the 4 contracts whose $6000 maintenance margin each
            // covers the $20000 left
            var orders = ExecuteMarginCall(portfolio, orderProcessor, marginCallOrders);
            Assert.AreEqual(2, orders.Count);
            Assert.AreEqual(-100, orders[0].Quantity);
            Assert.AreEqual(-Math.Sign(quantity) * 4, orders[1].Quantity);
        }

        [TestCase(10, OptionRight.Call)]
        [TestCase(-10, OptionRight.Put)]
        public void GeneratesMarginCallOrdersForFuturesSharedWithAFutureOptionStrategy(int quantity, OptionRight right)
        {
            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetSharedFuturesPortfolio(orderProcessor, 5000, quantity, right);
            Assert.AreEqual(4990, portfolio.TotalPortfolioValue);

            // the strategy's margin and the $54000 of the futures group leave a deficit larger than the latter, which is liquidated
            // entirely
            var marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(3, marginCallOrders.Count);
            Assert.AreEqual(-Math.Sign(quantity) * 9, marginCallOrders.Single(request => request.GroupOrderManager == null).Quantity);
            Assert.AreEqual(-Math.Sign(quantity), marginCallOrders.Single(request => request.Symbol == Symbols.Future_ESZ18_Dec2018
                && request.GroupOrderManager != null).Quantity);
            Assert.AreEqual(1, marginCallOrders.Single(request => request.Symbol.SecurityType == SecurityType.FutureOption).Quantity);
        }

        [TestCase(10, OptionRight.Call)]
        [TestCase(-10, OptionRight.Put)]
        public void LiquidatesFuturesSharedWithARejectedFutureOptionStrategyForTheDeficitAndFees(int quantity, OptionRight right)
        {
            // the brokerage rejects combo orders, so the futures group's order has to cover the deficit by itself
            var orderProcessor = new FakeOrderProcessor { RejectsOrder = requests => requests[0].OrderType == OrderType.ComboMarket };
            var portfolio = GetSharedFuturesPortfolio(orderProcessor, 0, quantity, right);
            portfolio.CashBook[Currencies.USD].AddAmount(portfolio.TotalMarginUsed - 12000 - portfolio.TotalPortfolioValue);
            portfolio.InvalidateTotalPortfolioValue();
            Assert.AreEqual(12000, portfolio.TotalMarginUsed - portfolio.TotalPortfolioValue);

            var tickets = portfolio.MarginCallModel.ExecuteMarginCall(portfolio.MarginCallModel.GetMarginCallOrders(out _));
            Assert.AreEqual(3, tickets.Count);
            var futuresTicket = tickets.Single(ticket => ticket.SubmitRequest.GroupOrderManager == null);
            Assert.AreEqual(-Math.Sign(quantity) * 3, futuresTicket.Quantity);

            // the fill and its $1 fee leave the account within its margin requirement
            var order = Order.CreateOrder(futuresTicket.SubmitRequest);
            portfolio.ProcessFills(new List<OrderEvent> { new OrderEvent(order, DateTime.UtcNow, new OrderFee(new CashAmount(1, Currencies.USD)))
                { FillPrice = 2900m, FillQuantity = order.Quantity, Status = OrderStatus.Filled } });
            Assert.GreaterOrEqual(portfolio.MarginRemaining, 0);
        }

        public enum OpenOrderKind
        {
            MarginCall,
            MarginCallReplacedOnMarginCall,
            StopLoss
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

        private SecurityPortfolioManager GetStocksPortfolio(IOrderProcessor orderProcessor, decimal cash,
            params (Symbol Symbol, decimal AveragePrice, SecurityExchangeHours ExchangeHours)[] stocks)
        {
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            // 100 shares of each stock, all at $100
            foreach (var (symbol, averagePrice, exchangeHours) in stocks)
            {
                var security = GetSecurity(symbol, portfolio.CashBook, exchangeHours);
                portfolio.Securities.Add(security);
                security.SetLeverage(2);
                security.SetMarketPrice(new Tick(DateTime.Now, symbol, 100m, 100m));
                security.Holdings.SetHoldings(averagePrice, 100);
            }
            portfolio.CashBook[Currencies.USD].SetAmount(cash);
            portfolio.InvalidateTotalPortfolioValue();

            return portfolio;
        }

        private SecurityPortfolioManager GetFuturesPortfolio(IOrderProcessor orderProcessor, decimal cash, int quantity)
        {
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            // the ES maintenance margin is $6000 per contract on this date
            var time = new DateTime(2018, 10, 1);
            var future = new Future(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.Future_ESZ18_Dec2018, Resolution.Minute, TimeZones.NewYork,
                    TimeZones.NewYork, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null);
            future.FeeModel = new ConstantFeeModel(0);
            portfolio.Securities.Add(future);
            future.SetMarketPrice(new Tick(time, future.Symbol, 2900m, 2900m));
            future.Holdings.SetHoldings(2900m, quantity);
            portfolio.CashBook[Currencies.USD].SetAmount(cash);
            portfolio.InvalidateTotalPortfolioValue();

            return portfolio;
        }

        /// <summary>
        /// Gets a futures portfolio where one of the futures covers a short future option, so the others are a group of their own
        /// </summary>
        private SecurityPortfolioManager GetSharedFuturesPortfolio(IOrderProcessor orderProcessor, decimal cash, int quantity,
            OptionRight right)
        {
            var portfolio = GetFuturesPortfolio(orderProcessor, cash, quantity);
            var future = portfolio.Securities[Symbols.Future_ESZ18_Dec2018];
            var optionSymbol = Symbol.CreateOption(future.Symbol, Market.CME, OptionStyle.American, right, 2900m, new DateTime(2018, 12, 21));
            var option = new QuantConnect.Securities.FutureOption.FutureOption(optionSymbol, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)), portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null, new SecurityCache(), future);
            option.FeeModel = new ConstantFeeModel(0);
            portfolio.Securities.Add(option);
            option.SetMarketPrice(new Tick(new DateTime(2018, 10, 1), optionSymbol, 10m, 10m));
            option.Holdings.SetHoldings(10m, -1);
            portfolio.InvalidateTotalPortfolioValue();
            Assert.AreEqual(2, portfolio.Positions.Groups.Count);

            return portfolio;
        }

        private SecurityPortfolioManager GetSpyPortfolio(IOrderProcessor orderProcessor, decimal cash, int callQuantity)
        {
            var portfolio = GetPortfolio(orderProcessor, 0);
            portfolio.MarginCallModel = new DefaultMarginCallModel(portfolio, null, 0m);
            portfolio.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;

            var underlying = GetSecurity(Symbols.SPY, portfolio.CashBook);
            var callOption = GetOption(Symbols.SPY_C_192_Feb19_2016, portfolio.CashBook);
            callOption.Underlying = underlying;
            portfolio.Securities.Add(underlying);
            portfolio.Securities.Add(callOption);

            var time = DateTime.Now;
            underlying.SetLeverage(2);
            underlying.SetMarketPrice(new Tick(time, underlying.Symbol, 100m, 100m));
            callOption.SetMarketPrice(new Tick(time, callOption.Symbol, 1m, 1m));

            underlying.Holdings.SetHoldings(100m, 300);
            callOption.Holdings.SetHoldings(1m, callQuantity);
            portfolio.CashBook[Currencies.USD].SetAmount(cash);
            portfolio.InvalidateTotalPortfolioValue();

            return portfolio;
        }

        /// <summary>
        /// Executes the margin call orders, which stay open like market orders waiting for the next daily bar in backtesting
        /// </summary>
        private static List<Order> ExecuteMarginCall(SecurityPortfolioManager portfolio, FakeOrderProcessor orderProcessor,
            List<SubmitOrderRequest> marginCallOrders)
        {
            var orders = new List<Order>();
            foreach (var ticket in portfolio.MarginCallModel.ExecuteMarginCall(marginCallOrders))
            {
                var order = Order.CreateOrder(ticket.SubmitRequest);
                orderProcessor.AddOrder(order);
                ticket.SetOrder(order);
                orders.Add(order);
            }
            return orders;
        }

        private Security GetSecurity(Symbol symbol, ICurrencyConverter currencyConverter = null, SecurityExchangeHours exchangeHours = null,
            SymbolProperties symbolProperties = null)
        {
            return new Security(
                exchangeHours ?? SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
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
                symbolProperties ?? SymbolProperties.GetDefault(Currencies.USD),
                currencyConverter ?? ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private Option GetOption(Symbol symbol, ICurrencyConverter currencyConverter = null)
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
                currencyConverter ?? ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
        }
    }
}
