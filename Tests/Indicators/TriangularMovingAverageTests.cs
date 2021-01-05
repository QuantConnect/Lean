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

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class TriangularMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new TriangularMovingAverage(5);
        }

        protected override string TestFileName => "spy_trima.txt";

        protected override string TestColumnName => "TRIMA";

        [Test]
        public override void ComparesAgainstExternalData()
        {
            foreach (var period in new[] {5, 6})
            {
                RunTestIndicator(new TriangularMovingAverage(period), period);
            }
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            foreach (var period in new[] { 5, 6 })
            {
                var indicator = new TriangularMovingAverage(period);
                RunTestIndicator(indicator, period);
                indicator.Reset();
                RunTestIndicator(indicator, period);
            }
        }

        private void RunTestIndicator(TriangularMovingAverage trima, int period)
        {
            TestHelper.TestIndicator(trima, TestFileName, TestColumnName + "_" + period, Assertion);
        }
    }
}