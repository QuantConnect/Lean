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
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;
using QuantConnect.Util;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomPriceGeneratorTests
    {
        private Security _security;

        public RandomPriceGeneratorTests()
        {
            _security = new Security(
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
        }

        [Test]
        public void ReturnsSameAsReference()
        {
            var randomMock = new Mock<IRandomValueGenerator>();
            randomMock
                .Setup(s =>
                    s.NextPrice(
                        It.IsAny<SecurityType>(),
                        It.IsNotNull<string>(),
                        It.IsAny<decimal>(),
                        It.IsAny<decimal>()
                    )
                )
                .Returns(50);
            var randomPriceGenerator = new RandomPriceGenerator(_security, randomMock.Object);
            _security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbols.SPY, 10, 100));

            var actual = randomPriceGenerator.NextValue(1, DateTime.MinValue);
            randomMock.Verify(
                s => s.NextPrice(It.IsAny<SecurityType>(), It.IsNotNull<string>(), 55, 1),
                Times.Once
            );
            Assert.AreEqual(50, actual);
        }

        [Test]
        public void AlwaysReady()
        {
            var priceGenerator = new RandomPriceGenerator(
                _security,
                Mock.Of<IRandomValueGenerator>()
            );
            Assert.True(priceGenerator.WarmedUp);
        }

        [Test]
        public void ComposeRandomDataGenerator()
        {
            Assert.NotNull(
                Composer.Instance.GetExportedValueByTypeName<QuantConnect.ToolBox.RandomDataGenerator.RandomDataGenerator>(
                    "QuantConnect.ToolBox.RandomDataGenerator.RandomDataGenerator"
                )
            );
        }
    }
}
