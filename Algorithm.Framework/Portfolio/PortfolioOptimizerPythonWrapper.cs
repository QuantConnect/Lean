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
 *
*/

using Python.Runtime;
using QuantConnect.Python;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Python wrapper for custom portfolio optimizer
    /// </summary>
    public class PortfolioOptimizerPythonWrapper : BasePythonWrapper<IPortfolioOptimizer>, IPortfolioOptimizer
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="portfolioOptimizer">The python model to wrapp</param>
        public PortfolioOptimizerPythonWrapper(PyObject portfolioOptimizer)
            : base(portfolioOptimizer)
        {
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
            return InvokeMethod<double[]>(nameof(Optimize), historicalReturns, expectedReturns, covariance);
        }
    }
}
