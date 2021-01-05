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

using System.Collections.Generic;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class ProbabilisticSharpeRatioTests
    {
        [Test]
        public void SameAsBenchmark()
        {
            var performance = new List<double> { 0.01, 0.02, 0.01, 0, 0, 3 };
            var benchmark = new List<double> { 0.01, 0.02, 0.01, 0, 0, 3 };

            var benchmarkSharpeRatio = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(benchmark);

            var result = QuantConnect.Statistics.Statistics.ProbabilisticSharpeRatio(performance,
                benchmarkSharpeRatio);

            // they zero each other out
            Assert.AreEqual(0.5d, result, 0.001);
        }

        [Test]
        public void BeatBenchmark()
        {
            var performance = new List<double> { 0.01, 0.02, 0.01, 0, 0,3 };
            var benchmark = new List<double> { 0, 0, 0, -0.1, 0, 0.01, 0 };

            var benchmarkSharpeRatio = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(benchmark);

            var result = QuantConnect.Statistics.Statistics.ProbabilisticSharpeRatio(performance,
                benchmarkSharpeRatio);

            Assert.AreEqual(1d, result, 0.001);
        }

        [Test]
        public void LoseAgainstBenchmark()
        {
            var benchmark = new List<double> { 0.01, 0.02, 0.01, 0, 0, 3 };
            var performance = new List<double> { 0, 0, 0, -0.1, 0, 0.01, 0 };

            var benchmarkSharpeRatio = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(benchmark);

            var result = QuantConnect.Statistics.Statistics.ProbabilisticSharpeRatio(performance,
                benchmarkSharpeRatio);

            Assert.AreEqual(0d, result, 0.001);
        }

        [Test]
        public void ZeroValues()
        {
            var benchmark = new List<double> { 0, 0, 0 };
            var performance = new List<double> { 0, 0, 0 };

            var benchmarkSharpeRatio = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(benchmark);

            var result = QuantConnect.Statistics.Statistics.ProbabilisticSharpeRatio(performance,
                benchmarkSharpeRatio);

            Assert.AreEqual(0d, result, 0.001);
        }
    }
}
