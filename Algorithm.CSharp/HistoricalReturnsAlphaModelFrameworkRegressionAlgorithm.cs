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
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert the behavior of <see cref="HistoricalReturnsAlphaModel"/>.
    /// </summary>
    public class HistoricalReturnsAlphaModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetAlpha(new HistoricalReturnsAlphaModel());
        }

        public override void OnEndOfAlgorithm()
        {
            const int expected = 74;
            if (Insights.TotalCount != expected)
            {
                throw new Exception($"The total number of insights should be {expected}. Actual: {Insights.TotalCount}");
            }
        }

        public override long DataPoints => 779;

        public override int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "58"},
            {"Average Win", "0.19%"},
            {"Average Loss", "-0.20%"},
            {"Compounding Annual Return", "37.277%"},
            {"Drawdown", "1.300%"},
            {"Expectancy", "0.269"},
            {"Start Equity", "100000"},
            {"End Equity", "102638.26"},
            {"Net Profit", "2.638%"},
            {"Sharpe Ratio", "4.183"},
            {"Sortino Ratio", "6.011"},
            {"Probabilistic Sharpe Ratio", "83.574%"},
            {"Loss Rate", "36%"},
            {"Win Rate", "64%"},
            {"Profit-Loss Ratio", "0.97"},
            {"Alpha", "0.253"},
            {"Beta", "-0.024"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "0.745"},
            {"Tracking Error", "0.077"},
            {"Treynor Ratio", "-10.434"},
            {"Total Fees", "$251.44"},
            {"Estimated Strategy Capacity", "$11000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.78%"},
            {"OrderListHash", "b7ff5378caf82356666be8307dc4e6e0"}
        };
    }
}
