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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Tests.Brokerages;
using System;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BybitBrokerageModelTests
    {
        private static readonly Symbol BTCUSDT = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bybit);
        private static readonly Symbol BTCUSDT_Future = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.Bybit);
        private Security _crypto;
        private Security _cryptoFuture;

        protected virtual BybitBrokerageModel BybitBrokerageModel => new();

        [SetUp]
        public void Init()
        {
            _crypto = TestsHelpers.GetSecurity(symbol: BTCUSDT.Value,
                securityType: BTCUSDT.SecurityType,
                market: BTCUSDT.ID.Market,
                quoteCurrency: "USDT");
            _cryptoFuture = TestsHelpers.GetSecurity(symbol: BTCUSDT_Future.Value,
                securityType: BTCUSDT_Future.SecurityType,
                market: BTCUSDT_Future.ID.Market,
                quoteCurrency: "USDT");
        }

        [TestCase(0.01, true)]
        [TestCase(0.000001, false)]
        public void CanSubmitMarketOrder_OrderSizeIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            var order = new Mock<MarketOrder>();
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            _crypto.Cache.AddData(new Tick
            {
                AskPrice = 50001,
                BidPrice = 49999,
                Time = DateTime.UtcNow,
                Symbol = BTCUSDT,
                TickType = TickType.Quote,
                AskSize = 1,
                BidSize = 1
            });

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(_crypto, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_crypto, order.Object.Quantity), message.Message);
            }
        }

        [TestCase(0.01, 4500, true)]
        [TestCase(0.000001, 4500, false)]
        public void CanSubmitLimitOrder_OrderSizeIsLargeEnough(decimal orderQuantity, decimal limitPrice, bool isValidOrderQuantity)
        {
            var order = new Mock<LimitOrder>
            {
                Object =
                {
                    LimitPrice = limitPrice
                }
            };

            order.Setup(x => x.Quantity).Returns(orderQuantity);

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(_crypto, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_crypto, order.Object.Quantity), message.Message);
            }
        }

        [TestCase(0.002, 5500, 5500, true)]
        [TestCase(0.000001, 5500, 5500, false)]
        [TestCase(0.002, 5500, 4500, true)]
        [TestCase(0.003, 4500, 5500, true)]
        [TestCase(0.003, 5500, 4500, true)]
        public void CanSubmitStopLimitOrder_OrderSizeIsLargeEnough(decimal orderQuantity, decimal stopPrice, decimal limitPrice, bool isValidOrderQuantity)
        {
            var order = new Mock<StopLimitOrder>
            {
                Object =
                {
                    StopPrice = stopPrice,
                    LimitPrice = limitPrice
                }
            };
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(_crypto, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_crypto, order.Object.Quantity), message.Message);
            }
        }

        [Test]
        public void CanSubmitMarketOrder_IfPriceNotInitialized()
        {
            var order = new Mock<MarketOrder>();
            order.Setup(x => x.Quantity).Returns(1m);

            var security = TestsHelpers.GetSecurity(symbol: BTCUSDT.Value, market: BTCUSDT.ID.Market, quoteCurrency: "USDT");

            Assert.AreEqual(true, BybitBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.Null(message);
        }

        [TestCase(0.002, 5500,  true)]
        [TestCase(0.000001, 5500,  false)]

        [TestCase(0.002, 4500,  true)]
        [TestCase(0.002, 5500,  true)]
        [TestCase(0.003, 4500,  true)]
        [TestCase(0.003, 5500,  true)]
        public void CanSubmitStopMarketOrder_OrderSizeIsLargeEnough(decimal orderQuantity, decimal stopPrice, bool isValidOrderQuantity)
        {
            var order = new Mock<StopMarketOrder>
            {
                Object =
                {
                    StopPrice = stopPrice
                }
            };
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            var security = TestsHelpers.GetSecurity(symbol: BTCUSDT.Value, market: BTCUSDT.ID.Market, quoteCurrency: "USDT");

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_crypto, order.Object.Quantity),message.Message);
            }
        }

        [TestCase(SecurityType.Crypto, AccountType.Cash, ExpectedResult = 1)]
        [TestCase(SecurityType.Crypto, AccountType.Margin, ExpectedResult = 10)]
        [TestCase(SecurityType.CryptoFuture, AccountType.Cash, ExpectedResult = 1)]
        [TestCase(SecurityType.CryptoFuture, AccountType.Margin, ExpectedResult = 10)]
        public decimal ReturnsCorrectLeverage(SecurityType securityType, AccountType accountType)
        {
            var security = securityType == SecurityType.Crypto ? _crypto : _cryptoFuture;
            return new BybitBrokerageModel(accountType).GetLeverage(security);
        }

        [TestCase(SecurityType.Crypto, AccountType.Cash, typeof(CashBuyingPowerModel))]
        [TestCase(SecurityType.Crypto, AccountType.Margin, typeof(SecurityMarginModel))]
        [TestCase(SecurityType.CryptoFuture, AccountType.Cash, typeof(CryptoFutureMarginModel))]
        [TestCase(SecurityType.CryptoFuture, AccountType.Margin, typeof(CryptoFutureMarginModel))]
        public void ReturnsCorrectBuyingPowerModel(SecurityType securityType, AccountType accountType, Type expectedMarginModelType)
        {
            var security = securityType == SecurityType.Crypto ? _crypto : _cryptoFuture;
            Assert.IsInstanceOf(expectedMarginModelType, new BybitBrokerageModel(accountType).GetBuyingPowerModel(security));
        }

        [TestCase(SecurityType.Crypto, typeof(BybitFeeModel))]
        [TestCase(SecurityType.CryptoFuture, typeof(BybitFuturesFeeModel))]
        [TestCase(SecurityType.Base, typeof(ConstantFeeModel))]
        public void ReturnCorrectFeeModel(SecurityType securityType, Type expectedFeeModelType)
        {
            var security = securityType switch
            {
                SecurityType.Crypto => _crypto,
                SecurityType.CryptoFuture => _cryptoFuture,
                _ => TestsHelpers.GetSecurity(symbol: "BTCUSDT", securityType: SecurityType.Base, market: Market.Bybit, quoteCurrency: "USDT")
            };

            Assert.IsInstanceOf(expectedFeeModelType, BybitBrokerageModel.GetFeeModel(security));
        }

        [Test]
        public virtual void CryptoMapped()
        {
            var defaultMarkets = BybitBrokerageModel.DefaultMarkets;
            Assert.AreEqual(Market.Bybit, defaultMarkets[SecurityType.Crypto]);
            Assert.AreEqual(Market.Bybit, defaultMarkets[SecurityType.CryptoFuture]);
        }

        [TestCase(SecurityType.Crypto, false)]
        [TestCase(SecurityType.CryptoFuture, true)]
        public void CannotUpdateOrder_IfSecurityIsNotCryptoFuture(SecurityType securityType, bool canUpdate)
        {
            var order = new Mock<MarketOrder>
            {
                Object =
                {
                    Id = 1,
                    Quantity = 1,
                    Status = OrderStatus.Submitted
                }
            };

            var updateRequest = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields { Quantity = 2 });

            var security = securityType == SecurityType.Crypto ? _crypto : _cryptoFuture;
            Assert.AreEqual(canUpdate, BybitBrokerageModel.CanUpdateOrder(security, order.Object, updateRequest, out var message));

            if (!canUpdate)
            {
                Assert.AreEqual(message.Message, Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
            }
        }

        [TestCase(OrderStatus.New, true)]
        [TestCase(OrderStatus.Submitted, true)]
        [TestCase(OrderStatus.PartiallyFilled, true)]
        [TestCase(OrderStatus.UpdateSubmitted, true)]
        [TestCase(OrderStatus.Canceled, false)]
        [TestCase(OrderStatus.Filled, false)]
        [TestCase(OrderStatus.Invalid, false)]
        [TestCase(OrderStatus.None, false)]
        [TestCase(OrderStatus.CancelPending, false)]
        public void CannotUpdateOrder_IfWrongOrderStatus(OrderStatus status, bool canUpdate)
        {
            var order = new LimitOrder(BTCUSDT, 1, 1000, DateTime.Now) { Status = status };
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields());

            Assert.AreEqual(canUpdate, BybitBrokerageModel.CanUpdateOrder(_cryptoFuture, order, request, out var message));
            if (!canUpdate)
            {
                Assert.AreEqual("NotSupported", message.Code);
            }

        }

        [TestCase(0.1, true)]
        [TestCase(0.000001, false)]
        public void CanUpdateOrder_OrderSizeIsLargeEnough(decimal newOrderQuantity, bool isLargeEnough)
        {
            var order = new LimitOrder(BTCUSDT, 1, 1000, DateTime.Now) { Status = OrderStatus.New };
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields { Quantity = newOrderQuantity });

            Assert.AreEqual(isLargeEnough, BybitBrokerageModel.CanUpdateOrder(_cryptoFuture, order, request, out var message));
            if (!isLargeEnough)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_cryptoFuture, request.Quantity.Value), message.Message);
            }

        }

        [TestFixture]
        public class Margin
        {

            private readonly Symbol _btcusdt = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bybit);
            private Security _security;
            private static BybitBrokerageModel BybitBrokerageModel => new(AccountType.Margin);

            [SetUp]
            public void Init()
            {
                _security = TestsHelpers.GetSecurity(symbol: _btcusdt.Value, market: _btcusdt.ID.Market, quoteCurrency: "USDT");
            }

            [Test]
            public void ReturnsSecurityMarginModel_ForMarginAccount()
            {
                Assert.IsInstanceOf<SecurityMarginModel>(BybitBrokerageModel.GetBuyingPowerModel(_security));
            }

            [Test]
            public virtual void Returns10m_IfMarginAccount()
            {
                Assert.AreEqual(10m, BybitBrokerageModel.GetLeverage(_security));
            }
        }
    }
}
