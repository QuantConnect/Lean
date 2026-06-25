/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by aaplicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Accord.Math;
using Accord.Statistics;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class MaximumSharpeRatioPortfolioOptimizerTests : PortfolioOptimizerTestsBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            HistoricalReturns = new List<double[,]>
            {
                new double[,] { { 0.02, -0.02, 0.28 }, { -0.50, -0.29, -0.13 }, { 0.81, 0.29, 0.31 }, { -0.03, -0.00, 0.01 } },
                new double[,] { { 0.10, 0.20, 0.4 }, { 0.12, 0.25, 0.4 }, { 0.11, 0.22, 0.4 } },
                new double[,] { { -0.19, 0.50, 0.45 }, { -0.62, -0.65, 0.07 }, { -0.14, 1.02, 0.01 }, { 0.00, -0.03, 0.01 } },
                new double[,] { { 0.46, 0.28, 0.58, 0.26, 0.14 }, { 0.52, 0.31, 0.43, 7.43, -0.00 }, { 0.13, 0.65, 0.52, 0.50, -0.08 }, { -0.41, -0.39, -0.28, -0.65, -0.20 }, { 0.77, 0.58, 0.58, 1.02, 0.03 }, { -0.03, -0.01, -0.01, -0.03, 0.07 } },
                new double[,] { { -0.50, -0.13 }, { 0.81, 0.31 }, { -0.02, 0.01 } },
                new double[,] { { 0.31, 0.25, 0.43 }, { 0.65, 0.60, 0.52 }, { -0.39, -0.22, -0.28 }, { 0.58, 0.13, 0.58 }, { -0.01, -0.00, -0.01 } },
                new double[,] { { 0.13, 0.65, 1.25 }, { -0.41, -0.39, -0.50 }, { 0.77, 0.58, 2.39 }, { -0.03, -0.01, 0.04 } },
                new double[,] { { 0.31, 0.43, 1.22, 0.03 }, { 0.65, 0.52, 1.25, 0.67 }, { -0.39, -0.28, -0.50, -0.10 }, { 0.58, 0.58, 2.39, -0.41 }, { -0.01, -0.01, 0.04, 0.03 } }
            };

            ExpectedReturns = new List<double[]>
            {
                new double[] { 0.08, -0.01, 0.12 },
                new double[] { 0.11, 0.23, 0.4 },
                new double[] { -0.24, 0.21, 0.14 },
                null,
                new double[] { 0.10, 0.06 },
                new double[] { 0.23, 0.15, 0.25 },
                null,
                new double[] { 0.23, 0.25, 0.88, 0.04 }
            };

            Covariances = new List<double[,]>
            {
                new double[,] { { 0.29, 0.13, 0.10 }, { 0.13, 0.06, 0.04 }, { 0.10, 0.04, 0.05 } },
                null,
                new double[,] { { 0.07, 0.12, -0.00 }, { 0.12, 0.51, 0.03 }, { -0.00, 0.03, 0.04 } },
                null,
                new double[,] { { 0.44, 0.15 }, { 0.15, 0.05 } },
                new double[,] { { 0.19, 0.11, 0.16 }, { 0.11, 0.09, 0.09 }, { 0.16, 0.09, 0.14 } },
                new double[,] { { 0.24, 0.20, 0.61 }, { 0.20, 0.25, 0.58 }, { 0.61, 0.58, 1.67 } },
                new double[,] { { 0.19, 0.16, 0.44, 0.05 }, { 0.16, 0.14, 0.40, 0.02 }, { 0.44, 0.40, 1.29, -0.06 }, { 0.05, 0.02, -0.06, 0.15 } }
            };

            ExpectedResults = new List<double[]>
            {
                new double[] { -0.5, 0.5, 1 },
                new double[] { 0, 0, 1 },
                new double[] { -0.404692, 0.404692, 1 },
                new double[] { -0.418338, 0.023261, 1, 0.040668, 0.35441 },
                new double[] { 0.5, 0.5 },
                new double[] { -0.670213, 0.670213, 1 },
                new double[] { -1, 1, 1 },
                new double[] { -1, 0.315476, 0.684524, 1 },
            };
        }

        protected override IPortfolioOptimizer CreateOptimizer()
        {
            return new MaximumSharpeRatioPortfolioOptimizer();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public override void OptimizeWeightings(int testCaseNumber)
        {
            base.OptimizeWeightings(testCaseNumber);
        }

        [TestCase(0)]
        public void OptimizeWeightingsSpecifyingLowerBoundAndRiskFreeRate(int testCaseNumber)
        {
            var testOptimizer = new MaximumSharpeRatioPortfolioOptimizer(lower: 0, riskFreeRate: 0.04);
            var expectedResult = new double[] { 0, 0, 1 };

            var result = testOptimizer.Optimize(HistoricalReturns[testCaseNumber]);

            Assert.AreEqual(expectedResult, result.Select(x => Math.Round(x, 6)));
        }

        [Test]
        public void SingleSecurityPortfolioReturnsFullWeight()
        {
            var testOptimizer = new MaximumSharpeRatioPortfolioOptimizer();
            var historicalReturns = new double[,] { { -0.1 } };
            var expectedReturns = new double[] { -0.1 };

            // With a single security the budget constraint Σw = 1 leaves it fully invested
            var expectedResult = new double[] { 1 };

            var result = testOptimizer.Optimize(historicalReturns, expectedReturns);

            Assert.AreEqual(result, expectedResult);
        }

        [Test]
        public void EqualWeightingsWhenNoSolutionFound()
        {
            var testOptimizer = new MaximumSharpeRatioPortfolioOptimizer();
            var historicalReturns = new double[,] { { -0.10, -0.20 }, { -0.12, -0.25 } };
            var expectedReturns = new double[] { -0.10, -0.25 };
            var covariance = new double[,] { { 0.25, 0.12 }, { 0.45, 0.2 } }; // non positive definite

            var expectedResult = new double[] { 0.5, 0.5 };

            var result = testOptimizer.Optimize(historicalReturns, expectedReturns, covariance);

            Assert.AreEqual(result, expectedResult);
        }

        [Test]
        public void BoundariesAreNotViolated()
        {
            var testCaseNumber = 1;
            var lower = 0d;
            var upper = 0.5d;
            var testOptimizer = new MaximumSharpeRatioPortfolioOptimizer(lower, upper);

            var result = testOptimizer.Optimize(HistoricalReturns[testCaseNumber], null, Covariances[testCaseNumber]);

            foreach (var x in result)
            {
                var rounded = Math.Round(x, 6);
                Assert.GreaterOrEqual(rounded, lower);
                Assert.LessOrEqual(rounded, upper);
            };
        }

        // Every case whose maximum Sharpe ratio portfolio is well defined. Case 1 has a
        // zero-variance asset (the ratio is unbounded) and case 4 an indefinite covariance
        // (it falls back to equal weights), so neither has a finite interior optimum and
        // both are excluded.
        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void OptimizedWeightsMaximizeSharpeRatio(int testCaseNumber)
        {
            // Independent of the hardcoded ExpectedResults: the returned portfolio must
            // achieve a Sharpe ratio no lower than equal weights or any other feasible
            // portfolio drawn from the constraint set. The mean and covariance are
            // reconstructed exactly as the optimizer does so the check uses the same inputs.
            var testOptimizer = new MaximumSharpeRatioPortfolioOptimizer();
            var historicalReturns = HistoricalReturns[testCaseNumber];
            var expectedReturns = ExpectedReturns[testCaseNumber] ?? historicalReturns.Mean(0);
            var covariance = Covariances[testCaseNumber] ?? historicalReturns.Covariance();

            var result = testOptimizer.Optimize(historicalReturns, ExpectedReturns[testCaseNumber], Covariances[testCaseNumber]);
            var optimalSharpe = SharpeRatio(result, expectedReturns, covariance);

            var size = result.Length;
            var equalWeights = Enumerable.Repeat(1.0 / size, size).ToArray();
            Assert.GreaterOrEqual(optimalSharpe, SharpeRatio(equalWeights, expectedReturns, covariance));

            // The Sharpe ratio cannot exceed the unconstrained tangency portfolio, a hard
            // analytic ceiling (Cauchy-Schwarz) that no feasible portfolio can beat.
            Assert.LessOrEqual(optimalSharpe, TangencySharpe(expectedReturns, covariance) + 1e-6);

            var random = new Random(0);
            for (var i = 0; i < 10000; i++)
            {
                var candidate = RandomFeasibleWeights(random, size, lower: -1.0, upper: 1.0);
                Assert.GreaterOrEqual(optimalSharpe + 1e-6, SharpeRatio(candidate, expectedReturns, covariance));
            }
        }

        private static double SharpeRatio(double[] weights, double[] expectedReturns, double[,] covariance)
        {
            var size = weights.Length;
            var portfolioReturn = 0.0;
            var portfolioVariance = 0.0;
            for (var i = 0; i < size; i++)
            {
                portfolioReturn += weights[i] * expectedReturns[i];
                for (var j = 0; j < size; j++)
                {
                    portfolioVariance += weights[i] * covariance[i, j] * weights[j];
                }
            }
            return portfolioReturn / Math.Sqrt(portfolioVariance);
        }

        private static double TangencySharpe(double[] expectedReturns, double[,] covariance)
        {
            // The unconstrained maximum Sharpe ratio is sqrt(mu' inv(S) mu), the largest
            // value any portfolio can reach regardless of the budget or weight bounds.
            var inverse = covariance.Inverse();
            var size = expectedReturns.Length;
            var quadraticForm = 0.0;
            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    quadraticForm += expectedReturns[i] * inverse[i, j] * expectedReturns[j];
                }
            }
            return Math.Sqrt(quadraticForm);
        }

        private static double[] RandomFeasibleWeights(Random random, int size, double lower, double upper)
        {
            // Draw weights uniformly from the box and keep only those summing to one.
            while (true)
            {
                var weights = new double[size];
                var sum = 0.0;
                for (var i = 0; i < size - 1; i++)
                {
                    weights[i] = lower + random.NextDouble() * (upper - lower);
                    sum += weights[i];
                }
                var last = 1.0 - sum;
                if (last >= lower && last <= upper)
                {
                    weights[size - 1] = last;
                    return weights;
                }
            }
        }
    }
}
