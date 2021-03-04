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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class PivotPointsHighLowTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
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
    }
}
