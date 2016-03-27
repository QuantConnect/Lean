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
    public class ChandeMomentumOscillatorTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var cmo = new ChandeMomentumOscillator("CMO", 5);

            TestIndicator(cmo);
        }

        [Test]
        public void ComparesAgainstExternalDataAfterReset()
        {
            var cmo = new ChandeMomentumOscillator("CMO", 5);

            TestIndicator(cmo);
            cmo.Reset();
            TestIndicator(cmo);
        }

        [Test]
        public void ResetsProperly()
        {
            var cmo = new ChandeMomentumOscillator("CMO", 5);

            TestHelper.TestIndicatorReset(cmo, "spy_cmo.txt");
        }

        private static void TestIndicator(ChandeMomentumOscillator cmo)
        {
            TestHelper.TestIndicator(cmo, "spy_cmo.txt", "CMO_5", (ind, expected) => Assert.AreEqual(expected, (double)ind.Current.Value, 1e-3));
        }
    }
}
