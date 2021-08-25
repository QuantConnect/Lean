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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    class MarketProfileTests : CommonIndicatorTests<TradeBar>
    {
        protected override string TestFileName => "ibm_mp.csv";

        protected override string TestColumnName => "POC";

        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new MarketProfile(22);
        }
        protected override Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.01); }
        }

        [Test]
        public override void ResetsProperly()
        {
            var mp = (MarketProfile)CreateIndicator();
            var reference = new System.DateTime(2020, 8, 1);
            for(int i=0; i < 22; i++)
            {
                mp.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Time = reference.AddDays(1+i) });
            }
            
            Assert.IsTrue(mp.IsReady);

            mp.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(mp);
            Assert.AreEqual(mp.POCPrice, 0m);
            Assert.AreEqual(mp.POCVolume, 0m);
            mp.Update(new TradeBar() { Symbol = Symbols.IBM, Close=1,Time = reference.AddDays(1) });
            Assert.AreEqual(mp.Current.Value, 1m);
        }
    }
}
