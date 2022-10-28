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

using Accord.Math;
using NUnit.Framework;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Indicators 
{
    [TestFixture]
    public class SortinoRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {   
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new SortinoRatio("SORTINO", 15);
        }

        protected override string TestFileName => "spy_sortino.csv";

        protected override string TestColumnName => "sortino_rf_0_period_15";

        [Test]
        public void TestConstantValues() 
        {
            // With the value not changing, the indicator should return default value 0m.
            var sortino = new SortinoRatio("SORTINO", 15);

            // push the value 100000 into the indicator 20 times
            var time = DateTime.MinValue;
            for(int i = 0; i < 20; i++) {
                IndicatorDataPoint point = new IndicatorDataPoint(time.AddDays(i), 100000m);
                sortino.Update(point);
            }
            
            Assert.AreEqual(sortino.Current.Value, 0m);
        }
        
        [Test]
        public void TestOnlyIncreasingValues() 
        {
            // With the value increasing each step, the indicator should return 0m.
            var sr = new SortinoRatio("SORTINO", 15);

            // push only increasing values into the indicator
            var time = DateTime.MinValue;
            for (int i = 20; i > 0; i--) {
                IndicatorDataPoint point = new IndicatorDataPoint(time.AddDays(20-i), 100000m + i);
                sr.Update(point);
            }
            
            Assert.AreNotEqual(sr.Current.Value, 0m);
        }

        [Test]
        public void TestValuesFromCMEGroup()
        {
            // Source: https://www.cmegroup.com/education/files/rr-sortino-a-sharper-ratio.pdf

            List<decimal> values = new List<decimal>();
            values.Add(1m);
            var annualReturns = new[] { .17, .15, .23, -.05, .12, .09, .13, -.04 };
            for (var i = 0; i < annualReturns.Count(x => true); i++)
            {
                values.Add(values[i] * (decimal)(1 + annualReturns[i]));
            }

            var periods = annualReturns.Count(x => true);
            var sr = new SortinoRatio("SORTINO", periods, minimumAcceptableReturn: 0);

            var time = DateTime.MinValue;
            for (int i = 0; i <= periods; i++)
            {
                IndicatorDataPoint point = new IndicatorDataPoint(time.AddDays(i), values[i]);
                sr.Update(point);
            }

            Assert.AreEqual(4.417, sr.Current.Value.RoundToSignificantDigits(4));
        }

        [Test]
        public void RunTestIndicatorWithNonZeroRiskFreeRate()
        {
            TestHelper.TestIndicator(new SortinoRatio("SORTINO", 15, 0.01), TestFileName, "sortino_rf_0.01_period_15", Assertion);
        }

    }
}
