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

        [Test]
        public void WickedOutputTypeIsRenkoBar()
        {
            var consolidator = new RenkoConsolidator(10.0m);

            Assert.AreEqual(typeof(RenkoBar), consolidator.OutputType);
        }

        [Test]
        public void WickedNoFallingRenko()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 9.1m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.0m);
            Assert.AreEqual(openRenko.Low, 9.1m);
            Assert.AreEqual(openRenko.Close, 9.1m);
        }

        [Test]
        public void WickedNoRisingRenko()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.9m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.9m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.9m);
        }

        [Test]
        public void WickedNoFallingRenkoKissLimit()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 9.0m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.0m);
            Assert.AreEqual(openRenko.Low, 9.0m);
            Assert.AreEqual(openRenko.Close, 9.0m);
        }

        [Test]
        public void WickedNoRisingRenkoKissLimit()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 11.0m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 11.0m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 11.0m);
        }

        [Test]
        public void WickedOneFallingRenko()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 8.9m));

            Assert.AreEqual(renkos.Count, 1);

            Assert.AreEqual(renkos[0].Open, 10.0m);
            Assert.AreEqual(renkos[0].High, 10.0m);
            Assert.AreEqual(renkos[0].Low, 9.0m);
            Assert.AreEqual(renkos[0].Close, 9.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[0].Spread, 1.0m);
            Assert.AreEqual(renkos[0].Start, tickOn1);
            Assert.AreEqual(renkos[0].EndTime, tickOn2);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Start, tickOn2);
            Assert.AreEqual(openRenko.EndTime, tickOn2);
            Assert.AreEqual(openRenko.Open, 9.0m);
            Assert.AreEqual(openRenko.High, 9.0m);
            Assert.AreEqual(openRenko.Low, 8.9m);
            Assert.AreEqual(openRenko.Close, 8.9m);
        }

        [Test]
        public void WickedOneRisingRenko()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 9.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.1m));

            Assert.AreEqual(renkos.Count, 1);

            Assert.AreEqual(renkos[0].Open, 9.0m);
            Assert.AreEqual(renkos[0].High, 10.0m);
            Assert.AreEqual(renkos[0].Low, 9.0m);
            Assert.AreEqual(renkos[0].Close, 10.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[0].Spread, 1.0m);
            Assert.AreEqual(renkos[0].Start, tickOn1);
            Assert.AreEqual(renkos[0].EndTime, tickOn2);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Start, tickOn2);
            Assert.AreEqual(openRenko.EndTime, tickOn2);
            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.1m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.1m);
        }

        [Test]
        public void WickedTwoFallingThenOneRisingRenkos()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

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

            Assert.AreEqual(renkos.Count, 3);

            Assert.AreEqual(renkos[0].Open, 10.0m);
            Assert.AreEqual(renkos[0].High, 10.5m);
            Assert.AreEqual(renkos[0].Low, 9.0m);
            Assert.AreEqual(renkos[0].Close, 9.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[0].Spread, 1.0m);

            Assert.AreEqual(renkos[1].Open, 9.0m);
            Assert.AreEqual(renkos[1].High, 9.2m);
            Assert.AreEqual(renkos[1].Low, 8.0m);
            Assert.AreEqual(renkos[1].Close, 8.0m);
            Assert.AreEqual(renkos[1].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[1].Spread, 1.0m);

            Assert.AreEqual(renkos[2].Open, 9.0m);
            Assert.AreEqual(renkos[2].High, 10.0m);
            Assert.AreEqual(renkos[2].Low, 7.6m);
            Assert.AreEqual(renkos[2].Close, 10.0m);
            Assert.AreEqual(renkos[2].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[2].Spread, 1.0m);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.1m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.1m);
        }

        [Test]
        public void WickedTwoRisingThenOneFallingRenkos()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

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

            Assert.AreEqual(renkos.Count, 3);

            Assert.AreEqual(renkos[0].Open, 10.0m);
            Assert.AreEqual(renkos[0].High, 11.0m);
            Assert.AreEqual(renkos[0].Low, 9.6m);
            Assert.AreEqual(renkos[0].Close, 11.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[0].Spread, 1.0m);

            Assert.AreEqual(renkos[1].Open, 11.0m);
            Assert.AreEqual(renkos[1].High, 12.0m);
            Assert.AreEqual(renkos[1].Low, 10.7m);
            Assert.AreEqual(renkos[1].Close, 12.0m);
            Assert.AreEqual(renkos[1].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[1].Spread, 1.0m);

            Assert.AreEqual(renkos[2].Open, 11.0m);
            Assert.AreEqual(renkos[2].High, 12.4m);
            Assert.AreEqual(renkos[2].Low, 10.0m);
            Assert.AreEqual(renkos[2].Close, 10.0m);
            Assert.AreEqual(renkos[2].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[2].Spread, 1.0m);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.0m);
            Assert.AreEqual(openRenko.Low, 9.9m);
            Assert.AreEqual(openRenko.Close, 9.9m);
        }

        [Test]
        public void WickedThreeRisingGapRenkos()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 14.0m));

            Assert.AreEqual(renkos.Count, 3);

            Assert.AreEqual(renkos[0].Start, tickOn1);
            Assert.AreEqual(renkos[0].EndTime, tickOn2);
            Assert.AreEqual(renkos[0].Open, 10.0m);
            Assert.AreEqual(renkos[0].High, 11.0m);
            Assert.AreEqual(renkos[0].Low, 10.0m);
            Assert.AreEqual(renkos[0].Close, 11.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[0].Spread, 1.0m);

            Assert.AreEqual(renkos[1].Start, tickOn2);
            Assert.AreEqual(renkos[1].EndTime, tickOn2);
            Assert.AreEqual(renkos[1].Open, 11.0m);
            Assert.AreEqual(renkos[1].High, 12.0m);
            Assert.AreEqual(renkos[1].Low, 11.0m);
            Assert.AreEqual(renkos[1].Close, 12.0m);
            Assert.AreEqual(renkos[1].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[1].Spread, 1.0m);

            Assert.AreEqual(renkos[2].Start, tickOn2);
            Assert.AreEqual(renkos[2].EndTime, tickOn2);
            Assert.AreEqual(renkos[2].Open, 12.0m);
            Assert.AreEqual(renkos[2].High, 13.0m);
            Assert.AreEqual(renkos[2].Low, 12.0m);
            Assert.AreEqual(renkos[2].Close, 13.0m);
            Assert.AreEqual(renkos[2].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[2].Spread, 1.0m);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Start, tickOn2);
            Assert.AreEqual(openRenko.EndTime, tickOn2);
            Assert.AreEqual(openRenko.Open, 13.0m);
            Assert.AreEqual(openRenko.High, 14.0m);
            Assert.AreEqual(openRenko.Low, 13.0m);
            Assert.AreEqual(openRenko.Close, 14.0m);
        }

        [Test]
        public void WickedThreeFallingGapRenkos()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 14.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.0m));

            Assert.AreEqual(renkos.Count, 3);

            Assert.AreEqual(renkos[0].Start, tickOn1);
            Assert.AreEqual(renkos[0].EndTime, tickOn2);
            Assert.AreEqual(renkos[0].Open, 14.0m);
            Assert.AreEqual(renkos[0].High, 14.0m);
            Assert.AreEqual(renkos[0].Low, 13.0m);
            Assert.AreEqual(renkos[0].Close, 13.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[0].Spread, 1.0m);

            Assert.AreEqual(renkos[1].Start, tickOn2);
            Assert.AreEqual(renkos[1].EndTime, tickOn2);
            Assert.AreEqual(renkos[1].Open, 13.0m);
            Assert.AreEqual(renkos[1].High, 13.0m);
            Assert.AreEqual(renkos[1].Low, 12.0m);
            Assert.AreEqual(renkos[1].Close, 12.0m);
            Assert.AreEqual(renkos[1].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[1].Spread, 1.0);

            Assert.AreEqual(renkos[2].Start, tickOn2);
            Assert.AreEqual(renkos[2].EndTime, tickOn2);
            Assert.AreEqual(renkos[2].Open, 12.0m);
            Assert.AreEqual(renkos[2].High, 12.0m);
            Assert.AreEqual(renkos[2].Low, 11.0m);
            Assert.AreEqual(renkos[2].Close, 11.0m);
            Assert.AreEqual(renkos[2].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[2].Spread, 1.0m);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 11.0m);
            Assert.AreEqual(openRenko.High, 11.0m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.0m);
        }

        [Test]
        public void WickedTwoFallingThenThreeRisingGapRenkos()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

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

            Assert.AreEqual(renkos.Count, 5);

            Assert.AreEqual(renkos[0].Open, 10.0m);
            Assert.AreEqual(renkos[0].High, 10.5m);
            Assert.AreEqual(renkos[0].Low, 9.0m);
            Assert.AreEqual(renkos[0].Close, 9.0m);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[0].Spread, 1.0m);

            Assert.AreEqual(renkos[1].Open, 9.0m);
            Assert.AreEqual(renkos[1].High, 9.2m);
            Assert.AreEqual(renkos[1].Low, 8.0m);
            Assert.AreEqual(renkos[1].Close, 8.0m);
            Assert.AreEqual(renkos[1].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[1].Spread, 1.0m);

            Assert.AreEqual(renkos[2].Open, 9.0m);
            Assert.AreEqual(renkos[2].High, 10.0m);
            Assert.AreEqual(renkos[2].Low, 7.6m);
            Assert.AreEqual(renkos[2].Close, 10.0m);
            Assert.AreEqual(renkos[2].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[2].Spread, 1.0m);

            Assert.AreEqual(renkos[3].Open, 10.0m);
            Assert.AreEqual(renkos[3].High, 11.0m);
            Assert.AreEqual(renkos[3].Low, 10.0m);
            Assert.AreEqual(renkos[3].Close, 11.0m);
            Assert.AreEqual(renkos[3].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[3].Spread, 1.0m);

            Assert.AreEqual(renkos[4].Open, 11.0m);
            Assert.AreEqual(renkos[4].High, 12.0m);
            Assert.AreEqual(renkos[4].Low, 11.0m);
            Assert.AreEqual(renkos[4].Close, 12.0m);
            Assert.AreEqual(renkos[4].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[4].Spread, 1.0m);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 12.0m);
            Assert.AreEqual(openRenko.High, 12.1m);
            Assert.AreEqual(openRenko.Low, 12.0m);
            Assert.AreEqual(openRenko.Close, 12.1m);
        }

        [Test]
        public void WickedTwoRisingThenThreeFallingGapRenkos()
        {
            var consolidator = new RenkoConsolidator(1.0m, RenkoType.Wicked);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

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

            Assert.AreEqual(renkos.Count, 5);

            Assert.AreEqual(renkos[0].Open, 10.0);
            Assert.AreEqual(renkos[0].High, 11.0);
            Assert.AreEqual(renkos[0].Low, 9.6);
            Assert.AreEqual(renkos[0].Close, 11.0);
            Assert.AreEqual(renkos[0].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[0].Spread, 1.0);

            Assert.AreEqual(renkos[1].Open, 11.0);
            Assert.AreEqual(renkos[1].High, 12.0);
            Assert.AreEqual(renkos[1].Low, 10.7);
            Assert.AreEqual(renkos[1].Close, 12.0);
            Assert.AreEqual(renkos[1].Direction, BarDirection.Rising);
            Assert.AreEqual(renkos[1].Spread, 1.0);

            Assert.AreEqual(renkos[2].Open, 11.0);
            Assert.AreEqual(renkos[2].High, 12.4);
            Assert.AreEqual(renkos[2].Low, 10.0);
            Assert.AreEqual(renkos[2].Close, 10.0);
            Assert.AreEqual(renkos[2].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[2].Spread, 1.0);

            Assert.AreEqual(renkos[3].Open, 10.0);
            Assert.AreEqual(renkos[3].High, 10.0);
            Assert.AreEqual(renkos[3].Low, 9.0);
            Assert.AreEqual(renkos[3].Close, 9.0);
            Assert.AreEqual(renkos[3].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[3].Spread, 1.0);

            Assert.AreEqual(renkos[4].Open, 9.0);
            Assert.AreEqual(renkos[4].High, 9.0);
            Assert.AreEqual(renkos[4].Low, 8.0);
            Assert.AreEqual(renkos[4].Close, 8.0);
            Assert.AreEqual(renkos[4].Direction, BarDirection.Falling);
            Assert.AreEqual(renkos[4].Spread, 1.0);

            var openRenko = consolidator.OpenRenkoBar;

            Assert.AreEqual(openRenko.Open, 8.0);
            Assert.AreEqual(openRenko.High, 8.0);
            Assert.AreEqual(openRenko.Low, 7.9);
            Assert.AreEqual(openRenko.Close, 7.9);
        }
    }
}
