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
    /// Regression algorithm to assert the behavior of <see cref="MaximumUnrealizedProfitPercentPerSecurity"/> Risk Management Model
    /// </summary>
    public class MaximumUnrealizedProfitPercentPerSecurityFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)));
            
            SetRiskManagement(new MaximumUnrealizedProfitPercentPerSecurity(0.004m));
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
            {"Average Win", "0.73%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "9.396%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100732.58"},
            {"Net Profit", "0.733%"},
            {"Sharpe Ratio", "2.696"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "90.684%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.054"},
            {"Beta", "0.021"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.56"},
            {"Tracking Error", "0.052"},
            {"Treynor Ratio", "2.791"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$46000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "6.62%"},
            {"OrderListHash", "e954f20bd08ee776fa710a325715354e"}
        };
    }
}
