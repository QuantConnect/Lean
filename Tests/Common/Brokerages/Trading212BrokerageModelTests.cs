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
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class Trading212BrokerageModelTests
    {
        private readonly Trading212BrokerageModel _brokerageModel = new();

        // Trading 212's public API accepts market, limit, stop and stop-limit orders for equities,
        // and a market order placed outside trading hours fills at the open (MarketOnOpen). A daily
        // market order placed during market hours is converted by Lean into MarketOnClose in backtests.
        [TestCase(SecurityType.Equity, OrderType.Market)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnOpen)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnClose)]
        [TestCase(SecurityType.Equity, OrderType.Limit)]
        [TestCase(SecurityType.Equity, OrderType.StopMarket)]
        [TestCase(SecurityType.Equity, OrderType.StopLimit)]
        public void CanSubmitOrderValidSecurityAndOrderTypeReturnsTrue(SecurityType securityType, OrderType orderType)
        {
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.IndexOption)]
        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.FutureOption)]
        [TestCase(SecurityType.Index)]
        [TestCase(SecurityType.Crypto)]
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain(nameof(Trading212BrokerageModel)));
        }

        [TestCase(SecurityType.Equity, OrderType.TrailingStop)]
        [TestCase(SecurityType.Equity, OrderType.LimitIfTouched)]
        [TestCase(SecurityType.Equity, OrderType.ComboMarket)]
        [TestCase(SecurityType.Equity, OrderType.ComboLimit)]
        public void CanSubmitOrderUnsupportedOrderTypeReturnsFalse(SecurityType securityType, OrderType orderType)
        {
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain(orderType.ToString()));
        }

        // Trading 212 is long only: a sell may reduce an existing position but never flip it through zero,
        // while a buy is always allowed, including one that covers a short position.
        [TestCase(OrderType.Market, 1, -2, false)]
        [TestCase(OrderType.MarketOnOpen, 1, -2, false)]
        [TestCase(OrderType.Limit, 1, -2, false)]
        [TestCase(OrderType.StopMarket, 1, -2, false)]
        [TestCase(OrderType.StopLimit, 1, -2, false)]
        [TestCase(OrderType.Market, 2, -1, true)]
        [TestCase(OrderType.MarketOnOpen, 2, -1, true)]
        [TestCase(OrderType.Limit, 2, -1, true)]
        [TestCase(OrderType.StopMarket, 2, -1, true)]
        [TestCase(OrderType.StopLimit, 2, -1, true)]
        [TestCase(OrderType.Market, -1, 1, true)]
        [TestCase(OrderType.Market, -1, 2, true)]
        public void CanSubmitOrderLongOnlyReturnsExpected(OrderType orderType, decimal holdingQuantity, decimal orderQuantity, bool expected)
        {
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (Symbols.AAPL, 209m, holdingQuantity))[Symbols.AAPL];
            var order = CreateOrder(orderType, security.Symbol, orderQuantity);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(expected));
            if (expected)
            {
                Assert.That(message, Is.Null);
            }
            else
            {
                Assert.That(message, Is.Not.Null);
                Assert.That(message.Message, Is.EqualTo(Messages.Trading212BrokerageModel.ShortSellingNotSupported(order)));
            }
        }

        // Trading 212 is long only. OrderCrossesZero only flags a position passing through zero, so a
        // sell from a flat (or already short) position needs the resulting position checked too.
        [TestCase(AccountType.Cash)]
        [TestCase(AccountType.Margin)]
        public void CanSubmitOrderSellFromFlatReturnsFalse(AccountType accountType)
        {
            var model = new Trading212BrokerageModel(accountType);
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (Symbols.AAPL, 209m, 0m))[Symbols.AAPL];
            var order = new MarketOrder(security.Symbol, -1m, DateTime.UtcNow);

            var canSubmit = model.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain("short"));
        }

        [TestCase(AccountType.Cash)]
        [TestCase(AccountType.Margin)]
        public void CanSubmitOrderSellWhenAlreadyShortReturnsFalse(AccountType accountType)
        {
            var model = new Trading212BrokerageModel(accountType);
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (Symbols.AAPL, 209m, -1m))[Symbols.AAPL];
            var order = new MarketOrder(security.Symbol, -1m, DateTime.UtcNow);

            var canSubmit = model.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain("short"));
        }

        [Test]
        public void CanSubmitOrderContingentOrderReturnsFalse()
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = CreateOrder(OrderType.Market, security.Symbol);
            order.Contingency = new OrderContingency(2, new[] { new ContingencyLink(0, ContingencyType.OneCancelsOther) });

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Code, Is.EqualTo("NotSupported"));
            Assert.That(message.Message, Is.EqualTo(Messages.DefaultBrokerageModel.UnsupportedContingentOrders(_brokerageModel)));
        }

        // Trading 212's order API only accepts DAY and GOOD_TILL_CANCEL orders.
        [TestCase(OrderType.Market)]
        [TestCase(OrderType.MarketOnOpen)]
        [TestCase(OrderType.Limit)]
        public void CanSubmitOrderSupportedTimeInForceReturnsTrue(OrderType orderType)
        {
            var security = GetSecurityForType(SecurityType.Equity);

            foreach (var timeInForce in new[] { TimeInForce.Day, TimeInForce.GoodTilCanceled })
            {
                var order = CreateOrder(orderType, security.Symbol);
                order.Properties.TimeInForce = timeInForce;

                var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

                Assert.That(canSubmit, Is.True, $"Expected {timeInForce.GetType().Name} to be supported");
                Assert.That(message, Is.Null);
            }
        }

        [TestCase(OrderType.Market)]
        [TestCase(OrderType.Limit)]
        public void CanSubmitOrderUnsupportedTimeInForceReturnsFalse(OrderType orderType)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = CreateOrder(orderType, security.Symbol);
            order.Properties.TimeInForce = TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7));

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Code, Is.EqualTo("NotSupported"));
            Assert.That(message.Message, Is.EqualTo(Messages.DefaultBrokerageModel.UnsupportedTimeInForce(_brokerageModel, order)));
        }

        [TestCase(AccountType.Cash)]
        [TestCase(AccountType.Margin)]
        public void CanSubmitOrderBuyFromFlatReturnsTrue(AccountType accountType)
        {
            var model = new Trading212BrokerageModel(accountType);
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (Symbols.AAPL, 209m, 0m))[Symbols.AAPL];
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = model.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void DefaultsToCashAccount()
        {
            Assert.That(_brokerageModel.AccountType, Is.EqualTo(AccountType.Cash));
        }

        // Trading 212's equity accounts cannot use margin, so leverage stays 1 even for a model
        // created with AccountType.Margin (for example through BrokerageModel.Create).
        [TestCase(AccountType.Cash)]
        [TestCase(AccountType.Margin)]
        public void GetLeverageReturnsOneForEquity(AccountType accountType)
        {
            var model = new Trading212BrokerageModel(accountType);
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(model.GetLeverage(security), Is.EqualTo(1m));
        }

        [Test]
        public void DefaultAccountTypeUsesAccountCurrencyImmediateEquitySettlement()
        {
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(_brokerageModel.GetSettlementModel(security), Is.InstanceOf<AccountCurrencyImmediateSettlementModel>());
        }

        // Trading 212's Invest Terms clause 8.3 allow sale proceeds to be used before the settlement date to
        // buy more shares, so equity proceeds settle immediately whichever account type the model was created
        // with, including the margin account that SetBrokerageModel(BrokerageName.Trading212) selects.
        [TestCase(AccountType.Cash)]
        [TestCase(AccountType.Margin)]
        public void GetSettlementModelReturnsAccountCurrencyImmediateSettlementForEquity(AccountType accountType)
        {
            var model = new Trading212BrokerageModel(accountType);
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(model.GetSettlementModel(security), Is.InstanceOf<AccountCurrencyImmediateSettlementModel>());
        }

        [Test]
        public void CreateWithMarginAccountTypeUsesAccountCurrencyImmediateEquitySettlement()
        {
            var model = BrokerageModel.Create(new OrderProvider(), BrokerageName.Trading212, AccountType.Margin);
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(model.GetSettlementModel(security), Is.InstanceOf<AccountCurrencyImmediateSettlementModel>());
        }

        // Non-equity fills also carry a base-currency leg in SecurityPortfolioModel.ProcessFill, so they
        // must keep the base settlement model instead of converting that leg to the account currency.
        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Crypto)]
        public void GetSettlementModelFallsBackToTheBaseModelForNonEquities(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);

            var settlementModel = _brokerageModel.GetSettlementModel(security);

            Assert.That(settlementModel, Is.InstanceOf<ImmediateSettlementModel>());
            Assert.That(settlementModel, Is.Not.InstanceOf<AccountCurrencyImmediateSettlementModel>());
        }

        [Test]
        public void CreateWithMarginAccountTypeStillUsesNoLeverage()
        {
            var model = BrokerageModel.Create(new OrderProvider(), BrokerageName.Trading212, AccountType.Margin);
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(model.GetLeverage(security), Is.EqualTo(1m));
        }

        [Test]
        public void CreateReturnsTrading212BrokerageModel()
        {
            var model = BrokerageModel.Create(new OrderProvider(), BrokerageName.Trading212, AccountType.Cash);

            Assert.That(model, Is.InstanceOf<Trading212BrokerageModel>());
            Assert.That(BrokerageModel.GetBrokerageName(model), Is.EqualTo(BrokerageName.Trading212));
        }

        private static Security GetSecurityForType(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Future:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Future,
                        symbol: Futures.Indices.SP500EMini, market: Market.CME);
                case SecurityType.FutureOption:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.FutureOption,
                        symbol: Futures.Indices.SP500EMini, market: Market.CME);
                case SecurityType.Crypto:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Crypto,
                        symbol: "BTCUSD", market: Market.Coinbase);
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return TestsHelpers.GetSecurity(securityType: securityType,
                        symbol: "EURUSD", market: Market.Oanda);
                case SecurityType.IndexOption:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.IndexOption,
                        symbol: "SPX", market: Market.CBOE);
                case SecurityType.Index:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Index,
                        symbol: "SPX", market: Market.USA);
                default:
                    return TestsHelpers.GetSecurity(securityType: securityType,
                        symbol: "AAPL", market: Market.USA);
            }
        }

        private static Order CreateOrder(OrderType orderType, Symbol symbol, decimal quantity = 1m)
        {
            switch (orderType)
            {
                case OrderType.Market:
                    return new MarketOrder(symbol, quantity, DateTime.UtcNow);
                case OrderType.Limit:
                    return new LimitOrder(symbol, quantity, 100m, DateTime.UtcNow);
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, quantity, 100m, DateTime.UtcNow);
                case OrderType.StopLimit:
                    return new StopLimitOrder(symbol, quantity, 105m, 100m, DateTime.UtcNow);
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, quantity, 100m, 1m, false, DateTime.UtcNow);
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, quantity, DateTime.UtcNow);
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, quantity, DateTime.UtcNow);
                case OrderType.LimitIfTouched:
                    return new LimitIfTouchedOrder(symbol, quantity, 105m, 100m, DateTime.UtcNow);
                case OrderType.ComboMarket:
                    return new ComboMarketOrder(symbol, quantity, DateTime.UtcNow, new GroupOrderManager(1, 1, quantity));
                case OrderType.ComboLimit:
                    return new ComboLimitOrder(symbol, quantity, 100m, DateTime.UtcNow, new GroupOrderManager(1, 1, quantity));
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}
