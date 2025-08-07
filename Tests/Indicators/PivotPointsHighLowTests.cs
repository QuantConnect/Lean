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
            // Even if the indicator is ready, there may be zero values
            ValueCanBeZero = true;
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

        [Test]
        public void StrictCheckingDefaultBehavior()
        {
            // Test that default behavior (strictChecking = true) works as before
            var pointsHighLow = new PivotPointsHighLow(2);

            // Create data where middle point equals neighbors - should NOT be a pivot with strict checking
            var bars = new[]
            {
                new TradeBar(DateTime.UtcNow.AddSeconds(0), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1
                new TradeBar(DateTime.UtcNow.AddSeconds(1), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (same as middle)
                new TradeBar(DateTime.UtcNow.AddSeconds(2), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (middle - same as neighbors)
                new TradeBar(DateTime.UtcNow.AddSeconds(3), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (same as middle)
                new TradeBar(DateTime.UtcNow.AddSeconds(4), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (same as middle)
            };

            foreach (var bar in bars)
            {
                pointsHighLow.Update(bar);
            }

            // With strict checking (default), no pivot points should be detected since all values are equal
            var pivotPoints = pointsHighLow.GetAllPivotPointsArray();
            Assert.AreEqual(0, pivotPoints.Length, "With strict checking, equal values should not create pivot points");
        }

        [Test]
        public void RelaxedCheckingAllowsTies()
        {
            // Test that relaxed checking (strictChecking = false) allows ties
            var pointsHighLow = new PivotPointsHighLow(2, strictChecking: false);

            // Create data where middle point equals neighbors - should be a pivot with relaxed checking
            var bars = new[]
            {
                new TradeBar(DateTime.UtcNow.AddSeconds(0), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1
                new TradeBar(DateTime.UtcNow.AddSeconds(1), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (same as middle)
                new TradeBar(DateTime.UtcNow.AddSeconds(2), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (middle - same as neighbors)
                new TradeBar(DateTime.UtcNow.AddSeconds(3), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (same as middle)
                new TradeBar(DateTime.UtcNow.AddSeconds(4), Symbols.AAPL, 1, 10, 1, 1, 1), // high=10, low=1 (same as middle)
            };

            foreach (var bar in bars)
            {
                pointsHighLow.Update(bar);
            }

            // With relaxed checking, pivot points should be detected even with equal values
            var pivotPoints = pointsHighLow.GetAllPivotPointsArray();
            Assert.IsTrue(pivotPoints.Length > 0, "With relaxed checking, equal values should create pivot points");
        }

        [Test]
        public void StrictVsRelaxedComparison()
        {
            var strictIndicator = new PivotPointsHighLow(2, strictChecking: true);
            var relaxedIndicator = new PivotPointsHighLow(2, strictChecking: false);

            // Test data with a clear high pivot point followed by ties
            var bars = new[]
            {
                new TradeBar(DateTime.UtcNow.AddSeconds(0), Symbols.AAPL, 1, 5, 1, 1, 1),   // high=5, low=1
                new TradeBar(DateTime.UtcNow.AddSeconds(1), Symbols.AAPL, 1, 5, 1, 1, 1),   // high=5, low=1
                new TradeBar(DateTime.UtcNow.AddSeconds(2), Symbols.AAPL, 1, 10, 1, 1, 1),  // high=10, low=1 (clear high pivot)
                new TradeBar(DateTime.UtcNow.AddSeconds(3), Symbols.AAPL, 1, 5, 1, 1, 1),   // high=5, low=1
                new TradeBar(DateTime.UtcNow.AddSeconds(4), Symbols.AAPL, 1, 5, 1, 1, 1),   // high=5, low=1
                // Now add bars with ties
                new TradeBar(DateTime.UtcNow.AddSeconds(5), Symbols.AAPL, 1, 8, 1, 1, 1),   // high=8, low=1
                new TradeBar(DateTime.UtcNow.AddSeconds(6), Symbols.AAPL, 1, 8, 1, 1, 1),   // high=8, low=1 (middle - tied)
                new TradeBar(DateTime.UtcNow.AddSeconds(7), Symbols.AAPL, 1, 8, 1, 1, 1),   // high=8, low=1
            };

            foreach (var bar in bars)
            {
                strictIndicator.Update(bar);
                relaxedIndicator.Update(bar);
            }

            var strictPivots = strictIndicator.GetAllPivotPointsArray();
            var relaxedPivots = relaxedIndicator.GetAllPivotPointsArray();

            // Both should detect the clear high pivot at 10
            Assert.IsTrue(strictPivots.Any(p => p.Value == 10), "Strict checking should detect clear high pivot");
            Assert.IsTrue(relaxedPivots.Any(p => p.Value == 10), "Relaxed checking should detect clear high pivot");

            // Relaxed checking should detect more pivots due to allowing ties
            Assert.IsTrue(relaxedPivots.Length >= strictPivots.Length, "Relaxed checking should detect same or more pivot points");
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
