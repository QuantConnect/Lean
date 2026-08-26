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
    public class ClearStreetBrokerageModelTests
    {
        private readonly ClearStreetBrokerageModel _brokerageModel = new();

        // Equity, Option and IndexOption accept every order type Clear Street knows.
        [TestCase(SecurityType.Equity, OrderType.Market)]
        [TestCase(SecurityType.Equity, OrderType.Limit)]
        [TestCase(SecurityType.Equity, OrderType.StopMarket)]
        [TestCase(SecurityType.Equity, OrderType.StopLimit)]
        [TestCase(SecurityType.Equity, OrderType.TrailingStop)]
        [TestCase(SecurityType.Option, OrderType.Market)]
        [TestCase(SecurityType.Option, OrderType.Limit)]
        [TestCase(SecurityType.Option, OrderType.StopMarket)]
        [TestCase(SecurityType.Option, OrderType.StopLimit)]
        [TestCase(SecurityType.Option, OrderType.TrailingStop)]
        [TestCase(SecurityType.IndexOption, OrderType.Market)]
        [TestCase(SecurityType.IndexOption, OrderType.Limit)]
        [TestCase(SecurityType.IndexOption, OrderType.StopMarket)]
        [TestCase(SecurityType.IndexOption, OrderType.StopLimit)]
        [TestCase(SecurityType.IndexOption, OrderType.TrailingStop)]
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
        [TestCase(SecurityType.Index)]
        [TestCase(SecurityType.Crypto)]
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain(nameof(ClearStreetBrokerageModel)));
        }

        [TestCase(SecurityType.Equity, OrderType.MarketOnClose)]
        [TestCase(SecurityType.Equity, OrderType.MarketOnOpen)]
        [TestCase(SecurityType.Equity, OrderType.LimitIfTouched)]
        [TestCase(SecurityType.Equity, OrderType.ComboMarket)]
        [TestCase(SecurityType.Equity, OrderType.ComboLimit)]
        [TestCase(SecurityType.Option, OrderType.MarketOnClose)]
        [TestCase(SecurityType.Option, OrderType.ComboLimit)]
        [TestCase(SecurityType.IndexOption, OrderType.MarketOnOpen)]
        [TestCase(SecurityType.IndexOption, OrderType.ComboMarket)]
        public void CanSubmitOrderUnsupportedOrderTypeReturnsFalse(SecurityType securityType, OrderType orderType)
        {
            var security = GetSecurityForType(securityType);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Message, Does.Contain(orderType.ToString()));
        }

        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.IndexOption)]
        public void CanUpdateOrderReturnsTrue(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new LimitOrder(security.Symbol, 1m, 100m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new UpdateOrderFields { LimitPrice = 99m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.That(canUpdate, Is.True);
            Assert.That(message, Is.Null);
        }

        [Test]
        public void CreateReturnsClearStreetBrokerageModel()
        {
            var model = BrokerageModel.Create(new OrderProvider(), BrokerageName.ClearStreet, AccountType.Margin);

            Assert.That(model, Is.InstanceOf<ClearStreetBrokerageModel>());
            Assert.That(BrokerageModel.GetBrokerageName(model), Is.EqualTo(BrokerageName.ClearStreet));
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
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, 1m, 100m, 1m, false, DateTime.UtcNow);
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.LimitIfTouched:
                    return new LimitIfTouchedOrder(symbol, 1m, 105m, 100m, DateTime.UtcNow);
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
