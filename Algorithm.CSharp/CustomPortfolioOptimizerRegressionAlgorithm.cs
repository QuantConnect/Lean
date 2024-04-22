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

using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting we can specify a custom portfolio optimizer with a MeanVarianceOptimizationPortfolioConstructionModel
    /// </summary>
    public class CustomPortfolioOptimizerRegressionAlgorithm : MeanVarianceOptimizationFrameworkAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            base.Initialize();
            SetPortfolioConstruction(new MeanVarianceOptimizationPortfolioConstructionModel(optimizer: new CustomPortfolioOptimizer()));
        }

        private class CustomPortfolioOptimizer : IPortfolioOptimizer
        {
            public double[] Optimize(double[,] historicalReturns, double[] expectedReturns = null, double[,] covariance = null)
            {
                var result = new double[historicalReturns.GetLength(0)];
                Array.Fill(result, 0.5);
                return result;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "13"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.14%"},
            {"Compounding Annual Return", "773.203%"},
            {"Drawdown", "3.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "103012.99"},
            {"Net Profit", "3.013%"},
            {"Sharpe Ratio", "12.422"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "62.198%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.949"},
            {"Beta", "2.094"},
            {"Annual Standard Deviation", "0.49"},
            {"Annual Variance", "0.24"},
            {"Information Ratio", "14.343"},
            {"Tracking Error", "0.287"},
            {"Treynor Ratio", "2.906"},
            {"Total Fees", "$39.73"},
            {"Estimated Strategy Capacity", "$3100000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "52.21%"},
            {"OrderListHash", "a18ad75219f800ac4435bfa4f750a67d"}
        };
    }
}
