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
    public class MinimumVariancePortfolioOptimizerTests : PortfolioOptimizerTestsBase
    {
        private Dictionary<int, double> _targetReturns;

        [OneTimeSetUp]
        public void Setup()
        {
            var historicalReturns1 = new double[,] { { 0.76, -0.06, 1.22, 0.17 }, { 0.02, 0.28, 1.25, -0.00 }, { -0.50, -0.13, -0.50, -0.03 }, { 0.81, 0.31, 2.39, 0.26 }, { -0.02, 0.02, 0.06, 0.01 } };
            var historicalReturns2 = new double[,] { { -0.15, 0.67, 0.45 }, { -0.44, -0.10, 0.07 }, { 0.04, -0.41, 0.01 }, { 0.01, 0.03, 0.02 } };
            var historicalReturns3 = new double[,] { { -0.02, 0.65, 1.25 }, { -0.29, -0.39, -0.50 }, { 0.29, 0.58, 2.39 }, { 0.00, -0.01, 0.06 } };
            var historicalReturns4 = new double[,] { { 0.76, 0.25, 0.21 }, { 0.02, -0.15, 0.45 }, { -0.50, -0.44, 0.07 }, { 0.81, 0.04, 0.01 }, { -0.02, 0.01, 0.02 } };

            var expectedReturns1 = new double[] { 0.21, 0.08, 0.88, 0.08 };
            var expectedReturns2 = new double[] { -0.13, 0.05, 0.14 };
            var expectedReturns3 = (double[])null;
            var expectedReturns4 = (double[])null;

            var covariance1 = new double[,] { { 0.31, 0.05, 0.55, 0.07 }, { 0.05, 0.04, 0.18, 0.01 }, { 0.55, 0.18, 1.28, 0.12 }, { 0.07, 0.01, 0.12, 0.02 } };
            var covariance2 = new double[,] { { 0.05, -0.02, -0.01 }, { -0.02, 0.21, 0.09 }, { -0.01, 0.09, 0.04 } };
            var covariance3 = new double[,] { { 0.06, 0.09, 0.28 }, { 0.09, 0.25, 0.58 }, { 0.28, 0.58, 1.66 } };
            var covariance4 = (double[,])null;

            HistoricalReturns = new List<double[,]>
            {
                historicalReturns1, 
                historicalReturns2,
                historicalReturns3,
                historicalReturns4,
                historicalReturns1,
                historicalReturns2,
                historicalReturns3,
                historicalReturns4
            };

            ExpectedReturns = new List<double[]>
            {
                expectedReturns1,
                expectedReturns2,
                expectedReturns3,
                expectedReturns4,
                expectedReturns1,
                expectedReturns2,
                expectedReturns3,
                expectedReturns4
            };

            Covariances = new List<double[,]>
            {
                covariance1,
                covariance2,
                covariance3,
                covariance4,
                covariance1,
                covariance2,
                covariance3,
                covariance4
            };

            ExpectedResults = new List<double[]>
            {
                new double[] { -0.089212, 0.23431, -0.040975, 0.635503 },
                new double[] { 0.366812, -0.139738, 0.49345 },
                new double[] { 0.562216, 0.36747, -0.070314 },
                new double[] { -0.119241, 0.443464, 0.437295 },
                new double[] { -0.215505, 0.130699, 0.084806, 0.56899 },
                new double[] { -0.275, 0.275, 0.45 },
                new double[] { -0.129512, 0.551139, 0.319349 },
                new double[] { 0.052859, 0.144177, 0.802964 },
            };

            _targetReturns = new Dictionary<int, double>
            {
                { 4, 0.15d },
                { 5, 0.25d },
                { 6, 0.5d },
                { 7, 0.125d }
            };
        }

        protected override IPortfolioOptimizer CreateOptimizer()
        {
            return new MinimumVariancePortfolioOptimizer();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public override void OptimizeWeightings(int testCaseNumber)
        {
            base.OptimizeWeightings(testCaseNumber);
        }

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void OptimizeWeightingsSpecifyingTargetReturns(int testCaseNumber)
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer(targetReturn: _targetReturns[testCaseNumber]);

            var result = testOptimizer.Optimize(
                HistoricalReturns[testCaseNumber],
                ExpectedReturns[testCaseNumber],
                Covariances[testCaseNumber]);

            Assert.AreEqual(ExpectedResults[testCaseNumber], result.Select(x => Math.Round(x, 6)));
            Assert.AreEqual(1d, result.Select(x => Math.Round(Math.Abs(x), 6)).Sum());
        }

        [TestCase(0)]
        public void EqualWeightingsWhenNoSolutionFound(int testCaseNumber)
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer(upper: -1);
            var expectedResult = new double[] { 0.25, 0.25, 0.25, 0.25 };

            var result = testOptimizer.Optimize(HistoricalReturns[testCaseNumber]);

            Assert.AreEqual(expectedResult, result);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void BoundariesAreNotViolated(int testCaseNumber)
        {
            var lower = 0d;
            var upper = 0.5d;
            var testOptimizer = new MinimumVariancePortfolioOptimizer(lower, upper);

            var result = testOptimizer.Optimize(
                HistoricalReturns[testCaseNumber],
                ExpectedReturns[testCaseNumber],
                Covariances[testCaseNumber]);

            foreach (var x in result)
            {
                var rounded = Math.Round(x, 6);
                Assert.GreaterOrEqual(rounded, lower);
                Assert.LessOrEqual(rounded, upper);
            };
        }

        [Test]
        public void SingleSecurityPortfolioReturnsOne()
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer();
            var historicalReturns = new double[,] { { 0.76 }, { 0.02 }, { -0.50 } };
            var expectedResult = new double[] { 1 };

            var result = testOptimizer.Optimize(historicalReturns);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
