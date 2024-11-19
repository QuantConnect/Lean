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
    public class ConnorsRelativeStrengthIndexTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new ConnorsRelativeStrengthIndex("test", 3, 2, 100);
        }
        protected override string TestFileName => "spy_crsi.csv";

        protected override string TestColumnName => "crsi";

        [Test]
        public void DoesNotThrowDivisionByZero()
        {
            var crsi = new ConnorsRelativeStrengthIndex(2, 2, 2);
            for (var i = 0; i < 10; i++)
            {
                Assert.DoesNotThrow(() => crsi.Update(DateTime.UtcNow, 0m));
            }
        }

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var rsiPeriod = 2;
            var rsiPeriodStreak = 3;
            var rocPeriod = 4;
            var crsi = new ConnorsRelativeStrengthIndex(rsiPeriod, rsiPeriodStreak, rocPeriod);
            int minInputValues = Math.Max(rsiPeriod, Math.Max(rsiPeriodStreak, rocPeriod + 1));
            for (int i = 0; i < minInputValues; i++)
            {
                Assert.IsFalse(crsi.IsReady);
                crsi.Update(DateTime.Now, i + 1);
            }
            Assert.IsTrue(crsi.IsReady);
        }

    }
}