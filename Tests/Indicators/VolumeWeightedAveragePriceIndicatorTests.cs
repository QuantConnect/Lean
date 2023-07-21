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
using QuantConnect.Indicators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class VolumeWeightedAveragePriceIndicatorTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            RenkoBarSize = 0.1m;
            return new VolumeWeightedAveragePriceIndicator(50);
        }

        protected override string TestFileName => "spy_with_vwap.txt";

        protected override string TestColumnName => "Moving VWAP 50";

        [Test]
        public void VwapComputesCorrectly()
        {
            const int period = 4;
            const int volume = 100;
            var ind = new VolumeWeightedAveragePriceIndicator(period);
            var data = new[] {1m, 10m, 100m, 1000m, 10000m, 1234m, 56789m};

            var seen = new List<decimal>();
            for (var i = 0; i < data.Length; i++)
            {
                var datum = data[i];
                seen.Add(datum);
                ind.Update(new TradeBar(DateTime.Now.AddSeconds(i), Symbols.SPY, datum, datum, datum, datum, volume));
                // When volume is constant, VWAP is a simple moving average
                Assert.AreEqual(Enumerable.Reverse(seen).Take(period).Average(), ind.Current.Value);
            }
        }

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var ind = new VolumeWeightedAveragePriceIndicator(3);

            ind.Update(new TradeBar(DateTime.UtcNow, Symbols.SPY, 1m, 1m, 1m, 1m, 1));
            ind.Update(new TradeBar(DateTime.UtcNow, Symbols.SPY, 1m, 1m, 1m, 1m, 1));
            Assert.IsFalse(ind.IsReady);
            ind.Update(new TradeBar(DateTime.UtcNow, Symbols.SPY, 1m, 1m, 1m, 1m, 1));
            Assert.IsTrue(ind.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var ind = CreateIndicator();

            foreach (var data in TestHelper.GetTradeBarStream(TestFileName))
            {
                ind.Update(data);
            }
            Assert.IsTrue(ind.IsReady);

            ind.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(ind);
            ind.Update(new TradeBar(DateTime.UtcNow, Symbols.SPY, 2m, 2m, 2m, 2m, 1));
            Assert.AreEqual(ind.Current.Value, 2m);
        }
        
        [Test]
        public void ResetsInnerVolumeWeightedAveragePriceIndicatorProperly()
        {
            var indicator = new TestVolumeWeightedAveragePriceIndicator(50);

            foreach (var data in TestHelper.GetTradeBarStream(TestFileName))
            {
                indicator.Update(data);
            }

            Assert.IsTrue(indicator.IsReady);

            var lastVWAPIndicator = indicator.GetInnerVolumeWeightedAveragePriceIndicator();

            Assert.AreNotEqual(0, lastVWAPIndicator.Samples);
            Assert.AreNotEqual(0, lastVWAPIndicator.Left.Samples);
            Assert.AreNotEqual(0, lastVWAPIndicator.Right.Samples);
            Assert.IsTrue(lastVWAPIndicator.IsReady);
            Assert.IsTrue(lastVWAPIndicator.Left.IsReady);
            Assert.IsTrue(lastVWAPIndicator.Right.IsReady);

            indicator.Reset();
            var newVWAPIndicator = indicator.GetInnerVolumeWeightedAveragePriceIndicator();

            Assert.IsTrue(Object.ReferenceEquals(lastVWAPIndicator, newVWAPIndicator));
            Assert.AreEqual(0, newVWAPIndicator.Samples);
            Assert.AreEqual(0, newVWAPIndicator.Left.Samples);
            Assert.AreEqual(0, newVWAPIndicator.Right.Samples);
            Assert.IsFalse(newVWAPIndicator.IsReady);
            Assert.IsFalse(newVWAPIndicator.Left.IsReady);
            Assert.IsFalse(newVWAPIndicator.Right.IsReady);
        }

        [Test]
        public void ResetsInnerPriceIndicatorProperly()
        {
            var indicator = new TestVolumeWeightedAveragePriceIndicator(50);

            foreach (var data in TestHelper.GetTradeBarStream(TestFileName))
            {
                indicator.Update(data);
            }

            Assert.IsTrue(indicator.IsReady);

            var lastPriceIndicator = indicator.GetInnerPriceIndicator();
            Assert.AreNotEqual(0, lastPriceIndicator.Samples);
            Assert.IsTrue(lastPriceIndicator.IsReady);

            indicator.Reset();

            var newPriceIndicator = indicator.GetInnerPriceIndicator();
            Assert.AreEqual(0, newPriceIndicator.Samples);
            Assert.IsFalse(newPriceIndicator.IsReady);
        }

        [Test]
        public void ResetsInnerVolumeIndicatorProperly()
        {
            var indicator = new TestVolumeWeightedAveragePriceIndicator(50);

            foreach (var data in TestHelper.GetTradeBarStream(TestFileName))
            {
                indicator.Update(data);
            }

            Assert.IsTrue(indicator.IsReady);

            var lastVolumeIndicator = indicator.GetInnerVolumeIndicator();
            Assert.AreNotEqual(0, lastVolumeIndicator.Samples);
            Assert.IsTrue(lastVolumeIndicator.IsReady);

            indicator.Reset();

            var newVolumeIndicator = indicator.GetInnerVolumeIndicator();
            Assert.AreEqual(0, newVolumeIndicator.Samples);
            Assert.IsFalse(newVolumeIndicator.IsReady);
        }
        
        /// <summary>
        /// The final value of this indicator is zero because it uses the Volume of the bars it receives.
        /// Since RenkoBar's don't always have Volume, the final current value is zero. Therefore we
        /// skip this test
        /// </summary>
        /// <param name="indicator"></param>
        protected override void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
        }
    }

    public class TestVolumeWeightedAveragePriceIndicator : VolumeWeightedAveragePriceIndicator
    {
        public TestVolumeWeightedAveragePriceIndicator(int period) : base(period)
        {
        }

        public CompositeIndicator GetInnerVolumeWeightedAveragePriceIndicator()
        {
            return VWAP;
        }

        public IndicatorBase GetInnerPriceIndicator()
        {
            return Price;
        }

        public IndicatorBase GetInnerVolumeIndicator()
        {
            return Volume;
        }
    }
}
