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
    public class AverageDirectionalIndexTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var adx = new AverageDirectionalIndex("adx", 14);

            const double epsilon = 1;

            TestHelper.TestIndicator(adx, "spy_with_adx.txt", "ADX 14",
                (ind, expected) => Assert.AreEqual(expected, (double)((AverageDirectionalIndex)ind).Current.Value, epsilon)
            );
        }

        [Test]
        public void ComparesAgainstExternalDataAfterReset()
        {
            var adx = new AverageDirectionalIndex("adx", 14);

            const double epsilon = 1;

            TestHelper.TestIndicator(adx, "spy_with_adx.txt", "ADX 14",
                (ind, expected) => Assert.AreEqual(expected, (double)((AverageDirectionalIndex)ind).Current.Value, epsilon));
            adx.Reset();
            TestHelper.TestIndicator(adx, "spy_with_adx.txt", "ADX 14",
                (ind, expected) => Assert.AreEqual(expected, (double)((AverageDirectionalIndex)ind).Current.Value, epsilon));
        }

        [Test]
        public void ResetsProperly()
        {
            var adxIndicator = new AverageDirectionalIndex("ADX", 14);
            foreach (var data in TestHelper.GetTradeBarStream("spy_with_adx.txt", false))
            {
                adxIndicator.Update(data);
            }

            Assert.IsTrue(adxIndicator.IsReady);

            adxIndicator.Reset();
        }
    }
}
