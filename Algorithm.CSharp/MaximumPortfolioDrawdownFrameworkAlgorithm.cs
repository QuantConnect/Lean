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
    /// Show example of how to use the <see cref="MaximumDrawdownPercentPortfolio"/> Risk Management Model
    /// </summary>
    public class MaximumPortfolioDrawdownFrameworkAlgorithm : BaseRiskManagementModelFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();

            // define risk management model as a composite of several risk management models
            SetRiskManagement(new CompositeRiskManagementModel(
                new MaximumDrawdownPercentPortfolio(0.01m), // Avoid loss of initial capital
                new MaximumDrawdownPercentPortfolio(0.015m, true) // Avoid profit losses
            ));
        }
        public override Dictionary<string, string> ExpectedStatistics => new ()
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.02%"},
            {"Compounding Annual Return", "79.043%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "-1"},
            {"Net Profit", "0.747%"},
            {"Sharpe Ratio", "4.054"},
            {"Probabilistic Sharpe Ratio", "58.417%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.694"},
            {"Beta", "0.67"},
            {"Annual Standard Deviation", "0.157"},
            {"Annual Variance", "0.025"},
            {"Information Ratio", "-15.375"},
            {"Tracking Error", "0.088"},
            {"Treynor Ratio", "0.948"},
            {"Total Fees", "$10.29"},
            {"Estimated Strategy Capacity", "$53000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.56%"},
            {"OrderListHash", "6fcdb25204c75944b194eb5ec9f39c88"}
        };
    }
}
