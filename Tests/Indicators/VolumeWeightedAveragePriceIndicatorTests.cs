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
        public void ResetsProperly()
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
    }
}