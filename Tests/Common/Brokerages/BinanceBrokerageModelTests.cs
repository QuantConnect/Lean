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
using System;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BinanceBrokerageModelTests
    {
        private static readonly Symbol _btceur = Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Binance);

        protected virtual BinanceBrokerageModel BinanceBrokerageModel => new();

        [TestCase(0.01, true)]
        [TestCase(0.000009, false)]
        public void CanSubmitMarketOrder_OrderSizeIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            var order = new Mock<MarketOrder>();
            order.Setup(mock => mock.Quantity).Returns(orderQuantity);

            var security = GetSecurity();
            security.Cache.AddData(new Tick
            {
                AskPrice = 50001,
                BidPrice = 49999,
                Time = DateTime.UtcNow,
                Symbol = _btceur,
                TickType = TickType.Quote,
                AskSize = 1,
                BidSize = 1
            });

            Assert.AreEqual(isValidOrderQuantity, BinanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                var price = order.Object.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderSize(security, order.Object.Quantity, price), message.Message);
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

            var security = GetSecurity();
            Assert.AreEqual(isValidOrderQuantity, BinanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderSize(security, order.Object.Quantity, order.Object.LimitPrice), message.Message);
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

            var security = GetSecurity();
            Assert.AreEqual(isValidOrderQuantity, BinanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
            if (!isValidOrderQuantity)
            {
                Assert.AreEqual(Messages.DefaultBrokerageModel.InvalidOrderSize(security, order.Object.Quantity, Math.Min(order.Object.LimitPrice, order.Object.StopPrice)), message.Message);
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

            var security = GetSecurity();

            Assert.AreEqual(false, BinanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            Assert.NotNull(message);
        }

        [TestCase(nameof(BinanceBrokerageModel), SecurityType.Crypto, false)]
        [TestCase(nameof(BinanceUSBrokerageModel), SecurityType.Crypto, false)]
        [TestCase(nameof(BinanceFuturesBrokerageModel), SecurityType.CryptoFuture, true)]
        [TestCase(nameof(BinanceCoinFuturesBrokerageModel), SecurityType.CryptoFuture, true)]
        public void CannotSubmitStopMarketOrder_Always(string binanceBrokerageMode, SecurityType securityType, bool isCanSubmit)
        {
            var binanceBrokerageModel = binanceBrokerageMode switch
            {
                "BinanceBrokerageModel" => new BinanceBrokerageModel(),
                "BinanceUSBrokerageModel" => new BinanceUSBrokerageModel(),
                "BinanceFuturesBrokerageModel" => new BinanceFuturesBrokerageModel(AccountType.Margin),
                "BinanceCoinFuturesBrokerageModel" => new BinanceCoinFuturesBrokerageModel(AccountType.Margin),
                _ => throw new ArgumentException($"Invalid binanceBrokerageModel value: '{binanceBrokerageMode}'.")
            };

            var order = new Mock<StopMarketOrder>
            {
                Object =
                {
                    StopPrice = 3_000
                }
            };
            order.Setup(mock => mock.Quantity).Returns(1);


            var ETHUSDT = Symbol.Create("ETHUSDT", securityType, Market.Binance);

            var security = TestsHelpers.GetSecurity(securityType: ETHUSDT.SecurityType, symbol: ETHUSDT.Value, market: ETHUSDT.ID.Market, quoteCurrency: "USDT");

            Assert.AreEqual(isCanSubmit, binanceBrokerageModel.CanSubmitOrder(security, order.Object, out var message));
            if (isCanSubmit)
            {
                Assert.IsNull(message);
            }
            else
            {
                Assert.NotNull(message);
            }
        }

        [Test]
        public void Returns1m_IfCashAccount()
        {
            var security = GetSecurity();
            Assert.AreEqual(1m, new BinanceBrokerageModel(AccountType.Cash).GetLeverage(security));
        }

        [Test]
        public void ReturnsCashBuyinPowerModel_ForCashAccount()
        {
            var security = GetSecurity();
            Assert.IsInstanceOf<CashBuyingPowerModel>(new BinanceBrokerageModel(AccountType.Cash).GetBuyingPowerModel(security));
        }

        [Test]
        public void ReturnBinanceFeeModel()
        {
            var security = GetSecurity();
            Assert.IsInstanceOf<BinanceFeeModel>(BinanceBrokerageModel.GetFeeModel(security));
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

        private static Security GetSecurity()
        {
            return TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");
        }
    }
}
