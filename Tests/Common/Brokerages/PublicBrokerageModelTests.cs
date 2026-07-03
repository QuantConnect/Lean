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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class PublicBrokerageModelTests
    {
        private readonly PublicBrokerageModel _brokerageModel = new();

        // Every supported security type accepts market, limit, stop and stop-limit orders.
        [TestCase(SecurityType.Equity, OrderType.Market)]
        [TestCase(SecurityType.Equity, OrderType.Limit)]
        [TestCase(SecurityType.Equity, OrderType.StopMarket)]
        [TestCase(SecurityType.Equity, OrderType.StopLimit)]
        [TestCase(SecurityType.Option, OrderType.Market)]
        [TestCase(SecurityType.Option, OrderType.Limit)]
        [TestCase(SecurityType.Option, OrderType.StopMarket)]
        [TestCase(SecurityType.Option, OrderType.StopLimit)]
        [TestCase(SecurityType.IndexOption, OrderType.Market)]
        [TestCase(SecurityType.IndexOption, OrderType.Limit)]
        [TestCase(SecurityType.IndexOption, OrderType.StopMarket)]
        [TestCase(SecurityType.IndexOption, OrderType.StopLimit)]
        [TestCase(SecurityType.Crypto, OrderType.Market)]
        [TestCase(SecurityType.Crypto, OrderType.Limit)]
        [TestCase(SecurityType.Crypto, OrderType.StopMarket)]
        [TestCase(SecurityType.Crypto, OrderType.StopLimit)]
        public void CanSubmitOrderValidSecurityAndOrderTypeReturnsTrue(SecurityType securityType, OrderType orderType)
        {
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.FutureOption)]
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [TestCase(SecurityType.Equity, OrderType.MarketOnClose)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnOpen)]
        [TestCase(SecurityType.Equity, OrderType.TrailingStop)]
        [TestCase(SecurityType.Equity, OrderType.ComboMarket)]
        [TestCase(SecurityType.Option, OrderType.TrailingStop)]
        public void CanSubmitOrderUnsupportedOrderTypeReturnsFalse(SecurityType securityType, OrderType orderType)
        {
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [Test]
        public void CanSubmitComboLimitOrderReturnsTrue()
        {
            var security = GetSecurityForType(SecurityType.Option);
            var order = CreateOrder(OrderType.ComboLimit, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        // Public.com handles crossing a zero position natively, so cross-zero orders — single-leg and combo — are accepted.
        [TestCase(OrderType.Limit, 1, -2)]
        [TestCase(OrderType.ComboLimit, -1, -2)]
        [TestCase(OrderType.ComboLimit, 1, -2)]
        public void CanSubmitCrossZeroOrderReturnsTrue(OrderType orderType, decimal holdingQuantity, decimal orderQuantity)
        {
            var equity = Symbols.AAPL;
            var groupOrderManager = orderType == OrderType.ComboLimit ? new GroupOrderManager(1, 2, quantity: 8) : null;
            var order = orderType == OrderType.ComboLimit
                ? TestsHelpers.CreateNewOrderByOrderType(orderType, equity, orderQuantity, groupOrderManager)
                : new LimitOrder(equity, orderQuantity, 80m, new DateTime(default));

            var security = TestsHelpers.InitializeSecurity(equity.SecurityType, (equity, 209m, holdingQuantity))[equity];

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        // A margin account keeps an explicit UseMargin choice and otherwise uses margin by default;
        // a cash account has no margin buying power, so UseMargin is always forced off there.
        [TestCase(AccountType.Margin, null, true)]
        [TestCase(AccountType.Margin, true, true)]
        [TestCase(AccountType.Margin, false, false)]
        [TestCase(AccountType.Cash, null, false)]
        [TestCase(AccountType.Cash, true, false)]
        [TestCase(AccountType.Cash, false, false)]
        public void CanSubmitOrderResolvesUseMarginFromAccountType(AccountType accountType, bool? requestedUseMargin, bool expectedUseMargin)
        {
            var brokerageModel = new PublicBrokerageModel(accountType);
            var security = GetSecurityForType(SecurityType.Equity);
            var properties = new PublicOrderProperties { UseMargin = requestedUseMargin };
            var order = new LimitOrder(security.Symbol, 1m, 100m, DateTime.UtcNow, properties: properties);

            var canSubmit = brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
            Assert.That(properties.UseMargin, Is.EqualTo(expectedUseMargin));
        }

        [Test]
        public void CanUpdateOrderSingleOrderReturnsTrue()
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = new LimitOrder(security.Symbol, 1m, 100m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new UpdateOrderFields { LimitPrice = 99m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.That(canUpdate, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void CanUpdateOrderComboOrderReturnsFalse()
        {
            var security = GetSecurityForType(SecurityType.Option);
            var groupOrderManager = new GroupOrderManager(1, 2, quantity: 8);
            var order = TestsHelpers.CreateNewOrderByOrderType(OrderType.ComboLimit, security.Symbol, 1m, groupOrderManager);
            var request = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new UpdateOrderFields { Quantity = 2m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.That(canUpdate, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [Test]
        public void GetFeeModelReturnsPublicFeeModel()
        {
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "AAPL", market: Market.USA);

            Assert.That(_brokerageModel.GetFeeModel(security), Is.InstanceOf<PublicFeeModel>());
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
                default:
                    return TestsHelpers.GetSecurity(securityType: securityType,
                        symbol: "AAPL", market: Market.USA);
            }
        }

        private static Order CreateOrder(OrderType orderType, Symbol symbol)
        {
            switch (orderType)
            {
                case OrderType.Market:
                    return new MarketOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.Limit:
                    return new LimitOrder(symbol, 1m, 100m, DateTime.UtcNow);
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, 1m, 100m, DateTime.UtcNow);
                case OrderType.StopLimit:
                    return new StopLimitOrder(symbol, 1m, 105m, 100m, DateTime.UtcNow);
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, 1m, 100m, 1m, false, DateTime.UtcNow);
                case OrderType.ComboMarket:
                    return new ComboMarketOrder(symbol, 1m, DateTime.UtcNow, new GroupOrderManager(1, 1, 1m));
                case OrderType.ComboLimit:
                    return new ComboLimitOrder(symbol, 1m, 100m, DateTime.UtcNow, new GroupOrderManager(1, 1, 1m));
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}
