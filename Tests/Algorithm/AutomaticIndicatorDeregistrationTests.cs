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
using System.Runtime.CompilerServices;
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

        private List<IDataConsolidator> ConsolidatorsOf(Symbol symbol)
        {
            return _algorithm.SubscriptionManager.Subscriptions
                .Where(config => config.Symbol == symbol)
                .SelectMany(config => config.Consolidators)
                .ToList();
        }

        private int ConsolidatorCount(Symbol symbol)
        {
            return ConsolidatorsOf(symbol).Count;
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
        public void RemovesMultiSymbolIndicatorConsolidatorsOneSymbolAtATime()
        {
            var beta = _algorithm.B(_spy, _ibm, 10, Resolution.Daily);
            Assert.AreEqual(2, beta.Consolidators.Count);

            _algorithm.Securities.Remove(_spy);

            Assert.AreEqual(1, beta.Consolidators.Count);
            Assert.AreEqual(0, ConsolidatorCount(_spy));
            Assert.AreEqual(1, ConsolidatorCount(_ibm));

            _algorithm.Securities.Remove(_ibm);

            Assert.IsEmpty(beta.Consolidators);
            Assert.AreEqual(0, ConsolidatorCount(_ibm));
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
        public void RemovesManuallyRegisteredIndicatorsAndRawConsolidators()
        {
            var sma = new SimpleMovingAverage(10);
            _algorithm.RegisterIndicator(_spy, sma, Resolution.Minute);

            var rawConsolidator = new TradeBarConsolidator(TimeSpan.FromHours(1));
            _algorithm.SubscriptionManager.AddConsolidator(_spy, rawConsolidator);

            _algorithm.Securities.Remove(_spy);

            Assert.IsEmpty(sma.Consolidators);
            Assert.AreEqual(0, ConsolidatorCount(_spy));
        }

        [Test]
        public void RemovingTheMirrorOptionKeepsTheContractConsolidators()
        {
            var aapl = _algorithm.AddEquity("AAPL").Symbol;
            var call = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("AAPL", OptionRight.Call, 250m, new DateTime(2016, 01, 15))).Symbol;
            var put = _algorithm.AddOptionContract(Symbols.CreateOptionSymbol("AAPL", OptionRight.Put, 250m, new DateTime(2016, 01, 15))).Symbol;

            var delta = _algorithm.D(call, put, resolution: Resolution.Minute);
            Assert.AreEqual(3, delta.Consolidators.Count);

            _algorithm.Securities.Remove(put);

            Assert.AreEqual(0, ConsolidatorCount(put));
            Assert.AreEqual(1, ConsolidatorCount(call));
            Assert.AreEqual(1, ConsolidatorCount(aapl));
            Assert.AreEqual(2, delta.Consolidators.Count);
            Assert.IsTrue(delta.Consolidators.All(consolidator => ConsolidatorsOf(call).Contains(consolidator) || ConsolidatorsOf(aapl).Contains(consolidator)));
        }

        [Test]
        public void RemovingTheReferenceSymbolKeepsTheTargetConsolidator()
        {
            var beta = _algorithm.B(_spy, _ibm, 10, Resolution.Daily);
            Assert.AreEqual(2, beta.Consolidators.Count);

            _algorithm.Securities.Remove(_ibm);

            Assert.AreEqual(0, ConsolidatorCount(_ibm));
            Assert.AreEqual(1, ConsolidatorCount(_spy));
            Assert.AreEqual(1, beta.Consolidators.Count);
            Assert.IsTrue(ConsolidatorsOf(_spy).Contains(beta.Consolidators.Single()));
        }

        [Test]
        public void DeregisteringMultiSymbolIndicatorsRemovesAllTheirConsolidators()
        {
            var beta = _algorithm.B(_spy, _ibm, 10, Resolution.Daily);

            _algorithm.DeregisterIndicator(beta);

            Assert.IsEmpty(beta.Consolidators);
            Assert.AreEqual(0, ConsolidatorCount(_spy));
            Assert.AreEqual(0, ConsolidatorCount(_ibm));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DoesNotKeepConsolidatorsRemovedThroughTheSubscriptionManager(bool automaticDeregistration)
        {
            _algorithm.Settings.AutomaticIndicatorDeregistration = automaticDeregistration;

            var consolidator = ConsolidateAndRemove();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.IsFalse(consolidator.IsAlive);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DoesNotKeepIndicatorsWhoseConsolidatorWasRemovedThroughTheSubscriptionManager(bool automaticDeregistration)
        {
            _algorithm.Settings.AutomaticIndicatorDeregistration = automaticDeregistration;

            var indicator = RegisterIndicatorAndRemoveItsConsolidator();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.IsFalse(indicator.IsAlive);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private WeakReference ConsolidateAndRemove()
        {
            var consolidator = _algorithm.Consolidate(_spy, Resolution.Hour, (TradeBar bar) => { });
            _algorithm.SubscriptionManager.RemoveConsolidator(_spy, consolidator);
            // the scan queue drops removed consolidators on the next scan
            _algorithm.SubscriptionManager.ScanPastConsolidators(DateTime.MaxValue, _algorithm);
            return new WeakReference(consolidator);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private WeakReference RegisterIndicatorAndRemoveItsConsolidator()
        {
            var sma = _algorithm.SMA(_spy, 10, Resolution.Minute);
            _algorithm.SubscriptionManager.RemoveConsolidator(_spy, sma.Consolidators.Single());
            _algorithm.SubscriptionManager.ScanPastConsolidators(DateTime.MaxValue, _algorithm);
            return new WeakReference(sma);
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
        public void DoesNotTrackIndicatorsRegisteredWhileDisabled()
        {
            _algorithm.Settings.AutomaticIndicatorDeregistration = false;
            var sma = _algorithm.SMA(_spy, 10, Resolution.Minute);
            _algorithm.Settings.AutomaticIndicatorDeregistration = true;

            _algorithm.Securities.Remove(_spy);

            // the consolidator is removed but the indicator was never linked to it
            Assert.AreEqual(0, ConsolidatorCount(_spy));
            Assert.AreEqual(1, sma.Consolidators.Count);
        }

        [Test]
        public void IsEnabledByDefault()
        {
            Assert.IsTrue(new QCAlgorithm().Settings.AutomaticIndicatorDeregistration);
        }
    }
}
