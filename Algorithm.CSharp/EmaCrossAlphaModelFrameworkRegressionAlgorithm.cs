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
            {"Total Trades", "56"},
            {"Average Win", "0.32%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "53.636%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "2.605"},
            {"Net Profit", "3.592%"},
            {"Sharpe Ratio", "6.804"},
            {"Probabilistic Sharpe Ratio", "94.596%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "6.21"},
            {"Alpha", "0.282"},
            {"Beta", "0.395"},
            {"Annual Standard Deviation", "0.053"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "2.825"},
            {"Tracking Error", "0.057"},
            {"Treynor Ratio", "0.913"},
            {"Total Fees", "$98.36"},
            {"Estimated Strategy Capacity", "$8200000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.97%"},
            {"OrderListHash", "c15f396207f4f6e8074b29dd0e532da4"}
        };
    }
}
