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
            var _consolidator = new WickoConsolidator(10.0m, x => x.Value);

            Assert.AreEqual(typeof(WickoBar), _consolidator.OutputType);
        }

        [Test]
        public void NoFallingWicko()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 9.1m));

            Assert.AreEqual(_wickos.Count, 0);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 10.0m);
            Assert.AreEqual(_openWicko.Low, 9.1m);
            Assert.AreEqual(_openWicko.Close, 9.1m);
        }

        [Test]
        public void NoRisingWicko()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 10.9m));

            Assert.AreEqual(_wickos.Count, 0);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 10.9m);
            Assert.AreEqual(_openWicko.Low, 10.0m);
            Assert.AreEqual(_openWicko.Close, 10.9m);
        }

        [Test]
        public void NoFallingWickoKissLimit()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 9.0m));

            Assert.AreEqual(_wickos.Count, 0);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 10.0m);
            Assert.AreEqual(_openWicko.Low, 9.0m);
            Assert.AreEqual(_openWicko.Close, 9.0m);
        }

        [Test]
        public void NoRisingWickoKissLimit()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 11.0m));

            Assert.AreEqual(_wickos.Count, 0);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 11.0m);
            Assert.AreEqual(_openWicko.Low, 10.0m);
            Assert.AreEqual(_openWicko.Close, 11.0m);
        }

        [Test]
        public void OneFallingWicko()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 8.9m));

            Assert.AreEqual(_wickos.Count, 1);

            Assert.AreEqual(_wickos[0].Open, 10.0m);
            Assert.AreEqual(_wickos[0].High, 10.0m);
            Assert.AreEqual(_wickos[0].Low, 9.0m);
            Assert.AreEqual(_wickos[0].Close, 9.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);
            Assert.AreEqual(_wickos[0].Start, _tickOn1);
            Assert.AreEqual(_wickos[0].EndTime, _tickOn2);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Start, _tickOn2);
            Assert.AreEqual(_openWicko.EndTime, _tickOn2);
            Assert.AreEqual(_openWicko.Open, 9.0m);
            Assert.AreEqual(_openWicko.High, 9.0m);
            Assert.AreEqual(_openWicko.Low, 8.9m);
            Assert.AreEqual(_openWicko.Close, 8.9m);
        }

        [Test]
        public void OneRisingWicko()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 10.1m));

            Assert.AreEqual(_wickos.Count, 1);

            Assert.AreEqual(_wickos[0].Open, 9.0m);
            Assert.AreEqual(_wickos[0].High, 10.0m);
            Assert.AreEqual(_wickos[0].Low, 9.0m);
            Assert.AreEqual(_wickos[0].Close, 10.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);
            Assert.AreEqual(_wickos[0].Start, _tickOn1);
            Assert.AreEqual(_wickos[0].EndTime, _tickOn2);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Start, _tickOn2);
            Assert.AreEqual(_openWicko.EndTime, _tickOn2);
            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 10.1m);
            Assert.AreEqual(_openWicko.Low, 10.0m);
            Assert.AreEqual(_openWicko.Close, 10.1m);
        }

        [Test]
        public void TwoFallingThenOneRisingWickos()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 8.9m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.1m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.2m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 8.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 7.8m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 7.6m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 8.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.2m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.1m));

            Assert.AreEqual(_wickos.Count, 3);

            Assert.AreEqual(_wickos[0].Open, 10.0m);
            Assert.AreEqual(_wickos[0].High, 10.5m);
            Assert.AreEqual(_wickos[0].Low, 9.0m);
            Assert.AreEqual(_wickos[0].Close, 9.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);

            Assert.AreEqual(_wickos[1].Open, 9.0m);
            Assert.AreEqual(_wickos[1].High, 9.2m);
            Assert.AreEqual(_wickos[1].Low, 8.0m);
            Assert.AreEqual(_wickos[1].Close, 8.0m);
            Assert.AreEqual(_wickos[1].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[1].Spread, 1.0m);

            Assert.AreEqual(_wickos[2].Open, 9.0m);
            Assert.AreEqual(_wickos[2].High, 10.0m);
            Assert.AreEqual(_wickos[2].Low, 7.6m);
            Assert.AreEqual(_wickos[2].Close, 10.0m);
            Assert.AreEqual(_wickos[2].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[2].Spread, 1.0m);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 10.1m);
            Assert.AreEqual(_openWicko.Low, 10.0m);
            Assert.AreEqual(_openWicko.Close, 10.1m);
        }

        [Test]
        public void TwoRisingThenOneFallingWickos()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.6m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.1m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.7m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.6m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.3m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.3m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.4m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.9m));

            Assert.AreEqual(_wickos.Count, 3);

            Assert.AreEqual(_wickos[0].Open, 10.0m);
            Assert.AreEqual(_wickos[0].High, 11.0m);
            Assert.AreEqual(_wickos[0].Low, 9.6m);
            Assert.AreEqual(_wickos[0].Close, 11.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);

            Assert.AreEqual(_wickos[1].Open, 11.0m);
            Assert.AreEqual(_wickos[1].High, 12.0m);
            Assert.AreEqual(_wickos[1].Low, 10.7m);
            Assert.AreEqual(_wickos[1].Close, 12.0m);
            Assert.AreEqual(_wickos[1].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[1].Spread, 1.0m);

            Assert.AreEqual(_wickos[2].Open, 11.0m);
            Assert.AreEqual(_wickos[2].High, 12.4m);
            Assert.AreEqual(_wickos[2].Low, 10.0m);
            Assert.AreEqual(_wickos[2].Close, 10.0m);
            Assert.AreEqual(_wickos[2].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[2].Spread, 1.0m);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 10.0m);
            Assert.AreEqual(_openWicko.High, 10.0m);
            Assert.AreEqual(_openWicko.Low, 9.9m);
            Assert.AreEqual(_openWicko.Close, 9.9m);
        }

        [Test]
        public void ThreeRisingGapWickos()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 14.0m));

            Assert.AreEqual(_wickos.Count, 3);

            Assert.AreEqual(_wickos[0].Start, _tickOn1);
            Assert.AreEqual(_wickos[0].EndTime, _tickOn2);
            Assert.AreEqual(_wickos[0].Open, 10.0m);
            Assert.AreEqual(_wickos[0].High, 11.0m);
            Assert.AreEqual(_wickos[0].Low, 10.0m);
            Assert.AreEqual(_wickos[0].Close, 11.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);

            Assert.AreEqual(_wickos[1].Start, _tickOn2);
            Assert.AreEqual(_wickos[1].EndTime, _tickOn2);
            Assert.AreEqual(_wickos[1].Open, 11.0m);
            Assert.AreEqual(_wickos[1].High, 12.0m);
            Assert.AreEqual(_wickos[1].Low, 11.0m);
            Assert.AreEqual(_wickos[1].Close, 12.0m);
            Assert.AreEqual(_wickos[1].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[1].Spread, 1.0m);

            Assert.AreEqual(_wickos[2].Start, _tickOn2);
            Assert.AreEqual(_wickos[2].EndTime, _tickOn2);
            Assert.AreEqual(_wickos[2].Open, 12.0m);
            Assert.AreEqual(_wickos[2].High, 13.0m);
            Assert.AreEqual(_wickos[2].Low, 12.0m);
            Assert.AreEqual(_wickos[2].Close, 13.0m);
            Assert.AreEqual(_wickos[2].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[2].Spread, 1.0m);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Start, _tickOn2);
            Assert.AreEqual(_openWicko.EndTime, _tickOn2);
            Assert.AreEqual(_openWicko.Open, 13.0m);
            Assert.AreEqual(_openWicko.High, 14.0m);
            Assert.AreEqual(_openWicko.Low, 13.0m);
            Assert.AreEqual(_openWicko.Close, 14.0m);
        }

        [Test]
        public void ThreeFallingGapWickos()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 14.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn2, 10.0m));

            Assert.AreEqual(_wickos.Count, 3);

            Assert.AreEqual(_wickos[0].Start, _tickOn1);
            Assert.AreEqual(_wickos[0].EndTime, _tickOn2);
            Assert.AreEqual(_wickos[0].Open, 14.0m);
            Assert.AreEqual(_wickos[0].High, 14.0m);
            Assert.AreEqual(_wickos[0].Low, 13.0m);
            Assert.AreEqual(_wickos[0].Close, 13.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);

            Assert.AreEqual(_wickos[1].Start, _tickOn2);
            Assert.AreEqual(_wickos[1].EndTime, _tickOn2);
            Assert.AreEqual(_wickos[1].Open, 13.0m);
            Assert.AreEqual(_wickos[1].High, 13.0m);
            Assert.AreEqual(_wickos[1].Low, 12.0m);
            Assert.AreEqual(_wickos[1].Close, 12.0m);
            Assert.AreEqual(_wickos[1].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[1].Spread, 1.0);

            Assert.AreEqual(_wickos[2].Start, _tickOn2);
            Assert.AreEqual(_wickos[2].EndTime, _tickOn2);
            Assert.AreEqual(_wickos[2].Open, 12.0m);
            Assert.AreEqual(_wickos[2].High, 12.0m);
            Assert.AreEqual(_wickos[2].Low, 11.0m);
            Assert.AreEqual(_wickos[2].Close, 11.0m);
            Assert.AreEqual(_wickos[2].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[2].Spread, 1.0m);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 11.0m);
            Assert.AreEqual(_openWicko.High, 11.0m);
            Assert.AreEqual(_openWicko.Low, 10.0m);
            Assert.AreEqual(_openWicko.Close, 10.0m);
        }

        [Test]
        public void TwoFallingThenThreeRisingGapWickos()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 8.9m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.1m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.2m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 8.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 7.8m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 7.6m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 8.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.2m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.1m));

            Assert.AreEqual(_wickos.Count, 5);

            Assert.AreEqual(_wickos[0].Open, 10.0m);
            Assert.AreEqual(_wickos[0].High, 10.5m);
            Assert.AreEqual(_wickos[0].Low, 9.0m);
            Assert.AreEqual(_wickos[0].Close, 9.0m);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[0].Spread, 1.0m);

            Assert.AreEqual(_wickos[1].Open, 9.0m);
            Assert.AreEqual(_wickos[1].High, 9.2m);
            Assert.AreEqual(_wickos[1].Low, 8.0m);
            Assert.AreEqual(_wickos[1].Close, 8.0m);
            Assert.AreEqual(_wickos[1].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[1].Spread, 1.0m);

            Assert.AreEqual(_wickos[2].Open, 9.0m);
            Assert.AreEqual(_wickos[2].High, 10.0m);
            Assert.AreEqual(_wickos[2].Low, 7.6m);
            Assert.AreEqual(_wickos[2].Close, 10.0m);
            Assert.AreEqual(_wickos[2].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[2].Spread, 1.0m);

            Assert.AreEqual(_wickos[3].Open, 10.0m);
            Assert.AreEqual(_wickos[3].High, 11.0m);
            Assert.AreEqual(_wickos[3].Low, 10.0m);
            Assert.AreEqual(_wickos[3].Close, 11.0m);
            Assert.AreEqual(_wickos[3].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[3].Spread, 1.0m);

            Assert.AreEqual(_wickos[4].Open, 11.0m);
            Assert.AreEqual(_wickos[4].High, 12.0m);
            Assert.AreEqual(_wickos[4].Low, 11.0m);
            Assert.AreEqual(_wickos[4].Close, 12.0m);
            Assert.AreEqual(_wickos[4].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[4].Spread, 1.0m);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 12.0m);
            Assert.AreEqual(_openWicko.High, 12.1m);
            Assert.AreEqual(_openWicko.Low, 12.0m);
            Assert.AreEqual(_openWicko.Close, 12.1m);
        }

        [Test]
        public void TwoRisingThenThreeFallingGapWickos()
        {
            var _consolidator = new WickoConsolidator(1.0m, x => x.Value);

            var _wickos = new List<WickoBar>();

            _consolidator.DataConsolidated += (sender, wicko) =>
                _wickos.Add(wicko);

            var _tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var _tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 9.6m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.1m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.0m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 10.7m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.6m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.3m));

            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.3m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 12.4m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 11.5m));
            _consolidator.Update(new IndicatorDataPoint(_tickOn1, 7.9m));

            Assert.AreEqual(_wickos.Count, 5);

            Assert.AreEqual(_wickos[0].Open, 10.0);
            Assert.AreEqual(_wickos[0].High, 11.0);
            Assert.AreEqual(_wickos[0].Low, 9.6);
            Assert.AreEqual(_wickos[0].Close, 11.0);
            Assert.AreEqual(_wickos[0].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[0].Spread, 1.0);

            Assert.AreEqual(_wickos[1].Open, 11.0);
            Assert.AreEqual(_wickos[1].High, 12.0);
            Assert.AreEqual(_wickos[1].Low, 10.7);
            Assert.AreEqual(_wickos[1].Close, 12.0);
            Assert.AreEqual(_wickos[1].Trend, WickoBarTrend.Rising);
            Assert.AreEqual(_wickos[1].Spread, 1.0);

            Assert.AreEqual(_wickos[2].Open, 11.0);
            Assert.AreEqual(_wickos[2].High, 12.4);
            Assert.AreEqual(_wickos[2].Low, 10.0);
            Assert.AreEqual(_wickos[2].Close, 10.0);
            Assert.AreEqual(_wickos[2].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[2].Spread, 1.0);

            Assert.AreEqual(_wickos[3].Open, 10.0);
            Assert.AreEqual(_wickos[3].High, 10.0);
            Assert.AreEqual(_wickos[3].Low, 9.0);
            Assert.AreEqual(_wickos[3].Close, 9.0);
            Assert.AreEqual(_wickos[3].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[3].Spread, 1.0);

            Assert.AreEqual(_wickos[4].Open, 9.0);
            Assert.AreEqual(_wickos[4].High, 9.0);
            Assert.AreEqual(_wickos[4].Low, 8.0);
            Assert.AreEqual(_wickos[4].Close, 8.0);
            Assert.AreEqual(_wickos[4].Trend, WickoBarTrend.Falling);
            Assert.AreEqual(_wickos[4].Spread, 1.0);

            var _openWicko = _consolidator.OpenWickoBar;

            Assert.AreEqual(_openWicko.Open, 8.0);
            Assert.AreEqual(_openWicko.High, 8.0);
            Assert.AreEqual(_openWicko.Low, 7.9);
            Assert.AreEqual(_openWicko.Close, 7.9);
        }
    }
}
