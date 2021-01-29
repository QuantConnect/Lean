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

using System.Linq;
using Accord.Math;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class TimeSeriesIndicatorTests
    {
        [Test]
        public void DifferencesSeries()
        {
            var test = EvenOddSeries();

            double[] heads;
            var differencer = TimeSeriesIndicator.DifferenceSeries(1, test, out heads);
            Assert.AreEqual(-1, differencer.Sum());
        }

        [Test]
        public void UndifferencesSeries()
        {
            var test = EvenOddSeries();

            double[] heads;
            var differencer = TimeSeriesIndicator.DifferenceSeries(1, test, out heads);
            Assert.AreEqual(test.Sum(), TimeSeriesIndicator.InverseDifferencedSeries(differencer, heads).Sum());
        }

        [Test]
        public void LagsSeriesTest()
        {
            var test = EvenOddSeries(0, 1);
            var lags = TimeSeriesIndicator.LaggedSeries(1, test);
            
            Assert.AreEqual(50, test.Sum());
            Assert.AreEqual(49d, lags.Sum());
        }

        [Test]
        public void CumulativeSumTest()
        {
            var test = EvenOddSeries(0, 1, 50);
            var sums = TimeSeriesIndicator.CumulativeSum(test.ToList());
            
            Assert.AreEqual(25, test.Sum());
            Assert.AreEqual(625, sums.Sum()); // From excel
        }

        private static double[] EvenOddSeries(int even = 1, int odd = 2, int len = 100)
        {
            var test = new double[len];
            for (var i = 0; i < test.Length; i++)
            {
                switch (i % 2)
                {
                    case 0:
                        test[i] = even;
                        break;
                    default:
                        test[i] = odd;
                        break;
                }
            }

            return test;
        }
    }
}