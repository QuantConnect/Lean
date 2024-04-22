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
    /// Regression algorithm to assert the behavior of <see cref="MaximumDrawdownPercentPortfolio"/> Risk Management Model
    /// </summary>
    public class MaximumDrawdownPercentPortfolioFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)));
            
            // define risk management model as a composite of several risk management models
            SetRiskManagement(new CompositeRiskManagementModel(
                new MaximumDrawdownPercentPortfolio(0.01m), // Avoid loss of initial capital
                new MaximumDrawdownPercentPortfolio(0.015m, true) // Avoid profit losses
            ));
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
            {"Total Orders", "2"},
            {"Average Win", "2.43%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "34.465%"},
            {"Drawdown", "2.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "102436.11"},
            {"Net Profit", "2.436%"},
            {"Sharpe Ratio", "2.474"},
            {"Sortino Ratio", "2.224"},
            {"Probabilistic Sharpe Ratio", "66.764%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.124"},
            {"Beta", "0.558"},
            {"Annual Standard Deviation", "0.093"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "0.429"},
            {"Tracking Error", "0.092"},
            {"Treynor Ratio", "0.413"},
            {"Total Fees", "$6.56"},
            {"Estimated Strategy Capacity", "$57000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "6.63%"},
            {"OrderListHash", "63a7d75043456cdd8c518f77df8f024f"}
        };
    }
}
