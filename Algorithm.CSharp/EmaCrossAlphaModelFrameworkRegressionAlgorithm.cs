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
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "37"},
            {"Average Win", "0.39%"},
            {"Average Loss", "-0.17%"},
            {"Compounding Annual Return", "68.362%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "1.648"},
            {"Start Equity", "100000"},
            {"End Equity", "104374.76"},
            {"Net Profit", "4.375%"},
            {"Sharpe Ratio", "8.179"},
            {"Sortino Ratio", "16.068"},
            {"Probabilistic Sharpe Ratio", "97.880%"},
            {"Loss Rate", "20%"},
            {"Win Rate", "80%"},
            {"Profit-Loss Ratio", "2.31"},
            {"Alpha", "0.368"},
            {"Beta", "0.405"},
            {"Annual Standard Deviation", "0.054"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "4.356"},
            {"Tracking Error", "0.058"},
            {"Treynor Ratio", "1.101"},
            {"Total Fees", "$79.34"},
            {"Estimated Strategy Capacity", "$9600000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "17.26%"},
            {"Drawdown Recovery", "6"},
            {"OrderListHash", "49a74c348d955700aa5b230f596ed85b"}
        };
    }
}
