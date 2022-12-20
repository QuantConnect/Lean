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

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class VolumeRenkoConsolidatorTests
    {
        [Test]
        public void OutputTypeIsVolumeRenkoBar()
        {
            var consolidator = new VolumeRenkoConsolidator(10);
            Assert.AreEqual(typeof(VolumeRenkoBar), consolidator.OutputType);
        }

        [Test]
        public void ConsolidatesOnTickVolumeReached()
        {
            VolumeRenkoBar bar = null;
            var consolidator = new VolumeRenkoConsolidator(10);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                bar = consolidated;
            };

            var reference = DateTime.Today;
            consolidator.Update(new Tick(reference, Symbol.Empty, String.Empty, String.Empty, 2m, 1m));
            Assert.IsNull(bar);

            consolidator.Update(new Tick(reference.AddHours(1), Symbol.Empty, String.Empty, String.Empty, 3m, 2m));
            Assert.IsNull(bar);

            consolidator.Update(new Tick(reference.AddHours(2), Symbol.Empty, String.Empty, String.Empty, 3m, 3m));
            Assert.IsNull(bar);

            consolidator.Update(new Tick(reference.AddHours(3), Symbol.Empty, String.Empty, String.Empty, 2m, 2m));
            Assert.IsNotNull(bar);

            Assert.AreEqual(1m, bar.Open);
            Assert.AreEqual(3m, bar.High);
            Assert.AreEqual(1m, bar.Low);
            Assert.AreEqual(2m, bar.Close);
            Assert.AreEqual(reference.AddHours(3), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void ConsolidatesOnTraderBarVolumeReached()
        {
            VolumeRenkoBar bar = null;
            var consolidator = new VolumeRenkoConsolidator(10);
            consolidator.DataConsolidated += (sender, consolidated) => 
            {
                bar = consolidated;
            };

            var reference = DateTime.Today;
            consolidator.Update(new TradeBar(reference, Symbol.Empty, 1m, 2m, 0.5m, 1.5m, 2m, new TimeSpan(1, 0, 0)));
            Assert.IsNull(bar);

            consolidator.Update(new TradeBar(reference.AddHours(1), Symbol.Empty, 1.5m, 3m, 1m, 3m, 3m, new TimeSpan(1, 0, 0)));
            Assert.IsNull(bar);

            consolidator.Update(new TradeBar(reference.AddHours(2), Symbol.Empty, 3m, 3m, 1m, 2m, 3m, new TimeSpan(1, 0, 0)));
            Assert.IsNull(bar);

            consolidator.Update(new TradeBar(reference.AddHours(3), Symbol.Empty, 2m, 4m, 1.5m, 2.5m, 2m, new TimeSpan(1, 0, 0)));
            Assert.IsNotNull(bar);

            Assert.AreEqual(1m, bar.Open);
            Assert.AreEqual(4m, bar.High);
            Assert.AreEqual(0.5m, bar.Low);
            Assert.AreEqual(2.5m, bar.Close);
            Assert.AreEqual(reference.AddHours(4), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void ConsolidatesOnQuoteBar()
        {
            VolumeRenkoBar bar = null;
            var consolidator = new VolumeRenkoConsolidator(10);

            var reference = DateTime.Today;
            Assert.Throws<ArgumentException>(() => 
                consolidator.Update(new QuoteBar(reference, Symbol.Empty, new Bar(1m, 1m, 1m, 1m), 1m, new Bar(1m, 1m, 1m, 1m), 1m, TimeSpan.MinValue)));
        }

        [Test]
        public void ConsistentRenkos()
        {
            // Test Renko bar consistency amongst three consolidators starting at different times

            var time = new DateTime(2016, 1, 1);
            var testValues = new List<decimal[]>
            {
                new decimal[]{5m, 5m}, new decimal[]{5m, 3m}, new decimal[]{5m, 7m}, new decimal[]{5m, 6m},
                new decimal[]{5m, 5m}, new decimal[]{5m, 3m}, new decimal[]{5m, 7m}, new decimal[]{5m, 6m},
                new decimal[]{5m, 5m}, new decimal[]{5m, 3m}, new decimal[]{5m, 7m}, new decimal[]{5m, 6m},
                new decimal[]{5m, 5m}, new decimal[]{5m, 3m}, new decimal[]{5m, 7m}, new decimal[]{5m, 6m}
            };

            var consolidator1 = new VolumeRenkoConsolidator(20m);
            var consolidator2 = new VolumeRenkoConsolidator(20m);
            var consolidator3 = new VolumeRenkoConsolidator(20m);

            // Update each of our consolidators starting at different indexes of test values
            for (int i = 0; i < testValues.Count; i++)
            {
                var data = new Tick(time.AddSeconds(i), Symbol.Empty, String.Empty, String.Empty, testValues[i][0], testValues[i][1]);
                consolidator1.Update(data);

                if (i > 3)
                {
                    consolidator2.Update(data);
                }

                if (i > 7)
                {
                    consolidator3.Update(data);
                }
            }

            // Assert that consolidator 2 and 3 price is the same as 1. Even though they started at different
            // indexes they should be the same
            var bar1 = consolidator1.Consolidated as VolumeRenkoBar;
            var bar2 = consolidator2.Consolidated as VolumeRenkoBar;
            var bar3 = consolidator3.Consolidated as VolumeRenkoBar;

            Assert.AreEqual(bar1.Close, bar2.Close);
            Assert.AreEqual(bar1.Close, bar3.Close);

            consolidator1.Dispose();
            consolidator2.Dispose();
            consolidator3.Dispose();
        }

        [Test]
        public void MultipleConsoldation()
        {
            VolumeRenkoBar bar = null;
            var consolidator = new VolumeRenkoConsolidator(10m);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                bar = consolidated;
            };

            var reference = DateTime.Today;
            consolidator.Update(new Tick(reference, Symbol.Empty, String.Empty, String.Empty, 2m, 1m));
            Assert.IsNull(bar);

            consolidator.Update(new Tick(reference.AddHours(1), Symbol.Empty, String.Empty, String.Empty, 3m, 2m));
            Assert.IsNull(bar);

            consolidator.Update(new Tick(reference.AddHours(2), Symbol.Empty, String.Empty, String.Empty, 3m, 3m));
            Assert.IsNull(bar);

            consolidator.Update(new Tick(reference.AddHours(3), Symbol.Empty, String.Empty, String.Empty, 2m, 2m));
            Assert.IsNotNull(bar);

            Assert.AreEqual(1m, bar.Open);
            Assert.AreEqual(3m, bar.High);
            Assert.AreEqual(1m, bar.Low);
            Assert.AreEqual(2m, bar.Close);
            Assert.AreEqual(reference.AddHours(3), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);
            
            consolidator.Update(new Tick(reference.AddHours(4), Symbol.Empty, String.Empty, String.Empty, 4m, 1m));

            consolidator.Update(new Tick(reference.AddHours(5), Symbol.Empty, String.Empty, String.Empty, 3m, 2m));

            consolidator.Update(new Tick(reference.AddHours(6), Symbol.Empty, String.Empty, String.Empty, 4m, 3m));

            Assert.AreEqual(2m, bar.Open);
            Assert.AreEqual(3m, bar.High);
            Assert.AreEqual(1m, bar.Low);
            Assert.AreEqual(3m, bar.Close);
            Assert.AreEqual(reference.AddHours(6), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);    // bar is always closed since it is the consolidated bar instance

            consolidator.Update(new Tick(reference.AddHours(7), Symbol.Empty, String.Empty, String.Empty, 5m, 10m));

            // Not yet consolidated, so bar is not updated yet
            Assert.AreEqual(2m, bar.Open);
            Assert.AreEqual(3m, bar.High);
            Assert.AreEqual(1m, bar.Low);
            Assert.AreEqual(3m, bar.Close);
            Assert.AreEqual(reference.AddHours(6), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SelectorCanBeOptionalWhenVolumeSelectorIsPassed(Language language)
        {
            if (language == Language.CSharp)
            {
                Assert.DoesNotThrow(() =>
                {
                    using var consolidator = new VolumeRenkoConsolidator(10);
                });
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("test", @"
from AlgorithmImports import *

def getConsolidator():
    return VolumeRenkoConsolidator(10)
");
                    Assert.DoesNotThrow(() =>
                    {
                        var consolidator = testModule.GetAttr("getConsolidator").Invoke();
                    });
                }
            }
        }
    }
}
