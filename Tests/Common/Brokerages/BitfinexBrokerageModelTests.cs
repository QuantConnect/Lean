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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BitfinexBrokerageModelTests
    {
        private readonly BitfinexBrokerageModel _bitfinexBrokerageModel = new BitfinexBrokerageModel();

        protected Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
        protected Crypto Security
        {
            get
            {
                return new Crypto(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash(Currencies.USD, 0, 1m),
                    new Cash("ETH", 0, 0),
                    new SubscriptionDataConfig(
                        typeof(TradeBar),
                        Symbol,
                        Resolution.Minute,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        false,
                        false,
                        false
                    ),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                );
            }
        }

        [Test]
        public void GetCashBuyingPowerModelTest()
        {
            BitfinexBrokerageModel model = new BitfinexBrokerageModel(AccountType.Cash);
            Assert.IsInstanceOf<CashBuyingPowerModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(1, model.GetLeverage(Security));
        }

        [Test]
        public void GetSecurityMarginModelTest()
        {
            BitfinexBrokerageModel model = new BitfinexBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<SecurityMarginModel>(model.GetBuyingPowerModel(Security));
            Assert.AreEqual(3.3M, model.GetLeverage(Security));
        }

        [Test]
        public void GetEquityLeverage_ThrowsArgumentException_Test()
        {
            var equity = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var model = new BitfinexBrokerageModel();
            Assert.Throws<ArgumentException>(() => model.GetLeverage(equity));
        }

        [Test]
        public void GetCustomDataLeverageTest()
        {
            var dummy = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    QuantConnect.Symbol.Create("DUMMY", SecurityType.Base, Market.Bitfinex),
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var model = new BitfinexBrokerageModel();
            Assert.AreEqual(1M, model.GetLeverage(dummy));
        }

        [Test]
        public void SetLeverage_ThrowsInvalidOperationException_Test()
        {
            Assert.Throws<InvalidOperationException>(() => Security.SetLeverage(2));
        }

        [Test]
        public void SetLeverage_ThrowsInvalidOperationException_BrokerageModelSecurityInitializer_Test()
        {
            var crypto = GetCrypto(Symbol);

            var brokerageInitializer = new BrokerageModelSecurityInitializer(
                new BitfinexBrokerageModel(AccountType.Cash),
                SecuritySeeder.Null);

            brokerageInitializer.Initialize(crypto);
            Assert.Throws<InvalidOperationException>(() => crypto.SetLeverage(2));
        }

        [Test]
        public void SetLeverage_DoesNotThrowInvalidOperationException_BrokerageModelSecurityInitializer_Test()
        {
            var crypto = GetCrypto(Symbol);

            var brokerageInitializer = new BrokerageModelSecurityInitializer(
                new BitfinexBrokerageModel(AccountType.Margin),
                SecuritySeeder.Null);

            brokerageInitializer.Initialize(crypto);
            Assert.DoesNotThrow(() => crypto.SetLeverage(2));
        }

        private Crypto GetCrypto(Symbol symbol)
        {
            return new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                new Cash(symbol.Value.RemoveFromEnd(Currencies.USD), 0, 0),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }

        [TestCase(0.01, true)]
        [TestCase(0.00003, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            Assert.AreEqual(isValidOrderQuantity, _bitfinexBrokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(market: Market.Bitfinex), order.Object, out message));
        }

        [TestCase(OrderType.Limit, true)]
        [TestCase(OrderType.Market, true)]
        [TestCase(OrderType.StopMarket, true)]
        [TestCase(OrderType.StopLimit, true)]
        [TestCase(OrderType.LimitIfTouched, false)]
        [TestCase(OrderType.MarketOnOpen, false)]
        [TestCase(OrderType.MarketOnClose, false)]
        [TestCase(OrderType.OptionExercise, false)]
        [TestCase(OrderType.ComboMarket, false)]
        [TestCase(OrderType.ComboLimit, false)]
        [TestCase(OrderType.ComboLegLimit, false)]
        [TestCase(OrderType.TrailingStop, false)]
        public void CanSubmitOrderValidatesSupportedAndUnsupportedOrderTypes(OrderType orderType, bool isSupported)
        {
            var security = TestsHelpers.GetSecurity(market: Market.Bitfinex);
            var order = new Mock<Order>();
            order.Setup(o => o.Type).Returns(orderType);
            order.Setup(o => o.Quantity).Returns(0.01m);
            var result = _bitfinexBrokerageModel.CanSubmitOrder(security, order.Object, out var message);

            // Verify correct handling of each order type
            Assert.AreEqual(isSupported, result);
        }
    }
}
