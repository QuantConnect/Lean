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
    public class LeastSquaresMovingAverageTest : CommonIndicatorTests<IndicatorDataPoint>
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
            125.51m  , 125.41m  , 125.328m , 125.381m , 125.4423m, 125.4591m,
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

            for (var i = 0; i < period.Value; i++)
            {
                indicator.Update(time.AddMinutes(i), Prices[i]);
                Assert.AreEqual(Expected[i], Math.Round(indicator.Current.Value, 4));
                Assert.AreEqual(i == period.Value - 1, indicator.IsReady);
            }
        }
    }
}