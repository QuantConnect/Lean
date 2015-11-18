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

namespace QuantConnect.Tests.Indicators {
    [TestFixture]
    public class CommodityChannelIndexTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var cci = new CommodityChannelIndex(14);
            TestHelper.TestIndicator(cci, "spy_with_cci.txt", "Commodity Channel Index (CCI) 14", 1e-2);
        }

        [Test]
        public void ResetsProperly()
        {
            var cci = new CommodityChannelIndex(2);
            cci.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = DateTime.Today,
                Open = 3m,
                High = 7m,
                Low = 2m,
                Close = 5m,
                Volume = 10
            });
            Assert.IsFalse(cci.IsReady);
            cci.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = DateTime.Today.AddSeconds(1),
                Open = 3m,
                High = 7m,
                Low = 2m,
                Close = 5m,
                Volume = 10
            });
            Assert.IsTrue(cci.IsReady);

            cci.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(cci);
            TestHelper.AssertIndicatorIsInDefaultState(cci.TypicalPriceAverage);
            TestHelper.AssertIndicatorIsInDefaultState(cci.TypicalPriceMeanDeviation);
        }
    }
}