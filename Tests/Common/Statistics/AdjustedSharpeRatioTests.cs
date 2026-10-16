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
    public class AdjustedSharpeRatioTests
    {
        [Test]
        public void MatchesIndependentReferenceValue()
        {
            // Visible negative skew and a fat left tail; reference value computed independently
            // in plain Python using the bias-corrected sample skewness (G1) and excess kurtosis (G2) formulas
            var performance = new List<double> { 0.01, 0.02, -0.01, 0.03, 0.01, -0.02, 0.015, 0.005, -0.005, 0.02, 0.01, -0.15 };
            var sharpeRatio = 1.2345;

            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sharpeRatio);

            Assert.AreEqual(1.2975333348608493d, result, 1e-9);
        }

        [Test]
        public void ReturnsZeroWithFewerThanFourSamples()
        {
            // Excess kurtosis (G2) is undefined for n < 4, so the statistic must fall back to 0
            var performance = new List<double> { 0.01, 0.02, -0.01 };

            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, 1.5d);

            Assert.AreEqual(0d, result);
        }

        [Test]
        public void MatchesIndependentReferenceValueForNegativeSharpeRatio()
        {
            // Same fat-tailed series as MatchesIndependentReferenceValue, but with a negative input
            // Sharpe ratio: pins the current sign behaviour (the factor does not depend on the sign
            // of sharpeRatio, only on the moments of listPerformance, so the result is the negation of
            // the positive-SR reference value). Reference value computed independently in plain Python
            // using the bias-corrected sample skewness (G1) and excess kurtosis (G2) formulas.
            var performance = new List<double> { 0.01, 0.02, -0.01, 0.03, 0.01, -0.02, 0.015, 0.005, -0.005, 0.02, 0.01, -0.15 };
            var sharpeRatio = -1.2345;

            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sharpeRatio);

            Assert.AreEqual(-1.2975333348608493d, result, 1e-9);
        }

        [Test]
        public void MatchesIndependentReferenceValueWithNonZeroRiskFreeRate()
        {
            // Same fat-tailed series as MatchesIndependentReferenceValue, with a non-zero per-sample
            // risk-free rate: fails if riskFreeRate is dropped from this helper's own calculation.
            // Reference value computed independently in plain Python using the bias-corrected sample
            // skewness (G1) and excess kurtosis (G2) formulas.
            var performance = new List<double> { 0.01, 0.02, -0.01, 0.03, 0.01, -0.02, 0.015, 0.005, -0.005, 0.02, 0.01, -0.15 };
            var sharpeRatio = 1.2345;
            var riskFreeRate = 0.001;

            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sharpeRatio, riskFreeRate);

            Assert.AreEqual(1.3077912137233962d, result, 1e-9);
        }

        [Test]
        public void NegativeSkewLowersRatioBelowSharpeRatio()
        {
            // Mostly small gains with one large loss: positive observed Sharpe ratio combined with
            // strong negative skew and fat tails, which must pull the Adjusted Sharpe Ratio below SharpeRatio
            var performance = new List<double> { 0.02, 0.02, 0.02, 0.02, 0.02, 0.02, 0.02, 0.02, 0.02, -0.05 };
            var sharpeRatio = 1.0d;

            var result = QuantConnect.Statistics.Statistics.AdjustedSharpeRatio(performance, sharpeRatio);

            Assert.Less(result, sharpeRatio);
        }
    }
}
