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
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class RachevRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        private const int _tradingDays = 252;

        protected override string TestFileName => "spy_rachev_ratio.csv";

        protected override string TestColumnName => "rachev_ratio";

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new RachevRatio(_tradingDays);
        }

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-6); }
        }

        [Test]
        public void ComparesAgainstExternalDataWithDifferentTailProbabilities()
        {
            var indicator = new RachevRatio(_tradingDays, alpha: 0.1, beta: 0.05);

            TestHelper.TestIndicator(indicator, TestFileName, "rachev_ratio_alpha_0.1_beta_0.05", Assertion);
        }

        [Test]
        public void ComparesAgainstExternalDataWithRiskFreeRate()
        {
            var indicator = new RachevRatio(_tradingDays, riskFreeRate: 0.0001);

            TestHelper.TestIndicator(indicator, TestFileName, "rachev_ratio_rf_0.0001", Assertion);
        }

        [Test]
        public void ComputesTailAveragesOfKnownReturns()
        {
            // Returns: -2%, +1%, +3%, -1%, +2%
            // With alpha = beta = 0.2 each tail holds one observation: 3% / 2% = 1.5
            var prices = new[] { 100m, 98m, 98.98m, 101.9494m, 100.929906m, 102.94850412m };
            var indicator = new RachevRatio(5, alpha: 0.2, beta: 0.2);

            var time = new DateTime(2024, 1, 1);
            for (var i = 0; i < prices.Length; i++)
            {
                Assert.IsFalse(indicator.IsReady);
                indicator.Update(new IndicatorDataPoint(time.AddDays(i), prices[i]));
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1.5, (double)indicator.Current.Value, 1e-10);
        }

        [Test]
        public void ConstantValuesReturnZero()
        {
            // With the value not changing there is no tail loss, so the indicator should return 0m
            var indicator = new RachevRatio(_tradingDays);

            var time = new DateTime(2024, 1, 1);
            for (var i = 0; i < indicator.WarmUpPeriod; i++)
            {
                indicator.Update(new IndicatorDataPoint(time.AddDays(i), 100m));
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(0m, indicator.Current.Value);
        }

        [TestCase(1, 0.05, 0.05)]
        [TestCase(10, 0, 0.05)]
        [TestCase(10, 1, 0.05)]
        [TestCase(10, 0.05, 0)]
        [TestCase(10, 0.05, 1)]
        public void InvalidParametersThrow(int period, double alpha, double beta)
        {
            Assert.Throws<ArgumentException>(() => new RachevRatio(period, alpha, beta));
        }
    }
}
