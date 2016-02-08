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
    public class AccumulationDistributionOscillatorTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var adOsc = new AccumulationDistributionOscillator("ADOSC", 3, 10);

            TestHelper.TestIndicator(adOsc, "spy_ad_osc.txt", "AdOsc_3_10", (ind, expected) => Assert.AreEqual(expected, (double)ind.Current.Value, 1e-3));
        }

        [Test]
        public void ResetsProperly()
        {
            var adOsc = new AccumulationDistributionOscillator("ADOSC", 3, 10);
            foreach (var data in TestHelper.GetTradeBarStream("spy_ad_osc.txt", false))
            {
                adOsc.Update(data);
            }

            Assert.IsTrue(adOsc.IsReady);

            adOsc.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(adOsc);
        }
    }
}
