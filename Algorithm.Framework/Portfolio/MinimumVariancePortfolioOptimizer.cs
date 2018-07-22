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
        protected double _lower;
        protected double _upper;
        protected double _targetReturn;
        protected double[,] _cov;
        protected List<LinearConstraint> _constraints;

        protected int Size => _cov == null ? 0 : _cov.GetLength(0);

        public MinimumVariancePortfolioOptimizer(double lower = -1, double upper = 1, double targetReturn = 0.02)
        {
            _lower = lower;
            _upper = upper;
            _targetReturn = targetReturn;
            _cov = null;
            _constraints = new List<LinearConstraint>();
        }

        protected double[] Optimize()
        {
            // sum(x) = 1
            _constraints.Add(new LinearConstraint(Size)
            {
                CombinedAs = Vector.Create(Size, 1.0),
                ShouldBe = ConstraintType.EqualTo,
                Value = 1.0
            });

            // lw <= x <= up
            for (int i = 0; i < Size; i++)
            {
                _constraints.Add(new LinearConstraint(1)
                {
                    VariablesAtIndices = new int[] { i },
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = _lower
                });
                _constraints.Add(new LinearConstraint(1)
                {
                    VariablesAtIndices = new int[] { i },
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = _upper
                });
            }

            var optfunc = new QuadraticObjectiveFunction(_cov, Vector.Create(Size, 0.0));
            var solver = new GoldfarbIdnani(optfunc, _constraints);
            _constraints.Clear();

            var init = Vector.Create(Size, 1.0 / Size);
            bool success = solver.Minimize(init);
            return success ? solver.Solution : init;
        }

        public virtual double[] Optimize(double[,] historicalReturns, double[] expectedReturns = null)
        {
            _cov = historicalReturns.Covariance();
            var returns = expectedReturns ?? historicalReturns.Mean(0);

            // mu^T x = R  or mu^T x >= 0
            _constraints.Add(new LinearConstraint(Size)
            {
                CombinedAs = returns,
                ShouldBe = _targetReturn > 0 ? ConstraintType.EqualTo : ConstraintType.GreaterThanOrEqualTo,
                Value = _targetReturn
            });

            return Optimize();
        }

    }
}
