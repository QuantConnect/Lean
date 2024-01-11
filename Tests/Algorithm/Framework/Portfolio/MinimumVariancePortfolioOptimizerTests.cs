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
    public class MinimumVariancePortfolioOptimizerTests
    {
        private Dictionary<int, double> _targetReturns = new();
        private Dictionary<int, double[,]> _historicalReturns = new();
        private Dictionary<int, double[]> _expectedReturns = new();
        private Dictionary<int, double[,]> _covariances = new();
        private Dictionary<int, double[]> _expectedResults = new();

        [OneTimeSetUp]
        public void Setup()
        {
            double targetReturn1 = 0.15;
            double targetReturn2 = 0.25;
            double targetReturn3 = 0.5;
            double targetReturn4 = 0.125;

            double[,] historicalReturns1 = new double[,] { { 0.76, -0.06, 1.22, 0.17 }, { 0.02, 0.28, 1.25, -0.00 }, { -0.50, -0.13, -0.50, -0.03 }, { 0.81, 0.31, 2.39, 0.26 }, { -0.02, 0.02, 0.06, 0.01 } };
            double[,] historicalReturns2 = new double[,] { { -0.15, 0.67, 0.45 }, { -0.44, -0.10, 0.07 }, { 0.04, -0.41, 0.01 }, { 0.01, 0.03, 0.02 } };
            double[,] historicalReturns3 = new double[,] { { -0.02, 0.65, 1.25 }, { -0.29, -0.39, -0.50 }, { 0.29, 0.58, 2.39 }, { 0.00, -0.01, 0.06 } };
            double[,] historicalReturns4 = new double[,] { { 0.76, 0.25, 0.21 }, { 0.02, -0.15, 0.45 }, { -0.50, -0.44, 0.07 }, { 0.81, 0.04, 0.01 }, { -0.02, 0.01, 0.02 } };

            double[] expectedReturns1 = new double[] { 0.21, 0.08, 0.88, 0.08 };
            double[] expectedReturns2 = new double[] { -0.13, 0.05, 0.14 };
            double[] expectedReturns3 = null;
            double[] expectedReturns4 = null;

            double[,] covariance1 = new double[,] { { 0.31, 0.05, 0.55, 0.07 }, { 0.05, 0.04, 0.18, 0.01 }, { 0.55, 0.18, 1.28, 0.12 }, { 0.07, 0.01, 0.12, 0.02 } };
            double[,] covariance2 = new double[,] { { 0.05, -0.02, -0.01 }, { -0.02, 0.21, 0.09 }, { -0.01, 0.09, 0.04 } };
            double[,] covariance3 = new double[,] { { 0.06, 0.09, 0.28 }, { 0.09, 0.25, 0.58 }, { 0.28, 0.58, 1.66 } };
            double[,] covariance4 = null;

            double[] expectedResult1 = new double[] { -0.089212, 0.23431, -0.040975, 0.635503 };
            double[] expectedResult2 = new double[] { 0.366812, -0.139738, 0.49345 };
            double[] expectedResult3 = new double[] { 0.562216, 0.36747, -0.070314 };
            double[] expectedResult4 = new double[] { -0.119241, 0.443464, 0.437295 };
            double[] expectedResult5 = new double[] { -0.215505, 0.130699, 0.084806, 0.56899 };
            double[] expectedResult6 = new double[] { -0.275, 0.275, 0.45 };
            double[] expectedResult7 = new double[] { -0.129512, 0.551139, 0.319349 };
            double[] expectedResult8 = new double[] { 0.052859, 0.144177, 0.802964 };

            _targetReturns.Add(5, targetReturn1);
            _targetReturns.Add(6, targetReturn2);
            _targetReturns.Add(7, targetReturn3);
            _targetReturns.Add(8, targetReturn4);

            _expectedReturns.Add(1, expectedReturns1);
            _expectedReturns.Add(2, expectedReturns2);
            _expectedReturns.Add(3, expectedReturns3);
            _expectedReturns.Add(4, expectedReturns4);
            _expectedReturns.Add(5, expectedReturns1);
            _expectedReturns.Add(6, expectedReturns2);
            _expectedReturns.Add(7, expectedReturns3);
            _expectedReturns.Add(8, expectedReturns4);

            _historicalReturns.Add(1, historicalReturns1);
            _historicalReturns.Add(2, historicalReturns2);
            _historicalReturns.Add(3, historicalReturns3);
            _historicalReturns.Add(4, historicalReturns4);
            _historicalReturns.Add(5, historicalReturns1);
            _historicalReturns.Add(6, historicalReturns2);
            _historicalReturns.Add(7, historicalReturns3);
            _historicalReturns.Add(8, historicalReturns4);

            _covariances.Add(1, covariance1);
            _covariances.Add(2, covariance2);
            _covariances.Add(3, covariance3);
            _covariances.Add(4, covariance4);
            _covariances.Add(5, covariance1);
            _covariances.Add(6, covariance2);
            _covariances.Add(7, covariance3);
            _covariances.Add(8, covariance4);

            _expectedResults.Add(1, expectedResult1);
            _expectedResults.Add(2, expectedResult2);
            _expectedResults.Add(3, expectedResult3);
            _expectedResults.Add(4, expectedResult4);
            _expectedResults.Add(5, expectedResult5);
            _expectedResults.Add(6, expectedResult6);
            _expectedResults.Add(7, expectedResult7);
            _expectedResults.Add(8, expectedResult8);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TestOptimizeWeightings(int testCaseNumber)
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer();

            var result = testOptimizer.Optimize(
                _historicalReturns[testCaseNumber],
                _expectedReturns[testCaseNumber],
                _covariances[testCaseNumber]);

            Assert.AreEqual(_expectedResults[testCaseNumber], result.Select(x => Math.Round(x, 6)));
            Assert.AreEqual(1d, result.Select(x => Math.Round(Math.Abs(x), 6)).Sum());
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void TestOptimizeWeightingsSpecifyingTargetReturns(int testCaseNumber)
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer(targetReturn: _targetReturns[testCaseNumber]);

            var result = testOptimizer.Optimize(
                _historicalReturns[testCaseNumber],
                _expectedReturns[testCaseNumber],
                _covariances[testCaseNumber]);

            Assert.AreEqual(_expectedResults[testCaseNumber], result.Select(x => Math.Round(x, 6)));
            Assert.AreEqual(1d, result.Select(x => Math.Round(Math.Abs(x), 6)).Sum());
        }

        [TestCase(1)]
        public void EqualWeightingsWhenNoSolutionFound(int testCaseNumber)
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer(upper: -1);
            var expectedResult = new double[] { 0.25, 0.25, 0.25, 0.25 };

            var result = testOptimizer.Optimize(_historicalReturns[testCaseNumber]);

            Assert.AreEqual(expectedResult, result);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void BoundariesAreNotViolated(int testCaseNumber)
        {
            double lower = 0;
            double upper = 0.5;
            var testOptimizer = new MinimumVariancePortfolioOptimizer(lower, upper);

            var result = testOptimizer.Optimize(
                _historicalReturns[testCaseNumber],
                _expectedReturns[testCaseNumber],
                _covariances[testCaseNumber]);

            foreach (double x in result)
            {
                var rounded = Math.Round(x, 6);
                Assert.GreaterOrEqual(rounded, lower);
                Assert.LessOrEqual(rounded, upper);
            };
        }

        public void EmptyPortfolioReturnsEmptyArrayOfDouble()
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer();
            var historicalReturns = new double[,] { { } };

            var result = testOptimizer.Optimize(historicalReturns);

            Assert.AreEqual(Array.Empty<double>(), result);
        }

        public void SingleSecurityPortfolioReturnsOne()
        {
            var testOptimizer = new MinimumVariancePortfolioOptimizer();
            double[,] historicalReturns = new double[,] { { 0.76 }, { 0.02 }, { -0.50 } };

            var result = testOptimizer.Optimize(historicalReturns);

            Assert.AreEqual(1d, result);
        }
    }
}
