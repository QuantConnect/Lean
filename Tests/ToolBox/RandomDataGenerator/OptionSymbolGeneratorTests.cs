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
using QuantConnect.ToolBox.RandomDataGenerator;
using System;
using System.Linq;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class OptionSymbolGeneratorTests
    {
        private const int Seed = 123456789;
        private static readonly IRandomValueGenerator _randomValueGenerator = new RandomValueGenerator(Seed);

        private BaseSymbolGenerator _symbolGenerator;
        private DateTime _minExpiry = new(2000, 01, 01);
        private DateTime _maxExpiry = new(2001, 01, 01);
        private decimal _underlyingPrice = 100m;
        private decimal _maximumStrikePriceDeviation = 50m;

        [SetUp]
        public void Setup()
        {
            // initialize using a seed for deterministic tests
            _symbolGenerator = new OptionSymbolGenerator(
                new RandomDataGeneratorSettings()
                {
                    Market = Market.USA,
                    Start = _minExpiry,
                    End = _maxExpiry
                },
                _randomValueGenerator,
                _underlyingPrice,
                _maximumStrikePriceDeviation);
        }

        [Test]
        [TestCase(SecurityType.Option)]
        public void ReturnsFutureSymbolGeneratorInstance(SecurityType securityType)
        {
            Assert.IsInstanceOf<OptionSymbolGenerator>(BaseSymbolGenerator.Create(
                new RandomDataGeneratorSettings { SecurityType = securityType },
                Mock.Of<IRandomValueGenerator>()
            ));
        }

        [Test]
        public void GetAvailableSymbolCount()
        {
            Assert.AreEqual(int.MaxValue,
                new OptionSymbolGenerator(Mock.Of<RandomDataGeneratorSettings>(), Mock.Of<RandomValueGenerator>(), 100m,
                    75m).GetAvailableSymbolCount());
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithCorrectSecurityTypeAndEquityUnderlying()
        {
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
            Assert.AreEqual(3, symbols.Count);

            var underlying = symbols[0];
            var option = symbols[1];

            Assert.AreEqual(SecurityType.Option, option.SecurityType);
            Assert.AreEqual(OptionRight.Put, symbols[1].ID.OptionRight);
            Assert.AreEqual(SecurityType.Option, symbols[2].SecurityType);
            Assert.AreEqual(OptionRight.Call, symbols[2].ID.OptionRight);

            var underlyingOrigin = option.Underlying;
            Assert.AreEqual(underlying.Value, underlyingOrigin.Value);
            Assert.AreEqual(Market.USA, underlying.ID.Market);
            Assert.AreEqual(SecurityType.Equity, underlying.SecurityType);
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedExpiration_OnFriday()
        {
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
            Assert.AreEqual(3, symbols.Count);

            foreach (var option in new[] { symbols[1], symbols[2] })
            {
                var expiration = option.ID.Date;
                Assert.LessOrEqual(_minExpiry, expiration);
                Assert.GreaterOrEqual(_maxExpiry, expiration);
            }
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithRequestedMarket()
        {
            for (int i = 0; i < 50; i++)
            {
                var price = _randomValueGenerator.NextPrice(SecurityType.Equity, Market.USA, 100m, 100m);
                var symbolGenerator = new OptionSymbolGenerator(
                    new RandomDataGeneratorSettings()
                    {
                        Market = Market.USA,
                        Start = _minExpiry,
                        End = _maxExpiry
                    },
                    _randomValueGenerator,
                    price,
                    50m);
                var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
                Assert.AreEqual(3, symbols.Count);

                var option = symbols[1];

                Assert.AreEqual(Market.USA, option.ID.Market);
            }
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedStrikePriceDeviation()
        {
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(_symbolGenerator).ToList();
            Assert.AreEqual(3, symbols.Count);

            foreach (var option in new []{ symbols[1], symbols[2] })
            {
                var strikePrice = option.ID.StrikePrice;
                var maximumDeviation = _underlyingPrice * (_maximumStrikePriceDeviation / 100m);
                Assert.LessOrEqual(_underlyingPrice - maximumDeviation, strikePrice);
                Assert.GreaterOrEqual(_underlyingPrice + maximumDeviation, strikePrice);
            }
        }

        [Test]
        [TestCase("2021, 6, 2 00:00:00", "2021, 6, 4 00:00:00")]
        [TestCase("2021, 6, 2 00:00:00", "2021, 6, 5 00:00:00")]
        [TestCase("2021, 6, 2 00:00:00", "2021, 7, 2 00:00:00")]
        [TestCase("2021, 6, 2 00:00:00", "2021, 6, 10 00:00:00")]
        [TestCase("2021, 6, 2 00:00:00", "2021, 6, 11 00:00:00")]
        [TestCase("2021, 6, 2 00:00:00", "2021, 8, 2 00:00:00")]
        [TestCase("2021, 6, 2 00:00:00", "2021, 6, 15 00:00:00")]
        public void OptionSymbolGeneratorCreatesOptionSymbol_WithExpirationDateAtLeastThreeDaysAfterMinExpiryDate(DateTime minExpiry, DateTime maxExpiry)
        {
            var symbolGenerator = new OptionSymbolGenerator(
                new RandomDataGeneratorSettings()
                {
                    Market = Market.USA,
                    Start = minExpiry,
                    End = maxExpiry
                },
                new RandomValueGenerator(),
                _underlyingPrice,
                _maximumStrikePriceDeviation);
            var symbols = BaseSymbolGeneratorTests.GenerateAsset(symbolGenerator).ToList();
            Assert.AreEqual(3, symbols.Count);

            foreach (var option in new[] { symbols[1], symbols[2] })
            {
                var expiration = option.ID.Date;
                Assert.LessOrEqual(minExpiry.AddDays(3), expiration);
            }
        }
    }
}
