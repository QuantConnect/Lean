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
    public class VolumeProfileTests : CommonIndicatorTests<TradeBar>
    {
        protected override string TestFileName => "vp_datatest.csv";

        protected override string TestColumnName => "POCPrice";

        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new VolumeProfile(3);
        }
        protected override Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.01); }
        }

        [Test]
        public void ComparesWithExternalDataPOCVolume()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "POCVolume",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).POCVolume)
                );
        }

        [Test]
        public void ComparesWithExternalDataProfileHigh()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "PH",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).ProfileHigh)
                );
        }

        [Test]
        public void ComparesWithExternalDataProfileLow()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "PL",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).ProfileLow)
                );
        }

        [Test]
        public void ComparesWithExternalDataValueArea()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "VA",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).ValueAreaVolume, 0.01)
                );
        }

        [Test]
        public void ComparesWithExternalDataVAH()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "VAH",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).ValueAreaHigh)
                );
        }

        [Test]
        public void ComparesWithExternalDataVAL()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "VAL",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).ValueAreaLow)
                );
        }

        [Test]
        public override void ResetsProperly()
        {
            var vp = (VolumeProfile)CreateIndicator();
            var reference = new System.DateTime(2020, 8, 1);
            Assert.IsFalse(vp.IsReady);
            for (int i = 0; i < 3; i++)
            {
                vp.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddDays(1 + i) });
            }
            Assert.IsTrue(vp.IsReady);
            vp.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(vp);
            vp.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume = 1, Time = reference.AddDays(1) });
            Assert.AreEqual(vp.Current.Value, 1m);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var vp = new VolumeProfile(20);
            var reference = new DateTime(2000, 1, 1);
            var period = ((IIndicatorWarmUpPeriodProvider)vp).WarmUpPeriod;

            // Check VolumeProfile indicator assigns properly a WarmUpPeriod
            Assert.AreEqual(20, period);
            for (var i = 0; i < period; i++)
            {
                vp.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddDays(1 + i) });
                Assert.AreEqual(i == period - 1, vp.IsReady);
            }
        }

        [Test]
        public void DoesNotFailWithRepeatedInputCloseValues()
        {
            var closeValues = new double[] { 313.25, 313.248, 313.241, 313.249, 313.243, 314.245, 315.241 };
            var vp = new VolumeProfile(2);
            var reference = new DateTime(2000, 1, 1);
            var period = ((IIndicatorWarmUpPeriodProvider)vp).WarmUpPeriod;
            for (var i = 0; i < closeValues.Length; i++)
            {
                Assert.DoesNotThrow(() => vp.Update(new TradeBar() { Symbol = Symbols.AAPL, Close = (decimal)closeValues[i], Volume = closeValues[i] != 313.243 ? 100 : 0, Time = reference.AddDays(1 + i) }));
            }
        }
    }
}

