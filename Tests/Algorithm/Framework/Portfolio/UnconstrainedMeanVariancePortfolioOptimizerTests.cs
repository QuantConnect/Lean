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
using System.Collections.Generic;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class UnconstrainedMeanVariancePortfolioOptimizerTests : PortfolioOptimizerTestsBase
    {

        [OneTimeSetUp]
        public void Setup()
        {
            HistoricalReturns = new List<double[,]>
            {
                new double[,] { { 0.76, -0.06, 1.22, 0.17 }, { 0.02, 0.28, 1.25, -0.00 }, { -0.50, -0.13, -0.50, -0.03 }, { 0.81, 0.31, 2.39, 0.26 }, { -0.02, 0.02, 0.06, 0.01 } },
                new double[,] { { -0.15, 0.67, 0.45 }, { -0.44, -0.10, 0.07 }, { 0.04, -0.41, 0.01 }, { 0.01, 0.03, 0.02 } },
                new double[,] { { -0.02, 0.65, 1.25 }, { -0.29, -0.39, -0.50 }, { 0.29, 0.58, 2.39 }, { 0.00, -0.01, 0.06 } },
                new double[,] { { 0.76, 0.25, 0.21 }, { 0.02, -0.15, 0.45 }, { -0.50, -0.44, 0.07 }, { 0.81, 0.04, 0.01 }, { -0.02, 0.01, 0.02 } }
            };

           ExpectedReturns = new List<double[]>
            {
                new double[] { 0.21, 0.08, 0.88, 0.08 }, 
                new double[] { -0.13, 0.05, 0.14 },
                null,
                null
            };

            Covariances = new List<double[,]>
            {
                new double[,] { { 0.31, 0.05, 0.55, 0.07 }, { 0.05, 0.04, 0.18, 0.01 }, { 0.55, 0.18, 1.28, 0.12 }, { 0.07, 0.01, 0.12, 0.02 } },
                new double[,] { { 0.05, -0.02, -0.01 }, { -0.02, 0.21, 0.09 }, { -0.01, 0.09, 0.04 } },
                new double[,] { { 0.06, 0.09, 0.28 }, { 0.09, 0.25, 0.58 }, { 0.28, 0.58, 1.66 } },
                null
            };

            ExpectedResults = new List<double[]>
            {
                new double[] { -13.288136, -23.322034, 8.79661, 9.389831 },
                new double[] { -0.142857, -35.285714, 82.857143 },
                new double[] { -13.232262, -3.709534, 4.009978 },
                new double[] { 4.621852, -9.651736, 5.098332 },
            };
        }
        
        protected override IPortfolioOptimizer CreateOptimizer()
        {
            return new UnconstrainedMeanVariancePortfolioOptimizer();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public override void OptimizeWeightings(int testCaseNumber)
        {
            base.OptimizeWeightings(testCaseNumber);
        }
    }
}
