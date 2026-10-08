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
using System.Linq;
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
            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 0.0, 252.0);
            Assert.AreEqual(0.0, result);

            var decimalResult = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 0m, 252);
            Assert.AreEqual(0m, decimalResult);
        }

        [Test]
        public void LessThanThreeSamplesReturnsZero()
        {
            var singleSample = new List<double> { 0.05 };
            var twoSamples = new List<double> { 0.01, 0.02 };

            Assert.AreEqual(0.0, QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(singleSample, 0.0, 252.0));
            Assert.AreEqual(0.0, QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(twoSamples, 0.0, 252.0));
            Assert.AreEqual(0m, QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(twoSamples, 0m, 252));
        }

        [Test]
        public void MatchesPezierWhiteFormulaAtConsistentFrequency()
        {
            var performance = new List<double> { 0.01, 0.02, -0.005, 0.015, -0.01, 0.03, -0.02 };
            var tradingDaysPerYear = 252.0;
            var riskFreeRate = 0.0;

            var observedSharpeRatio = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(performance, riskFreeRate);
            var skewness = performance.Skewness();
            var excessKurtosis = performance.Kurtosis(); // Excess kurtosis from MathNet (K - 3)

            var expectedPeriodAsr = observedSharpeRatio * (1.0 + (skewness / 6.0) * observedSharpeRatio - (excessKurtosis / 24.0) * Math.Pow(observedSharpeRatio, 2));
            var expectedAnnualizedAsr = expectedPeriodAsr * Math.Sqrt(tradingDaysPerYear);

            var actual = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, riskFreeRate, tradingDaysPerYear);
            Assert.AreEqual(expectedAnnualizedAsr, actual, 1e-10);

            var actualDecimal = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, (decimal)riskFreeRate, (int)tradingDaysPerYear);
            Assert.AreEqual((decimal)expectedAnnualizedAsr, actualDecimal);
        }

        [Test]
        public void AnnualizedSharpeOverloadMatchesDirectCalculation()
        {
            var performance = new List<double> { 0.005, 0.012, -0.003, 0.008, -0.006, 0.015, -0.004 };
            var tradingDaysPerYear = 252;
            var riskFreeRate = 0.0;

            var observedSharpe = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(performance, riskFreeRate);
            var annualizedSharpe = observedSharpe * Math.Sqrt(tradingDaysPerYear);

            var directAsr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, riskFreeRate, tradingDaysPerYear);
            var scaledOverloadAsr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, annualizedSharpe, tradingDaysPerYear);

            Assert.AreEqual(directAsr, scaledOverloadAsr, 1e-10);
        }

        [Test]
        public void FrequencyConsistencyAcrossSamplingHorizons()
        {
            // Simulate daily return series
            var random = new Random(42);
            var dailyReturns = new List<double>();
            for (var i = 0; i < 2520; i++)
            {
                // Mixture of normal to introduce mild skewness and kurtosis
                var u1 = random.NextDouble();
                var u2 = random.NextDouble();
                var z = Math.Sqrt(-2.0 * Math.Log(1.0 - u1)) * Math.Cos(2.0 * Math.PI * u2);
                var r = (random.NextDouble() < 0.05) ? (0.0005 + 0.03 * z - 0.01) : (0.0005 + 0.01 * z);
                dailyReturns.Add(r);
            }

            // Aggregate into 21-day (monthly) returns
            var monthlyReturns = new List<double>();
            var chunkSize = 21;
            for (var i = 0; i < dailyReturns.Count; i += chunkSize)
            {
                var chunk = dailyReturns.Skip(i).Take(chunkSize);
                var compounded = chunk.Aggregate(1.0, (acc, val) => acc * (1.0 + val)) - 1.0;
                monthlyReturns.Add(compounded);
            }

            var dailyAsr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(dailyReturns, 0.0, 252.0);
            var monthlyAsr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(monthlyReturns, 0.0, 252.0 / chunkSize);

            // Both frequencies estimate annualized ASR consistently without being distorted by unscaled daily kurtosis
            Assert.AreEqual(dailyAsr, monthlyAsr, 0.15);
        }

        [Test]
        public void NegativeSkewPenalizesSharpeRatio()
        {
            // Negatively skewed returns: frequent small gains and occasional large drawdowns
            var performance = new List<double> { 0.01, 0.012, 0.009, 0.011, 0.01, 0.013, -0.08 };
            var skewness = performance.Skewness();
            Assert.Less(skewness, 0);

            var observedSharpe = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(performance, 0.0);
            var annualizedSharpe = observedSharpe * Math.Sqrt(252);
            var asr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 0.0, 252.0);

            Assert.Less(asr, annualizedSharpe);
        }

        [Test]
        public void PositiveSkewIncreasesSharpeRatio()
        {
            // Positively skewed returns: frequent small losses/flat and occasional big right-tail wins
            var performance = new List<double> { -0.002, -0.001, 0.001, -0.003, -0.001, 0.002, 0.08 };
            var skewness = performance.Skewness();
            Assert.Greater(skewness, 0);

            var observedSharpe = QuantConnect.Statistics.Statistics.ObservedSharpeRatio(performance, 0.0);
            var annualizedSharpe = observedSharpe * Math.Sqrt(252);
            var asr = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 0.0, 252.0);

            Assert.Greater(asr, annualizedSharpe);
        }

        [Test]
        public void HandlesNaNAndInfinityGracefully()
        {
            var constantPerformance = new List<double> { 0.01, 0.01, 0.01, 0.01 };
            // Zero variance results in NaN for skewness / kurtosis
            Assert.DoesNotThrow(() =>
            {
                var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(constantPerformance, 0.0, 252.0);
                Assert.AreEqual(0.0, result);
            });
        }
    }
}
