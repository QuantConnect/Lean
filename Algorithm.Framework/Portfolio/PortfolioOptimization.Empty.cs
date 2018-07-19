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

namespace QuantConnect.Algorithm.Framework.Portfolio.Optimization
{
    public class NoSolverException : SystemException
    {
        public NoSolverException() : base("No solver avaible for protfolio optimization method. Please refere to Vendor\\README.md for more information.") {}
    }

    public class MeanVariancePortfolio : IPortfolioOptimization
    {
        public MeanVariancePortfolio(double lower, double upper, double targetReturn = 0.0) {}

        public double[] Optimize(double[] expectedReturns)
        {
            throw new NoSolverException();
        }

        public void SetCovariance(double[,] cov)
        {
            throw new NoSolverException();
        }
    }

    public class MaxSharpeRatioPortfolio : IPortfolioOptimization
    {
        public MaxSharpeRatioPortfolio(double lower, double upper, double riskFreeRate = 0.0) {}

        public double[] Optimize(double[] expectedReturns)
        {
            throw new NoSolverException();
        }

        public void SetCovariance(double[,] cov)
        {
            throw new NoSolverException();
        }
    }
}
