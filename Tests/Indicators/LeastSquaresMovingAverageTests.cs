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

using NUnit.Framework;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Result tested vs. Python available at: http://tinyurl.com/o7redso
    /// </summary>
    [TestFixture]
    public class LeastSquaresMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new LeastSquaresMovingAverage(20);
        }

        protected override string TestFileName => string.Empty;

        protected override string TestColumnName => string.Empty;

        #region Array input
        // Real AAPL minute data rounded to 2 decimals.
        public static decimal[] Prices =
        {
            125.99m, 125.91m, 125.75m, 125.62m, 125.54m, 125.45m, 125.47m,
            125.4m , 125.43m, 125.45m, 125.42m, 125.36m, 125.23m, 125.32m,
            125.26m, 125.31m, 125.41m, 125.5m , 125.51m, 125.41m, 125.54m,
            125.51m, 125.61m, 125.43m, 125.42m, 125.42m, 125.46m, 125.43m,
            125.4m , 125.35m, 125.3m , 125.28m, 125.21m, 125.37m, 125.32m,
            125.34m, 125.37m, 125.26m, 125.28m, 125.16m
        };
        #endregion Array input

        #region Array expected
        public static decimal[] Expected =
        {
            125.99m  , 125.91m  , 125.75m  , 125.62m  , 125.54m  , 125.45m  ,
            125.47m  , 125.4m   , 125.43m  , 125.45m  , 125.42m  , 125.36m  ,
            125.23m  , 125.32m  , 125.26m  , 125.31m  , 125.41m  , 125.5m   ,
            125.51m  , 125.2679m  , 125.328m , 125.381m , 125.4423m, 125.4591m,
            125.4689m, 125.4713m, 125.4836m, 125.4834m, 125.4803m, 125.4703m,
            125.4494m, 125.4206m, 125.3669m, 125.3521m, 125.3214m, 125.2986m,
            125.2909m, 125.2723m, 125.2619m, 125.2224m,
        };
        #endregion Array input

        protected override void RunTestIndicator(IndicatorBase<IndicatorDataPoint> indicator)
        {
            var time = DateTime.Now;

            for (var i = 0; i < Prices.Length; i++)
            {
                indicator.Update(time.AddMinutes(i), Prices[i]);
                Assert.AreEqual(Expected[i], Math.Round(indicator.Current.Value, 4));
            }
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = CreateIndicator();
            var time = DateTime.Now;

            for (var i = 0; i < 20; i++)
            {
                indicator.Update(time.AddMinutes(i), Prices[i]);
                Assert.AreEqual(Expected[i], Math.Round(indicator.Current.Value, 4));
            }

            Assert.IsTrue(indicator.IsReady, "LeastSquaresMovingAverage Ready");
            indicator.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(indicator);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue) return;

            var time = DateTime.Now;

            for (var i = 1; i < period.Value; i++)
            {
                indicator.Update(time.AddMinutes(i - 1), Prices[i - 1]);
                Assert.AreEqual(Expected[i - 1], Math.Round(indicator.Current.Value, 4));
                Assert.IsFalse(indicator.IsReady);
            }

            indicator.Update(time.AddMinutes(period.Value - 1), Prices[period.Value - 1]);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void WithReferenceIsNotReadyUntilBothWindowsFull()
        {
            var reference = Symbols.SPY;
            var lsma = new LeastSquaresMovingAverage("LSMA", reference, 5);
            var time = DateTime.Now;

            for (var i = 0; i < 5; i++)
            {
                lsma.Update(new IndicatorDataPoint(Symbols.AAPL, time.AddMinutes(i), 100m + i));
            }

            Assert.IsFalse(lsma.IsReady, "Should not be ready without reference data");

            for (var i = 0; i < 4; i++)
            {
                lsma.Update(new IndicatorDataPoint(reference, time.AddMinutes(i), 200m + i));
            }

            Assert.IsFalse(lsma.IsReady, "Should not be ready with insufficient reference data");

            lsma.Update(new IndicatorDataPoint(reference, time.AddMinutes(4), 204m));
            Assert.IsTrue(lsma.IsReady, "Should be ready when both windows are full");
        }

        [Test]
        public void WithReferenceRegressesAgainstBenchmark()
        {
            var target = Symbols.AAPL;
            var reference = Symbols.SPY;
            var lsma = new LeastSquaresMovingAverage("LSMA", reference, 5);
            var time = DateTime.Now;

            // y = 2*x + 1 (target = 2*reference + 1)
            // reference: 1, 2, 3, 4, 5
            // target:    3, 5, 7, 9, 11
            for (var i = 0; i < 5; i++)
            {
                var refValue = (decimal)(i + 1);
                var targetValue = 2m * refValue + 1m;
                lsma.Update(new IndicatorDataPoint(target, time.AddMinutes(i), targetValue));
                lsma.Update(new IndicatorDataPoint(reference, time.AddMinutes(i), refValue));
            }

            Assert.IsTrue(lsma.IsReady);

            // slope should be 2, intercept should be 1
            Assert.AreEqual(2.0, (double)lsma.Slope.Current.Value, 0.0001);
            Assert.AreEqual(1.0, (double)lsma.Intercept.Current.Value, 0.0001);

            // projected value = intercept + slope * latest_reference = 1 + 2*5 = 11
            Assert.AreEqual(11.0, (double)lsma.Current.Value, 0.0001);
        }

        [Test]
        public void WithReferenceResetsProperly()
        {
            var target = Symbols.AAPL;
            var reference = Symbols.SPY;
            var lsma = new LeastSquaresMovingAverage("LSMA", reference, 3);
            var time = DateTime.Now;

            for (var i = 0; i < 3; i++)
            {
                lsma.Update(new IndicatorDataPoint(target, time.AddMinutes(i), 10m + i));
                lsma.Update(new IndicatorDataPoint(reference, time.AddMinutes(i), 20m + i));
            }

            Assert.IsTrue(lsma.IsReady);

            lsma.Reset();

            Assert.IsFalse(lsma.IsReady);
            Assert.AreEqual(0m, lsma.Current.Value);
            Assert.AreEqual(0m, lsma.Intercept.Current.Value);
            Assert.AreEqual(0m, lsma.Slope.Current.Value);
        }

        [Test]
        public void WithoutReferenceBehavesIdentically()
        {
            var withRef = new LeastSquaresMovingAverage(20);
            var without = new LeastSquaresMovingAverage(20);
            var time = DateTime.Now;

            for (var i = 0; i < Prices.Length; i++)
            {
                withRef.Update(time.AddMinutes(i), Prices[i]);
                without.Update(time.AddMinutes(i), Prices[i]);

                Assert.AreEqual(
                    Math.Round(without.Current.Value, 4),
                    Math.Round(withRef.Current.Value, 4));
            }
        }
    }
}
