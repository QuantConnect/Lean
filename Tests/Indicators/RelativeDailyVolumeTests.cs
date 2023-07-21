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
    public class RelativeDailyVolumeTests : CommonIndicatorTests<TradeBar>
    {
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            RenkoBarSize = 0.1m;
            return new RelativeDailyVolume(2);
        }

        protected override string TestFileName => "spy_rdv.txt";

        protected override string TestColumnName => "RDV";

        protected override Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.001); }
        }

        [Test]
        public override void ResetsProperly()
        {
            var rdv = new RelativeDailyVolume(2); // Default resolution is daily
            var reference = System.DateTime.Today;

            rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddDays(1) });
            rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 3, High = 4, Volume = 200, Time = reference.AddDays(2) });
            rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 5, High = 6, Volume = 300, Time = reference.AddDays(3) });
            Assert.IsTrue(rdv.IsReady);
            Assert.AreNotEqual(0m, rdv.Current.Value);

            rdv.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(rdv);
        }

        public void AddTradeBarData(ref RelativeDailyVolume rdv, int iterations, Resolution resolution, DateTime reference)
        {
            for (int i = 0; i < iterations; i++)
            {
                if (resolution == Resolution.Daily)
                {
                    rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddDays(1 + i) });
                }
                else if (resolution == Resolution.Hour)
                {
                    rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddHours(1 + i) });
                }
                else if (resolution == Resolution.Minute)
                {
                    rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddMinutes(1 + i) });
                }
                else if (resolution == Resolution.Second)
                {
                    rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddSeconds(1 + i) });
                }
                else
                {
                    rdv.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddSeconds(1 + i) });
                }
            }
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var rdv1 = new RelativeDailyVolume(2);
            var rdv2 = new RelativeDailyVolume(2);
            var rdv3 = new RelativeDailyVolume(2);
            var rdv4 = new RelativeDailyVolume(2);
            var rdv5 = new RelativeDailyVolume(2);
            var rdv6 = new RelativeDailyVolume(2);
            var rdv7 = new RelativeDailyVolume(2);
            var rdv8 = new RelativeDailyVolume(2);
            var reference = new DateTime(2000, 1, 1, 0, 0, 0);

            AddTradeBarData(ref rdv1, 2 + 1, Resolution.Daily, reference); // Needs one more datapoint after x days to be ready
            AddTradeBarData(ref rdv2, 48, Resolution.Hour, reference);
            AddTradeBarData(ref rdv3, (1440 * 2), Resolution.Minute, reference);
            AddTradeBarData(ref rdv4, (86400 * 2), Resolution.Second, reference);
            AddTradeBarData(ref rdv5, 2, Resolution.Daily, reference);
            AddTradeBarData(ref rdv6, 47, Resolution.Hour, reference);
            AddTradeBarData(ref rdv7, (1440 * 2) - 1, Resolution.Minute, reference);
            AddTradeBarData(ref rdv8, (86400 * 2) - 1, Resolution.Second, reference);

            Assert.IsTrue(rdv1.IsReady);
            Assert.IsTrue(rdv2.IsReady);
            Assert.IsTrue(rdv3.IsReady);
            Assert.IsTrue(rdv4.IsReady);
            Assert.IsFalse(rdv5.IsReady);
            Assert.IsFalse(rdv6.IsReady);
            Assert.IsFalse(rdv7.IsReady);
            Assert.IsFalse(rdv8.IsReady);
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
