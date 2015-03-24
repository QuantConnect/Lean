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
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Tests.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class RenkoConsolidatorTests
    {
        [Test]
        public void CyclesUpAndDownOnSineWave()
        {
            RenkoBar bar;
            int count = 0;
            int rcount = 0;
            var consolidator = new RenkoConsolidator(1m, x => x.Value, x => 0);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                rcount++;
                bar = consolidated;
                Console.WriteLine("O {0} C {1}", bar.Open.ToString("0.00"), bar.Close.ToString("0.00"));
            };

            foreach (var data in TestHelper.GetTradeBarStream("spy_10_min.txt"))
            {
                consolidator.Update(data);
                count++;
            }

            Console.WriteLine("RC: " + rcount + " C " + count);
        }

        [Test]
        public void OututTypeIsRenkoBar()
        {
            var consolidator = new RenkoConsolidator(10, x => x.Value, x => 0);
            Assert.AreEqual(typeof(RenkoBar), consolidator.OutputType);
        }

        [Test]
        public void ConsolidatesAfterPassingBrickHigh()
        {
            RenkoBar bar = null;
            var consolidator = new RenkoConsolidator(10, x => x.Value, x => 0);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                bar = consolidated;
            };

            var reference = DateTime.Today;
            consolidator.Update(new IndicatorDataPoint(reference, 0m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddHours(1), 5m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddHours(2), 10m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddHours(3), 10m + Extensions.GetDecimalEpsilon()));
            Assert.IsNotNull(bar);

            Assert.AreEqual(0m, bar.Open);
            Assert.AreEqual(10m, bar.Close);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void ConsolidatesAfterPassingBrickLow()
        {
            RenkoBar bar = null;
            var consolidator = new RenkoConsolidator(10, x => x.Value, x => 0);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                bar = consolidated;
            };

            var reference = DateTime.Today;
            consolidator.Update(new IndicatorDataPoint(reference, 10m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddHours(1), 2m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddHours(2), 0m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddHours(3), 0m - Extensions.GetDecimalEpsilon()));
            Assert.IsNotNull(bar);

            Assert.AreEqual(10m, bar.Open);
            Assert.AreEqual(0m, bar.Close);
            Assert.IsTrue(bar.IsClosed);
        }
    }
}
