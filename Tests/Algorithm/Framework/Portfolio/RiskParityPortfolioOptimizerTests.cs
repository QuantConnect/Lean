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

using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class RiskParityPortfolioOptimizerTests
    {
        private Dictionary<int, double[][]> _covariances = new();
        private Dictionary<int, double[]> _riskBudgets = new();
        private Dictionary<int, double[]> _expectedResults = new();

        [SetUp]
        public virtual void SetUp()
        {
            var riskBudget1 = new double[] {0.5d, 0.5d};                                // equal risk distribution
            var riskBudget2 = new double[] {0.25d, 0.75d};                              // 25% risk assigned to A, 75% risk assigned to B

            var covariance1 = new double[][] {new[]{0.25, -0.2}, new[]{-0.2, 0.25}};    // both 50% variance, -80% correlation
            var covariance2 = new double[][] {new[]{0.01, 0}, new[]{0, 0.04}};          // sigma(A) 10%, sigma(B) 20%, 0% correlation
            var covariance3 = new double[][] {new[]{1, 0.45}, new[]{0.45, 0.25}};       // sigma(A) 100%, sigma(B) 50%, 90% correlation
            var covariance4 = new double[][] {new[]{0.25, 0.05}, new[]{0.05, 0.04}};    // sigma(A) 50%, sigma(B) 10%, 25% correlation

            var expectedResult1 = new double[] {3.162278d, 3.162278d};
            var expectedResult2 = new double[] {7.071068d, 3.535534d};
            var expectedResult3 = new double[] {0.512989d, 1.025978d};
            var expectedResult4 = new double[] {1.154701d, 2.886751d};
            var expectedResult5 = new double[] {2.965685d, 3.285618d};
            var expectedResult6 = new double[] {5d, 4.330127d};
            var expectedResult7 = new double[] {0.264749d, 1.510089d};
            var expectedResult8 = new double[] {0.681774d, 3.924933d};

            _covariances.TryAdd(1, covariance1);
            _covariances.TryAdd(2, covariance2);
            _covariances.TryAdd(3, covariance3);
            _covariances.TryAdd(4, covariance4);
            _covariances.TryAdd(5, covariance1);
            _covariances.TryAdd(6, covariance2);
            _covariances.TryAdd(7, covariance3);
            _covariances.TryAdd(8, covariance4);

            _riskBudgets.TryAdd(1, riskBudget1);
            _riskBudgets.TryAdd(2, riskBudget1);
            _riskBudgets.TryAdd(3, riskBudget1);
            _riskBudgets.TryAdd(4, riskBudget1);
            _riskBudgets.TryAdd(5, riskBudget2);
            _riskBudgets.TryAdd(6, riskBudget2);
            _riskBudgets.TryAdd(7, riskBudget2);
            _riskBudgets.TryAdd(8, riskBudget2);

            _expectedResults.TryAdd(1, expectedResult1);
            _expectedResults.TryAdd(2, expectedResult2);
            _expectedResults.TryAdd(3, expectedResult3);
            _expectedResults.TryAdd(4, expectedResult4);
            _expectedResults.TryAdd(5, expectedResult5);
            _expectedResults.TryAdd(6, expectedResult6);
            _expectedResults.TryAdd(7, expectedResult7);
            _expectedResults.TryAdd(8, expectedResult8);
        }

        [TestCase(1)]
        [TestCase(0)]
        [TestCase(1001)]
        public void TestForExtremeNumberOfVariablesRiskParityNewtonMethodOptimization(int numberOfVariables)
        {
            var testOptimizer = new TestRiskParityPortfolioOptimizer();

            if (numberOfVariables < 1 || numberOfVariables > 1000)
            {
                var exception = Assert.Throws<ArgumentException>(() => testOptimizer.TestOptimization(numberOfVariables, new double[,]{}, new double[]{}));
                Assert.That(exception.Message, Is.EqualTo("Argument \"numberOfVariables\" must be a positive integer between 1 and 1000"));
                return;
            }

            var result = testOptimizer.TestOptimization(numberOfVariables, new double[,]{}, new double[]{});
            Assert.AreEqual(new double[] {1d}, result);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void TestRiskParityNewtonMethodOptimizationWeightings(int testCaseNumber)
        {
            var testOptimizer = new TestRiskParityPortfolioOptimizer();
            var covariance = JaggedArrayTo2DArray(_covariances[testCaseNumber]);

            var result = testOptimizer.TestOptimization(2, covariance, _riskBudgets[testCaseNumber]);
            result = result.Select(x => Math.Round(x, 6)).ToArray();

            Assert.AreEqual(_expectedResults[testCaseNumber], result);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void TestOptimizeWeightings(int testCaseNumber)
        {
            var testOptimizer = new TestRiskParityPortfolioOptimizer();
            var covariance = JaggedArrayTo2DArray(_covariances[testCaseNumber]);
            var result = testOptimizer.Optimize(new double[,]{}, _riskBudgets[testCaseNumber], covariance);
            result = result.Select(x => Math.Round(x, 6)).ToArray();

            var expected = _expectedResults[testCaseNumber];
            expected = Elementwise.Divide(expected, expected.Sum()).Select(x => Math.Clamp(x, 1e-05, double.MaxValue)).ToArray();
            expected = expected.Select(x => Math.Round(x, 6)).ToArray();
            
            Assert.AreEqual(expected, result);
        }

        private static T[,] JaggedArrayTo2DArray<T>(T[][] source)
        {
            int FirstDim = source.Length;
            int SecondDim = source.GroupBy(row => row.Length).Single().Key;

            var result = new T[FirstDim, SecondDim];
            for (int i = 0; i < FirstDim; ++i)
                for (int j = 0; j < SecondDim; ++j)
                    result[i, j] = source[i][j];

            return result;
        }

        private class TestRiskParityPortfolioOptimizer : RiskParityPortfolioOptimizer
        {
            public TestRiskParityPortfolioOptimizer()
                : base()
            {
            }

            public double[] TestOptimization(int numOfVar, double[,] covariance, double[] budget)
            {
                return base.RiskParityNewtonMethodOptimization(numOfVar, covariance, budget);
            }
        }
    }
}