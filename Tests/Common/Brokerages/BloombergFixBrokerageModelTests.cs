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
    public class BloombergFixBrokerageModelTests
    {
        private readonly BloombergFixBrokerageModel _brokerageModel = new BloombergFixBrokerageModel();

        // Equity: all five order types supported
        [TestCase(SecurityType.Equity, OrderType.Market)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnOpen)]
        [TestCase(SecurityType.Equity, OrderType.Limit)]
        [TestCase(SecurityType.Equity, OrderType.StopMarket)]
        [TestCase(SecurityType.Equity, OrderType.StopLimit)]
        // Option
        [TestCase(SecurityType.Option, OrderType.Market)]
        [TestCase(SecurityType.Option, OrderType.Limit)]
        [TestCase(SecurityType.Option, OrderType.StopMarket)]
        [TestCase(SecurityType.Option, OrderType.StopLimit)]
        // IndexOption
        [TestCase(SecurityType.IndexOption, OrderType.Market)]
        [TestCase(SecurityType.IndexOption, OrderType.Limit)]
        [TestCase(SecurityType.IndexOption, OrderType.StopLimit)]
        // Future: MarketOnOpen is excluded because the base model rejects it for futures
        [TestCase(SecurityType.Future, OrderType.Market)]
        [TestCase(SecurityType.Future, OrderType.Limit)]
        [TestCase(SecurityType.Future, OrderType.StopMarket)]
        [TestCase(SecurityType.Future, OrderType.StopLimit)]
        public void CanSubmitOrderValidSecurityAndOrderTypeReturnsTrue(SecurityType securityType, OrderType orderType)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Crypto)]
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [TestCase(OrderType.LimitIfTouched)]
        [TestCase(OrderType.TrailingStop)]
        [TestCase(OrderType.ComboLimit)]
        public void CanSubmitOrderUnsupportedOrderTypeReturnsFalse(OrderType orderType)
        {
            // Arrange
            var security = GetSecurityForType(SecurityType.Equity);
            var order = CreateOrder(orderType, security.Symbol);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [Test]
        public void CanUpdateOrderReturnsTrue()
        {
            // Arrange
            var security = GetSecurityForType(SecurityType.Equity);
            var order = new LimitOrder(security.Symbol, 1m, 100m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new UpdateOrderFields { LimitPrice = 99m });

            // Act
            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            // Assert
            Assert.That(canUpdate, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void ConstructorWithCashAccountTypeThrowsNotSupportedException()
        {
            Assert.That(() => new BloombergFixBrokerageModel(AccountType.Cash), Throws.TypeOf<NotSupportedException>());
        }

        private static Security GetSecurityForType(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Future:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Future,
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
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.Limit:
                    return new LimitOrder(symbol, 1m, 100m, DateTime.UtcNow);
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, 1m, 100m, DateTime.UtcNow);
                case OrderType.StopLimit:
                    return new StopLimitOrder(symbol, 1m, stopPrice: 105m, limitPrice: 100m, DateTime.UtcNow);
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.LimitIfTouched:
                    return new LimitIfTouchedOrder(symbol, 1m, triggerPrice: 100m, limitPrice: 100m, DateTime.UtcNow);
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, 1m, stopPrice: 100m, trailingAmount: 1m, trailingAsPercentage: false, DateTime.UtcNow);
                case OrderType.ComboLimit:
                    return new ComboLimitOrder(symbol, 1m, 100m, DateTime.UtcNow, new GroupOrderManager(1, 1, 1m));
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}
