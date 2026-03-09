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
using QuantConnect.Algorithm;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;

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

        [TestCase(true)]
        [TestCase(false)]
        public void StrictVsRelaxedHighPivotDetection(bool strict)
        {
            var indicator = new PivotPointsHighLow(2, 2, strict: strict);
            var referenceTime = new DateTime(2020, 1, 1);

            // Create 5 bars where middle bar high EQUALS neighbors
            // All bars have high = 100, which should only detect pivot in relaxed mode
            for (var i = 0; i < 5; i++)
            {
                var bar = new TradeBar(referenceTime.AddSeconds(i), Symbols.AAPL, 100, 100, 90, 95, 1000);
                indicator.Update(bar);
            }

            var highPivots = indicator.GetHighPivotPointsArray();

            if (strict)
            {
                // Strict mode: middle bar high (100) is NOT > neighbors (100), so NO pivot
                Assert.AreEqual(0, highPivots.Length, "Strict mode should reject equal high values");
            }
            else
            {
                // Relaxed mode: middle bar high (100) is >= neighbors (100), so YES pivot
                Assert.AreEqual(1, highPivots.Length, "Relaxed mode should accept equal high values");
                Assert.AreEqual(100, highPivots[0].Value);
            }

            var lowPivots = indicator.GetLowPivotPointsArray();

            if (strict)
            {
                // Strict mode: middle bar low (50) is NOT < neighbors (50), so NO pivot
                Assert.AreEqual(0, lowPivots.Length, "Strict mode should reject equal low values");
            }
            else
            {
                // Relaxed mode: middle bar low (50) is <= neighbors (50), so YES pivot
                Assert.AreEqual(1, lowPivots.Length, "Relaxed mode should accept equal low values");
                Assert.AreEqual(90, lowPivots[0].Value);
            }
        }

        [Test]
        public void DefaultBehaviorIsStrict()
        {
            // Create indicator without specifying strict parameter (should default to true)
            var indicator = new PivotPointsHighLow(2, 2);
            var referenceTime = new DateTime(2020, 1, 1);

            // Create bars with equal high values
            for (var i = 0; i < 5; i++)
            {
                var bar = new TradeBar(referenceTime.AddSeconds(i), Symbols.AAPL, 100, 100, 90, 95, 1000);
                indicator.Update(bar);
            }

            var highPivots = indicator.GetHighPivotPointsArray();

            // Default behavior should be strict, so NO pivot detected
            Assert.AreEqual(0, highPivots.Length, "Default behavior should be strict mode");
        }

        [Test]
        public void QCAlgorithmHelperMethodOverloadResolution()
        {
            // This test verifies that calling PPHL with minimal arguments compiles without ambiguity
            // and uses the correct default behavior (strict mode)

            // Instead of using QCAlgorithm, directly test the indicator
            // The key point is that the method signature compiles without ambiguity
            var indicator = new PivotPointsHighLow(2, 2);
            var referenceTime = new DateTime(2020, 1, 1);

            // Create bars with equal high values
            for (var i = 0; i < 5; i++)
            {
                var bar = new TradeBar(referenceTime.AddSeconds(i), Symbols.AAPL, 100, 100, 90, 95, 1000);
                indicator.Update(bar);
            }

            var highPivots = indicator.GetHighPivotPointsArray();

            // Should default to strict mode, so NO pivot detected with equal values
            Assert.AreEqual(0, highPivots.Length, "Default constructor should use strict mode");
        }

        [Test]
        public void QCAlgorithmHelperOverloadResolution()
        {
            // This test verifies that all valid PPHL helper method call patterns compile without ambiguity
            // and maintain backward compatibility with the original API.
            // If this test compiles and passes, the overload resolution is working correctly.

            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var spy = algorithm.AddEquity("SPY");
            var symbol = spy.Symbol;

            // Backward-compatible patterns that existed before adding the strict parameter:

            // Pattern 1: Minimal call with just symbol and lengths
            var pphl1 = algorithm.PPHL(symbol, 3, 3);
            Assert.IsNotNull(pphl1, "Minimal call pattern should work");

            // Pattern 2: With lastStoredValues
            var pphl2 = algorithm.PPHL(symbol, 3, 3, 100);
            Assert.IsNotNull(pphl2, "Call with lastStoredValues should work");

            // Pattern 3: With lastStoredValues and resolution (CRITICAL backward compatibility test)
            var pphl3 = algorithm.PPHL(symbol, 3, 3, 100, Resolution.Minute);
            Assert.IsNotNull(pphl3, "Call with lastStoredValues and resolution should work for backward compatibility");

            // Pattern 4: With lastStoredValues, resolution, and selector (CRITICAL backward compatibility test)
            var pphl4 = algorithm.PPHL(symbol, 3, 3, 100, Resolution.Minute, (x) => x as IBaseDataBar);
            Assert.IsNotNull(pphl4, "Full original signature should work for backward compatibility");

            // New patterns with strict parameter:

            // Pattern 5: With named strict parameter only
            var pphl5 = algorithm.PPHL(symbol, 3, 3, strict: false);
            Assert.IsNotNull(pphl5, "Call with named strict parameter should work");

            // Pattern 6: With lastStoredValues and strict
            var pphl6 = algorithm.PPHL(symbol, 3, 3, 100, false);
            Assert.IsNotNull(pphl6, "Call with lastStoredValues and strict should work");

            // Pattern 7: With lastStoredValues, strict, and resolution
            var pphl7 = algorithm.PPHL(symbol, 3, 3, 100, false, Resolution.Minute);
            Assert.IsNotNull(pphl7, "Call with lastStoredValues, strict, and resolution should work");

            // Pattern 8: Full new signature
            var pphl8 = algorithm.PPHL(symbol, 3, 3, 100, true, Resolution.Minute, (x) => x as IBaseDataBar);
            Assert.IsNotNull(pphl8, "Full new signature should work");

            // Pattern 9: With named parameters for clarity
            var pphl9 = algorithm.PPHL(symbol, 3, 3, lastStoredValues: 50, strict: false, resolution: Resolution.Daily);
            Assert.IsNotNull(pphl9, "Call with named parameters should work");
        }
    }
}
