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
            order.Quantity = -101;
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "ShortOrderIsGtc", "You cannot place short stock orders with GTC, only day orders are allowed");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [Test]
        public void CanSubmitOrderReturnsFalseWhenSellShortOrderLastPriceBelow5()
        {
            var order = GetOrder();
            order.Quantity = -101;
            order.Properties.TimeInForce = TimeInForce.Day;
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "SellShortOrderLastPriceBelow5", "Sell Short order cannot be placed for stock priced below $5");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [Test]
        public void CanSubmitOrderReturnsFalseWhenMarketOrderIsGtc()
        {
            var order = GetOrder();
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "MarketOrderIsGtc", "You cannot place market orders with GTC, only day orders are allowed");
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        [Test]
        public void CanSubmitOrderReturnsFalseWhenIncorrectOrderQuantity()
        {
            var order = GetOrder();
            order.Properties.TimeInForce = TimeInForce.Day;
            order.Quantity = 0.5m;
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order, out var message));
            var expectedMessage = new BrokerageMessageEvent(BrokerageMessageType.Warning, "IncorrectOrderQuantity", "Quantity should be between 1 and 10,000,000");
            Assert.AreEqual(expectedMessage.Message, message.Message);

            order.Quantity = 10000001;
            Assert.IsFalse(_tradierBrokerageModel.CanSubmitOrder(_security, order, out message));
            Assert.AreEqual(expectedMessage.Message, message.Message);
        }

        private Order GetOrder()
        {
            var order = new Mock<Order>();
            order.Object.Symbol = _security.Symbol;
            return order.Object;
        }
    }
}
