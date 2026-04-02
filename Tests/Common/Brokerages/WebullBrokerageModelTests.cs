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
    public class WebullBrokerageModelTests
    {
        private readonly WebullBrokerageModel _brokerageModel = new WebullBrokerageModel();

        // Equity: all five order types supported
        [TestCase(SecurityType.Equity, OrderType.Market)]
        [TestCase(SecurityType.Equity, OrderType.Limit)]
        [TestCase(SecurityType.Equity, OrderType.StopMarket)]
        [TestCase(SecurityType.Equity, OrderType.StopLimit)]
        [TestCase(SecurityType.Equity, OrderType.TrailingStop)]
        // Option: Market and TrailingStop are not supported
        [TestCase(SecurityType.Option, OrderType.Limit)]
        [TestCase(SecurityType.Option, OrderType.StopMarket)]
        [TestCase(SecurityType.Option, OrderType.StopLimit)]
        // IndexOption: same restrictions as Option
        [TestCase(SecurityType.IndexOption, OrderType.Limit)]
        [TestCase(SecurityType.IndexOption, OrderType.StopMarket)]
        [TestCase(SecurityType.IndexOption, OrderType.StopLimit)]
        // Future: all five order types supported
        [TestCase(SecurityType.Future, OrderType.Market)]
        [TestCase(SecurityType.Future, OrderType.Limit)]
        [TestCase(SecurityType.Future, OrderType.StopMarket)]
        [TestCase(SecurityType.Future, OrderType.StopLimit)]
        [TestCase(SecurityType.Future, OrderType.TrailingStop)]
        // Crypto: StopMarket and TrailingStop are not supported
        [TestCase(SecurityType.Crypto, OrderType.Market)]
        [TestCase(SecurityType.Crypto, OrderType.Limit)]
        [TestCase(SecurityType.Crypto, OrderType.StopLimit)]
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
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            // Arrange
            var security = TestsHelpers.GetSecurity(securityType: securityType, symbol: "EURUSD", market: Market.Oanda);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // Equity does not support exchange-session orders or combo orders
        [TestCase(SecurityType.Equity, OrderType.MarketOnClose)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnOpen)]
        [TestCase(SecurityType.Equity, OrderType.ComboMarket)]
        // Option does not support TrailingStop
        [TestCase(SecurityType.Option, OrderType.TrailingStop)]
        // IndexOption has the same restrictions as Option
        [TestCase(SecurityType.IndexOption, OrderType.TrailingStop)]
        // Crypto does not support StopMarket or TrailingStop
        [TestCase(SecurityType.Crypto, OrderType.StopMarket)]
        [TestCase(SecurityType.Crypto, OrderType.TrailingStop)]
        public void CanSubmitOrder_UnsupportedOrderTypeForSecurityType_ReturnsFalse(
            SecurityType securityType, OrderType orderType)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // ── CanSubmitOrder — Option/IndexOption TimeInForce restrictions ────────
        // https://developer.webull.com/apis/docs/trade-api/options#time-in-force
        // Sell → Day only | Buy → GoodTilCanceled only

        [TestCase(SecurityType.Option, OrderDirection.Sell)]   // Sell + Day
        [TestCase(SecurityType.Option, OrderDirection.Buy)]    // Buy  + GTC
        [TestCase(SecurityType.IndexOption, OrderDirection.Sell)]
        [TestCase(SecurityType.IndexOption, OrderDirection.Buy)]
        public void CanSubmitOrderOptionOrderWithValidTimeInForceReturnsTrue(SecurityType securityType, OrderDirection direction)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            var tif = direction == OrderDirection.Sell
                ? TimeInForce.Day
                : TimeInForce.GoodTilCanceled;
            var order = CreateLimitOrder(security.Symbol, direction, tif);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Option, OrderDirection.Sell)]   // Sell + GTC  → rejected
        [TestCase(SecurityType.Option, OrderDirection.Buy)]    // Buy  + Day  → rejected
        [TestCase(SecurityType.IndexOption, OrderDirection.Sell)]
        [TestCase(SecurityType.IndexOption, OrderDirection.Buy)]
        public void CanSubmitOrderOptionOrderWithInvalidTimeInForceReturnsFalse(
            SecurityType securityType, OrderDirection direction)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            // Deliberately use the wrong TIF for the direction
            var tif = direction == OrderDirection.Sell
                ? TimeInForce.GoodTilCanceled
                : TimeInForce.Day;
            var order = CreateLimitOrder(security.Symbol, direction, tif);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain(tif.GetType().Name));
            Assert.That(message.Message, Does.Contain(security.Type.ToString()));
        }

        // ── CanSubmitOrder — OutsideRegularTradingHours ──────────────────────────
        // https://developer.webull.com/apis/docs/trade-api — Applicable to U.S. stock market orders only.

        [Test]
        public void CanSubmitOrderOutsideRegularTradingHoursOnEquityReturnsTrue()
        {
            // Arrange
            var security = GetSecurityForType(SecurityType.Equity);
            var properties = new WebullOrderProperties { OutsideRegularTradingHours = true };
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow, properties: properties);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.IndexOption)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Crypto)]
        public void CanSubmitOrderOutsideRegularTradingHoursOnNonEquityReturnsFalse(SecurityType securityType)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            var properties = new WebullOrderProperties { OutsideRegularTradingHours = true };
            var order = new LimitOrder(security.Symbol, 1m, 100m, DateTime.UtcNow, properties: properties);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain(nameof(WebullOrderProperties.OutsideRegularTradingHours)));
            Assert.That(message.Message, Does.Contain(securityType.ToString()));
        }

        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Crypto)]
        public void CanSubmitOrderOutsideRegularTradingHoursFalseOnNonEquityReturnsTrue(SecurityType securityType)
        {
            // Arrange
            var security = GetSecurityForType(securityType);
            var properties = new WebullOrderProperties { OutsideRegularTradingHours = false };
            var order = new LimitOrder(security.Symbol, 1m, 100m, DateTime.UtcNow, properties: properties);

            // Act
            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            // Assert
            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void GetFeeModelReturnsWebullFeeModel()
        {
            // Arrange
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "AAPL", market: Market.USA);

            // Act / Assert
            Assert.That(_brokerageModel.GetFeeModel(security), Is.InstanceOf<WebullFeeModel>());
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

        private static LimitOrder CreateLimitOrder(Symbol symbol, OrderDirection direction, TimeInForce timeInForce)
        {
            var quantity = direction == OrderDirection.Buy ? 1m : -1m;
            var properties = new OrderProperties { TimeInForce = timeInForce };
            return new LimitOrder(symbol, quantity, 100m, DateTime.UtcNow, properties: properties);
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}
