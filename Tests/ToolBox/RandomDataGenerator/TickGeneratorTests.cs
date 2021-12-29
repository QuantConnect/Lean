using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class TickGeneratorTests
    {
        private Dictionary<SecurityType, List<TickType>> _tickTypesPerSecurityType = SubscriptionManager.DefaultDataTypes();
        private Symbol _symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        private ITickGenerator _tickGenerator;

        [SetUp]
        public void Setup()
        {
            // initialize using a seed for deterministic tests
            _symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar),
                    _symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true, true, false),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            _tickGenerator = TickGenerator.Create(
                new RandomDataGeneratorSettings(),
                _tickTypesPerSecurityType[_symbol.SecurityType].ToArray(),
                new RandomValueGenerator(),
                security);

        }

        [Test]
        public void NextTick_CreatesTradeTick_WithPriceAndQuantity()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var tick = _tickGenerator.NextTick(dateTime, TickType.Trade, 100m, 1m);

            Assert.AreEqual(_symbol, tick.Symbol);
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
            var tick = _tickGenerator.NextTick(dateTime, TickType.Quote, 100m, 1m);

            Assert.AreEqual(_symbol, tick.Symbol);
            Assert.AreEqual(dateTime, tick.Time);
            Assert.AreEqual(TickType.Quote, tick.TickType);
            Assert.GreaterOrEqual(tick.Value, 99m);
            Assert.LessOrEqual(tick.Value, 101m);
        }

        [Test]
        public void NextTick_CreatesQuoteTick_WithBidData()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var tick = _tickGenerator.NextTick(dateTime, TickType.Quote, 100m, 1m);

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
            var tick = _tickGenerator.NextTick(dateTime, TickType.Quote, 100m, 1m);

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
            var tick = _tickGenerator.NextTick(dateTime, TickType.OpenInterest, 10000m, 10m);

            Assert.AreEqual(dateTime, tick.Time);
            Assert.AreEqual(TickType.OpenInterest, tick.TickType);
            Assert.AreEqual(_symbol, tick.Symbol);
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
            var marketHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(_symbol.ID.Market, _symbol, _symbol.SecurityType);
            for (int i = 0; i < count; i++)
            {
                var next = _tickGenerator.NextTickTime(previous, resolution, density);
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
    }
}
