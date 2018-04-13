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
using QuantConnect.Tests.Brokerages.GDAX;
using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class GDAXBrokerageModelTests
    {
        private readonly GDAXBrokerageModel _unit = new GDAXBrokerageModel();

        [Test]
        public void GetLeverageTest()
        {
            Assert.AreEqual(1, _unit.GetLeverage(GDAXTestsHelpers.GetSecurity()));
        }

        [Test]
        public void GetFeeModelTest()
        {
            Assert.IsInstanceOf<GDAXFeeModel>(_unit.GetFeeModel(GDAXTestsHelpers.GetSecurity()));
        }

        [Test]
        public void GetBuyingPowerModelTest()
        {
            Assert.IsInstanceOf<CashBuyingPowerModel>(_unit.GetBuyingPowerModel(GDAXTestsHelpers.GetSecurity(), AccountType.Cash));
        }

        [Test]
        public void CanUpdateOrderTest()
        {
            BrokerageMessageEvent message;
            Assert.AreEqual(false, _unit.CanUpdateOrder(GDAXTestsHelpers.GetSecurity(), Mock.Of<Order>(),
                new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields()), out message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CanSubmitOrder_WhenBrokerageIdIsCorrect(bool isUpdate)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();
            order.Object.Quantity = 10.0m;

            if (isUpdate)
            {
                order.Object.BrokerId = new List<string>() {"abc123"};
            }

            Assert.AreEqual(!isUpdate, _unit.CanSubmitOrder(GDAXTestsHelpers.GetSecurity(), order.Object, out message));
        }

        [TestCase(0.01, true)]
        [TestCase(0.00009, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();

            order.Object.Quantity = orderQuantity;

            Assert.AreEqual(isValidOrderQuantity, _unit.CanSubmitOrder(GDAXTestsHelpers.GetSecurity(), order.Object, out message));
        }

        [TestCase(SecurityType.Crypto, true)]
        [TestCase(SecurityType.Option, false)]
        [TestCase(SecurityType.Cfd, false)]
        [TestCase(SecurityType.Forex, false)]
        [TestCase(SecurityType.Future, false)]
        [TestCase(SecurityType.Equity, false)]
        public void CanOnlySubmitCryptoOrders(SecurityType securityType, bool isValidSecurityType)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();
            order.Object.Quantity = 10.0m;

            Assert.AreEqual(isValidSecurityType, _unit.CanSubmitOrder(GDAXTestsHelpers.GetSecurity(1.0m, securityType), order.Object, out message));
        }

        [TestCase(OrderType.Market, true)]
        [TestCase(OrderType.Limit, true)]
        [TestCase(OrderType.MarketOnClose, false)]
        [TestCase(OrderType.MarketOnOpen, false)]
        [TestCase(OrderType.StopLimit, false)]
        [TestCase(OrderType.StopMarket, true)]
        public void CanSubmit_CertainOrderTypes(OrderType orderType, bool isValidOrderType)
        {
            BrokerageMessageEvent message;
            var security = GDAXTestsHelpers.GetSecurity();
            var order = Order.CreateOrder(new SubmitOrderRequest(orderType, SecurityType.Crypto, security.Symbol, 10.0m, 1.0m, 10.0m, DateTime.Now, "Test Order"));

            Assert.AreEqual(isValidOrderType, _unit.CanSubmitOrder(security, order, out message));
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForTakerMarketOrder()
        {
            var security = GDAXTestsHelpers.GetSecurity();
            security.FeeModel = new GDAXFeeModel();
            security.SetMarketPrice(new TradeBar { Symbol = security.Symbol, Close = 5000m });
            var orderFee = security.FeeModel.GetOrderFee(security, new MarketOrder(security.Symbol, 1, DateTime.MinValue));

            Assert.AreEqual(12.5m, orderFee);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForMakerLimitOrdersTickResolution()
        {
            var security = GDAXTestsHelpers.GetSecurity(resolution: Resolution.Tick);
            security.FeeModel = new GDAXFeeModel();
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 5000m });

            var orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 4999.99m, DateTime.MinValue)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(0, orderFee);

            security.SetMarketPrice(new Tick { Symbol = security.Symbol, BidPrice = 5000m, AskPrice = 5000.01m });
            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 5000m, DateTime.MinValue)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(0, orderFee);

            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, -1, 5000.01m, DateTime.MinValue)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(0, orderFee);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForTakerLimitOrdersTickResolution()
        {
            var security = GDAXTestsHelpers.GetSecurity(resolution: Resolution.Tick);
            security.FeeModel = new GDAXFeeModel();
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 5000m });
            var orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 5000.01m, DateTime.MinValue)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            // marketable buy limit fill at 5000
            Assert.AreEqual(12.5m, orderFee);

            security.SetMarketPrice(new Tick { Symbol = security.Symbol, BidPrice = 5000m, AskPrice = 5000.01m });
            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 5000.01m, DateTime.MinValue)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            // marketable buy limit fill at 5000.01
            Assert.AreEqual(12.500025m, orderFee);

            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, -1, 5000m, DateTime.MinValue)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            // marketable sell limit fill at 5000
            Assert.AreEqual(12.5m, orderFee);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForMakerLimitOrdersMinuteResolution()
        {
            var time = new DateTime(2018, 4, 10);
            var security = GDAXTestsHelpers.GetSecurity();

            security.FeeModel = new GDAXFeeModel();
            security.SetMarketPrice(new TradeBar { Symbol = security.Symbol, Close = 5000m, EndTime = time.AddSeconds(75) });

            var orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 4999.99m, time)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(0, orderFee);

            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, -1, 5000.01m, time)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(0, orderFee);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForTakerLimitOrdersMinuteResolution()
        {
            var time = new DateTime(2018, 4, 10);
            var security = GDAXTestsHelpers.GetSecurity();

            security.FeeModel = new GDAXFeeModel();
            security.SetMarketPrice(new TradeBar { Symbol = security.Symbol, Close = 5000m, EndTime = time.AddMinutes(1) });

            var orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 5000m, time)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(12.5m, orderFee);

            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, 1, 5050m, time)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(12.5m, orderFee);

            orderFee = security.FeeModel.GetOrderFee(security, new LimitOrder(security.Symbol, -1, 4950m, time)
            {
                OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
            });

            Assert.AreEqual(12.5m, orderFee);
        }

        [Test]
        public void ThrowsWhenCalledWithMarginAccountType()
        {
            Assert.Throws<Exception>(() =>
            {
                new GDAXBrokerageModel(AccountType.Margin);
            }, "The GDAX brokerage does not currently support Margin trading.");
        }
    }
}