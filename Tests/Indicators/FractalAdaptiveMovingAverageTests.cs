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
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class FractalAdaptiveMovingAverageTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            FractalAdaptiveMovingAverage frama = new FractalAdaptiveMovingAverage("", 6, 100);

            decimal[] values = { 441m, 442m, 446m, 438m, 400m, 442m, 448m, 437m, 435m, 431m, 450m, 451m };
            decimal[] expected = { 441m, 442m, 446m, 438m, 400m, 442m, 444.51m, 441.67m, 438.58m, 434.69m, 435.84m, 436.73m, 438m };
            for (int i = 0; i < values.Length; i++)
            {
                frama.Update(new TradeBar { High = values[i], Low = values[i] });
                Assert.AreEqual(expected[i], Math.Round(frama.Current.Value, 2));
            }

        }

        [Test]
        public void ResetsProperly()
        {

            FractalAdaptiveMovingAverage frama = new FractalAdaptiveMovingAverage("", 6, 198);

            foreach (var data in TestHelper.GetDataStream(7))
            {
                frama.Update(new TradeBar { High = data, Low = data });
            }
            Assert.IsTrue(frama.IsReady);
            Assert.AreNotEqual(0m, frama.Current.Value);
            Assert.AreNotEqual(0, frama.Samples);

            frama.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(frama);
        }

        [Test]
        public void ComparesAgainstExternalData()
        {
            var indicator = new FractalAdaptiveMovingAverage("", 16, 198);
            RunTestIndicator(indicator);
        }

        private static void RunTestIndicator(TradeBarIndicator indicator)
        {
            TestHelper.TestIndicator(indicator, "frama.txt", "Filt", (actual, expected) => {AssertResult(expected, actual.Current.Value);});
        }

        private static void AssertResult(double expected, decimal actual)
        {
            Assert.IsTrue(Math.Abs((decimal)expected - actual) < 0.006m);
        }

    }
}
