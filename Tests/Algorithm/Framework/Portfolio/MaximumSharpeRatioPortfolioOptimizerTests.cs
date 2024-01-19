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
                new double[] { -0.562396, 0.608942, 0.953453 },
                new double[] { 0.686025, -0.269589, 0.583023 },
                new double[] { 0.26394, -0.043374, 0.779434 },
                new double[] { -0.223905, 0.401036, 1, 0.065329, -0.24246 },
                new double[] { 0.5, 0.5 },
                new double[] { -0.5, 0.5, 1 },
                new double[] { -0.242647, 1, 0.242647 },
                new double[] { -1, 0.922902, 0.364512, 0.712585 },
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
            var expectedResult = new double[] { 0, 0.44898, 0.55102 };

            var result = testOptimizer.Optimize(HistoricalReturns[testCaseNumber]);

            Assert.AreEqual(expectedResult, result.Select(x => Math.Round(x, 6)));
        }

        [Test]
        public void SingleSecurityPortfolioReturnsNaN()
        {
            var testOptimizer = new MaximumSharpeRatioPortfolioOptimizer();
            var historicalReturns = new double[,] { { -0.1 } };
            var expectedReturns = new double[] { -0.1 };

            var expectedResult = new double[] { double.NaN };

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
    }
}
