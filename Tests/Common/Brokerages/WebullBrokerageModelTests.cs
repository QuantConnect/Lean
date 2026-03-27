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

        // ── CanSubmitOrder — valid combinations ───────────────────────────────────

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
        public void CanSubmitOrder_ValidSecurityAndOrderType_ReturnsTrue(SecurityType securityType, OrderType orderType)
        {
            var security = TestsHelpers.GetSecurity(securityType: securityType, symbol: "AAPL", market: Market.USA);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        // ── CanSubmitOrder — unsupported security types ───────────────────────────

        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Cfd)]
        public void CanSubmitOrder_UnsupportedSecurityType_ReturnsFalse(SecurityType securityType)
        {
            var security = TestsHelpers.GetSecurity(securityType: securityType, symbol: "EURUSD", market: Market.Oanda);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // ── CanSubmitOrder — unsupported order types ──────────────────────────────

        [TestCase(OrderType.MarketOnClose)]
        [TestCase(OrderType.MarketOnOpen)]
        [TestCase(OrderType.TrailingStop)]
        [TestCase(OrderType.ComboMarket)]
        public void CanSubmitOrder_UnsupportedOrderType_ReturnsFalse(OrderType orderType)
        {
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "AAPL", market: Market.USA);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // ── GetFeeModel ───────────────────────────────────────────────────────────

        [Test]
        public void GetFeeModel_ReturnsWebullFeeModel()
        {
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "AAPL", market: Market.USA);

            Assert.That(_brokerageModel.GetFeeModel(security), Is.InstanceOf<WebullFeeModel>());
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

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
