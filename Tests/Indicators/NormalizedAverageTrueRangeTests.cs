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
    public class NormalizedAverageTrueRangeTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var indicator = new NormalizedAverageTrueRange(5);

            RunTestIndicator(indicator);
        }

        [Test]
        public void ComparesAgainstExternalDataAfterReset()
        {
            var indicator = new NormalizedAverageTrueRange(5);

            RunTestIndicator(indicator);
            indicator.Reset();
            RunTestIndicator(indicator);
        }

        [Test]
        public void ResetsProperly()
        {
            var indicator = new NormalizedAverageTrueRange(5);

            TestHelper.TestIndicatorReset(indicator, "spy_natr.txt");
        }

        private static void RunTestIndicator(TradeBarIndicator indicator)
        {
            TestHelper.TestIndicator(indicator, "spy_natr.txt", "NATR_5", (ind, expected) => Assert.AreEqual(expected, (double)ind.Current.Value, 1e-3));
        }
    }
}
