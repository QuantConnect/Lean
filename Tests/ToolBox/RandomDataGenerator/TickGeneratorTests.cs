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
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<SecurityType, List<TickType>> _tickTypesPerSecurityType =
            SubscriptionManager.DefaultDataTypes();

        private Symbol _symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        private Security _security;
        private ITickGenerator _tickGenerator;

        [SetUp]
        public void Setup()
        {
            var start = new DateTime(2020, 1, 6);
            var end = new DateTime(2020, 1, 10);

            // initialize using a seed for deterministic tests
            _symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            _security = new Security(
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
            _security.SetMarketPrice(new Tick(start, _security.Symbol, 100, 100));
            _security.SetMarketPrice(new OpenInterest(start, _security.Symbol, 10000));

            _tickGenerator = new TickGenerator(
                new RandomDataGeneratorSettings()
                {
                    Start = start,
                    End = end
                },
                new TickType[3] { TickType.Trade, TickType.Quote, TickType.OpenInterest },
                _security,
                new RandomValueGenerator());

        }

        [Test]
        public void NextTick_CreatesTradeTick_WithPriceAndQuantity()
        {
            var dateTime = new DateTime(2000, 01, 01);
            var tick = _tickGenerator.NextTick(dateTime, TickType.Trade, 1m);

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
            var tick = _tickGenerator.NextTick(dateTime, TickType.Quote, 1m);

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
            var tick = _tickGenerator.NextTick(dateTime, TickType.Quote, 1m);

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
            var tick = _tickGenerator.NextTick(dateTime, TickType.Quote, 1m);

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
            var tick = _tickGenerator.NextTick(dateTime, TickType.OpenInterest, 10m);

            Assert.AreEqual(dateTime, tick.Time);
            Assert.AreEqual(TickType.OpenInterest, tick.TickType);
            Assert.AreEqual(typeof(OpenInterest), tick.GetType());
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

            var marketHours = MarketHoursDatabase.FromDataFolder()
                .GetExchangeHours(_symbol.ID.Market, _symbol, _symbol.SecurityType);
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

        [Test]
        public void HistoryIsNotEmpty()
        {
            var history = _tickGenerator.GenerateTicks().ToList();
            Assert.IsNotEmpty(history);
            Assert.That(history.Select(s => s.Symbol), Is.All.EqualTo(_symbol));
        }

        [Test]
        public void HistoryIsBetweenStartAndEndDate()
        {
            var start = new DateTime(2020, 1, 6);
            var end = new DateTime(2020, 1, 10);
            var history = _tickGenerator.GenerateTicks().ToList();
            Assert.IsNotEmpty(history);
            Assert.That(history.All(s => start <= s.Time && s.Time <= end));
        }

        [Test]
        public void HistoryGeneratesOpenInterestDataAsExpected()
        {
            var start = new DateTime(2020, 1, 6);
            var end = new DateTime(2020, 1, 10);
            var history = _tickGenerator.GenerateTicks().ToList();
            Assert.IsNotEmpty(history);
            Assert.AreEqual(3, history.Where(s => s.TickType == TickType.OpenInterest).Count());
        }
    }
}
