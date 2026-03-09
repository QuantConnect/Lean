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
    public class KeltnerChannelsTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new KeltnerChannels(20, 1.5m);
        }

        protected override string TestFileName => "spy_with_keltner.csv";

        protected override string TestColumnName => "Middle Band";

        [Test]
        public void ComparesWithExternalDataUpperBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Keltner Channels 20 Top",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double) ((KeltnerChannels) ind).UpperBand.Current.Value,
                    1e-3
                )
            );
        }

        [Test]
        public void ComparesWithExternalDataLowerBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Keltner Channels 20 Bottom",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double) ((KeltnerChannels) ind).LowerBand.Current.Value,
                    1e-3
                )
            );
        }

        [Test]
        public void ComparesTimeStampBetweenKeltnerChannelAndMiddleBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Middle Band",
                (ind, expected) => Assert.AreEqual(
                    ((KeltnerChannels)ind).Current.EndTime,
                    ((KeltnerChannels)ind).MiddleBand.Current.EndTime
                )
            );
        }

        [Test]
        public override void ResetsProperly()
        {
            var kch = CreateIndicator() as KeltnerChannels;
            foreach (var data in TestHelper.GetTradeBarStream(TestFileName, false))
            {
               kch.Update(data);
            }

            Assert.IsTrue(kch.IsReady);
            Assert.IsTrue(kch.UpperBand.IsReady);
            Assert.IsTrue(kch.LowerBand.IsReady);
            Assert.IsTrue(kch.MiddleBand.IsReady);
            Assert.IsTrue(kch.AverageTrueRange.IsReady);

            kch.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(kch);
            TestHelper.AssertIndicatorIsInDefaultState(kch.UpperBand);
            TestHelper.AssertIndicatorIsInDefaultState(kch.LowerBand);
            TestHelper.AssertIndicatorIsInDefaultState(kch.AverageTrueRange);
        }
    }
}
