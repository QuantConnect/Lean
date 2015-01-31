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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class BollingerBandsTests
    {
        [Test]
        public void ComparesWithExternalData()
        {
            const int maxFailures = 0;
            int totalFailures = 0;
            var bb = new BollingerBands (20, 20, 2, MovingAverageType.Simple);
            TestHelper.TestIndicator (bb, "spy_bollinger_bands.txt", "Moving Average 50", (i, expected) => {
                try {
                    var actual = (double)bb.MiddleBand.Current.Value;
                    Console.WriteLine(String.Format("expected={0} actual={1}", expected, actual));
                    Assert.AreEqual(expected, actual, 1e-3);
                } catch {
                    totalFailures++;
                }
            });

            if (totalFailures > maxFailures) {
                Assert.Fail("BollingerBands failed: {0} Expected: {1}", totalFailures, maxFailures);
            }
        }
    }
}
