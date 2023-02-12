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
    public class RelativeVolumeTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new RelativeVolume(50);
        }

        protected override string TestFileName => "spy_rvol.csv";

        protected override string TestColumnName => "RVOL";

        protected override Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.001); }
        }

        [Test]
        public override void ResetsProperly()
        {
            var rvol = new RelativeVolume(50); // Default resolution is daily
            var reference = System.DateTime.Today;

            rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddDays(1) });
            rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 3, High = 4, Volume = 200, Time = reference.AddDays(2) });
            rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 5, High = 6, Volume = 300, Time = reference.AddDays(3) });
            Assert.IsTrue(rvol.IsReady);
            Assert.AreNotEqual(0m, rvol.Current.Value);

            rvol.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(rvol);
        }

        public void AddTradeBarData(ref RelativeVolume rvol, int iterations, Resolution resolution, DateTime reference)
        {
            for (int i = 0; i < iterations; i++)
            {
                if (resolution == Resolution.Daily)
                {
                    rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddDays(1 + i) });
                }
                else if (resolution == Resolution.Hour)
                {
                    rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddHours(1 + i) });
                }
                else if (resolution == Resolution.Minute)
                {
                    rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddMinutes(1 + i) });
                }
                else if (resolution == Resolution.Second)
                {
                    rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddSeconds(1 + i) });
                }
                else
                {
                    rvol.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddSeconds(1 + i) });
                }
            }
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var rvol1 = new RelativeVolume(2);
            var rvol2 = new RelativeVolume(2);
            var rvol3 = new RelativeVolume(2);
            var rvol4 = new RelativeVolume(2);
            var rvol5 = new RelativeVolume(2);
            var rvol6 = new RelativeVolume(2);
            var rvol7 = new RelativeVolume(2);
            var rvol8 = new RelativeVolume(2);
            var reference = new DateTime(2000, 1, 1, 0, 0, 0);

            AddTradeBarData(ref rvol1, 2 + 1, Resolution.Daily, reference); // Needs one more datapoint after x days to be ready
            AddTradeBarData(ref rvol2, 48, Resolution.Hour, reference);
            AddTradeBarData(ref rvol3, (1440 * 2), Resolution.Minute, reference);
            AddTradeBarData(ref rvol4, (86400 * 2), Resolution.Second, reference);
            AddTradeBarData(ref rvol5, 2, Resolution.Daily, reference);
            AddTradeBarData(ref rvol6, 47, Resolution.Hour, reference);
            AddTradeBarData(ref rvol7, (1440 * 2) - 1, Resolution.Minute, reference);
            AddTradeBarData(ref rvol8, (86400 * 2) - 1, Resolution.Second, reference);

            Assert.IsTrue(rvol1.IsReady);
            Assert.IsTrue(rvol2.IsReady);
            Assert.IsTrue(rvol3.IsReady);
            Assert.IsTrue(rvol4.IsReady);
            Assert.IsFalse(rvol5.IsReady);
            Assert.IsFalse(rvol6.IsReady);
            Assert.IsFalse(rvol7.IsReady);
            Assert.IsFalse(rvol8.IsReady);
        }
    }
}
