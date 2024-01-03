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
    public class BinanceBrokerageModelTests
    {
        private readonly Symbol _btceur = Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Binance);
        private Security _security;

        protected virtual BinanceBrokerageModel BinanceBrokerageModel => new();

        [SetUp]
        public void Init()
        {
            _security = TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");
        }

        [TestCase(0.01, true)]
        [TestCase(0.000009, false)]
        public void CanSubmitMarketOrder_OrderSizeIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            var order = new Mock<MarketOrder>();
            order.Setup(mock => mock.Quantity).Returns(orderQuantity);

            _security.Cache.AddData(new Tick
            {
                AskPrice = 50001,
                BidPrice = 49999,
                Time = DateTime.UtcNow,
                Symbol = _btceur,
                TickType = TickType.Quote,
                AskSize = 1,
                BidSize = 1
            });

            Assert.AreEqual(isValidOrderQuantity, BinanceBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                var price = order.Object.Direction == OrderDirection.Buy ? _security.AskPrice : _security.BidPrice;
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderSize(_security, order.Object.Quantity, price), message.Message);
            }
        }

        [TestCase(0.002, 5500, true)]
        [TestCase(0.003, 4500, true)]
        [TestCase(0.0002, 4500, false)]
        public void CanSubmitLimitOrder_OrderSizeIsLargeEnough(decimal orderQuantity, decimal limitPrice, bool isValidOrderQuantity)
        {
            var order = new Mock<LimitOrder>();
            order.Setup(mock => mock.Quantity).Returns(orderQuantity);
            order.Object.LimitPrice = limitPrice;

            Assert.AreEqual(isValidOrderQuantity, BinanceBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderSize(_security, order.Object.Quantity, order.Object.LimitPrice), message.Message);
            }
        }

        [TestCase(0.002, 5500, 5500, true)]
        [TestCase(0.001, 4500, 5500, false)]
        [TestCase(0.001, 5500, 4500, false)]
        [TestCase(0.003, 4500, 5500, true)]
        [TestCase(0.003, 5500, 4500, true)]
        public void CanSubmitStopLimitOrder_OrderSizeIsLargeEnough(decimal orderQuantity, decimal stopPrice, decimal limitPrice, bool isValidOrderQuantity)
        {
            var order = new Mock<StopLimitOrder>();
            order.Setup(mock => mock.Quantity).Returns(orderQuantity);
            order.Object.StopPrice = stopPrice;
            order.Object.LimitPrice = limitPrice;

            Assert.AreEqual(isValidOrderQuantity, BinanceBrokerageModel.CanSubmitOrder(_security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderSize(_security, order.Object.Quantity, Math.Min(order.Object.LimitPrice, order.Object.StopPrice)), message.Message);
            }
        }

        [Test]
        public void CannotSubmitMarketOrder_IfPriceNotInitialized()
        {
            var order = new Mock<MarketOrder>
            {
                Object =
                {
                    Quantity = 1
                }
            };

            var security = TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");

            Assert.AreEqual(false, BinanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.NotNull(message);
        }

        [Test]
        public void CannotSubmitStopMarketOrder_Always()
        {
            var order = new Mock<StopMarketOrder>
            {
                Object =
                {
                    Quantity = 100000,
                    StopPrice = 100000
                }
            };

            var security = TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");

            Assert.AreEqual(false, BinanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.NotNull(message);
        }

        [Test]
        public void Returns1m_IfCashAccount()
        {
            Assert.AreEqual(1m, new BinanceBrokerageModel(AccountType.Cash).GetLeverage(_security));
        }

        [Test]
        public void ReturnsCashBuyinPowerModel_ForCashAccount()
        {
            Assert.IsInstanceOf<CashBuyingPowerModel>(new BinanceBrokerageModel(AccountType.Cash).GetBuyingPowerModel(_security));
        }

        [Test]
        public void ReturnBinanceFeeModel()
        {
            Assert.IsInstanceOf<BinanceFeeModel>(BinanceBrokerageModel.GetFeeModel(_security));
        }

        [Test]
        public virtual void CryptoMapped()
        {
            var defaultMarkets = BinanceBrokerageModel.DefaultMarkets;
            Assert.AreEqual(Market.Binance, defaultMarkets[SecurityType.Crypto]);
        }

        [TestFixture]
        public class Margin
        {

            private readonly Symbol _btceur = Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Binance);
            private Security _security;
            private BinanceBrokerageModel _binanceBrokerageModel = new(AccountType.Margin);

            [SetUp]
            public void Init()
            {
                _security = TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");
            }

            [Test]
            public void ReturnsSecurityMarginModel_ForMarginAccount()
            {
                Assert.IsInstanceOf<SecurityMarginModel>(_binanceBrokerageModel.GetBuyingPowerModel(_security));
            }

            [Test]
            public virtual void Returns3m_IfMarginAccount()
            {
                Assert.AreEqual(3m, _binanceBrokerageModel.GetLeverage(_security));
            }
        }
    }
}
