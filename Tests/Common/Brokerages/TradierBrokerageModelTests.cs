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

using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Orders;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Data.Market;
using System;
using QuantConnect.Orders.TimeInForces;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class TradierBrokerageModelTests
    {
        private TradierBrokerageModel _tradierBrokerageModel = new TradierBrokerageModel();
        private Security _security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "IBM", market: Market.USA);

        [SetUp]
        public void Init()
        {
            _security.SetMarketPrice(new TradeBar(new DateTime(2025, 05, 28, 10, 0, 0), _security.Symbol, 1, 1, 1, 1, 100, TimeSpan.FromMinutes(1)));
            _security.Holdings.SetHoldings(1, 100);
        }

        [Test]
        public void CanSubmitOrderReturnsFalseWhenShortGTCOrder()
        {
            var order = GetOrder();
            order.Setup(x => x.Quantity).Returns(-101);
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ShortOrderIsGtc", "You cannot place short stock orders with GTC, only day orders are allowed");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [Test]
        public void CanSubmitOrderReturnsFalseWhenSellShortOrderLastPriceBelow5()
        {
            var order = GetOrder();
            order.Setup(x => x.Quantity).Returns(-101);
            order.Object.Properties.TimeInForce = TimeInForce.Day;
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "SellShortOrderLastPriceBelow5", "Sell Short order cannot be placed for stock priced below $5");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [Test]
        public void CanSubmitOrderReturnsFalseWhenTimeInForceIsGoodTilDate()
        {
            var order = GetOrder();
            order.Setup(x => x.Quantity).Returns(101);
            order.Object.Properties.TimeInForce = TimeInForce.GoodTilDate(new DateTime());
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", $"This model only supports orders with the following time in force types: {typeof(DayTimeInForce)} and {typeof(GoodTilCanceledTimeInForce)}");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [TestCase(0.5)]
        [TestCase(10000001)]
        public void CanSubmitOrderReturnsFalseWhenIncorrectOrderQuantity(decimal quantity)
        {
            var order = GetOrder();
            order.Object.Properties.TimeInForce = TimeInForce.Day;
            order.Setup(x => x.Quantity).Returns(quantity);
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "IncorrectOrderQuantity", "Quantity should be between 1 and 10,000,000");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [Test]
        public void CanSubmitOrderReturnsTrueQuantityIsValidAndNotGTC()
        {
            var order = GetOrder();
            order.Setup(x => x.Quantity).Returns(-100);
            order.Object.Properties.TimeInForce = TimeInForce.Day;
            Assert.IsTrue(_tradierBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
        }

        [Test]
        public void CanSubmitOrderReturnsTrueWhenQuantityIsValidAndNotGTCAndPriceAbove5()
        {
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(-101);
            order.Object.Properties.TimeInForce = TimeInForce.Day;
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "IBM", market: Market.USA);
            security.SetMarketPrice(new TradeBar(new DateTime(2025, 05, 28, 10, 0, 0), security.Symbol, 100, 100, 100, 100, 100));
            security.Holdings.SetHoldings(6, 100);
            order.Object.Symbol = security.Symbol;
            Assert.IsTrue(_tradierBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
        }

        [Test]
        public void CanSubmitOrderReturnsTrueWhenQuantityIsValidIsMarketOrderAndPriceAbove5()
        {
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(-100);
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "IBM", market: Market.USA);
            security.SetMarketPrice(new TradeBar(new DateTime(2025, 05, 28, 10, 0, 0), security.Symbol, 100, 100, 100, 100, 100));
            security.Holdings.SetHoldings(6, 100);
            order.Object.Symbol = security.Symbol;
            Assert.IsTrue(_tradierBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
        }

        private static IEnumerable<TestCaseData> ExtendedHoursTestCases
        {
            get
            {
                var preMarketTime = new DateTime(2025, 05, 28, 8, 0, 0);
                var postMarketTime = new DateTime(2025, 05, 28, 19, 0, 0);

                foreach (var time in new[] { preMarketTime, postMarketTime })
                {
                    var equity = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "IBM",
                        market: Market.USA, marketAlwaysOpen: false);
                    equity.SetMarketPrice(new TradeBar(time, equity.Symbol, 100, 100, 100, 100, 100));

                    yield return new TestCaseData(time, equity, OrderType.Limit, true);
                    yield return new TestCaseData(time, equity, OrderType.Market, false);
                    yield return new TestCaseData(time, equity, OrderType.StopMarket, false);
                    yield return new TestCaseData(time, equity, OrderType.StopLimit, false);

                    var option = TestsHelpers.GetSecurity(securityType: SecurityType.Option, symbol: "IBM",
                        market: Market.USA, marketAlwaysOpen: false);
                    option.SetMarketPrice(new TradeBar(time, option.Symbol, 100, 100, 100, 100, 100));

                    yield return new TestCaseData(time, option, OrderType.Limit, false);
                    yield return new TestCaseData(time, option, OrderType.Market, false);
                    yield return new TestCaseData(time, option, OrderType.StopMarket, false);
                    yield return new TestCaseData(time, option, OrderType.StopLimit, false);
                }
            }
        }

        [TestCaseSource(nameof(ExtendedHoursTestCases))]
        public void CanSubmitOrderOnExtendedHours(DateTime time, Security security, OrderType orderType, bool expectedResult)
        {
            var orderProperties = new TradierOrderProperties { OutsideRegularTradingHours = true };
            Order order = orderType switch
            {
                OrderType.Market => new MarketOrder(security.Symbol, 100, time, properties: orderProperties),
                OrderType.Limit => new LimitOrder(security.Symbol, 100, 100, time, properties: orderProperties),
                OrderType.StopMarket => new StopMarketOrder(security.Symbol, 100, 100, time, properties: orderProperties),
                OrderType.StopLimit => new StopLimitOrder(security.Symbol, 100, 100, 100, time, properties: orderProperties),
                _ => throw new ArgumentException($"Unsupported order type: {orderType}", nameof(orderType))
            };

            Assert.AreEqual(expectedResult, _tradierBrokerageModel.CanSubmitOrder(security, order, out var message));

            if (!expectedResult)
            {
                var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ExtendedMarket",
                    Messages.TradierBrokerageModel.ExtendedMarketHoursTradingNotSupportedOutsideExtendedSession);
                Assert.AreEqual(expectedMessage.Message, message.Message);
            }
        }

        private Mock<Order> GetOrder()
        {
            var order = new Mock<Order>();
            order.Object.Symbol = _security.Symbol;
            return order;
        }
    }
}
