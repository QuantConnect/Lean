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
    public class ChaikinMoneyFlowTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new ChaikinMoneyFlow("CMF", 20);
        }

        protected override string TestFileName => "spy_cmf.txt";

        protected override string TestColumnName => "CMF_20";
        
        [Test]
        public void TestTradeBarsWithNoVolume()
        {
            // As volume is a multiplier in numerator, should return default value 0m.
            var cmf = new ChaikinMoneyFlow("CMF", 3);
            foreach (var data in TestHelper.GetDataStream(4))
            {
                var tradeBar = new TradeBar
                {
                    Open = data.Value,
                    Close = data.Value,
                    High = data.Value,
                    Low = data.Value,
                    Volume = 0
                };
                cmf.Update(tradeBar); 
            }
            Assert.AreEqual(cmf.Current.Value, 0m);
        }
        [Test]
        public void TestDivByZero()
        {
            var cmf = new ChaikinMoneyFlow("CMF", 3);
            foreach (var data in TestHelper.GetDataStream(4))
            {
                // Should handle High = Low case by returning 0m.
                var tradeBar = new TradeBar
                {
                    Open = data.Value,
                    Close = data.Value,
                    High = 1,
                    Low = 1,
                    Volume = 1
                };
                cmf.Update(tradeBar); 
            }
            Assert.AreEqual(cmf.Current.Value, 0m);
        }

        /// <summary>
        /// The final value of this indicator is zero because it uses the Volume of the bars it receives.
        /// Since RenkoBar's don't always have Volume, the final current value is zero. Therefore we
        /// skip this test
        /// </summary>
        /// <param name="indicator"></param>
        protected override void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
        }
    }
}
      