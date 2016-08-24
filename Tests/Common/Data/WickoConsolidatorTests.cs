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
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class WickoConsolidatorTests
    {
        [Test]
        public void OututTypeIsRenkoBar()
        {
            var consolidator = new WickoConsolidator(10, x => x.Value);

            Assert.AreEqual(typeof(WickoBar), consolidator.OutputType);
        }

        [Test]
        public void OneFallingWicko()
        {
            var consolidator = new WickoConsolidator(0.0001m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 0.001m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 0.00089m));

            Assert.AreEqual(wickos.Count, 1);

            Assert.AreEqual(wickos[0].Open, 0.001m);
            Assert.AreEqual(wickos[0].High, 0.001m);
            Assert.AreEqual(wickos[0].Low, 0.0009m);
            Assert.AreEqual(wickos[0].Close, 0.0009m);
            Assert.AreEqual(wickos[0].Trend, Trend.Falling);
            Assert.AreEqual(wickos[0].Spread, 0.0001m);
            Assert.AreEqual(wickos[0].Start, tickOn1);
            Assert.AreEqual(wickos[0].EndTime, tickOn2);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Start, tickOn2);
            Assert.AreEqual(openWicko.EndTime, tickOn2);
            Assert.AreEqual(openWicko.Open, 0.0009m);
            Assert.AreEqual(openWicko.High, 0.0009m);
            Assert.AreEqual(openWicko.Low, 0.00089m);
            Assert.AreEqual(openWicko.Close, 0.00089m);
        }


















    }
}
