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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Framework algorithm uses <see cref="MaximumUnrealizedProfitPercentPerSecurity"/> risk management model.
    /// </summary>
    public class MaximumUnrealizedProfitPercentPerSecurityFrameworkAlgorithm : BasicTemplateFrameworkAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            SetRiskManagement(new MaximumUnrealizedProfitPercentPerSecurity(0.01m));
        }
        
        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "1.02%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "296.066%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.775%"},
            {"Sharpe Ratio", "9.373"},
            {"Probabilistic Sharpe Ratio", "68.302%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.105"},
            {"Beta", "1.021"},
            {"Annual Standard Deviation", "0.227"},
            {"Annual Variance", "0.052"},
            {"Information Ratio", "25.083"},
            {"Tracking Error", "0.006"},
            {"Treynor Ratio", "2.086"},
            {"Total Fees", "$10.33"},
            {"Estimated Strategy Capacity", "$38000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.74%"},
            {"OrderListHash", "af3a9c98c190d1b6b36fad184e796b0b"}
        };
    }
}
