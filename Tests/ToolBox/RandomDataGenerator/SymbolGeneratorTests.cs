using Moq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;
using System;
using System.Linq;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class SymbolGeneratorTests
    {
        private const int Seed = 123456789;
        private static readonly IRandomValueGenerator _randomValueGenerator = new RandomValueGenerator(Seed);

        [TestFixture]
        public class SpotSymbolGeneratorTests
        {
            [Test]
            [TestCase(SecurityType.Equity, Market.USA, true)]
            [TestCase(SecurityType.Cfd, Market.FXCM, false)]
            [TestCase(SecurityType.Cfd, Market.Oanda, false)]
            [TestCase(SecurityType.Forex, Market.FXCM, false)]
            [TestCase(SecurityType.Forex, Market.Oanda, false)]
            [TestCase(SecurityType.Crypto, Market.GDAX, false)]
            [TestCase(SecurityType.Crypto, Market.Bitfinex, false)]
            public void GetAvailableSymbolCount(SecurityType securityType, string market, bool expectInfinity)
            {
                var expected = expectInfinity
                    ? int.MaxValue
                    : SymbolPropertiesDatabase.FromDataFolder().GetSymbolPropertiesList(market, securityType).Count();

                var symbolGenerator = new SpotSymbolGenerator(new RandomDataGeneratorSettings
                {
                    SecurityType = securityType,
                    Market = market
                }, Mock.Of<RandomValueGenerator>());

                Assert.AreEqual(expected, symbolGenerator.GetAvailableSymbolCount());
            }
        }

        [TestFixture]
        public class FutureSymbolGeneratorTests
        {
            private SymbolGenerator _symbolGenerator;
            private DateTime _minExpiry = new(2000, 01, 01);
            private DateTime _maxExpiry = new(2001, 01, 01);

            [SetUp]
            public void Setup()
            {
                // initialize using a seed for deterministic tests
                _symbolGenerator = new FutureSymbolGenerator(
                    new RandomDataGeneratorSettings()
                    {
                        Market = Market.CME,
                        Start = _minExpiry,
                        End = _maxExpiry
                    },
                    new RandomValueGenerator(Seed));
            }

            [Test]
            public void GetAvailableSymbolCount()
            {
                Assert.AreEqual(int.MaxValue, _symbolGenerator.GetAvailableSymbolCount());
            }

            [Test]
            public void NextFuture_CreatesSymbol_WithFutureSecurityTypeAndRequestedMarket()
            {
                var symbol = _symbolGenerator.GenerateSingle();

                Assert.AreEqual(Market.CME, symbol.ID.Market);
                Assert.AreEqual(SecurityType.Future, symbol.SecurityType);
            }

            [Test]
            public void NextFuture_CreatesSymbol_WithFutureWithValidFridayExpiry()
            {
                var symbol = _symbolGenerator.GenerateSingle();

                var expiry = symbol.ID.Date;
                Assert.Greater(expiry, _minExpiry);
                Assert.LessOrEqual(expiry, _maxExpiry);
                Assert.AreEqual(DayOfWeek.Friday, expiry.DayOfWeek);
            }

            [Test]
            public void NextFuture_CreatesSymbol_WithEntryInSymbolPropertiesDatabase()
            {
                var symbol = _symbolGenerator.GenerateSingle();

                var db = SymbolPropertiesDatabase.FromDataFolder();
                Assert.IsTrue(db.ContainsKey(Market.CME, symbol, SecurityType.Future));
            }
        }

        [TestFixture]
        public class OptionSymbolGeneratorTests
        {
            private SymbolGenerator _symbolGenerator;
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
            public void GetAvailableSymbolCount()
            {
                Assert.AreEqual(int.MaxValue, new OptionSymbolGenerator(Mock.Of<RandomDataGeneratorSettings>(), Mock.Of<RandomValueGenerator>(), 100m, 75m).GetAvailableSymbolCount());
            }

            [Test]
            public void NextOptionSymbol_CreatesOptionSymbol_WithCorrectSecurityTypeAndEquityUnderlying()
            {
                var symbol = _symbolGenerator.GenerateSingle();

                Assert.AreEqual(SecurityType.Option, symbol.SecurityType);

                var underlying = symbol.Underlying;
                Assert.AreEqual(Market.USA, underlying.ID.Market);
                Assert.AreEqual(SecurityType.Equity, underlying.SecurityType);
            }

            [Test]
            public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedExpiration_OnFriday()
            {
                var symbol = _symbolGenerator.GenerateSingle();

                var expiration = symbol.ID.Date;
                Assert.LessOrEqual(_minExpiry, expiration);
                Assert.GreaterOrEqual(_maxExpiry, expiration);
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
                    var symbol = _symbolGenerator.GenerateSingle();

                    Assert.AreEqual(Market.USA, symbol.ID.Market);
                }
            }

            [Test]
            public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedStrikePriceDeviation()
            {
                var symbol = _symbolGenerator.GenerateSingle();

                var strikePrice = symbol.ID.StrikePrice;
                var maximumDeviation = _underlyingPrice * (_maximumStrikePriceDeviation / 100m);
                Assert.LessOrEqual(_underlyingPrice - maximumDeviation, strikePrice);
                Assert.GreaterOrEqual(_underlyingPrice + maximumDeviation, strikePrice);
            }
        }
    }
}
