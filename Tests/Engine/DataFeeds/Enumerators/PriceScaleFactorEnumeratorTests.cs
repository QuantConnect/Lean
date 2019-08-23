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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using Tick = QuantConnect.Data.Market.Tick;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class PriceScaleFactorEnumeratorTests
    {
        private SubscriptionDataConfig _config;
        private FactorFile _factorFile;
        private RawDataEnumerator _rawDataEnumerator;

        [SetUp]
        public void Setup()
        {
            _config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            _factorFile = FactorFile.Read(
                _config.Symbol.Value, _config.Symbol.ID.Market);
            _rawDataEnumerator = new RawDataEnumerator();
        }

        [Test]
        public void EquityTradeBar()
        {
            var enumerator = new PriceScaleFactorEnumerator(
                _rawDataEnumerator,
                _config,
                new Lazy<FactorFile>(() => _factorFile));
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
                new Lazy<FactorFile>(() => _factorFile));
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
                new Lazy<FactorFile>(() => _factorFile));
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
                new Lazy<FactorFile>(() => _factorFile));
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
                new Lazy<FactorFile>(() => _factorFile));
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
                new Lazy<FactorFile>(() => _factorFile));

            // Before factor file update date (2018, 3, 15)
            _rawDataEnumerator.CurrentValue = new Tick(
                dateBeforeUpadate,
                _config.Symbol,
                10,
                10,
                10);

            Assert.IsTrue(enumerator.MoveNext());
            var expectedFactor = _factorFile.GetPriceScaleFactor(dateBeforeUpadate);
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
            var expectedFactor2 = _factorFile.GetPriceScaleFactor(dateAtUpadate);
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
            var expectedFactor3 = _factorFile.GetPriceScaleFactor(dateAfterUpadate);
            var tick3 = enumerator.Current as Tick;
            Assert.AreEqual(expectedFactor3, _config.PriceScaleFactor);
            Assert.AreEqual(10 * expectedFactor3, tick3.Price);
            Assert.AreEqual(10 * expectedFactor3, tick3.Value);

            enumerator.Dispose();
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
