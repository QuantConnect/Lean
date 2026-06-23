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
using System.Linq;
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Statistics;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of a portfolio optimizer that maximizes the portfolio Sharpe Ratio.
    /// The interval of weights in optimization method can be changed based on the long-short algorithm.
    /// The default model uses flat risk free rate and weight for an individual security range from -1 to 1.
    /// </summary>
    public class MaximumSharpeRatioPortfolioOptimizer : IPortfolioOptimizer
    {
        private double _lower;
        private double _upper;
        private double _riskFreeRate;

        /// <summary>
        /// Initialize a new instance of <see cref="MaximumSharpeRatioPortfolioOptimizer"/>
        /// </summary>
        /// <param name="lower">Lower constraint</param>
        /// <param name="upper">Upper constraint</param>
        /// <param name="riskFreeRate"></param>
        public MaximumSharpeRatioPortfolioOptimizer(double lower = -1, double upper = 1, double riskFreeRate = 0.0)
        {
            _lower = lower;
            _upper = upper;
            _riskFreeRate = riskFreeRate;
        }

        /// <summary>
        /// Sum of all weight is one: 1^T w = 1 / Σw = 1
        /// </summary>
        /// <param name="size">number of variables</param>
        /// <returns>linear constraint object</returns>
        protected LinearConstraint GetBudgetConstraint(int size)
        {
            return new LinearConstraint(size)
            {
                CombinedAs = Vector.Create(size, 1.0),
                ShouldBe = ConstraintType.EqualTo,
                Value = 1.0
            };
        }

        /// <summary>
        /// Boundary constraints on weights: lw ≤ w ≤ up
        /// </summary>
        /// <param name="size">number of variables</param>
        /// <returns>enumeration of linear constraint objects</returns>
        protected IEnumerable<LinearConstraint> GetBoundaryConditions(int size)
        {
            for (int i = 0; i < size; i++)
            {
                yield return new LinearConstraint(1)
                {
                    VariablesAtIndices = new int[] { i },
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = _lower
                };
                yield return new LinearConstraint(1)
                {
                    VariablesAtIndices = new int[] { i },
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = _upper
                };
            }
        }

        /// <summary>
        /// Perform portfolio optimization for a provided matrix of historical returns and an array of expected returns
        /// </summary>
        /// <param name="historicalReturns">Matrix of annualized historical returns where each column represents a security and each row returns for the given date/time (size: K x N).</param>
        /// <param name="expectedReturns">Array of double with the portfolio annualized expected returns (size: K x 1).</param>
        /// <param name="covariance">Multi-dimensional array of double with the portfolio covariance of annualized returns (size: K x K).</param>
        /// <returns>Array of double with the portfolio weights (size: K x 1)</returns>
        public double[] Optimize(double[,] historicalReturns, double[] expectedReturns = null, double[,] covariance = null)
        {
            covariance = covariance ?? historicalReturns.Covariance();
            var returns = (expectedReturns ?? historicalReturns.Mean(0)).Subtract(_riskFreeRate);

            var size = covariance.GetLength(0);
            var x0 = Vector.Create(size, 1.0 / size);
            var variableCount = size + 1;
            var kappaIndex = size;

            var constraints = new List<LinearConstraint>
            {
                // Sharpe maximization under quadratic constraints via Charnes-Cooper:
                // y = w * kappa, kappa > 0, (µ - r_f)^T y = 1.
                // https://quant.stackexchange.com/questions/18521/sharpe-maximization-under-quadratic-constraints
                new LinearConstraint(variableCount)
                {
                    CombinedAs = returns.Concat(new[] { 0.0 }).ToArray(),
                    ShouldBe = ConstraintType.EqualTo,
                    Value = 1.0
                },
                // Σy = kappa
                new LinearConstraint(variableCount)
                {
                    CombinedAs = Enumerable.Range(0, variableCount)
                        .Select(i => i == kappaIndex ? -1.0 : 1.0)
                        .ToArray(),
                    ShouldBe = ConstraintType.EqualTo,
                    Value = 0.0
                }
            };

            for (var i = 0; i < size; i++)
            {
                // y_i >= lower * kappa
                constraints.Add(new LinearConstraint(variableCount)
                {
                    CombinedAs = Enumerable.Range(0, variableCount)
                        .Select(j => j == i ? 1.0 : j == kappaIndex ? -_lower : 0.0)
                        .ToArray(),
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = 0.0
                });
                // y_i <= upper * kappa
                constraints.Add(new LinearConstraint(variableCount)
                {
                    CombinedAs = Enumerable.Range(0, variableCount)
                        .Select(j => j == i ? 1.0 : j == kappaIndex ? -_upper : 0.0)
                        .ToArray(),
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = 0.0
                });
            }

            constraints.Add(new LinearConstraint(1)
            {
                VariablesAtIndices = new[] { kappaIndex },
                ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                Value = 0.0
            });

            // Setup solver
            var objective = new double[variableCount, variableCount];
            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    objective[row, column] = covariance[row, column];
                }
            }
            var optfunc = new QuadraticObjectiveFunction(objective, Vector.Create(variableCount, 0.0));
            var solver = new GoldfarbIdnani(optfunc, constraints);

            // Solve problem
            var y0 = x0.Concat(new[] { 1.0 }).ToArray();
            var success = solver.Minimize(Vector.Copy(y0));
            if (!success || solver.Solution[kappaIndex].IsNaNOrInfinity() || solver.Solution[kappaIndex] <= 0)
            {
                return x0;
            }

            var solution = solver.Solution.Take(size).ToArray().Divide(solver.Solution[kappaIndex]);
            return solution.Any(x => x.IsNaNOrInfinity()) ? x0 : solution;
        }
    }
}
