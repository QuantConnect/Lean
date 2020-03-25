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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class TrackingErrorTests
    {
        [Test]
        public void IdenticalPerformance()
        {
            var random = new Random();

            var benchmarkPerformance = Enumerable.Repeat(random.NextDouble(), 252).ToList();
            var algoPerformance = benchmarkPerformance.Select(element => element).ToList();

            var result = QuantConnect.Statistics.Statistics.TrackingError(algoPerformance, benchmarkPerformance);

            Assert.AreEqual(0.0, result);
        }

        [Test]
        public void DifferentPerformance()
        {
            var benchmarkPerformance = new List<double>();
            var algoPerformance = new List<double>();

            // Gives us two sequences whose difference is always -175
            // This sequence will have variance 0
            var baseReturn = -176;
            for (var i = 1; i <= 252; i++)
            {
                benchmarkPerformance.Add(baseReturn + 1);
                algoPerformance.Add((baseReturn * 2) + 2);
            }

            var result = QuantConnect.Statistics.Statistics.TrackingError(algoPerformance, benchmarkPerformance);

            Assert.AreEqual(0.0, result);
        }

        [Test]
        public void AllZeros()
        {
            var benchmarkPerformance = Enumerable.Repeat(0.0, 252).ToList();
            var algoPerformance = Enumerable.Repeat(0.0, 252).ToList();

            var result = QuantConnect.Statistics.Statistics.TrackingError(algoPerformance, benchmarkPerformance);

            Assert.AreEqual(0.0, result);
        }
    }
}
