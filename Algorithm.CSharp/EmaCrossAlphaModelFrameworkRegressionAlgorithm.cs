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
            {"Total Trades", "34"},
            {"Average Win", "0.41%"},
            {"Average Loss", "-0.16%"},
            {"Compounding Annual Return", "54.253%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "1.537"},
            {"Starting Equity", "100000"},
            {"Ending Equity", "103626.611"},
            {"Net Profit", "3.627%"},
            {"Sharpe Ratio", "6.744"},
            {"Sortino Ratio", "11.822"},
            {"Probabilistic Sharpe Ratio", "94.855%"},
            {"Loss Rate", "29%"},
            {"Win Rate", "71%"},
            {"Profit-Loss Ratio", "2.55"},
            {"Alpha", "0.282"},
            {"Beta", "0.393"},
            {"Annual Standard Deviation", "0.053"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "2.891"},
            {"Tracking Error", "0.057"},
            {"Treynor Ratio", "0.909"},
            {"Total Fees", "$76.37"},
            {"Estimated Strategy Capacity", "$9600000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.91%"},
            {"OrderListHash", "fb7c25741a277934243a610c837fa05b"}
        };
    }
}
