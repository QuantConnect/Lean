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
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class RelativeMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new RelativeMovingAverage(5);
        }

        protected override string TestFileName => "spy_with_rma.csv";

        protected override string TestColumnName => "RMA5";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get
            {
                return (indicator, expected) =>
                    Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-2);
            }
        }

        [Test]
        public override void ResetsProperly()
        {
            var rma = new RelativeMovingAverage(5);
            foreach (var data in TestHelper.GetDataStream(5 * 3))
            {
                Assert.IsFalse(rma.IsReady);
                rma.Update(data);
            }
            Assert.IsTrue(rma.IsReady);

            rma.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(rma);
            TestHelper.AssertIndicatorIsInDefaultState(rma.ShortAverage);
            TestHelper.AssertIndicatorIsInDefaultState(rma.MediumAverage);
            TestHelper.AssertIndicatorIsInDefaultState(rma.LongAverage);
            rma.Update(DateTime.UtcNow, 2.0m);
            Assert.AreEqual(rma.Current.Value, 2.0m);
        }
    }
}
