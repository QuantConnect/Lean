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
        public void OutputTypeIsRenkoBar()
        {
            var consolidator = new WickoConsolidator(10.0m, x => x.Value);

            Assert.AreEqual(typeof(WickoBar), consolidator.OutputType);
        }

        [Test]
        public void NoFallingWicko()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 9.1m));

            Assert.AreEqual(wickos.Count, 0);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 10.0m);
            Assert.AreEqual(openWicko.Low, 9.1m);
            Assert.AreEqual(openWicko.Close, 9.1m);
        }

        [Test]
        public void NoRisingWicko()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.9m));

            Assert.AreEqual(wickos.Count, 0);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 10.9m);
            Assert.AreEqual(openWicko.Low, 10.0m);
            Assert.AreEqual(openWicko.Close, 10.9m);
        }

        [Test]
        public void NoFallingWickoKissLimit()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 9.0m));

            Assert.AreEqual(wickos.Count, 0);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 10.0m);
            Assert.AreEqual(openWicko.Low, 9.0m);
            Assert.AreEqual(openWicko.Close, 9.0m);
        }

        [Test]
        public void NoRisingWickoKissLimit()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 11.0m));

            Assert.AreEqual(wickos.Count, 0);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 11.0m);
            Assert.AreEqual(openWicko.Low, 10.0m);
            Assert.AreEqual(openWicko.Close, 11.0m);
        }

        [Test]
        public void OneFallingWicko()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 8.9m));

            Assert.AreEqual(wickos.Count, 1);

            Assert.AreEqual(wickos[0].Open, 10.0m);
            Assert.AreEqual(wickos[0].High, 10.0m);
            Assert.AreEqual(wickos[0].Low, 9.0m);
            Assert.AreEqual(wickos[0].Close, 9.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[0].Spread, 1.0m);
            Assert.AreEqual(wickos[0].Start, tickOn1);
            Assert.AreEqual(wickos[0].EndTime, tickOn2);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Start, tickOn2);
            Assert.AreEqual(openWicko.EndTime, tickOn2);
            Assert.AreEqual(openWicko.Open, 9.0m);
            Assert.AreEqual(openWicko.High, 9.0m);
            Assert.AreEqual(openWicko.Low, 8.9m);
            Assert.AreEqual(openWicko.Close, 8.9m);
        }

        [Test]
        public void OneRisingWicko()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.1m));

            Assert.AreEqual(wickos.Count, 1);

            Assert.AreEqual(wickos[0].Open, 9.0m);
            Assert.AreEqual(wickos[0].High, 10.0m);
            Assert.AreEqual(wickos[0].Low, 9.0m);
            Assert.AreEqual(wickos[0].Close, 10.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[0].Spread, 1.0m);
            Assert.AreEqual(wickos[0].Start, tickOn1);
            Assert.AreEqual(wickos[0].EndTime, tickOn2);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Start, tickOn2);
            Assert.AreEqual(openWicko.EndTime, tickOn2);
            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 10.1m);
            Assert.AreEqual(openWicko.Low, 10.0m);
            Assert.AreEqual(openWicko.Close, 10.1m);
        }

        [Test]
        public void TwoFallingThenOneRisingWickos()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 8.9m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.1m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.2m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 8.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 7.8m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 7.6m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 8.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.2m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.1m));

            Assert.AreEqual(wickos.Count, 3);

            Assert.AreEqual(wickos[0].Open, 10.0m);
            Assert.AreEqual(wickos[0].High, 10.5m);
            Assert.AreEqual(wickos[0].Low, 9.0m);
            Assert.AreEqual(wickos[0].Close, 9.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[0].Spread, 1.0m);

            Assert.AreEqual(wickos[1].Open, 9.0m);
            Assert.AreEqual(wickos[1].High, 9.2m);
            Assert.AreEqual(wickos[1].Low, 8.0m);
            Assert.AreEqual(wickos[1].Close, 8.0m);
            Assert.AreEqual(wickos[1].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[1].Spread, 1.0m);

            Assert.AreEqual(wickos[2].Open, 9.0m);
            Assert.AreEqual(wickos[2].High, 10.0m);
            Assert.AreEqual(wickos[2].Low, 7.6m);
            Assert.AreEqual(wickos[2].Close, 10.0m);
            Assert.AreEqual(wickos[2].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[2].Spread, 1.0m);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 10.1m);
            Assert.AreEqual(openWicko.Low, 10.0m);
            Assert.AreEqual(openWicko.Close, 10.1m);
        }

        [Test]
        public void TwoRisingThenOneFallingWickos()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.6m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.1m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.7m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.6m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.3m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.3m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.4m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.9m));

            Assert.AreEqual(wickos.Count, 3);

            Assert.AreEqual(wickos[0].Open, 10.0m);
            Assert.AreEqual(wickos[0].High, 11.0m);
            Assert.AreEqual(wickos[0].Low, 9.6m);
            Assert.AreEqual(wickos[0].Close, 11.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[0].Spread, 1.0m);

            Assert.AreEqual(wickos[1].Open, 11.0m);
            Assert.AreEqual(wickos[1].High, 12.0m);
            Assert.AreEqual(wickos[1].Low, 10.7m);
            Assert.AreEqual(wickos[1].Close, 12.0m);
            Assert.AreEqual(wickos[1].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[1].Spread, 1.0m);

            Assert.AreEqual(wickos[2].Open, 11.0m);
            Assert.AreEqual(wickos[2].High, 12.4m);
            Assert.AreEqual(wickos[2].Low, 10.0m);
            Assert.AreEqual(wickos[2].Close, 10.0m);
            Assert.AreEqual(wickos[2].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[2].Spread, 1.0m);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 10.0m);
            Assert.AreEqual(openWicko.High, 10.0m);
            Assert.AreEqual(openWicko.Low, 9.9m);
            Assert.AreEqual(openWicko.Close, 9.9m);
        }

        [Test]
        public void ThreeRisingGapWickos()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 14.0m));

            Assert.AreEqual(wickos.Count, 3);

            Assert.AreEqual(wickos[0].Start, tickOn1);
            Assert.AreEqual(wickos[0].EndTime, tickOn2);
            Assert.AreEqual(wickos[0].Open, 10.0m);
            Assert.AreEqual(wickos[0].High, 11.0m);
            Assert.AreEqual(wickos[0].Low, 10.0m);
            Assert.AreEqual(wickos[0].Close, 11.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[0].Spread, 1.0m);

            Assert.AreEqual(wickos[1].Start, tickOn2);
            Assert.AreEqual(wickos[1].EndTime, tickOn2);
            Assert.AreEqual(wickos[1].Open, 11.0m);
            Assert.AreEqual(wickos[1].High, 12.0m);
            Assert.AreEqual(wickos[1].Low, 11.0m);
            Assert.AreEqual(wickos[1].Close, 12.0m);
            Assert.AreEqual(wickos[1].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[1].Spread, 1.0m);

            Assert.AreEqual(wickos[2].Start, tickOn2);
            Assert.AreEqual(wickos[2].EndTime, tickOn2);
            Assert.AreEqual(wickos[2].Open, 12.0m);
            Assert.AreEqual(wickos[2].High, 13.0m);
            Assert.AreEqual(wickos[2].Low, 12.0m);
            Assert.AreEqual(wickos[2].Close, 13.0m);
            Assert.AreEqual(wickos[2].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[2].Spread, 1.0m);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Start, tickOn2);
            Assert.AreEqual(openWicko.EndTime, tickOn2);
            Assert.AreEqual(openWicko.Open, 13.0m);
            Assert.AreEqual(openWicko.High, 14.0m);
            Assert.AreEqual(openWicko.Low, 13.0m);
            Assert.AreEqual(openWicko.Close, 14.0m);
        }

        [Test]
        public void ThreeFallingGapWickos()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 14.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.0m));

            Assert.AreEqual(wickos.Count, 3);

            Assert.AreEqual(wickos[0].Start, tickOn1);
            Assert.AreEqual(wickos[0].EndTime, tickOn2);
            Assert.AreEqual(wickos[0].Open, 14.0m);
            Assert.AreEqual(wickos[0].High, 14.0m);
            Assert.AreEqual(wickos[0].Low, 13.0m);
            Assert.AreEqual(wickos[0].Close, 13.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[0].Spread, 1.0m);

            Assert.AreEqual(wickos[1].Start, tickOn2);
            Assert.AreEqual(wickos[1].EndTime, tickOn2);
            Assert.AreEqual(wickos[1].Open, 13.0m);
            Assert.AreEqual(wickos[1].High, 13.0m);
            Assert.AreEqual(wickos[1].Low, 12.0m);
            Assert.AreEqual(wickos[1].Close, 12.0m);
            Assert.AreEqual(wickos[1].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[1].Spread, 1.0);

            Assert.AreEqual(wickos[2].Start, tickOn2);
            Assert.AreEqual(wickos[2].EndTime, tickOn2);
            Assert.AreEqual(wickos[2].Open, 12.0m);
            Assert.AreEqual(wickos[2].High, 12.0m);
            Assert.AreEqual(wickos[2].Low, 11.0m);
            Assert.AreEqual(wickos[2].Close, 11.0m);
            Assert.AreEqual(wickos[2].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[2].Spread, 1.0m);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 11.0m);
            Assert.AreEqual(openWicko.High, 11.0m);
            Assert.AreEqual(openWicko.Low, 10.0m);
            Assert.AreEqual(openWicko.Close, 10.0m);
        }

        [Test]
        public void TwoFallingThenThreeRisingGapWickos()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 8.9m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.1m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.2m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 8.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 7.8m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 7.6m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 8.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.2m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.1m));

            Assert.AreEqual(wickos.Count, 5);

            Assert.AreEqual(wickos[0].Open, 10.0m);
            Assert.AreEqual(wickos[0].High, 10.5m);
            Assert.AreEqual(wickos[0].Low, 9.0m);
            Assert.AreEqual(wickos[0].Close, 9.0m);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[0].Spread, 1.0m);

            Assert.AreEqual(wickos[1].Open, 9.0m);
            Assert.AreEqual(wickos[1].High, 9.2m);
            Assert.AreEqual(wickos[1].Low, 8.0m);
            Assert.AreEqual(wickos[1].Close, 8.0m);
            Assert.AreEqual(wickos[1].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[1].Spread, 1.0m);

            Assert.AreEqual(wickos[2].Open, 9.0m);
            Assert.AreEqual(wickos[2].High, 10.0m);
            Assert.AreEqual(wickos[2].Low, 7.6m);
            Assert.AreEqual(wickos[2].Close, 10.0m);
            Assert.AreEqual(wickos[2].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[2].Spread, 1.0m);

            Assert.AreEqual(wickos[3].Open, 10.0m);
            Assert.AreEqual(wickos[3].High, 11.0m);
            Assert.AreEqual(wickos[3].Low, 10.0m);
            Assert.AreEqual(wickos[3].Close, 11.0m);
            Assert.AreEqual(wickos[3].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[3].Spread, 1.0m);

            Assert.AreEqual(wickos[4].Open, 11.0m);
            Assert.AreEqual(wickos[4].High, 12.0m);
            Assert.AreEqual(wickos[4].Low, 11.0m);
            Assert.AreEqual(wickos[4].Close, 12.0m);
            Assert.AreEqual(wickos[4].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[4].Spread, 1.0m);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 12.0m);
            Assert.AreEqual(openWicko.High, 12.1m);
            Assert.AreEqual(openWicko.Low, 12.0m);
            Assert.AreEqual(openWicko.Close, 12.1m);
        }

        [Test]
        public void TwoRisingThenThreeFallingGapWickos()
        {
            var consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var wickos = new List<WickoBar>();

            consolidator.DataConsolidated += (sender, wicko) =>
                wickos.Add(wicko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.6m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.1m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.7m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.6m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.3m));

            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.3m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 12.4m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 11.5m));
            consolidator.Update(new IndicatorDataPoint(tickOn1, 7.9m));

            Assert.AreEqual(wickos.Count, 5);

            Assert.AreEqual(wickos[0].Open, 10.0);
            Assert.AreEqual(wickos[0].High, 11.0);
            Assert.AreEqual(wickos[0].Low, 9.6);
            Assert.AreEqual(wickos[0].Close, 11.0);
            Assert.AreEqual(wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[0].Spread, 1.0);

            Assert.AreEqual(wickos[1].Open, 11.0);
            Assert.AreEqual(wickos[1].High, 12.0);
            Assert.AreEqual(wickos[1].Low, 10.7);
            Assert.AreEqual(wickos[1].Close, 12.0);
            Assert.AreEqual(wickos[1].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(wickos[1].Spread, 1.0);

            Assert.AreEqual(wickos[2].Open, 11.0);
            Assert.AreEqual(wickos[2].High, 12.4);
            Assert.AreEqual(wickos[2].Low, 10.0);
            Assert.AreEqual(wickos[2].Close, 10.0);
            Assert.AreEqual(wickos[2].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[2].Spread, 1.0);

            Assert.AreEqual(wickos[3].Open, 10.0);
            Assert.AreEqual(wickos[3].High, 10.0);
            Assert.AreEqual(wickos[3].Low, 9.0);
            Assert.AreEqual(wickos[3].Close, 9.0);
            Assert.AreEqual(wickos[3].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[3].Spread, 1.0);

            Assert.AreEqual(wickos[4].Open, 9.0);
            Assert.AreEqual(wickos[4].High, 9.0);
            Assert.AreEqual(wickos[4].Low, 8.0);
            Assert.AreEqual(wickos[4].Close, 8.0);
            Assert.AreEqual(wickos[4].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(wickos[4].Spread, 1.0);

            var openWicko = consolidator.OpenWickoBar;

            Assert.AreEqual(openWicko.Open, 8.0);
            Assert.AreEqual(openWicko.High, 8.0);
            Assert.AreEqual(openWicko.Low, 7.9);
            Assert.AreEqual(openWicko.Close, 7.9);
        }
    }
}
