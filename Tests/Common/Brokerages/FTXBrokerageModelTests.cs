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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Brokerages;
using System;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class FTXBrokerageModelTests
    {
        private FTXBrokerageModel _brokerageModel;
        private Symbol _symbol;

        [SetUp]
        public void Init()
        {
            _brokerageModel = new();
            _symbol = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.FTX);
        }

        protected Crypto Security =>
            new(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    _symbol,
                    Resolution.Minute,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

        [Test]
        public void GetCashBuyingPowerModelTest()
        {
            var model = new FTXBrokerageModel(AccountType.Cash);
            Assert.IsInstanceOf<CashBuyingPowerModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(1, model.GetLeverage(Security));
        }

        [Test]
        public void GetSecurityMarginModelTest()
        {
            var model = new FTXBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<SecurityMarginModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(3M, model.GetLeverage(Security));
        }

        [Test]
        public void GetFeeModelTest()
        {
            Assert.IsInstanceOf<FTXFeeModel>(_brokerageModel.GetFeeModel(Security));
        }

        [TestCase(SecurityType.Crypto)]
        public void ShouldReturnFTXMarket(SecurityType securityType)
        {
            Assert.AreEqual(Market.FTX, _brokerageModel.DefaultMarkets[securityType]);
        }

        [TestCase(0.01, true)]
        [TestCase(0.00005, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();

            order.Object.Quantity = orderQuantity;

            Assert.AreEqual(isValidOrderQuantity, _brokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(market: Market.FTX), order.Object, out message));
        }

        [Test]
        public void CannotUpdateOrder()
        {
            var orderMock = new Mock<Order>();
            var order = orderMock.Object;
            order.Quantity = 0.01m;

            var updateRequestMock = new Mock<UpdateOrderRequest>(DateTime.UtcNow, 1, new UpdateOrderFields());

            Assert.False(_brokerageModel.CanUpdateOrder(
                TestsHelpers.GetSecurity(), 
                order, 
                updateRequestMock.Object, 
                out var message));
            Assert.NotNull(message);
        }
    }
}
