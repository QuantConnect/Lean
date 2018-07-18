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

namespace QuantConnect.Algorithm.Framework.Portfolio.Optimization
{
    public enum ConstraintType { Equal = 0, Less = -1, More = 1 };

    /// <summary>
    /// Interface for portfolio optimization algorithms
    /// </summary>
    public interface IPortfolioOptimization
    {
        /// <summary>
        /// Provide a covariance matrix to an optimization algorithm
        /// </summary>
        /// <param name="cov">Covariance matrix</param>
        void SetCovariance(double[,] cov);

        /// <summary>
        /// Perform portfolio optimization for a provided expected returns
        /// </summary>
        /// <param name="W">Portfolio weights</param>
        /// <param name="expectedReturns">Vector of expected returns</param>
        /// <returns>Error code</returns>
        int Optimize(out double[] W, double[] expectedReturns);
    }
}
