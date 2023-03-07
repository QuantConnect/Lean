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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Data;
using System.Collections;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using Tick = QuantConnect.Data.Market.Tick;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using System.Linq;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class PriceScaleFactorEnumeratorTests
    {
        private SubscriptionDataConfig _config;
        private RawDataEnumerator _rawDataEnumerator;

        [SetUp]
        public void Setup()
        {
            _config = GetConfig(Symbols.SPY, Resolution.Daily);
            _rawDataEnumerator = new RawDataEnumerator();
        }

        [Test]
        public void EquityTradeBar()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                TestGlobals.FactorFileProvider);
            _rawDataEnumerator.CurrentValue = new TradeBar(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                10,
                10,
                10,
                10,
                100);
            Assert.IsTrue(enumerator.MoveNext());
            var tradeBar = enumerator.Current as TradeBar;
            var expectedValue = 10 * _config.PriceScaleFactor;

            Assert.Less(expectedValue, 10);
            Assert.AreEqual(expectedValue, tradeBar.Price);
            Assert.AreEqual(expectedValue, tradeBar.Open);
            Assert.AreEqual(expectedValue, tradeBar.Close);
            Assert.AreEqual(expectedValue, tradeBar.High);
            Assert.AreEqual(expectedValue, tradeBar.Low);
            Assert.AreEqual(expectedValue, tradeBar.Value);

            enumerator.Dispose();
        }

        [Test]
        public void EquityQuoteBar()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                TestGlobals.FactorFileProvider);
            _rawDataEnumerator.CurrentValue = new QuoteBar(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                new Bar(10, 10, 10, 10),
                100,
                new Bar(10, 10, 10, 10),
                100);
            Assert.IsTrue(enumerator.MoveNext());
            var quoteBar = enumerator.Current as QuoteBar;
            var expectedValue = 10 * _config.PriceScaleFactor;

            Assert.Less(expectedValue, 10);

            Assert.AreEqual(expectedValue, quoteBar.Price);
            Assert.AreEqual(expectedValue, quoteBar.Value);
            Assert.AreEqual(expectedValue, quoteBar.Open);
            Assert.AreEqual(expectedValue, quoteBar.Close);
            Assert.AreEqual(expectedValue, quoteBar.High);
            Assert.AreEqual(expectedValue, quoteBar.Low);
            // bid
            Assert.AreEqual(expectedValue, quoteBar.Bid.Open);
            Assert.AreEqual(expectedValue, quoteBar.Bid.Close);
            Assert.AreEqual(expectedValue, quoteBar.Bid.High);
            Assert.AreEqual(expectedValue, quoteBar.Bid.Low);
            // ask
            Assert.AreEqual(expectedValue, quoteBar.Ask.Open);
            Assert.AreEqual(expectedValue, quoteBar.Ask.Close);
            Assert.AreEqual(expectedValue, quoteBar.Ask.High);
            Assert.AreEqual(expectedValue, quoteBar.Ask.Low);

            enumerator.Dispose();
        }

        [Test]
        public void EquityTick()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                TestGlobals.FactorFileProvider);
            _rawDataEnumerator.CurrentValue = new Tick(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                10,
                10,
                10);
            Assert.IsTrue(enumerator.MoveNext());
            var tick = enumerator.Current as Tick;
            var expectedValue = 10 * _config.PriceScaleFactor;

            Assert.Less(expectedValue, 10);
            Assert.AreEqual(expectedValue, tick.Price);
            Assert.AreEqual(expectedValue, tick.Value);

            enumerator.Dispose();
        }

        [Test]
        public void FactorFileIsNull()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                null);
            _rawDataEnumerator.CurrentValue = new Tick(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                10,
                10,
                10);
            Assert.IsTrue(enumerator.MoveNext());
            var tick = enumerator.Current as Tick;
            Assert.AreEqual(10, tick.Price);
            Assert.AreEqual(10, tick.Value);

            enumerator.Dispose();
        }

        [Test]
        public void RawEnumeratorReturnsFalse()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                TestGlobals.FactorFileProvider);
            _rawDataEnumerator.CurrentValue = new Tick(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                10,
                10,
                10);
            _rawDataEnumerator.MoveNextReturnValue = false;
            Assert.IsFalse(enumerator.MoveNext());
            Assert.AreEqual(_rawDataEnumerator.CurrentValue, enumerator.Current);

            enumerator.Dispose();
        }

        [Test]
        public void RawEnumeratorCurrentIsNull()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                TestGlobals.FactorFileProvider);
            _rawDataEnumerator.CurrentValue = null;
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            enumerator.Dispose();
        }

        [Test]
        public void UpdatesFactorFileCorrectly()
        {
            var dateBeforeUpadate = new DateTime(2018, 3, 14);
            var dateAtUpadate = new DateTime(2018, 3, 15);
            var dateAfterUpadate = new DateTime(2018, 3, 16);

            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                TestGlobals.FactorFileProvider);

            // Before factor file update date (2018, 3, 15)
            _rawDataEnumerator.CurrentValue = new Tick(
                dateBeforeUpadate,
                _config.Symbol,
                10,
                10,
                10);

            Assert.IsTrue(enumerator.MoveNext());
            var factorFile = TestGlobals.FactorFileProvider.Get(_config.Symbol);
            var expectedFactor = factorFile.GetPriceFactor(dateBeforeUpadate, DataNormalizationMode.Adjusted);
            var tick = enumerator.Current as Tick;
            Assert.AreEqual(expectedFactor, _config.PriceScaleFactor);
            Assert.AreEqual(10 * expectedFactor, tick.Price);
            Assert.AreEqual(10 * expectedFactor, tick.Value);

            // At factor file update date (2018, 3, 15)
            _rawDataEnumerator.CurrentValue = new Tick(
                dateAtUpadate,
                _config.Symbol,
                10,
                10,
                10);
            Assert.IsTrue(enumerator.MoveNext());
            var expectedFactor2 = factorFile.GetPriceFactor(dateAtUpadate, DataNormalizationMode.Adjusted);
            var tick2 = enumerator.Current as Tick;
            Assert.AreEqual(expectedFactor2, _config.PriceScaleFactor);
            Assert.AreEqual(10 * expectedFactor2, tick2.Price);
            Assert.AreEqual(10 * expectedFactor2, tick2.Value);

            // After factor file update date (2018, 3, 15)
            _rawDataEnumerator.CurrentValue = new Tick(
                dateAfterUpadate,
                _config.Symbol,
                10,
                10,
                10);
            Assert.IsTrue(enumerator.MoveNext());
            var expectedFactor3 = factorFile.GetPriceFactor(dateAfterUpadate, DataNormalizationMode.Adjusted);
            var tick3 = enumerator.Current as Tick;
            Assert.AreEqual(expectedFactor3, _config.PriceScaleFactor);
            Assert.AreEqual(10 * expectedFactor3, tick3.Price);
            Assert.AreEqual(10 * expectedFactor3, tick3.Value);

            enumerator.Dispose();
        }

        [Test]
        public void PricesAreProperlyAdjustedForLookAheadScaledRawDataNormalizationMode2()
        {
            var factorFileEntries = new[]
            {
                new DateTime(2005, 02, 25),
                new DateTime(2012, 08, 08),
                new DateTime(2013, 05, 08),
                new DateTime(2014, 08, 06),
                new DateTime(2015, 08, 05)
            };
            var endDate = factorFileEntries.Last().AddDays(1);

            var config = GetConfig(Symbols.AAPL, Resolution.Daily);
            config.DataNormalizationMode = DataNormalizationMode.ScaledRaw;

            using var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                config,
                TestGlobals.FactorFileProvider,
                endDate: endDate);

            var price = 100m;
            var factorFile = TestGlobals.FactorFileProvider.Get(config.Symbol);
            var endDateFactor = factorFile.GetPriceFactor(endDate, config.DataNormalizationMode);

            var performAssertions = (DateTime date) =>
            {
                var expectedFactor = factorFile.GetPriceFactor(date, config.DataNormalizationMode);
                Assert.AreEqual(expectedFactor / endDateFactor, config.PriceScaleFactor);

                var tradeBar = enumerator.Current as TradeBar;
                var expectedValue = price / config.PriceScaleFactor;
                Assert.AreEqual(expectedValue, tradeBar.Price);
                Assert.AreEqual(expectedValue, tradeBar.Open);
                Assert.AreEqual(expectedValue, tradeBar.Close);
                Assert.AreEqual(expectedValue, tradeBar.High);
                Assert.AreEqual(expectedValue, tradeBar.Low);
                Assert.AreEqual(expectedValue, tradeBar.Value);

                return expectedFactor;
            };

            foreach (var factorFileDate in factorFileEntries)
            {
                // before split
                var dateBeforeSplit = factorFileDate.AddDays(-1);
                _rawDataEnumerator.CurrentValue = new TradeBar(dateBeforeSplit, config.Symbol, price, price, price, price, price);
                Assert.IsTrue(enumerator.MoveNext());
                var expectedFactorBeforeSplit = performAssertions(dateBeforeSplit);

                // at split
                _rawDataEnumerator.CurrentValue = new TradeBar(factorFileDate, config.Symbol, price, price, price, price, price);
                Assert.IsTrue(enumerator.MoveNext());
                var expectedFactorAtSplit = performAssertions(factorFileDate);
                Assert.AreEqual(expectedFactorBeforeSplit, expectedFactorAtSplit);

                // after split
                var dateAfterSplit = factorFileDate.AddDays(1);
                _rawDataEnumerator.CurrentValue = new TradeBar(dateAfterSplit, config.Symbol, price, price, price, price, price);
                Assert.IsTrue(enumerator.MoveNext());
                var expectedFactorAfterSplit = performAssertions(dateAfterSplit);
                Assert.AreNotEqual(expectedFactorAtSplit, expectedFactorAfterSplit);

                if (factorFileDate == factorFileEntries.Last())
                {
                    // prices should have been adjusted to the end date prices, instead of the latest factor file entry (today),
                    // So the last factor should be 1.
                    Assert.AreEqual(1m, config.PriceScaleFactor);
                }
            }
        }

        private static SubscriptionDataConfig GetConfig(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(typeof(TradeBar),
                symbol,
                resolution,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
        }

        private class RawDataEnumerator : IEnumerator<BaseData>
        {
            public bool MoveNextReturnValue { get; set; }
            public BaseData CurrentValue { get; set; }

            public BaseData Current => CurrentValue;

            object IEnumerator.Current => CurrentValue;

            public RawDataEnumerator()
            {
                MoveNextReturnValue = true;
            }
            public bool MoveNext()
            {
                return MoveNextReturnValue;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
            public void Dispose()
            {
            }
        }
    }
}
