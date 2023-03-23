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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Risk;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Show cases how to use the <see cref="TrailingStopRiskManagementModel"/>
    /// </summary>
    public class TrailingStopRiskFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)));

            SetRiskManagement(new TrailingStopRiskManagementModel(0.01m));
        }

        public override void OnEndOfAlgorithm()
        {
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 304;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "12"},
            {"Average Win", "2.58%"},
            {"Average Loss", "-0.49%"},
            {"Compounding Annual Return", "48.248%"},
            {"Drawdown", "3.300%"},
            {"Expectancy", "0.264"},
            {"Net Profit", "3.252%"},
            {"Sharpe Ratio", "3.269"},
            {"Probabilistic Sharpe Ratio", "74.233%"},
            {"Loss Rate", "80%"},
            {"Win Rate", "20%"},
            {"Profit-Loss Ratio", "5.32"},
            {"Alpha", "0.312"},
            {"Beta", "0.075"},
            {"Annual Standard Deviation", "0.1"},
            {"Annual Variance", "0.01"},
            {"Information Ratio", "1.173"},
            {"Tracking Error", "0.109"},
            {"Treynor Ratio", "4.382"},
            {"Total Fees", "$48.26"},
            {"Estimated Strategy Capacity", "$16000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "36.60%"},
            {"OrderListHash", "7d7e5f8f0f637f270b6bc0b7a102c27c"}
        };
    }
}
