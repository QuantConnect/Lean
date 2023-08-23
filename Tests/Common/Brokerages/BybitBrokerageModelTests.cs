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

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BybitBrokerageModelTests
    {
        private static readonly Symbol BTCUSDT = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bybit);
        private Security _security;

        protected virtual BybitBrokerageModel BybitBrokerageModel => new();

        [SetUp]
        public void Init()
        {
            _security = TestsHelpers.GetSecurity(symbol: BTCUSDT.Value, market: BTCUSDT.ID.Market, quoteCurrency: "USDT");
        }

        [TestCase(0.01, true)]
        [TestCase(0.000001, false)]
        public void CanSubmitMarketOrder_OrderSizeIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            var order = new Mock<MarketOrder>
            {
                Object =
                {
                    Quantity = orderQuantity
                }
            };

            _security.Cache.AddData(new Tick
            {
                AskPrice = 50001,
                BidPrice = 49999,
                Time = DateTime.UtcNow,
                Symbol = BTCUSDT,
                TickType = TickType.Quote,
                AskSize = 1,
                BidSize = 1
            });

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                var price = order.Object.Direction == OrderDirection.Buy ? _security.AskPrice : _security.BidPrice;
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_security, order.Object.Quantity), message.Message);
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
                    Quantity = orderQuantity,
                    LimitPrice = limitPrice
                }
            };

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_security, order.Object.Quantity), message.Message);
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
                    Quantity = orderQuantity,
                    StopPrice = stopPrice,
                    LimitPrice = limitPrice
                }
            };

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_security, order.Object.Quantity), message.Message);
            }
        }

        [Test]
        public void CanSubmitMarketOrder_IfPriceNotInitialized()
        {
            var order = new Mock<MarketOrder>
            {
                Object =
                {
                    Quantity = 1
                }
            };

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
                    Quantity = orderQuantity,
                    StopPrice = stopPrice
                }
            };

            var security = TestsHelpers.GetSecurity(symbol: BTCUSDT.Value, market: BTCUSDT.ID.Market, quoteCurrency: "USDT");

            Assert.AreEqual(isValidOrderQuantity, BybitBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_security, order.Object.Quantity),message.Message);
            }
        }

        [Test]
        public void Returns1m_IfCashAccount()
        {
            Assert.AreEqual(1m, new BybitBrokerageModel(AccountType.Cash).GetLeverage(_security));
        }

        [Test]
        public void ReturnsCashBuyinPowerModel_ForCashAccount()
        {
            Assert.IsInstanceOf<CashBuyingPowerModel>(new BybitBrokerageModel(AccountType.Cash).GetBuyingPowerModel(_security));
        }

        [Test]
        public void ReturnBybitFeeModel()
        {
            Assert.IsInstanceOf<BybitFeeModel>(BybitBrokerageModel.GetFeeModel(_security));
        }

        [Test]
        public virtual void CryptoMapped()
        {
            var defaultMarkets = BybitBrokerageModel.DefaultMarkets;
            Assert.AreEqual(Market.Bybit, defaultMarkets[SecurityType.Crypto]);
        }

        [Test]
        public void CannotUpdateOrder_IfSecurityIsNotCryptoFuture()
        {

            var order = new Mock<MarketOrder>
            {
                Object =
                {
                    Id = 1,
                    Quantity = 1,
                }
            };

            var updateRequest = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields { Quantity = 2 });
            
            Assert.AreEqual(false,BybitBrokerageModel.CanUpdateOrder(_security, order.Object, updateRequest, out var message));
            Assert.AreEqual(message.Message, Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
        }

     

        [TestFixture]
        public class Margin
        {

            private readonly Symbol _btcusdt = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bybit);
            private Security _security;
            private BybitBrokerageModel _bybitBrokerageModel = new(AccountType.Margin);

            [SetUp]
            public void Init()
            {
                _security = TestsHelpers.GetSecurity(symbol: _btcusdt.Value, market: _btcusdt.ID.Market, quoteCurrency: "USDT");
            }

            [Test]
            public void ReturnsSecurityMarginModel_ForMarginAccount()
            {
                Assert.IsInstanceOf<SecurityMarginModel>(_bybitBrokerageModel.GetBuyingPowerModel(_security));
            }

            [Test]
            public virtual void Returns10m_IfMarginAccount()
            {
                Assert.AreEqual(10m, _bybitBrokerageModel.GetLeverage(_security));
            }
        }
    }

    [TestFixture, Parallelizable(ParallelScope.All)]

    public class BybitFuturesBrokerageModelTests 
    {
        private static readonly Symbol BTCUSDT = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.Bybit);
        private Security _security;

        protected virtual BybitBrokerageModel BybitBrokerageModel => new BybitFuturesBrokerageModel();
        
        [SetUp]
        public void Init()
        {
            _security = TestsHelpers.GetSecurity(symbol: BTCUSDT.Value, market: BTCUSDT.ID.Market, quoteCurrency: "USDT");
        }
        
        [Test]
        public void ReturnBybitFuturesFeeModel()
        {
            Assert.IsInstanceOf<BybitFuturesFeeModel>(BybitBrokerageModel.GetFeeModel(_security));
        }
        
        [TestCase(OrderStatus.Canceled, false)]
        [TestCase(OrderStatus.Filled, false)]
        [TestCase(OrderStatus.Invalid, false)]
        [TestCase(OrderStatus.New, true)]
        [TestCase(OrderStatus.Submitted, false)]
        [TestCase(OrderStatus.None, false)]
        [TestCase(OrderStatus.CancelPending, false)]
        [TestCase(OrderStatus.PartiallyFilled, true)]
        [TestCase(OrderStatus.UpdateSubmitted, false)]
        public void CannotUpdateOrder_IfWrongOrderStatus(OrderStatus status, bool canUpdate)
        {
            var order = new LimitOrder(BTCUSDT, 1, 1000, DateTime.Now) { Status = status };
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields());

            Assert.AreEqual(canUpdate,BybitBrokerageModel.CanUpdateOrder(_security, order, request, out var message));
            if (!canUpdate)
            {
                Assert.AreEqual("NotSupported",message.Code);
            }

        }

        [TestCase(0.1, true)]
        [TestCase(0.000001, false)]
        public void CanUpdateOrder_OrderSizeIsLargeEnough(decimal newOrderQuantity, bool isLargeEnough)
        {
            var order = new LimitOrder(BTCUSDT, 1, 1000, DateTime.Now) { Status = OrderStatus.New };
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields{Quantity = newOrderQuantity});
            
            Assert.AreEqual(isLargeEnough,BybitBrokerageModel.CanUpdateOrder(_security, order, request, out var message));
            if (!isLargeEnough)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderQuantity(_security,request.Quantity.Value), message.Message);
            }

        }
    }
}
