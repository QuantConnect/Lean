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
            const int expected = 78;
            if (Insights.TotalCount != expected)
            {
                throw new RegressionTestException($"The total number of insights should be {expected}. Actual: {Insights.TotalCount}");
            }
        }

        public override long DataPoints => 779;

        public override int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "69"},
            {"Average Win", "0.18%"},
            {"Average Loss", "-0.15%"},
            {"Compounding Annual Return", "42.429%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "0.367"},
            {"Start Equity", "100000"},
            {"End Equity", "102949.54"},
            {"Net Profit", "2.950%"},
            {"Sharpe Ratio", "5.164"},
            {"Sortino Ratio", "8.556"},
            {"Probabilistic Sharpe Ratio", "90.449%"},
            {"Loss Rate", "38%"},
            {"Win Rate", "62%"},
            {"Profit-Loss Ratio", "1.22"},
            {"Alpha", "0.306"},
            {"Beta", "-0.129"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "1.181"},
            {"Tracking Error", "0.077"},
            {"Treynor Ratio", "-2.186"},
            {"Total Fees", "$267.37"},
            {"Estimated Strategy Capacity", "$6000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "65.87%"},
            {"OrderListHash", "d527d869fde7539958e06050ee7d9951"}
        };
    }
}
