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
        public void WithReferenceRegressesAgainstBenchmarkWhenTargetArrivesFirst()
        {
            var indicator = new LeastSquaresMovingAverage("LSMA", Symbols.SPY, 3);

            UpdatePair(indicator, 1, 3, targetFirst: true);
            Assert.IsFalse(indicator.IsReady);

            UpdatePair(indicator, 2, 5, targetFirst: true);
            Assert.IsFalse(indicator.IsReady);

            UpdatePair(indicator, 3, 7, targetFirst: true);

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, Math.Round(indicator.Intercept.Current.Value, 8));
            Assert.AreEqual(2m, Math.Round(indicator.Slope.Current.Value, 8));
            Assert.AreEqual(7m, Math.Round(indicator.Current.Value, 8));
        }

        [Test]
        public void WithReferenceRegressesAgainstBenchmarkWhenReferenceArrivesFirst()
        {
            var indicator = new LeastSquaresMovingAverage("LSMA", Symbols.SPY, 3);

            UpdatePair(indicator, 5, 11, targetFirst: false);
            Assert.IsFalse(indicator.IsReady);

            UpdatePair(indicator, 6, 13, targetFirst: false);
            Assert.IsFalse(indicator.IsReady);

            UpdatePair(indicator, 7, 15, targetFirst: false);

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, Math.Round(indicator.Intercept.Current.Value, 8));
            Assert.AreEqual(2m, Math.Round(indicator.Slope.Current.Value, 8));
            Assert.AreEqual(15m, Math.Round(indicator.Current.Value, 8));
        }

        [Test]
        public void WithReferenceWaitsForMatchingTimes()
        {
            var indicator = new LeastSquaresMovingAverage("LSMA", Symbols.SPY, 2);
            var time = DateTime.UtcNow;

            indicator.Update(new IndicatorDataPoint(Symbols.AAPL, time, 3));
            indicator.Update(new IndicatorDataPoint(Symbols.SPY, time.AddMinutes(1), 2));

            Assert.IsFalse(indicator.IsReady);
            Assert.AreEqual(0m, indicator.Current.Value);

            indicator.Update(new IndicatorDataPoint(Symbols.AAPL, time.AddMinutes(1), 5));

            Assert.IsFalse(indicator.IsReady);
            Assert.AreEqual(5m, indicator.Current.Value);

            UpdatePair(indicator, 3, 7, time.AddMinutes(2), targetFirst: true);

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1m, Math.Round(indicator.Intercept.Current.Value, 8));
            Assert.AreEqual(2m, Math.Round(indicator.Slope.Current.Value, 8));
            Assert.AreEqual(7m, Math.Round(indicator.Current.Value, 8));
        }

        [Test]
        public void WithReferenceResetsProperly()
        {
            var indicator = new LeastSquaresMovingAverage("LSMA", Symbols.SPY, 3);

            UpdatePair(indicator, 1, 3, targetFirst: true);
            UpdatePair(indicator, 2, 5, targetFirst: true);
            UpdatePair(indicator, 3, 7, targetFirst: true);

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(indicator);

            UpdatePair(indicator, 4, 9, targetFirst: false);
            UpdatePair(indicator, 5, 11, targetFirst: false);
            UpdatePair(indicator, 6, 13, targetFirst: false);

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(13m, Math.Round(indicator.Current.Value, 8));
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

        private static void UpdatePair(LeastSquaresMovingAverage indicator, decimal referenceValue, decimal targetValue, bool targetFirst)
        {
            UpdatePair(indicator, referenceValue, targetValue, DateTime.UtcNow.AddMinutes((double)referenceValue), targetFirst);
        }

        private static void UpdatePair(LeastSquaresMovingAverage indicator, decimal referenceValue, decimal targetValue, DateTime time, bool targetFirst)
        {
            var target = new IndicatorDataPoint(Symbols.AAPL, time, targetValue);
            var reference = new IndicatorDataPoint(Symbols.SPY, time, referenceValue);

            if (targetFirst)
            {
                indicator.Update(target);
                indicator.Update(reference);
            }
            else
            {
                indicator.Update(reference);
                indicator.Update(target);
            }
        }
    }
}
