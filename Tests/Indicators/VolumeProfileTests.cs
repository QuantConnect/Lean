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
    class VolumeProfileTests : CommonIndicatorTests<TradeBar>
    {
        protected override string TestFileName => "vp_datatest.csv";

        protected override string TestColumnName => "POC";

        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            return new VolumeProfile(22);
        }
        protected override Action<IndicatorBase<TradeBar>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 0.01); }
        }

        [Test]
        public void ComparesWithExternalDataPOCVolume()
        {
            TestHelper.TestIndicator(
                new VolumeProfile("VP(22)", 22),
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
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).PH)
                );
        }

        [Test]
        public void ComparesWithExternalDataProfileLow()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "PL",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).PL)
                );
        }

        [Test]
        public void ComparesWithExternalDataValueArea()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "VA",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).ValueArea)
                );
        }

        [Test]
        public void ComparesWithExternalDataVAH()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "VAH",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).VAH)
                );
        }

        [Test]
        public void ComparesWithExternalDataVAL()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "VAL",
                (ind, expected) => Assert.AreEqual(expected, (double)((VolumeProfile)ind).VAL)
                );
        }

        [Test]
        public override void ResetsProperly()
        {
            var vp = (VolumeProfile)CreateIndicator();
            var reference = new System.DateTime(2020, 8, 1);
            for (int i = 0; i < 22; i++)
            {
                vp.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1,Volume=1, Time = reference.AddDays(1 + i) });
            }
            Assert.IsTrue(vp.IsReady);
            vp.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(vp);
            Assert.AreEqual(vp.POCPrice, 0m);
            Assert.AreEqual(vp.POCVolume, 0m);
            vp.Update(new TradeBar() { Symbol = Symbols.IBM, Close = 1, Volume=1, Time = reference.AddDays(1) });
            Assert.AreEqual(vp.Current.Value, 1m);
        }
    }
}

