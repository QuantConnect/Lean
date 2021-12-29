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


        [Test]
        [TestCase(2, 5)]
        [TestCase(3, 3)]
        [TestCase(1, 4)]
        public void NextUpperCaseString_CreatesString_WithinSpecifiedMinMaxLength(int min, int max)
        {
            var symbolGenerator = new Mock<SymbolGenerator>(Mock.Of<RandomDataGeneratorSettings>(), new RandomValueGenerator()).Object;
            var str = symbolGenerator.NextUpperCaseString(min, max);
            Assert.LessOrEqual(min, str.Length);
            Assert.GreaterOrEqual(max, str.Length);
        }

        [Test]
        public void NextUpperCaseString_CreatesUpperCaseString()
        {
            var symbolGenerator = new Mock<SymbolGenerator>(Mock.Of<RandomDataGeneratorSettings>(), new RandomValueGenerator()).Object;
            var str = symbolGenerator.NextUpperCaseString(10, 10);
            Assert.IsTrue(str.All(char.IsUpper));
        }

        [Test]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        public void ThrowsArgumentException_ForDerivativeSymbols(SecurityType securityType)
        {
            var symbolGenerator = new Mock<SymbolGenerator>(Mock.Of<RandomDataGeneratorSettings>(), new RandomValueGenerator()).Object;
            Assert.Throws<ArgumentException>(() =>
                symbolGenerator.NextSymbol(securityType, Market.USA)
            );
        }

        [TestFixture]
        public class SpotSymbolGeneratorTests
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
                    _randomValueGenerator);
            }

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

            [Test]
            [TestCase(SecurityType.Equity, Market.USA)]
            [TestCase(SecurityType.Cfd, Market.FXCM)]
            [TestCase(SecurityType.Cfd, Market.Oanda)]
            [TestCase(SecurityType.Forex, Market.FXCM)]
            [TestCase(SecurityType.Forex, Market.Oanda)]
            [TestCase(SecurityType.Crypto, Market.GDAX)]
            [TestCase(SecurityType.Crypto, Market.Bitfinex)]
            public void NextSymbol_CreatesSymbol_WithRequestedSecurityTypeAndMarket(SecurityType securityType, string market)
            {
                var symbolGenerator = new SpotSymbolGenerator(new RandomDataGeneratorSettings
                {
                    SecurityType = securityType,
                    Market = market
                }, _randomValueGenerator);

                var symbols = symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(1, symbols.Count);

                var symbol = symbols.First();
                Assert.AreEqual(securityType, symbol.SecurityType);
                Assert.AreEqual(market, symbol.ID.Market);
            }

            [Test]
            [TestCase(SecurityType.Equity, Market.USA)]
            [TestCase(SecurityType.Cfd, Market.FXCM)]
            [TestCase(SecurityType.Cfd, Market.Oanda)]
            [TestCase(SecurityType.Forex, Market.FXCM)]
            [TestCase(SecurityType.Forex, Market.Oanda)]
            [TestCase(SecurityType.Crypto, Market.GDAX)]
            [TestCase(SecurityType.Crypto, Market.Bitfinex)]
            public void NextSymbol_CreatesSymbol_WithEntryInSymbolPropertiesDatabase(SecurityType securityType, string market)
            {
                var symbolGenerator = new SpotSymbolGenerator(new RandomDataGeneratorSettings
                {
                    SecurityType = securityType,
                    Market = market
                }, _randomValueGenerator);

                var symbols = symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(1, symbols.Count);

                var symbol = symbols.First();

                var db = SymbolPropertiesDatabase.FromDataFolder();
                if (db.ContainsKey(market, SecurityDatabaseKey.Wildcard, securityType))
                {
                    // there is a wildcard entry, so no need to check whether there is a specific entry for the symbol
                    Assert.Pass();
                }
                else
                {
                    // there is no wildcard entry, so there should be a specific entry for the symbol instead
                    Assert.IsTrue(db.ContainsKey(market, symbol, securityType));
                }
            }

            [Test]
            [TestCase(SecurityType.Cfd, Market.FXCM)]
            [TestCase(SecurityType.Cfd, Market.Oanda)]
            [TestCase(SecurityType.Forex, Market.FXCM)]
            [TestCase(SecurityType.Forex, Market.Oanda)]
            [TestCase(SecurityType.Crypto, Market.GDAX)]
            [TestCase(SecurityType.Crypto, Market.Bitfinex)]
            public void NextSymbol_ThrowsNoTickersAvailableException_WhenAllSymbolsGenerated(SecurityType securityType, string market)
            {
                var db = SymbolPropertiesDatabase.FromDataFolder();
                var symbolCount = db.GetSymbolPropertiesList(market, securityType).Count();

                var symbolGenerator = new SpotSymbolGenerator(new RandomDataGeneratorSettings
                {
                    SecurityType = securityType,
                    Market = market
                }, _randomValueGenerator);

                for (var i = 0; i < symbolCount; i++)
                {
                    var symbols = symbolGenerator.GenerateAsset().ToList();
                    Assert.AreEqual(1, symbols.Count);
                }

                Assert.Throws<NoTickersAvailableException>(() =>
                    symbolGenerator.GenerateAsset().ToList()
                );
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
                    _randomValueGenerator);
            }

            [Test]
            public void GetAvailableSymbolCount()
            {
                Assert.AreEqual(int.MaxValue, _symbolGenerator.GetAvailableSymbolCount());
            }

            [Test]
            public void NextFuture_CreatesSymbol_WithFutureSecurityTypeAndRequestedMarket()
            {
                var symbols = _symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(1, symbols.Count);

                var symbol = symbols.First();

                Assert.AreEqual(Market.CME, symbol.ID.Market);
                Assert.AreEqual(SecurityType.Future, symbol.SecurityType);
            }

            [Test]
            public void NextFuture_CreatesSymbol_WithFutureWithValidFridayExpiry()
            {
                var symbols = _symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(1, symbols.Count);

                var symbol = symbols.First();

                var expiry = symbol.ID.Date;
                Assert.Greater(expiry, _minExpiry);
                Assert.LessOrEqual(expiry, _maxExpiry);
                Assert.AreEqual(DayOfWeek.Friday, expiry.DayOfWeek);
            }

            [Test]
            public void NextFuture_CreatesSymbol_WithEntryInSymbolPropertiesDatabase()
            {
               var symbols = _symbolGenerator.GenerateAsset().ToList();
               Assert.AreEqual(1, symbols.Count);

                var symbol = symbols.First();

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
                var securities = _symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(2, securities.Count);

                var underlying = securities[0];
                var option = securities[1];

                Assert.AreEqual(SecurityType.Option, option.SecurityType);

                var underlyingOrigin = option.Underlying;
                Assert.AreEqual(underlying.Value, underlyingOrigin.Value);
                Assert.AreEqual(Market.USA, underlying.ID.Market);
                Assert.AreEqual(SecurityType.Equity, underlying.SecurityType);
            }

            [Test]
            public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedExpiration_OnFriday()
            {
                var securities = _symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(2, securities.Count);
                
                var option = securities[1];

                var expiration = option.ID.Date;
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
                    var securities = symbolGenerator.GenerateAsset().ToList();
                    Assert.AreEqual(2, securities.Count);

                    var option = securities[1];

                    Assert.AreEqual(Market.USA, option.ID.Market);
                }
            }

            [Test]
            public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedStrikePriceDeviation()
            {
                var securities = _symbolGenerator.GenerateAsset().ToList();
                Assert.AreEqual(2, securities.Count);
                
                var option = securities[1];

                var strikePrice = option.ID.StrikePrice;
                var maximumDeviation = _underlyingPrice * (_maximumStrikePriceDeviation / 100m);
                Assert.LessOrEqual(_underlyingPrice - maximumDeviation, strikePrice);
                Assert.GreaterOrEqual(_underlyingPrice + maximumDeviation, strikePrice);
            }
        }
    }
}
