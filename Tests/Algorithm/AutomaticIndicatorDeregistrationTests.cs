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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AutomaticIndicatorDeregistrationTests
    {
        private QCAlgorithm _algorithm;
        private Symbol _spy;
        private Symbol _ibm;

        [SetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _spy = _algorithm.AddEquity("SPY").Symbol;
            _ibm = _algorithm.AddEquity("IBM").Symbol;
        }

        private int ConsolidatorCount(Symbol symbol)
        {
            return _algorithm.SubscriptionManager.Subscriptions
                .Where(config => config.Symbol == symbol)
                .Sum(config => config.Consolidators.Count);
        }

        [Test]
        public void DeregistersHelperCreatedIndicatorsOfTheRemovedSecurityOnly()
        {
            var rsi = _algorithm.RSI(_spy, 14, resolution: Resolution.Minute);
            var sma = _algorithm.SMA(_spy, 10, Resolution.Minute);
            var ibmSma = _algorithm.SMA(_ibm, 10, Resolution.Minute);

            Assert.AreEqual(2, ConsolidatorCount(_spy));
            Assert.AreEqual(1, ConsolidatorCount(_ibm));

            _algorithm.Securities.Remove(_spy);

            Assert.IsEmpty(rsi.Consolidators);
            Assert.IsEmpty(sma.Consolidators);
            Assert.AreEqual(0, ConsolidatorCount(_spy));

            Assert.AreEqual(1, ibmSma.Consolidators.Count);
            Assert.AreEqual(1, ConsolidatorCount(_ibm));
        }

        [Test]
        public void DeregistersHelperCreatedConsolidators()
        {
            _algorithm.Consolidate(_spy, Resolution.Hour, (TradeBar bar) => { });

            Assert.AreEqual(1, ConsolidatorCount(_spy));

            _algorithm.Securities.Remove(_spy);

            Assert.AreEqual(0, ConsolidatorCount(_spy));
        }

        [Test]
        public void DeregistersMultiSymbolIndicatorsThroughAnyOfTheirSymbols()
        {
            var beta = _algorithm.B(_spy, _ibm, 10, Resolution.Daily);

            Assert.AreEqual(2, beta.Consolidators.Count);

            _algorithm.Securities.Remove(_spy);

            Assert.IsEmpty(beta.Consolidators);
            Assert.AreEqual(0, ConsolidatorCount(_spy));
            Assert.AreEqual(0, ConsolidatorCount(_ibm));

            Assert.DoesNotThrow(() => _algorithm.Securities.Remove(_ibm));
        }

        [Test]
        public void IsANoOpAfterManualDeregistration()
        {
            var sma = _algorithm.SMA(_spy, 10, Resolution.Minute);

            _algorithm.DeregisterIndicator(sma);
            Assert.AreEqual(0, ConsolidatorCount(_spy));

            Assert.DoesNotThrow(() => _algorithm.Securities.Remove(_spy));
            Assert.AreEqual(0, ConsolidatorCount(_spy));
        }

        [Test]
        public void DeregistersManuallyRegisteredIndicatorsButKeepsRawConsolidators()
        {
            var sma = new SimpleMovingAverage(10);
            _algorithm.RegisterIndicator(_spy, sma, Resolution.Minute);

            using var rawConsolidator = new TradeBarConsolidator(TimeSpan.FromHours(1));
            _algorithm.SubscriptionManager.AddConsolidator(_spy, rawConsolidator);

            _algorithm.Securities.Remove(_spy);

            Assert.IsEmpty(sma.Consolidators);
            Assert.AreEqual(1, ConsolidatorCount(_spy));
            Assert.IsTrue(_algorithm.SubscriptionManager.Subscriptions
                .Where(config => config.Symbol == _spy)
                .Any(config => config.Consolidators.Contains(rawConsolidator)));
        }

        [Test]
        public void DoesNotDeregisterWhenDisabled()
        {
            _algorithm.Settings.AutomaticIndicatorDeregistration = false;
            var sma = _algorithm.SMA(_spy, 10, Resolution.Minute);

            _algorithm.Securities.Remove(_spy);

            Assert.AreEqual(1, sma.Consolidators.Count);
            Assert.AreEqual(1, ConsolidatorCount(_spy));
        }

        [Test]
        public void IsEnabledByDefault()
        {
            Assert.IsTrue(new QCAlgorithm().Settings.AutomaticIndicatorDeregistration);
        }
    }
}
