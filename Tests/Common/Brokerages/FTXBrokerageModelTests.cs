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
            _brokerageModel = GetBrokerageModel();
            _symbol = Symbol.Create("ETHUSD", SecurityType.Crypto, Market);
        }

        protected Crypto Security =>
            new(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 1m),
                new Cash("ETH", 0, 0),
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

        protected virtual string Market => QuantConnect.Market.FTX;

        [Test]
        public void GetCashBuyingPowerModelTest()
        {
            var model = GetBrokerageModel(AccountType.Cash);
            Assert.IsInstanceOf<CashBuyingPowerModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(1, model.GetLeverage(Security));
        }

        [Test]
        public void GetSecurityMarginModelTest()
        {
            var model = GetBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<SecurityMarginModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(3M, model.GetLeverage(Security));
        }

        [Test]
        public virtual void GetFeeModelTest()
        {
            Assert.IsInstanceOf<FTXFeeModel>(_brokerageModel.GetFeeModel(Security));
        }

        [TestCase(SecurityType.Crypto)]
        public void ShouldReturnProperMarket(SecurityType securityType)
        {
            Assert.AreEqual(Market, _brokerageModel.DefaultMarkets[securityType]);
        }

        [TestCase(0.01, true)]
        [TestCase(0.00005, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            Assert.AreEqual(isValidOrderQuantity, _brokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(market: Market), order.Object, out _));
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

        [TestCase(-1, 100000)]
        [TestCase(1, 10000)]
        public void CannotSubmitStopMarketOrder(decimal quantity, decimal stopPrice)
        {
            var order = new Mock<StopMarketOrder>
            {
                Object =
                {
                    Quantity = quantity,
                    StopPrice =  stopPrice
                }
            };
            order.SetupGet(s => s.Type).Returns(OrderType.StopMarket);

            CannotSubmitStopOrder_WhenPriceMissingMarketPrice(order.Object);
        }

        [TestCase(-1, 100000)]
        [TestCase(1, 10000)]
        public void CannotSubmitStopLimitOrder(decimal quantity, decimal stopPrice)
        {
            var order = new Mock<StopLimitOrder>
            {
                Object =
                {
                    Quantity = quantity,
                    StopPrice =  stopPrice
                }
            };
            order.SetupGet(s => s.Type).Returns(OrderType.StopLimit);


            CannotSubmitStopOrder_WhenPriceMissingMarketPrice(order.Object);
        }

        private void CannotSubmitStopOrder_WhenPriceMissingMarketPrice(Order order)
        {
            var security = TestsHelpers.GetSecurity(symbol: _symbol.Value, market: _symbol.ID.Market, quoteCurrency: "USD");

            security.Cache.AddData(new Tick
            {
                AskPrice = 50001,
                BidPrice = 49999,
                Time = DateTime.UtcNow,
                Symbol = _symbol,
                TickType = TickType.Quote,
                AskSize = 1,
                BidSize = 1
            });

            Assert.AreEqual(false, _brokerageModel.CanSubmitOrder(security, order, out var message));
            Assert.NotNull(message);
        }

        [TestCase(OrderType.StopMarket)]
        [TestCase(OrderType.StopLimit)]
        public void CannotSubmitMarketOrder_IfPriceNotInitialized(OrderType orderType)
        {
            var order = new Mock<StopLimitOrder>
            {
                Object =
                {
                    Quantity = 1,
                    StopPrice =  100
                }
            };
            order.SetupGet(s => s.Type).Returns(orderType);

            var security = TestsHelpers.GetSecurity(symbol: _symbol.Value, market: _symbol.ID.Market, quoteCurrency: "USD");

            Assert.AreEqual(false, _brokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.NotNull(message);
        }

        protected virtual FTXBrokerageModel GetBrokerageModel(AccountType accountType = AccountType.Margin) => new(accountType);
    }
}
