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

            // one of the liquidations is canceled while the other one is still open, and each was sized for the whole deficit
            foreach (var order in orders.Where(order => (order.GroupOrderManager != null) == cancelComboLiquidation))
            {
                order.Status = OrderStatus.Canceled;
            }
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            if (cancelComboLiquidation)
            {
                // the open stock liquidation (-151 SPY) covers the deficit
                Assert.AreEqual(0, marginCallOrders.Count);
            }
            else
            {
                // the open covered call liquidation releases $5000 of the $7500 deficit: the stock group is liquidated for the rest
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

            // the MSFT liquidation fills, the IBM one is still open and then MSFT falls further:
            // selling all of IBM doesn't cover the deficit, beyond the margin buffer, anymore
            msftOrder.Status = OrderStatus.Filled;
            msft.Holdings.SetHoldings(100m, 100 + msftOrder.Quantity);
            portfolio.CashBook[Currencies.USD].AddAmount(-msftOrder.Quantity * 100m);
            msft.SetMarketPrice(new Tick(time, Symbols.MSFT, 70m, 70m));
            portfolio.InvalidateTotalPortfolioValue();

            // MSFT is liquidated for the deficit the open IBM order leaves, $485 at $35 of margin per share,
            // and IBM, which the open order already sells entirely, isn't liquidated again
            marginCallOrders = portfolio.MarginCallModel.GetMarginCallOrders(out _);
            Assert.AreEqual(1, marginCallOrders.Count);
            Assert.AreEqual(Symbols.MSFT, marginCallOrders[0].Symbol);
            Assert.AreEqual(-14, marginCallOrders[0].Quantity);
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

        private Security GetSecurity(Symbol symbol, ICurrencyConverter currencyConverter = null)
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
