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
    /// Regression algorithm to assert the behavior of <see cref="MacdAlphaModel"/>.
    /// </summary>
    public class MacdAlphaModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetAlpha(new MacdAlphaModel());
        }

        public override void OnEndOfAlgorithm()
        {
            const int expected = 4;
            if (Insights.TotalCount != expected)
            {
                throw new RegressionTestException($"The total number of insights should be {expected}. Actual: {Insights.TotalCount}");
            }
        }

        public override int AlgorithmHistoryDataPoints => 136;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "29"},
            {"Average Win", "0.36%"},
            {"Average Loss", "-0.49%"},
            {"Compounding Annual Return", "30.410%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "0.335"},
            {"Start Equity", "100000"},
            {"End Equity", "102206.31"},
            {"Net Profit", "2.206%"},
            {"Sharpe Ratio", "3.159"},
            {"Sortino Ratio", "5.45"},
            {"Probabilistic Sharpe Ratio", "75.413%"},
            {"Loss Rate", "23%"},
            {"Win Rate", "77%"},
            {"Profit-Loss Ratio", "0.73"},
            {"Alpha", "0.275"},
            {"Beta", "-0.371"},
            {"Annual Standard Deviation", "0.065"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "0.14"},
            {"Tracking Error", "0.091"},
            {"Treynor Ratio", "-0.55"},
            {"Total Fees", "$65.88"},
            {"Estimated Strategy Capacity", "$7500000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.15%"},
            {"OrderListHash", "028da9558692fed9991723beeb8eeb23"}
        };
    }
}
