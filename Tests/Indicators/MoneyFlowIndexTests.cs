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
    public class MoneyFlowIndexTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            RenkoBarSize = 0.1m;
            return new MoneyFlowIndex(20);
        }

        protected override string TestFileName => "spy_mfi.txt";

        protected override string TestColumnName => "Money Flow Index 20";

        [Test]
        public void TestTradeBarsWithNoVolume()
        {
            var mfi = new MoneyFlowIndex(3);
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
                mfi.Update(tradeBar);
            }
            Assert.AreEqual(mfi.Current.Value, 100.0m);
        }
    }
}