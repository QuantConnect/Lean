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
            order.Setup(x => x.Quantity).Returns(-100);
            order.Object.Properties.TimeInForce = TimeInForce.Day;
            var security = TestsHelpers.GetSecurity(securityType: SecurityType.Equity, symbol: "IBM", market: Market.USA);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 1000));
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
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 1000));
            security.Holdings.SetHoldings(6, 100);
            order.Object.Symbol = security.Symbol;
            Assert.IsTrue(_tradierBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
        }

        private Mock<Order> GetOrder()
        {
            var order = new Mock<Order>();
            order.Object.Symbol = _security.Symbol;
            return order;
        }
    }
}
