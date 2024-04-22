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

using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert the behavior of <see cref="EmaCrossAlphaModel"/>.
    /// </summary>
    public class EmaCrossAlphaModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetAlpha(new EmaCrossAlphaModel());
        }

        public override void OnEndOfAlgorithm()
        {
        }

        public override int AlgorithmHistoryDataPoints => 152;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "31"},
            {"Average Win", "0.44%"},
            {"Average Loss", "-0.17%"},
            {"Compounding Annual Return", "61.576%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "1.747"},
            {"Start Equity", "100000"},
            {"End Equity", "104022.40"},
            {"Net Profit", "4.022%"},
            {"Sharpe Ratio", "7.552"},
            {"Sortino Ratio", "14.355"},
            {"Probabilistic Sharpe Ratio", "97.071%"},
            {"Loss Rate", "23%"},
            {"Win Rate", "77%"},
            {"Profit-Loss Ratio", "2.57"},
            {"Alpha", "0.323"},
            {"Beta", "0.419"},
            {"Annual Standard Deviation", "0.053"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "3.736"},
            {"Tracking Error", "0.057"},
            {"Treynor Ratio", "0.962"},
            {"Total Fees", "$73.33"},
            {"Estimated Strategy Capacity", "$9600000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.84%"},
            {"OrderListHash", "757d4ceeedcb454aa0d629eed8e8af18"}
        };
    }
}
