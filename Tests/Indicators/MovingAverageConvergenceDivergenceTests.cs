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
    public class MovingAverageConvergenceDivergenceTests
    {
        private string _externalDataFilename = "spy_with_macd.txt";

        [Test]
        public void ComparesWithExternalDataMACD()
        {
            var macd = new MovingAverageConvergenceDivergence(fastPeriod: 12, slowPeriod: 26, signalPeriod: 9);
            TestHelper.TestIndicator(
                macd,
                _externalDataFilename,
                "MACD",
                (ind, expected) => Assert.AreEqual(expected, (double)((MovingAverageConvergenceDivergence)ind).Current.Value, delta: 1e-4));
        }

        [Test]
        public void ComparesWithExternalDataMACDHistogram()
        {
            var macd = new MovingAverageConvergenceDivergence(fastPeriod: 12, slowPeriod: 26, signalPeriod: 9);
            TestHelper.TestIndicator(
                macd,
                _externalDataFilename,
                "Histogram",
                (ind, expected) => Assert.AreEqual(expected, (double)((MovingAverageConvergenceDivergence)ind).Histogram.Current.Value, delta: 1e-4));
        }

        [Test]
        public void ComparesWithExternalDataMACDSignal()
        {
            var macd = new MovingAverageConvergenceDivergence(fastPeriod: 12, slowPeriod: 26, signalPeriod: 9);
            TestHelper.TestIndicator(
                macd,
                _externalDataFilename,
                "Signal",
                (ind, expected) => Assert.AreEqual(expected, (double)((MovingAverageConvergenceDivergence)ind).Signal.Current.Value, delta: 1e-4));
        }
    }
}