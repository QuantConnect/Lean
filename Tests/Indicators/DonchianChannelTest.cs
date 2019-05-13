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
    public class DonchianChannelTest : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new DonchianChannel(50);
        }

        protected override string TestFileName => "spy_with_don50.txt";

        protected override string TestColumnName => "Donchian Channels 50 Mean";

        [Test]
        public void CompareAgainstExternalDataForUpperBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Donchian Channels 50 Top",
                (ind, expected) => Assert.AreEqual(expected, (double) ((DonchianChannel) ind).UpperBand.Current.Value)
            );
        }

        [Test]
        public void CompareAgainstExternalDataForLowerBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Donchian Channels 50 Bottom",
                (ind, expected) => Assert.AreEqual(expected, (double) ((DonchianChannel) ind).LowerBand.Current.Value)
            );
        }
    }
}