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

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class RenkoConsolidatorTests
    {
        [Test]
        public void ClassicOutputTypeIsRenkoBar()
        {
            var consolidator = new RenkoConsolidator(10, x => x.Value, x => 0);
            Assert.AreEqual(typeof(RenkoBar), consolidator.OutputType);
        }

        [Test]
        public void ClassicConsolidatesOnBrickHigh()
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
            Assert.IsNotNull(bar);

            Assert.AreEqual(0m, bar.Open);
            Assert.AreEqual(10m, bar.Close);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void ClassicConsolidatesOnBrickLow()
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
            Assert.IsNotNull(bar);

            Assert.AreEqual(10m, bar.Open);
            Assert.AreEqual(0m, bar.Close);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void ClassicCyclesUpAndDown()
        {
            RenkoBar bar = null;
            int rcount = 0;
            var consolidator = new RenkoConsolidator(1m, x => x.Value, x => 0);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                rcount++;
                bar = consolidated;
            };

            var reference = DateTime.Today;

            // opens at 0
            consolidator.Update(new IndicatorDataPoint(reference, 0));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(1), .5m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(2), 1m));
            Assert.IsNotNull(bar);

            Assert.AreEqual(0m, bar.Open);
            Assert.AreEqual(1m, bar.Close);
            Assert.AreEqual(0, bar.Volume);
            Assert.AreEqual(1m, bar.High);
            Assert.AreEqual(0m, bar.Low);
            Assert.IsTrue(bar.IsClosed);
            Assert.AreEqual(reference, bar.Start);
            Assert.AreEqual(reference.AddSeconds(2), bar.EndTime);

            bar = null;

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(3), 1.5m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(4), 1m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(5), .5m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(6), 0m));
            Assert.IsNotNull(bar);

            // ReSharper disable HeuristicUnreachableCode - ReSharper doesn't realiz this can be set via the event handler
            Assert.AreEqual(1m, bar.Open);
            Assert.AreEqual(0m, bar.Close);
            Assert.AreEqual(0, bar.Volume);
            Assert.AreEqual(1.5m, bar.High);
            Assert.AreEqual(0m, bar.Low);
            Assert.IsTrue(bar.IsClosed);
            Assert.AreEqual(reference.AddSeconds(2), bar.Start);
            Assert.AreEqual(reference.AddSeconds(6), bar.EndTime);

            bar = null;

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(7), -0.5m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(8), -0.9999999m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(9), -0.01m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(10), 0.25m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(9), 0.75m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(10), 0.9999999m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(10), 0.25m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(9), -0.25m));
            Assert.IsNull(bar);

            consolidator.Update(new IndicatorDataPoint(reference.AddSeconds(10), -1m));
            Assert.IsNotNull(bar);

            Assert.AreEqual(0m, bar.Open);
            Assert.AreEqual(-1m, bar.Close);
            Assert.AreEqual(0, bar.Volume);
            Assert.AreEqual(0.9999999m, bar.High);
            Assert.AreEqual(-1m, bar.Low);
            Assert.IsTrue(bar.IsClosed);
            Assert.AreEqual(reference.AddSeconds(6), bar.Start);
            Assert.AreEqual(reference.AddSeconds(10), bar.EndTime);

            // ReSharper restore HeuristicUnreachableCode
        }
    }
}
