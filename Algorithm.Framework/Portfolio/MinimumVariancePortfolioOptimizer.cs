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
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Statistics;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of a minimum variance portfolio optimizer that calculate the optimal weights 
    /// with the weight range from -1 to 1 and minimize the portfolio variance with a target return of 2%
    /// </summary>
    public class MinimumVariancePortfolioOptimizer : IPortfolioOptimizer
    {
        private double _lower;
        private double _upper;
        private double _targetReturn;

        public MinimumVariancePortfolioOptimizer(double lower = -1, double upper = 1, double targetReturn = 0.02)
        {
            _lower = lower;
            _upper = upper;
            _targetReturn = targetReturn;
        }

        /// <summary>
        /// Sum of all weight is one: 1^T w = 1 / Σw = 1
        /// </summary>
        /// <param name="size">number of variables</param>
        /// <returns>linear constaraint object</returns>
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
        /// <returns>enumeration of linear constaraint objects</returns>
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

        public double[] Optimize(double[,] historicalReturns, double[] expectedReturns = null)
        {
            var cov = historicalReturns.Covariance();
            var size = cov.GetLength(0);
            var returns = expectedReturns ?? historicalReturns.Mean(0);
            var constraints = new List<LinearConstraint>();

            // w^T µ ≥ β
            constraints.Add(new LinearConstraint(size)
            {
                CombinedAs = returns,
                ShouldBe = _targetReturn > 0 ? ConstraintType.EqualTo : ConstraintType.GreaterThanOrEqualTo,
                Value = _targetReturn
            });

            // Σw = 1
            constraints.Add(GetBudgetConstraint(size));

            // lw ≤ w ≤ up
            constraints.AddRange(GetBoundaryConditions(size));

            // Setup solver
            var optfunc = new QuadraticObjectiveFunction(cov, Vector.Create(size, 0.0));
            var solver = new GoldfarbIdnani(optfunc, constraints);

            // Solve problem
            var x0 = Vector.Create(size, 1.0 / size);
            bool success = solver.Minimize(Vector.Copy(x0));
            return success ? solver.Solution : x0;
        }
    }
}
