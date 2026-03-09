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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class SuperTrendTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new SuperTrend(10,3);
        }

        protected override string TestFileName => "dwac_supertrend.txt";

        protected override string TestColumnName => "Super";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.05); }
        }

        [Test]
        public void Getters()
        {
            var STR = new SuperTrend(10, 3);
            foreach (var data in TestHelper.GetDataStream(100))
            {
                var tradeBar = new TradeBar
                {
                    Open = data.Value,
                    Close = data.Value,
                    High = data.Value,
                    Low = data.Value,
                    Volume = data.Value
                };
                STR.Update(tradeBar);
            }
            Assert.IsTrue(STR.IsReady);
            Assert.AreNotEqual(0, STR.BasicUpperBand);
            Assert.AreNotEqual(0, STR.BasicLowerBand);
            Assert.AreNotEqual(0, STR.CurrentTrailingUpperBand);
            Assert.AreNotEqual(0, STR.CurrentTrailingLowerBand);
        }

        [Test]
        public override void ResetsProperly()
        {
            var STR = new SuperTrend(10, 3);
            foreach (var data in TestHelper.GetDataStream(100))
            {
                var tradeBar = new TradeBar
                {
                    Open = data.Value,
                    Close = data.Value,
                    High = data.Value,
                    Low = data.Value,
                    Volume = data.Value
                };
                STR.Update(tradeBar);
            }
            Assert.IsTrue(STR.IsReady);

            STR.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(STR);
        }

    }
}
