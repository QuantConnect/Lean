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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class PivotPointsHighLowTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new PivotPointsHighLow(10);
        }

        protected override string TestFileName => "spy_pivot_pnt_hl.txt";

        protected override string TestColumnName => "PPHL";

        [Test]
        public override void ComparesAgainstExternalData()
        {
            var indicator = (PivotPointsHighLow)CreateIndicator();
            RunTestIndicator(indicator);

            var highPivotPoints = indicator.GetHighPivotPointsArray();
            var lowPivotPoints = indicator.GetLowPivotPointsArray();
            var pivotPoints = indicator.GetAllPivotPointsArray();

            Assert.True(highPivotPoints.Length > 0);
            Assert.True(lowPivotPoints.Length > 0);
            Assert.True(pivotPoints.Length > 0);
            Assert.AreEqual(pivotPoints.Length, highPivotPoints.Length + lowPivotPoints.Length);

            Assert.That(pivotPoints, Is.Ordered.Descending.By("Time"));
        }

        [TestCase(PivotPointType.Low)]
        [TestCase(PivotPointType.High)]
        [TestCase(PivotPointType.Both)]
        [TestCase(PivotPointType.None)]
        public void PivotPointPerType(PivotPointType pointType)
        {
            var pointsHighLow = new PivotPointsHighLow(10, 20);

            for (var i = 0; i < pointsHighLow.WarmUpPeriod; i++)
            {
                Assert.IsFalse(pointsHighLow.IsReady);

                var low = 1;
                var high = 1;
                if (i == 10)
                {
                    if (pointType == PivotPointType.Low || pointType == PivotPointType.Both)
                    {
                        low = 0;
                    }
                    if (pointType == PivotPointType.High || pointType == PivotPointType.Both)
                    {
                        high = 2;
                    }
                }

                var bar = new TradeBar(DateTime.UtcNow.AddSeconds(i), Symbols.AAPL, i, high, low, i, i);
                pointsHighLow.Update(bar);
            }

            Assert.IsTrue(pointsHighLow.IsReady);

            var bothPivotPoint = pointsHighLow.GetAllPivotPointsArray();
            var lowPivotPoint = pointsHighLow.GetLowPivotPointsArray();
            var highPivotPoint = pointsHighLow.GetHighPivotPointsArray();

            if (pointType == PivotPointType.None)
            {
                Assert.AreEqual(0, bothPivotPoint.Length);
            }
            if (pointType == PivotPointType.Both)
            {
                Assert.AreEqual(2, bothPivotPoint.Length);
                Assert.AreEqual(1, lowPivotPoint.Length);
                Assert.AreEqual(1, highPivotPoint.Length);
                Assert.IsTrue(lowPivotPoint.Any(point => point.Value == 0));
                Assert.IsTrue(highPivotPoint.Any(point => point.Value == 2));
            }
            if (pointType == PivotPointType.High)
            {
                Assert.AreEqual(1, bothPivotPoint.Length);
                Assert.AreEqual(0, lowPivotPoint.Length);
                Assert.AreEqual(1, highPivotPoint.Length);
                Assert.IsTrue(highPivotPoint.Any(point => point.Value == 2));
            }
            if (pointType == PivotPointType.Low)
            {
                Assert.AreEqual(1, bothPivotPoint.Length);
                Assert.AreEqual(1, lowPivotPoint.Length);
                Assert.AreEqual(0, highPivotPoint.Length);
                Assert.IsTrue(lowPivotPoint.Any(point => point.Value == 0));
            }
        }

        /// <summary>
        /// The expected value for this indicator is always zero
        /// </summary>
        /// <param name="indicator"></param>
        protected override void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
        }

        /// <summary>
        /// The expected value for this indicator is always zero
        /// </summary>
        protected override void IndicatorValueIsNotZeroAfterReceiveVolumeRenkoBars(IndicatorBase indicator)
        {
        }
    }
}