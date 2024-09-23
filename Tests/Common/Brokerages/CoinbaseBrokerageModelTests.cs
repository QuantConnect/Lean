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
using QuantConnect.Tests.Brokerages;
using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class CoinbaseBrokerageModelTests
    {
        private readonly CoinbaseBrokerageModel _coinbaseBrokerageModel = new CoinbaseBrokerageModel();

        [Test]
        public void GetLeverageTest()
        {
            Assert.AreEqual(1, _coinbaseBrokerageModel.GetLeverage(TestsHelpers.GetSecurity()));
        }

        [Test]
        public void GetFeeModelTest()
        {
            Assert.IsInstanceOf<CoinbaseFeeModel>(_coinbaseBrokerageModel.GetFeeModel(TestsHelpers.GetSecurity()));
        }

        [Test]
        public void GetBuyingPowerModelTest()
        {
            Assert.IsInstanceOf<CashBuyingPowerModel>(_coinbaseBrokerageModel.GetBuyingPowerModel(TestsHelpers.GetSecurity()));
        }

        [Test]
        public void CanUpdateOrderTest()
        {
            BrokerageMessageEvent message;
            Assert.AreEqual(false, _coinbaseBrokerageModel.CanUpdateOrder(TestsHelpers.GetSecurity(), Mock.Of<Order>(),
                new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields()), out message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CanSubmitOrder_WhenBrokerageIdIsCorrect(bool isUpdate)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(10.0m);

            if (isUpdate)
            {
                order.Object.BrokerId = new List<string>() {"abc123"};
            }

            Assert.AreEqual(!isUpdate, _coinbaseBrokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(), order.Object, out message));
        }

        [TestCase(0.01, true)]
        [TestCase(0.0000000015, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            Assert.AreEqual(isValidOrderQuantity, _coinbaseBrokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(market: Market.Coinbase), order.Object, out message));
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
            order.Setup(x => x.Quantity).Returns(10.0m);

            Assert.AreEqual(isValidSecurityType, _coinbaseBrokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(1.0m, securityType), order.Object, out message));
        }

        [TestCase(OrderType.Market, 2019, 2, 1, 0, 0, 0, true)]
        [TestCase(OrderType.Limit, 2019, 2, 1, 0, 0, 0, true)]
        [TestCase(OrderType.MarketOnClose, 2019, 2, 1, 0, 0, 0, false)]
        [TestCase(OrderType.MarketOnOpen, 2019, 2, 1, 0, 0, 0, false)]
        [TestCase(OrderType.StopLimit, 2019, 2, 1, 0, 0, 0, true)]
        [TestCase(OrderType.StopMarket, 2019, 2, 1, 0, 0, 0, true)]
        [TestCase(OrderType.StopMarket, 2019, 3, 23, 0, 59, 59, true)]
        [TestCase(OrderType.StopMarket, 2019, 3, 23, 1, 0, 0, false)]
        public void CanSubmit_CertainOrderTypes(OrderType orderType, int year, int month, int day, int hour, int minute, int second, bool isValidOrderType)
        {
            var utcTime = new DateTime(year, month, day, hour, minute, second);

            BrokerageMessageEvent message;
            var security = TestsHelpers.GetSecurity();
            var order = Order.CreateOrder(new SubmitOrderRequest(orderType, SecurityType.Crypto, security.Symbol, 10.0m, 1.0m, 10.0m, utcTime, "Test Order"));

            Assert.AreEqual(isValidOrderType, _coinbaseBrokerageModel.CanSubmitOrder(security, order, out message));
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForTakerMarketOrder()
        {
            var security = TestsHelpers.GetSecurity();
            security.FeeModel = new CoinbaseFeeModel();
            security.SetMarketPrice(new TradeBar { Symbol = security.Symbol, Close = 5000m });
            var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security,
                new MarketOrder(security.Symbol, 1, DateTime.MinValue)));

            Assert.AreEqual(15m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForMakerLimitOrdersTickResolution()
        {
            var security = TestsHelpers.GetSecurity(resolution: Resolution.Tick);
            security.FeeModel = new CoinbaseFeeModel();
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 5000m });

            var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 4999.99m, DateTime.MinValue)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(0, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            security.SetMarketPrice(new Tick { Symbol = security.Symbol, BidPrice = 5000m, AskPrice = 5000.01m, TickType = TickType.Quote });
            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 5000m, DateTime.MinValue)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(0, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, -1, 5000.01m, DateTime.MinValue)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(0, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForTakerLimitOrdersTickResolution()
        {
            var security = TestsHelpers.GetSecurity(resolution: Resolution.Tick);
            security.FeeModel = new CoinbaseFeeModel();
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 5000m });
            var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 5000.01m, DateTime.MinValue)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            // marketable buy limit fill at 5000
            Assert.AreEqual(15m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            security.SetMarketPrice(new Tick { Symbol = security.Symbol, BidPrice = 5000m, AskPrice = 5000.01m, TickType = TickType.Quote });
            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 5000.01m, DateTime.MinValue)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            // marketable buy limit fill at 5000.01
            Assert.AreEqual(15.00003m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, -1, 5000m, DateTime.MinValue)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            // marketable sell limit fill at 5000
            Assert.AreEqual(15m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForMakerLimitOrdersMinuteResolution()
        {
            var time = new DateTime(2018, 4, 10);
            var security = TestsHelpers.GetSecurity();

            security.FeeModel = new CoinbaseFeeModel();
            security.SetMarketPrice(new TradeBar { Symbol = security.Symbol, Close = 5000m, EndTime = time.AddSeconds(75) });

            var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 4999.99m, time)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(0, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, -1, 5000.01m, time)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(0, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);
        }

        [Test]
        public void FeeModelReturnsCorrectOrderFeeForTakerLimitOrdersMinuteResolution()
        {
            var time = new DateTime(2018, 4, 10);
            var security = TestsHelpers.GetSecurity();

            security.FeeModel = new CoinbaseFeeModel();
            security.SetMarketPrice(new TradeBar { Symbol = security.Symbol, Close = 5000m, EndTime = time.AddMinutes(1) });

            var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 5000m, time)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(15m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, 1, 5050m, time)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(15m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);

            orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(
                security, new LimitOrder(security.Symbol, -1, 4950m, time)
                {
                    OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, security.Price)
                }));

            Assert.AreEqual(15m, orderFee.Value.Amount);
            Assert.AreEqual(Currencies.USD, orderFee.Value.Currency);
        }

        [Test]
        public void ThrowsWhenCalledWithMarginAccountType()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new CoinbaseBrokerageModel(AccountType.Margin);
            }, "The Coinbase brokerage does not currently support Margin trading.");
        }
    }
}
