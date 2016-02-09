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
    public class TriangularMovingAverageTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var trimaOdd = new TriangularMovingAverage("TRIMA", 5);

            RunTestIndicator(trimaOdd, 5);

            var trimaEven = new TriangularMovingAverage("TRIMA", 6);

            RunTestIndicator(trimaEven, 6);
        }

        [Test]
        public void ComparesAgainstExternalDataAfterReset()
        {
            var trimaOdd = new TriangularMovingAverage("TRIMA", 5);

            RunTestIndicator(trimaOdd, 5);
            trimaOdd.Reset();
            RunTestIndicator(trimaOdd, 5);

            var trimaEven = new TriangularMovingAverage("TRIMA", 6);

            RunTestIndicator(trimaEven, 6);
            trimaEven.Reset();
            RunTestIndicator(trimaEven, 6);
        }

        [Test]
        public void ResetsProperly()
        {
            var trima = new TriangularMovingAverage("TRIMA", 5);

            TestHelper.TestIndicatorReset(trima, "spy_trima.txt");
        }

        private static void RunTestIndicator(TriangularMovingAverage trima, int period)
        {
            TestHelper.TestIndicator(trima, "spy_trima.txt", "TRIMA_" + period, (ind, expected) => Assert.AreEqual(expected, (double)ind.Current.Value, 1e-3));
        }
    }
}
