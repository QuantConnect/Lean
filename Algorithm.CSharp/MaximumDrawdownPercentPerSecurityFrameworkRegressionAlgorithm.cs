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
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert the behavior of <see cref="MaximumDrawdownPercentPerSecurity"/> Risk Management Model
    /// </summary>
    public class MaximumDrawdownPercentPerSecurityFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)));

            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.004m));
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 304;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.42%"},
            {"Compounding Annual Return", "-2.230%"},
            {"Drawdown", "5.500%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.183%"},
            {"Sharpe Ratio", "-0.095"},
            {"Probabilistic Sharpe Ratio", "35.758%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.096"},
            {"Beta", "0.433"},
            {"Annual Standard Deviation", "0.108"},
            {"Annual Variance", "0.012"},
            {"Information Ratio", "-1.908"},
            {"Tracking Error", "0.109"},
            {"Treynor Ratio", "-0.024"},
            {"Total Fees", "$70.17"},
            {"Estimated Strategy Capacity", "$21000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "56.50%"},
            {"OrderListHash", "db535bc20e28cd3ab4913b44a80cf924"}
        };
    }
}
