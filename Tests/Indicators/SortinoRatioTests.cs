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
    [TestFixture]
    public class SortinoRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {   
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new SortinoRatio("SORTINO", 15);
        }

        protected override string TestFileName => "spy_sortino.csv";

        protected override string TestColumnName => "sortino_15_rf_0";

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
        public void RunTestIndicatorWithNonZeroRiskFreeRate()
        {
            TestHelper.TestIndicator(new SortinoRatio("SORTINO", 15, 0.01m), TestFileName, "sortino_15_rf_0.01", Assertion);
        }

    }
}
