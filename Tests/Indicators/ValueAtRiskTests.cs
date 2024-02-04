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
    [TestFixture(0.99d, "VaR_99", TestName = nameof(ValueAtRiskTests))]
    [TestFixture(0.95d, "VaR_95", TestName = nameof(ValueAtRiskTests))]
    [TestFixture(0.9d, "VaR_90", TestName = nameof(ValueAtRiskTests))]
    public class ValueAtRiskTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        private const int _tradingDays = 252;
        private readonly double _confidenceLevel;
        private readonly string _testColumnName;
        
        protected override string TestFileName => "spy_valueatrisk.csv";

        protected override string TestColumnName => _testColumnName;

        public ValueAtRiskTests(double confidenceLevel, string testColumnName)
        {
            _confidenceLevel = confidenceLevel;
            _testColumnName = testColumnName;
        }

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new ValueAtRisk(_tradingDays, _confidenceLevel);
        }

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-3); }
        }

        [Test]
        public void DivisonByZero()
        {
            var indicator = CreateIndicator();

            for (int i = 0; i < 50; i++)
            {
                var indicatorDataPoint = new IndicatorDataPoint(new DateTime(), 0);
                indicator.Update(indicatorDataPoint);
            }

            Assert.AreEqual(indicator.Current.Value, 0m);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void PeriodBelowMinimumThrows()
        {
            var period = 2; 

            var exception = Assert.Throws<ArgumentException>(() => new ValueAtRisk(period, 0.99d));
            Assert.That(exception.Message, Is.EqualTo($"Period parameter for ValueAtRisk indicator must be greater than 2 but was {period}"));
        }
    }
}

