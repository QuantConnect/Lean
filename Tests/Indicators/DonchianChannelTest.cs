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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DonchianChannelTest
    {
        [Test]
        public void CompareAgainstExternalDataForUpperBand()
        {
            var donchianChannel = new DonchianChannel("dch", 50);

            TestHelper.TestIndicator(donchianChannel, "spy_with_don50.txt", "Donchian Channels 50 Top",
                    (ind, expected) => Assert.AreEqual(expected, (double)((DonchianChannel)ind).UpperBand.Current.Value));
        }

        [Test]
        public void CompareAgainstExternalDataForLowerBand()
        {
            var donchianChannel = new DonchianChannel("dch", 50);

            TestHelper.TestIndicator(donchianChannel, "spy_with_don50.txt", "Donchian Channels 50 Bottom",
                    (ind, expected) => Assert.AreEqual(expected, (double)((DonchianChannel)ind).LowerBand.Current.Value));
        }
        
        [Test]
        public void ComputesPrimaryOutputCorrectly()
        {
            var donchianChannel = new DonchianChannel("dch", 50);

            TestHelper.TestIndicator(donchianChannel, "spy_with_don50.txt", "Donchian Channels 50 Mean",
                    (ind, expected) => Assert.AreEqual(expected, (double)((DonchianChannel)ind).Current.Value));

        }

        [Test]
        public void ResetsProperly()
        {
            var donchianChannelIndicator = new DonchianChannel("DCH", 50);
            foreach (var data in TestHelper.GetTradeBarStream("spy_with_don50.txt", false))
            {
                donchianChannelIndicator.Update(data);
            }

            Assert.IsTrue(donchianChannelIndicator.IsReady);
            Assert.IsTrue(donchianChannelIndicator.UpperBand.IsReady);
            Assert.IsTrue(donchianChannelIndicator.LowerBand.IsReady);

            donchianChannelIndicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(donchianChannelIndicator);
            TestHelper.AssertIndicatorIsInDefaultState(donchianChannelIndicator.UpperBand);
            TestHelper.AssertIndicatorIsInDefaultState(donchianChannelIndicator.LowerBand);

        }
    }
}
