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
    public class EtoroBrokerageModelTests
    {
        private readonly EtoroBrokerageModel _brokerageModel = new();

        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Crypto)]
        public void CanSubmitOrderMarketOrderReturnsTrue(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Index)]
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [TestCase(OrderType.Limit)]
        [TestCase(OrderType.StopMarket)]
        [TestCase(OrderType.StopLimit)]
        [TestCase(OrderType.MarketOnOpen)]
        [TestCase(OrderType.MarketOnClose)]
        [TestCase(OrderType.TrailingStop)]
        public void CanSubmitOrderUnsupportedOrderTypeReturnsFalse(OrderType orderType)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // A sell with no holdings opens a short position, which eToro accepts only with a positive stop loss rate.
        [TestCase(false, null, false)]
        [TestCase(true, null, false)]
        [TestCase(true, 0, false)]
        [TestCase(true, 150, true)]
        public void CanSubmitOrderShortSaleRequiresStopLossRate(bool hasEtoroProperties, decimal? stopLossRate, bool expectedCanSubmit)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var properties = hasEtoroProperties ? new EtoroOrderProperties { StopLossRate = stopLossRate } : null;
            var order = new MarketOrder(security.Symbol, -1m, DateTime.UtcNow, properties: properties);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            if (expectedCanSubmit)
            {
                Assert.That(message, Is.Null);
            }
            else
            {
                Assert.That(message.Message, Does.Contain(nameof(EtoroOrderProperties.StopLossRate)));
            }
        }

        // A sell within the long holdings closes units and needs no stop loss rate; a sell beyond them crosses zero and is refused.
        [TestCase(2, -1, true)]
        [TestCase(2, -2, true)]
        [TestCase(1, -2, false)]
        public void CanSubmitOrderSellAgainstLongHoldingsRefusesCrossZero(decimal holdingQuantity, decimal orderQuantity, bool expectedCanSubmit)
        {
            var equity = Symbols.AAPL;
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (equity, 209m, holdingQuantity))[equity];
            var order = new MarketOrder(equity, orderQuantity, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            Assert.That(message, expectedCanSubmit ? Is.Null : Is.Not.Null);
        }

        [Test]
        public void CanUpdateOrderReturnsFalse()
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new UpdateOrderFields { Quantity = 2m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.That(canUpdate, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [Test]
        public void GetFeeModelReturnsEtoroFeeModel()
        {
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(_brokerageModel.GetFeeModel(security), Is.InstanceOf<EtoroFeeModel>());
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}
