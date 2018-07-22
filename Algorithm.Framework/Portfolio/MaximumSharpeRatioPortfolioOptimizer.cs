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
    public class MaximumSharpeRatioPortfolioOptimizer : MinimumVariancePortfolioOptimizer
    {
        private double _riskFreeRate;

        public MaximumSharpeRatioPortfolioOptimizer(double lower = -1, double upper = 1, double riskFreeRate = 0.0) : base(lower, upper, 0.0)
        {
            _riskFreeRate = riskFreeRate;
        }

        public override double[] Optimize(double[,] historicalReturns, double[] expectedReturns = null)
        {
            _cov = historicalReturns.Covariance();

            // (r -r_f)^T x = R or (r -r_f)^T x >= 0
            _constraints.Add(new LinearConstraint(Size)
            {
                CombinedAs = (expectedReturns ?? historicalReturns.Mean(0)).Subtract(_riskFreeRate),
                ShouldBe = _targetReturn > 0 ? ConstraintType.EqualTo : ConstraintType.GreaterThanOrEqualTo,
                Value = _targetReturn
            });

            return Optimize();
        }
    }
}
