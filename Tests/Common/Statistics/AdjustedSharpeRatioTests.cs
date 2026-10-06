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
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using NUnit.Framework;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class AdjustedSharpeRatioTests
    {
        [Test]
        public void ZeroSharpeRatioReturnsZero()
        {
            var performance = new List<double> { 0.01, -0.02, 0.015, -0.005 };
            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 0.0);
            Assert.AreEqual(0.0, result);

            var decimalResult = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 0m);
            Assert.AreEqual(0m, decimalResult);
        }

        [Test]
        public void LessThanThreeSamplesReturnsZero()
        {
            var singleSample = new List<double> { 0.05 };
            var twoSamples = new List<double> { 0.01, 0.02 };

            Assert.AreEqual(0.0, QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(singleSample, 1.5));
            Assert.AreEqual(0.0, QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(twoSamples, 1.5));
            Assert.AreEqual(0m, QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(twoSamples, 1.5m));
        }

        [Test]
        public void MatchesPezierWhiteFormula()
        {
            var performance = new List<double> { 0.01, 0.02, -0.005, 0.015, -0.01, 0.03, -0.02 };
            var sr = 1.8;

            var skewness = performance.Skewness();
            var kurtosis = performance.Kurtosis();
            var expected = sr * (1.0 + (skewness / 6.0) * sr - (kurtosis / 24.0) * Math.Pow(sr, 2));

            var actual = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sr);
            Assert.AreEqual(expected, actual, 1e-10);

            var actualDecimal = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, (decimal)sr);
            Assert.AreEqual((decimal)expected, actualDecimal);
        }

        [Test]
        public void NegativeSkewPenalizesSharpeRatio()
        {
            // Negatively skewed returns: frequent small gains and occasional large drawdowns
            var performance = new List<double> { 0.01, 0.012, 0.009, 0.011, 0.01, 0.013, -0.08 };
            var sr = 1.5;

            var skewness = performance.Skewness();
            Assert.Less(skewness, 0);

            var asr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sr);
            // With negative skew and positive kurtosis penalty, ASR must be strictly less than SR
            Assert.Less(asr, sr);
        }

        [Test]
        public void PositiveSkewIncreasesSharpeRatio()
        {
            // Positively skewed returns: frequent small losses/flat and occasional big right-tail wins
            var performance = new List<double> { -0.002, -0.001, 0.001, -0.003, -0.001, 0.002, 0.08 };
            var sr = 0.8;

            var skewness = performance.Skewness();
            Assert.Greater(skewness, 0);

            var asr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sr);
            Assert.Greater(asr, sr);
        }

        [Test]
        public void HandlesNaNAndInfinityGracefully()
        {
            var constantPerformance = new List<double> { 0.01, 0.01, 0.01, 0.01 };
            // Skewness and kurtosis on constant values may be NaN due to zero variance
            Assert.DoesNotThrow(() =>
            {
                var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(constantPerformance, 1.0);
                Assert.AreEqual(0.0, result);
            });
        }
    }
}
