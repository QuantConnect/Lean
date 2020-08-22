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
    public class EaseOfMovementValueTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new EaseOfMovementValue();
        }

        protected override string TestFileName => "spy_emv.txt";

        protected override string TestColumnName => "EMV";

        [Test]
        public void TestTradeBarsWithVolume()
        {
            var emv = new EaseOfMovementValue();
            foreach (var data in TestHelper.GetDataStream(4))
            {
                var tradeBar = new TradeBar
                {
                    Open = data.Value,
                    Close = data.Value,
                    High = data.Value,
                    Low = data.Value,
                    Volume = data.Value
                };
                emv.Update(tradeBar);
            }
        }

        protected override Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1); }
        }

        [Test]
        public virtual void PeriodSet()
        {
            var emv = new EaseOfMovementValue(period: 3, scale: 1);
            var reference = System.DateTime.Today;

            emv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddMinutes(1) });
            Assert.AreEqual(0, emv.Current.Value);
            Assert.IsFalse(emv.IsReady);

            emv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 3, High = 4, Volume = 200, Time = reference.AddMinutes(2) });
            Assert.AreEqual(0.005, (double)emv.Current.Value, 0.00001);
            Assert.IsFalse(emv.IsReady);

            emv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 5, High = 6, Volume = 300, Time = reference.AddMinutes(3) });
            Assert.AreEqual(0.00556, (double)emv.Current.Value, 0.00001);
            Assert.IsTrue(emv.IsReady);

            emv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 6, High = 7, Volume = 400, Time = reference.AddMinutes(4) });
            Assert.AreEqual(0.00639, (double)emv.Current.Value, 0.00001);
            Assert.IsTrue(emv.IsReady);
        }
    }
}
