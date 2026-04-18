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

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 304;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new ()
        {
            { "Total Orders", "2" },
            { "Average Win", "0%" },
            { "Average Loss", "-0.41%" },
            { "Compounding Annual Return", "-4.899%" },
            { "Drawdown", "1.100%" },
            { "Expectancy", "-1" },
            { "Net Profit", "-0.407%" },
            { "Sharpe Ratio", "-3.521"},
            { "Probabilistic Sharpe Ratio", "0.370%" },
            { "Loss Rate", "100%" },
            { "Win Rate", "0%" },
            { "Profit-Loss Ratio", "0" },
            { "Alpha", "-0.04" },
            { "Beta", "-0.012" },
            { "Annual Standard Deviation", "0.012" },
            { "Annual Variance", "0" },
            { "Information Ratio", "-4.647" },
            { "Tracking Error", "0.05" },
            { "Treynor Ratio", "3.644" },
            { "Total Fees", "$2.00" },
            { "Estimated Strategy Capacity", "$74000000.00" },
            { "Lowest Capacity Asset", "AAPL R735QTJ8XC9X" },
            { "Portfolio Turnover", "6.66%" },
            { "OrderListHash", "ab2645a4eeb3bbd6b2862df5260d86b4" }
        };
    }
}
