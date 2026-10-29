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
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class Trading212BrokerageModelTests
    {
        private readonly Trading212BrokerageModel _brokerageModel = new();

        // Trading 212's public API accepts market, limit, stop and stop-limit orders for equities.
        [TestCase(SecurityType.Equity, OrderType.Market)]
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

        [TestCase(SecurityType.Equity, OrderType.MarketOnClose)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnOpen)]
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

        // Trading 212 is long only: a sell may reduce an existing position but never flip it through zero.
        [TestCase(OrderType.Market, 1, -2, false)]
        [TestCase(OrderType.Limit, 1, -2, false)]
        [TestCase(OrderType.StopMarket, 1, -2, false)]
        [TestCase(OrderType.StopLimit, 1, -2, false)]
        [TestCase(OrderType.Market, 2, -1, true)]
        [TestCase(OrderType.Limit, 2, -1, true)]
        [TestCase(OrderType.StopMarket, 2, -1, true)]
        [TestCase(OrderType.StopLimit, 2, -1, true)]
        public void CanSubmitOrderCrossZeroReturnsExpected(OrderType orderType, decimal holdingQuantity, decimal orderQuantity, bool expected)
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
                Assert.That(message.Message, Does.Contain(nameof(Trading212BrokerageModel)));
            }
        }

        // Trading 212 is long only. OrderCrossesZero only flags a position passing through zero, so a
        // sell from a flat (or already short) position would open or add to a short without being caught.
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
        public void CreateWithMarginAccountTypeStillUsesNoLeverage()
        {
            var model = BrokerageModel.Create(new OrderProvider(), BrokerageName.Trading212, AccountType.Margin);
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(model.GetLeverage(security), Is.EqualTo(1m));
        }

        [Test]
        public void GetSettlementModelReturnsAccountCurrencyDelayedSettlementModel()
        {
            var security = GetSecurityForType(SecurityType.Equity);

            var settlementModel = _brokerageModel.GetSettlementModel(security);

            Assert.That(settlementModel, Is.InstanceOf<AccountCurrencyDelayedSettlementModel>());
        }

        // Trading 212 converts every trade to the account currency, so a foreign currency buy
        // debits the account currency cash instead of leaving a balance in the quote currency.
        [Test]
        public void SettlementModelSettlesForeignCurrencyBuysInTheAccountCurrency()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetAccountCurrency(Currencies.GBP, 1000m);
            var security = algorithm.AddSecurity(Symbols.AAPL);
            var portfolio = algorithm.Portfolio;
            // One USD is worth 0.8 GBP.
            portfolio.SetCash(Currencies.USD, 0m, 0.8m);

            var settlementModel = _brokerageModel.GetSettlementModel(security);
            settlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, DateTime.UtcNow,
                new CashAmount(-1000m, Currencies.USD), null));

            Assert.That(portfolio.CashBook[Currencies.GBP].Amount, Is.EqualTo(200m));
            Assert.That(portfolio.CashBook[Currencies.USD].Amount, Is.EqualTo(0m));
        }

        // Sale proceeds wait for the T+1 settlement, converted to the account currency.
        [Test]
        public void SettlementModelHoldsForeignCurrencySaleProceedsUntilSettlement()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetAccountCurrency(Currencies.GBP, 1000m);
            var security = algorithm.AddSecurity(Symbols.AAPL);
            var portfolio = algorithm.Portfolio;
            // One USD is worth 0.8 GBP.
            portfolio.SetCash(Currencies.USD, 0m, 0.8m);

            var settlementModel = _brokerageModel.GetSettlementModel(security);
            settlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, DateTime.UtcNow,
                new CashAmount(1000m, Currencies.USD), null));

            Assert.That(portfolio.CashBook[Currencies.GBP].Amount, Is.EqualTo(1000m));
            Assert.That(portfolio.CashBook[Currencies.USD].Amount, Is.EqualTo(0m));
            Assert.That(portfolio.UnsettledCashBook[Currencies.GBP].Amount, Is.EqualTo(800m));
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
