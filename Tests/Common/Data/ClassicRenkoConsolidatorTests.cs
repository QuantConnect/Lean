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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class ClassicRenkoConsolidatorTests: BaseConsolidatorTests
    {
        [Test]
        public void ClassicOutputTypeIsRenkoBar()
        {
            using var consolidator = new ClassicRenkoConsolidator(10, x => x.Value, x => 0);
            Assert.AreEqual(typeof(RenkoBar), consolidator.OutputType);
        }

        [Test]
        public void ClassicConsolidatesOnBrickHigh()
        {
            RenkoBar bar = null;
            using var consolidator = new ClassicRenkoConsolidator(10, x => x.Value, x => 0);
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
            using var consolidator = new ClassicRenkoConsolidator(10, x => x.Value, x => 0);
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
        public void ConsistentRenkos()
        {
            // Test Renko bar consistency amongst three consolidators starting at different times

            var time = new DateTime(2016, 1, 1);
            var testValues = new List<decimal>
            {
                1.38687m, 1.38688m, 1.38687m, 1.38686m, 1.38685m, 1.38683m,
                1.38682m, 1.38682m, 1.38684m, 1.38682m, 1.38682m, 1.38680m,
                1.38681m, 1.38686m, 1.38688m, 1.38688m, 1.38690m, 1.38690m,
                1.38691m, 1.38692m, 1.38694m, 1.38695m, 1.38697m, 1.38697m,
                1.38700m, 1.38699m, 1.38699m, 1.38699m, 1.38698m, 1.38699m,
                1.38697m, 1.38698m, 1.38698m, 1.38697m, 1.38698m, 1.38698m,
                1.38697m, 1.38697m, 1.38700m, 1.38702m, 1.38701m, 1.38699m,
                1.38697m, 1.38698m, 1.38696m, 1.38698m, 1.38697m, 1.38695m,
                1.38695m, 1.38696m, 1.38693m, 1.38692m, 1.38693m, 1.38693m,
                1.38692m, 1.38693m, 1.38692m, 1.38690m, 1.38686m, 1.38685m,
                1.38687m, 1.38686m, 1.38686m, 1.38686m, 1.38686m, 1.38685m,
                1.38684m, 1.38678m, 1.38679m, 1.38680m, 1.38680m, 1.38681m,
                1.38685m, 1.38685m, 1.38683m, 1.38682m, 1.38682m, 1.38683m,
                1.38682m, 1.38683m, 1.38682m, 1.38681m, 1.38680m, 1.38681m,
                1.38681m, 1.38681m, 1.38682m, 1.38680m, 1.38679m, 1.38678m,
                1.38675m, 1.38678m, 1.38678m, 1.38678m, 1.38682m, 1.38681m,
                1.38682m, 1.38680m, 1.38682m, 1.38683m, 1.38685m, 1.38683m,
                1.38683m, 1.38684m, 1.38683m, 1.38683m, 1.38684m, 1.38685m,
                1.38684m, 1.38683m, 1.38686m, 1.38685m, 1.38685m, 1.38684m,
                1.38685m, 1.38682m, 1.38684m, 1.38683m, 1.38682m, 1.38683m,
                1.38685m, 1.38685m, 1.38685m, 1.38683m, 1.38685m, 1.38684m,
                1.38686m, 1.38693m, 1.38695m, 1.38693m, 1.38694m, 1.38693m,
                1.38692m, 1.38693m, 1.38695m, 1.38697m, 1.38698m, 1.38695m,
                1.38696m
            };


            var consolidator1 = new ClassicRenkoConsolidator(0.0001m);
            var consolidator2 = new ClassicRenkoConsolidator(0.0001m);
            var consolidator3 = new ClassicRenkoConsolidator(0.0001m);

            // Update each of our consolidators starting at different indexes of test values
            for (int i = 0; i < testValues.Count; i++)
            {
                var data = new IndicatorDataPoint(time.AddSeconds(i), testValues[i]);
                consolidator1.Update(data);

                if (i > 10)
                {
                    consolidator2.Update(data);
                }

                if (i > 20)
                {
                    consolidator3.Update(data);
                }
            }

            // Assert that consolidator 2 and 3 price is the same as 1. Even though they started at different
            // indexes they should be the same
            var bar1 = consolidator1.Consolidated as RenkoBar;
            var bar2 = consolidator2.Consolidated as RenkoBar;
            var bar3 = consolidator3.Consolidated as RenkoBar;

            Assert.AreEqual(bar1.Close, bar2.Close);
            Assert.AreEqual(bar1.Close, bar3.Close);

            consolidator1.Dispose();
            consolidator2.Dispose();
            consolidator3.Dispose();
        }

        [Test]
        public void ClassicCyclesUpAndDown()
        {
            RenkoBar bar = null;
            int rcount = 0;
            using var consolidator = new ClassicRenkoConsolidator(1m, x => x.Value, x => 0);
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

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SelectorCanBeOptionalWhenVolumeSelectorIsPassed(Language language)
        {
            if (language == Language.CSharp)
            {
                Assert.DoesNotThrow(() =>
                {
                    using var consolidator = new ClassicRenkoConsolidator(10, null, x => x.Value);
                });
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("test", @"
from AlgorithmImports import *

def getConsolidator():
    return ClassicRenkoConsolidator(10, None, lambda x: x.Value)
");
                    Assert.DoesNotThrow(() =>
                    {
                        var consolidator = testModule.GetAttr("getConsolidator").Invoke();
                    });
                }
            }
        }

        protected override IDataConsolidator CreateConsolidator()
        {
            return new ClassicRenkoConsolidator(0.0001m);
        }
    }
}
