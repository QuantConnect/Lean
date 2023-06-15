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
using System.Linq;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class RenkoConsolidatorTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        public void WickedRenkoConsolidatorFailsWhenBarSizeIsZero(double barSize)
        {
            var message = Assert.Throws<ArgumentException>( () => new RenkoConsolidator((decimal)barSize));
            Assert.AreEqual("Renko consolidator BarSize must be strictly greater than zero", message.Message);
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
            var consolidator = new TestRenkoConsolidator(1.0m);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 9.1m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.0m);
            Assert.AreEqual(openRenko.Low, 9.1m);
            Assert.AreEqual(openRenko.Close, 9.1m);
        }

        [Test]
        public void WickedNoRisingRenko()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 10.9m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.9m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.9m);
        }

        [Test]
        public void WickedNoFallingRenkoKissLimit()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 9.0m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.0m);
            Assert.AreEqual(openRenko.Low, 9.0m);
            Assert.AreEqual(openRenko.Close, 9.0m);
        }

        [Test]
        public void WickedNoRisingRenkoKissLimit()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

            var renkos = new List<RenkoBar>();

            consolidator.DataConsolidated += (sender, renko) =>
                renkos.Add(renko);

            var tickOn1 = new DateTime(2016, 1, 1, 17, 0, 0, 0);
            var tickOn2 = new DateTime(2016, 1, 1, 17, 0, 0, 1);

            consolidator.Update(new IndicatorDataPoint(tickOn1, 10.0m));
            consolidator.Update(new IndicatorDataPoint(tickOn2, 11.0m));

            Assert.AreEqual(renkos.Count, 0);

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 11.0m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 11.0m);
        }

        [Test]
        public void WickedOneFallingRenko()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

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
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

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
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.1m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.1m);
        }

        [Test]
        public void WickedTwoRisingThenOneFallingRenkos()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 10.0m);
            Assert.AreEqual(openRenko.High, 10.0m);
            Assert.AreEqual(openRenko.Low, 9.9m);
            Assert.AreEqual(openRenko.Close, 9.9m);
        }

        [Test]
        public void WickedThreeRisingGapRenkos()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

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
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 11.0m);
            Assert.AreEqual(openRenko.High, 11.0m);
            Assert.AreEqual(openRenko.Low, 10.0m);
            Assert.AreEqual(openRenko.Close, 10.0m);
        }

        [Test]
        public void WickedTwoFallingThenThreeRisingGapRenkos()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 12.0m);
            Assert.AreEqual(openRenko.High, 12.1m);
            Assert.AreEqual(openRenko.Low, 12.0m);
            Assert.AreEqual(openRenko.Close, 12.1m);
        }

        [Test]
        public void WickedTwoRisingThenThreeFallingGapRenkos()
        {
            var consolidator = new TestRenkoConsolidator(1.0m);

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

            var openRenko = consolidator.OpenRenko();

            Assert.AreEqual(openRenko.Open, 8.0);
            Assert.AreEqual(openRenko.High, 8.0);
            Assert.AreEqual(openRenko.Low, 7.9);
            Assert.AreEqual(openRenko.Close, 7.9);
        }

        [TestCase(new double[] {1.38687, 1.38688, 1.38687, 1.38686, 1.38685, 1.38683,
                1.38682, 1.38682, 1.38684, 1.38682, 1.38682, 1.38680,
                1.38681, 1.38686, 1.38688, 1.38688, 1.38690, 1.38690,
                1.38691, 1.38692, 1.38694, 1.38695, 1.38697, 1.38697,
                1.38700, 1.38699, 1.38699, 1.38699, 1.38698, 1.38699,
                1.38697, 1.38698, 1.38698, 1.38697, 1.38698, 1.38698,
                1.38697, 1.38697, 1.38700, 1.38702, 1.38701, 1.38699,
                1.38697, 1.38698, 1.38696, 1.38698, 1.38697, 1.38695,
                1.38695, 1.38696, 1.38693, 1.38692, 1.38693, 1.38693,
                1.38692, 1.38693, 1.38692, 1.38690, 1.38686, 1.38685,
                1.38687, 1.38686, 1.38686, 1.38686, 1.38686, 1.38685,
                1.38684, 1.38678, 1.38679, 1.38680, 1.38680, 1.38681,
                1.38685, 1.38685, 1.38683, 1.38682, 1.38682, 1.38683,
                1.38682, 1.38683, 1.38682, 1.38681, 1.38680, 1.38681,
                1.38681, 1.38681, 1.38682, 1.38680, 1.38679, 1.38678,
                1.38675, 1.38678, 1.38678, 1.38678, 1.38682, 1.38681,
                1.38682, 1.38680, 1.38682, 1.38683, 1.38685, 1.38683,
                1.38683, 1.38684, 1.38683, 1.38683, 1.38684, 1.38685,
                1.38684, 1.38683, 1.38686, 1.38685, 1.38685, 1.38684,
                1.38685, 1.38682, 1.38684, 1.38683, 1.38682, 1.38683,
                1.38685, 1.38685, 1.38685, 1.38683, 1.38685, 1.38684,
                1.38686, 1.38693, 1.38695, 1.38693, 1.38694, 1.38693,
                1.38692, 1.38693, 1.38695, 1.38697, 1.38698, 1.38695,
                1.38696}, 0.0001)]
        [TestCase(new double[] {90.38687, 12.38688, 33.38687, 69.38686, 22.38685, 19.38683,
                19.38682, 51.38682, 12.38684, 41.38682, 47.38682, 30.38680,
                81.38681, 16.38686, 21.38688, 14.38688, 89.38690, 72.38690,
                71.38691, 71.38692, 3.38694, 71.38695, 50.38697, 97.38697,
                16.38700, 18.38699, 14.38699, 91.38699, 60.38698, 35.38699,
                51.38697, 91.38698, 41.38698, 21.38697, 44.38698, 35.38698,
                14.38697, 10.38697, 5.38700, 1.38702, 1.38701, 15.38699,
                31.38697, 11.38698, 16.38696, 21.38698, 16.38697, 19.38695,
                12.38695, 21.38696, 61.38693, 32.38692, 20.38693, 23.38693,
                11.38692, 13.38693, 7.38692, 16.38690, 30.38686, 34.38685,
                91.38687, 41.38686, 18.38686, 12.38686, 40.38686, 44.38685,
                18.38684, 15.38678, 81.38679, 19.38680, 32.38680, 37.38681,
                71.38685, 61.38685, 9.38683, 21.38682, 27.38682, 28.38683,
                16.38682, 17.38683, 0.38682, 81.38681, 60.38680, 65.38681,
                51.38681, 81.38681, 11.38682, 81.38680, 60.38679, 65.38678,
                41.38675, 19.38678, 11.38678, 51.38678, 25.38682, 30.38681,
                13.38682, 1.38680, 2.38682, 51.38683, 47.38685, 55.38683,
                21.38683, 11.38684, 13.38683, 81.38683, 70.38684, 75.38685,
                11.38684, 21.38683, 31.38686, 91.38685, 87.38685, 92.38684,
                10.38685, 13.38682, 4.38684, 21.38683, 29.38682, 34.38683,
                19.38685, 41.38685, 51.38685, 12.38683, 28.38685, 34.38684,
                81.38686, 15.38693, 15.38695, 1.38693, 8.38694, 13.38693,
                17.38692, 61.38693, 6.38695, 13.38697, 4.38698, 9.38695,
                61.38696}, 5)]
        public void ConsistentRenkos(double[] values, double barSize)
        {
            // Reproduce issue #5479
            // Test Renko bar consistency amongst three consolidators starting at different times

            var time = new DateTime(2016, 1, 1);
            var testValues = new List<decimal> (values.Select(x => (decimal)x));


            var consolidator1 = new RenkoConsolidator((decimal)barSize);
            var consolidator2 = new RenkoConsolidator((decimal)barSize);
            var consolidator3 = new RenkoConsolidator((decimal)barSize);

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

        [TestCase(12.38684, 0.0001, 12.3868)]
        [TestCase(12.38686, 0.0001, 12.3869)]
        [TestCase(3.38694, 0.001, 3.387)]
        [TestCase(3.38644, 0.001, 3.386)]
        [TestCase(41.38698, 0.01, 41.39)]
        [TestCase(41.38498, 0.01, 41.38)]
        [TestCase(16.38696, 0.1, 16.4)]
        [TestCase(16.32696, 0.1, 16.3)]
        [TestCase(7.38692, 1, 7)]
        [TestCase(7.78692, 1, 8)]
        [TestCase(81.38679, 10, 80)]
        [TestCase(88.38679, 10, 90)]
        [TestCase(1247.38682, 100, 1200)]
        [TestCase(1257.38682, 100, 1300)]
        [TestCase(44500.2349, 1000, 45000)]
        [TestCase(44300.2349, 1000, 44000)]
        public void GetClosestMultipleWorksAsExpected(double price, double barSize, double expectedClosestMultiple)
        {
            Assert.AreEqual((decimal)expectedClosestMultiple, RenkoConsolidator.GetClosestMultiple((decimal)price, (decimal)barSize));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GetClosestMultipleFailsWhenBarSizeIsLessThanZero(double barSize)
        {
            var message = Assert.Throws<ArgumentException>(() => RenkoConsolidator.GetClosestMultiple((decimal)34.78989, (decimal)barSize));
            Assert.AreEqual("BarSize must be strictly greater than zero", message.Message);
        }

        private class TestRenkoConsolidator : RenkoConsolidator
        {
            public TestRenkoConsolidator(decimal barSize)
                : base(barSize)
            {
            }

            public RenkoBar OpenRenko()
            {
                return new RenkoBar(null, OpenOn, CloseOn, BarSize, OpenRate, HighRate, LowRate, CloseRate);
            }
        }
    }
}
