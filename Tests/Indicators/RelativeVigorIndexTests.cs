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
    public class RelativeVigorIndexTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new RelativeVigorIndex("RVI", 10);
        }

        protected override string TestFileName => "spy_rvi.txt";
        protected override string TestColumnName => "RVI";

        [Test]
        public void ComparesWithExternalDataRviSignal()
        {
            var rvi = CreateIndicator();
            TestHelper.TestIndicator(rvi, TestFileName, "RVI_S",
                (ind, expected) => Assert.AreEqual(expected, 
                    (double) ((RelativeVigorIndex) ind).Signal.Current.Value, 0.06));
        }

        [Test]
        public void TestDivByZero() // Should give 0 (default) to avoid div by zero errors.
        {
            var rvi = CreateIndicator();
            for (int i = 0; i < 13; i++)
            {
                var tradeBar = new TradeBar
                    {
                        Open = 0m,
                        Close = 0m,
                        High = 0m,
                        Low = 0m,
                        Volume = 1
                    };
                    rvi.Update(tradeBar);
            }
            Assert.AreEqual(rvi.Current.Value, 0m);
            Assert.AreEqual(((RelativeVigorIndex) rvi).Signal.Current.Value, 0m);
        }
    }
}
