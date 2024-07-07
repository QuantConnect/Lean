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

using System;
using Moq;
using NUnit.Framework;
using QLNet;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.ToolBox.RandomDataGenerator;
using Cash = QuantConnect.Securities.Cash;
using Option = QuantConnect.Securities.Option.Option;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class OptionPriceModelPriceGeneratorTests
    {
        private Security _underlying;
        private Option _option;

        public OptionPriceModelPriceGeneratorTests()
        {
            _underlying = new Security(
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
                new Cash("USD", 0, 1m),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var optionSymbol = Symbol.CreateOption(
                _underlying.Symbol,
                _underlying.Symbol.ID.Market,
                _underlying.Symbol.SecurityType.DefaultOptionStyle(),
                OptionRight.Call,
                20,
                new DateTime(2022, 1, 1)
            );

            _option = new Option(
                optionSymbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 1m),
                new OptionSymbolProperties(_underlying.SymbolProperties),
                new CashBook(),
                new RegisteredSecurityDataTypesProvider(),
                new OptionCache(),
                _underlying
            );
        }

        [Test]
        public void ThrowsIfSecurityIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new OptionPriceModelPriceGenerator(null);
            });
        }

        [Test]
        public void ThrowsIfSecurityIsNotOption()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new OptionPriceModelPriceGenerator(_underlying);
            });
        }

        [Test]
        public void ReturnsNewPrice()
        {
            var priceModelMock = new Mock<IOptionPriceModel>();
            priceModelMock
                .Setup(s =>
                    s.Evaluate(It.IsAny<Security>(), It.IsAny<Slice>(), It.IsAny<OptionContract>())
                )
                .Returns(new OptionPriceModelResult(1000, new Greeks()));
            _option.PriceModel = priceModelMock.Object;
            var randomPriceGenerator = new OptionPriceModelPriceGenerator(_option);

            Assert.AreEqual(1000, randomPriceGenerator.NextValue(50, new DateTime(2020, 1, 1)));
        }

        [Test]
        public void WarmedUpIfNotQLOptionPriceModel()
        {
            _option.PriceModel = Mock.Of<IOptionPriceModel>();
            var blackScholesModel = new OptionPriceModelPriceGenerator(_option);

            Assert.True(blackScholesModel.WarmedUp);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void WarmedUpSameQLOptionPriceModel(bool warmUp)
        {
            var volatilityModel = new Mock<IQLUnderlyingVolatilityEstimator>();
            volatilityModel.SetupGet(s => s.IsReady).Returns(warmUp);
            _option.PriceModel = new QLOptionPriceModel(
                process => new AnalyticEuropeanEngine(process),
                volatilityModel.Object,
                null,
                null
            );

            var blackScholesModel = new OptionPriceModelPriceGenerator(_option);

            Assert.AreEqual(warmUp, blackScholesModel.WarmedUp);
        }
    }
}
