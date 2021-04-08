using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomValueGeneratorTests
    {
        private const int Seed = 123456789;
        private RandomValueGenerator randomValueGenerator;

        [SetUp]
        public void Setup()
        {
            // initialize using a seed for deterministic tests
            randomValueGenerator = new RandomValueGenerator(Seed);
        }

        [Test]
        [TestCase(2,5)]
        [TestCase(3,3)]
        [TestCase(1,4)]
        public void NextUpperCaseString_CreatesString_WithinSpecifiedMinMaxLength(int min, int max)
        {
            var str = randomValueGenerator.NextUpperCaseString(min, max);
            Assert.LessOrEqual(min, str.Length);
            Assert.GreaterOrEqual(max, str.Length);
        }

        [Test]
        public void NextUpperCaseString_CreatesUpperCaseString()
        {
            var str = randomValueGenerator.NextUpperCaseString(10, 10);
            Assert.IsTrue(str.All(char.IsUpper));
        }

        [Test]
        public void NextDateTime_CreatesDateTime_WithinSpecifiedMinMax()
        {
            var min = new DateTime(2000, 01, 01);
            var max = new DateTime(2001, 01, 01);
            var dateTime = randomValueGenerator.NextDate(min, max, dayOfWeek: null);

            Assert.LessOrEqual(min, dateTime);
            Assert.GreaterOrEqual(max, dateTime);
        }

        [Test]
        [TestCase(DayOfWeek.Sunday)]
        [TestCase(DayOfWeek.Monday)]
        [TestCase(DayOfWeek.Tuesday)]
        [TestCase(DayOfWeek.Wednesday)]
        [TestCase(DayOfWeek.Thursday)]
        [TestCase(DayOfWeek.Friday)]
        [TestCase(DayOfWeek.Saturday)]
        public void NextDateTime_CreatesDateTime_OnSpecifiedDayOfWeek(DayOfWeek dayOfWeek)
        {
            var min = new DateTime(2000, 01, 01);
            var max = new DateTime(2001, 01, 01);
            var dateTime = randomValueGenerator.NextDate(min, max, dayOfWeek);

            Assert.AreEqual(dayOfWeek, dateTime.DayOfWeek);
        }

        [Test]
        public void NextDateTime_ThrowsArgumentException_WhenMaxIsLessThanMin()
        {
            var min = new DateTime(2000, 01, 01);
            var max = min.AddDays(-1);
            Assert.Throws<ArgumentException>(() =>
                randomValueGenerator.NextDate(min, max, dayOfWeek: null)
            );
        }

        [Test]
        public void NextDateTime_ThrowsArgumentException_WhenRangeIsTooSmallToProduceDateTimeOnRequestedDayOfWeek()
        {
            var min = new DateTime(2019, 01, 15);
            var max = new DateTime(2019, 01, 20);
            Assert.Throws<ArgumentException>(() =>
                // no monday between these dates, so impossible to fulfill request
                randomValueGenerator.NextDate(min, max, DayOfWeek.Monday)
            );
        }

        [Test]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        public void NextSymbol_ThrowsArgumentException_ForDerivativeSymbols(SecurityType securityType)
        {
            Assert.Throws<ArgumentException>(() =>
                randomValueGenerator.NextSymbol(securityType, Market.USA)
            );
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
            var symbol = randomValueGenerator.NextSymbol(securityType, market);

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
            var symbol = randomValueGenerator.NextSymbol(securityType, market);

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

            for (var i = 0; i < symbolCount; i++)
            {
                randomValueGenerator.NextSymbol(securityType, market);
            }

            Assert.Throws<NoTickersAvailableException>(() =>
                randomValueGenerator.NextSymbol(securityType, market)
            );
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithCorrectSecurityTypeAndEquityUnderlying()
        {
            var minExpiry = new DateTime(2000, 01, 01);
            var maxExpiry = new DateTime(2001, 01, 01);
            var symbol = randomValueGenerator.NextOption(Market.USA, minExpiry, maxExpiry, 100m, 50);

            Assert.AreEqual(SecurityType.Option, symbol.SecurityType);

            var underlying = symbol.Underlying;
            Assert.AreEqual(Market.USA, underlying.ID.Market);
            Assert.AreEqual(SecurityType.Equity, underlying.SecurityType);
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedExpiration_OnFriday()
        {
            var minExpiry = new DateTime(2000, 01, 01);
            var maxExpiry = new DateTime(2001, 01, 01);
            var symbol = randomValueGenerator.NextOption(Market.USA, minExpiry, maxExpiry, 100m, 50);

            var expiration = symbol.ID.Date;
            Assert.LessOrEqual(minExpiry, expiration);
            Assert.GreaterOrEqual(maxExpiry, expiration);
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithRequestedMarket()
        {
            for (int i = 0; i < 50; i++)
            {
                var minExpiry = new DateTime(2000, 01, 01);
                var maxExpiry = new DateTime(2001, 01, 01);
                var price = randomValueGenerator.NextPrice(SecurityType.Equity, Market.USA, 100m, 100m);
                var symbol = randomValueGenerator.NextOption(Market.USA, minExpiry, maxExpiry, price, 50);

                Assert.AreEqual(Market.USA, symbol.ID.Market);
            }
        }

        [Test]
        public void NextOptionSymbol_CreatesOptionSymbol_WithinSpecifiedStrikePriceDeviation()
        {
            var underlyingPrice = 100m;
            var maximumStrikePriceDeviation = 50m;
            var minExpiry = new DateTime(2000, 01, 01);
            var maxExpiry = new DateTime(2001, 01, 01);
            var symbol = randomValueGenerator.NextOption(Market.USA, minExpiry, maxExpiry, underlyingPrice, maximumStrikePriceDeviation);

            var strikePrice = symbol.ID.StrikePrice;
            var maximumDeviation = underlyingPrice * (maximumStrikePriceDeviation / 100m);
            Assert.LessOrEqual(underlyingPrice - maximumDeviation, strikePrice);
            Assert.GreaterOrEqual(underlyingPrice + maximumDeviation, strikePrice);
        }

        [Test]
        public void NextTick_CreatesTradeTick_WithPriceAndQuantity()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var symbol = randomValueGenerator.NextSymbol(SecurityType.Equity, Market.USA);
            var tick = randomValueGenerator.NextTick(symbol, dateTime, TickType.Trade, 100m, 1m);

            Assert.AreEqual(symbol, tick.Symbol);
            Assert.AreEqual(dateTime, tick.Time);
            Assert.AreEqual(TickType.Trade, tick.TickType);
            Assert.LessOrEqual(99m, tick.Value);
            Assert.GreaterOrEqual(101m, tick.Value);

            Assert.Greater(tick.Quantity, 0);
            Assert.LessOrEqual(tick.Quantity, 1500);
        }

        [Test]
        public void NextTick_CreatesQuoteTick_WithCommonValues()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var symbol = randomValueGenerator.NextSymbol(SecurityType.Equity, Market.USA);
            var tick = randomValueGenerator.NextTick(symbol, dateTime, TickType.Quote, 100m, 1m);

            Assert.AreEqual(symbol, tick.Symbol);
            Assert.AreEqual(dateTime, tick.Time);
            Assert.AreEqual(TickType.Quote, tick.TickType);
            Assert.GreaterOrEqual(tick.Value, 99m);
            Assert.LessOrEqual(tick.Value, 101m);
        }

        [Test]
        public void NextTick_CreatesQuoteTick_WithBidData()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var symbol = randomValueGenerator.NextSymbol(SecurityType.Equity, Market.USA);
            var tick = randomValueGenerator.NextTick(symbol, dateTime, TickType.Quote, 100m, 1m);

            Assert.Greater(tick.BidSize, 0);
            Assert.LessOrEqual(tick.BidSize, 1500);
            Assert.GreaterOrEqual(tick.BidPrice, 98.9m);
            Assert.LessOrEqual(tick.BidPrice, 100.9m);
            Assert.GreaterOrEqual(tick.Value, tick.BidPrice);
        }

        [Test]
        public void NextTick_CreatesQuoteTick_WithAskData()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var symbol = randomValueGenerator.NextSymbol(SecurityType.Equity, Market.USA);
            var tick = randomValueGenerator.NextTick(symbol, dateTime, TickType.Quote, 100m, 1m);

            Assert.GreaterOrEqual(tick.AskSize, 0);
            Assert.LessOrEqual(tick.AskSize, 1500);
            Assert.GreaterOrEqual(tick.AskPrice, 99.1m);
            Assert.LessOrEqual(tick.AskPrice, 101.1m);
            Assert.LessOrEqual(tick.Value, tick.AskPrice);
        }

        [Test]
        public void NextTick_CreatesOpenInterestTick()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var symbol = randomValueGenerator.NextSymbol(SecurityType.Equity, Market.USA);
            var tick = randomValueGenerator.NextTick(symbol, dateTime, TickType.OpenInterest, 10000m, 10m);

            Assert.AreEqual(dateTime, tick.Time);
            Assert.AreEqual(TickType.OpenInterest, tick.TickType);
            Assert.AreEqual(symbol, tick.Symbol);
            Assert.GreaterOrEqual(tick.Quantity, 9000);
            Assert.LessOrEqual(tick.Quantity, 11000);
            Assert.AreEqual(tick.Value, tick.Quantity);
        }

        [Test]
        [TestCase(Resolution.Tick, DataDensity.Dense)]
        [TestCase(Resolution.Second, DataDensity.Dense)]
        [TestCase(Resolution.Minute, DataDensity.Dense)]
        [TestCase(Resolution.Hour, DataDensity.Dense)]
        [TestCase(Resolution.Daily, DataDensity.Dense)]
        [TestCase(Resolution.Tick, DataDensity.Sparse)]
        [TestCase(Resolution.Second, DataDensity.Sparse)]
        [TestCase(Resolution.Minute, DataDensity.Sparse)]
        [TestCase(Resolution.Hour, DataDensity.Sparse)]
        [TestCase(Resolution.Daily, DataDensity.Sparse)]
        [TestCase(Resolution.Tick, DataDensity.VerySparse)]
        [TestCase(Resolution.Second, DataDensity.VerySparse)]
        [TestCase(Resolution.Minute, DataDensity.VerySparse)]
        [TestCase(Resolution.Hour, DataDensity.VerySparse)]
        [TestCase(Resolution.Daily, DataDensity.VerySparse)]
        public void NextTickTime_CreatesTimes(Resolution resolution, DataDensity density)
        {
            var count = 100;
            var deltaSum = TimeSpan.Zero;
            var previous = new DateTime(2019, 01, 14, 9, 30, 0);
            var increment = resolution.ToTimeSpan();
            if (increment == TimeSpan.Zero)
            {
                increment = TimeSpan.FromMilliseconds(500);
            }
            var symbol = randomValueGenerator.NextSymbol(SecurityType.Equity, Market.USA);
            var marketHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            for (int i = 0; i < count; i++)
            {
                var next = randomValueGenerator.NextTickTime(symbol, previous, resolution, density);
                var barStart = next.Subtract(increment);
                Assert.Less(previous, next);
                Assert.IsTrue(marketHours.IsOpen(barStart, next, false));

                var delta = next - previous;
                deltaSum += delta;

                previous = next;
            }

            var avgDelta = TimeSpan.FromTicks(deltaSum.Ticks / count);
            switch (density)
            {
                case DataDensity.Dense:
                    // more frequent than once an increment
                    Assert.Less(avgDelta, increment);
                    break;

                case DataDensity.Sparse:
                    // less frequent that once an increment
                    Assert.Greater(avgDelta, increment);
                    break;

                case DataDensity.VerySparse:
                    // less frequent than one every 10 increments
                    Assert.Greater(avgDelta, TimeSpan.FromTicks(increment.Ticks * 10));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(density), density, null);
            }
        }

        [Test]
        public void NextFuture_CreatesSymbol_WithFutureSecurityTypeAndRequestedMarket()
        {
            var minExpiry = new DateTime(2000, 01, 01);
            var maxExpiry = new DateTime(2001, 01, 01);
            var symbol = randomValueGenerator.NextFuture(Market.CME, minExpiry, maxExpiry);

            Assert.AreEqual(Market.CME, symbol.ID.Market);
            Assert.AreEqual(SecurityType.Future, symbol.SecurityType);
        }

        [Test]
        public void NextFuture_CreatesSymbol_WithFutureWithValidFridayExpiry()
        {
            var minExpiry = new DateTime(2000, 01, 01);
            var maxExpiry = new DateTime(2001, 01, 01);
            var symbol = randomValueGenerator.NextFuture(Market.CME, minExpiry, maxExpiry);

            var expiry = symbol.ID.Date;
            Assert.Greater(expiry, minExpiry);
            Assert.LessOrEqual(expiry, maxExpiry);
            Assert.AreEqual(DayOfWeek.Friday, expiry.DayOfWeek);
        }

        [Test]
        public void NextFuture_CreatesSymbol_WithEntryInSymbolPropertiesDatabase()
        {
            var minExpiry = new DateTime(2000, 01, 01);
            var maxExpiry = new DateTime(2001, 01, 01);
            var symbol = randomValueGenerator.NextFuture(Market.CME, minExpiry, maxExpiry);

            var db = SymbolPropertiesDatabase.FromDataFolder();
            Assert.IsTrue(db.ContainsKey(Market.CME, symbol, SecurityType.Future));
        }

        [Test]
        [TestCase(SecurityType.Equity, Market.USA, true)]
        [TestCase(SecurityType.Cfd, Market.FXCM, false)]
        [TestCase(SecurityType.Cfd, Market.Oanda, false)]
        [TestCase(SecurityType.Forex, Market.FXCM, false)]
        [TestCase(SecurityType.Forex, Market.Oanda, false)]
        [TestCase(SecurityType.Crypto, Market.GDAX, false)]
        [TestCase(SecurityType.Crypto, Market.Bitfinex, false)]
        [TestCase(SecurityType.Option, Market.USA, true)]
        [TestCase(SecurityType.Future, Market.CME, true)]
        [TestCase(SecurityType.Future, Market.CBOE, true)]
        public void GetAvailableSymbolCount(SecurityType securityType, string market, bool expectInfinity)
        {
            var expected = expectInfinity
                ? int.MaxValue
                : SymbolPropertiesDatabase.FromDataFolder().GetSymbolPropertiesList(market, securityType).Count();

            Assert.AreEqual(expected, randomValueGenerator.GetAvailableSymbolCount(securityType, market));
        }
    }
}
