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

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using <see cref="QCAlgorithm.AddRiskManagement(IRiskManagementModel)"/>
    /// </summary>
    public class AddRiskManagementAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            AddRiskManagement(new MaximumDrawdownPercentPortfolio(0.02m));
            AddRiskManagement(new MaximumUnrealizedProfitPercentPerSecurity(0.01m));
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "1.02%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "289.119%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.752%"},
            {"Sharpe Ratio", "9.235"},
            {"Probabilistic Sharpe Ratio", "68.013%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.105"},
            {"Beta", "1.022"},
            {"Annual Standard Deviation", "0.224"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "24.59"},
            {"Tracking Error", "0.006"},
            {"Treynor Ratio", "2.029"},
            {"Total Fees", "$9.77"},
            {"Fitness Score", "0.747"},
            {"Kelly Criterion Estimate", "38.64"},
            {"Kelly Criterion Probability Value", "0.229"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "107.109"},
            {"Portfolio Turnover", "0.747"},
            {"Total Insights Generated", "100"},
            {"Total Insights Closed", "99"},
            {"Total Insights Analysis Completed", "99"},
            {"Long Insight Count", "100"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$126657.6305"},
            {"Total Accumulated Estimated Alpha Value", "$20405.9516"},
            {"Mean Population Estimated Insight Value", "$206.1207"},
            {"Mean Population Direction", "54.5455%"},
            {"Mean Population Magnitude", "54.5455%"},
            {"Rolling Averaged Population Direction", "59.8056%"},
            {"Rolling Averaged Population Magnitude", "59.8056%"},
            {"OrderListHash", "0b8cbbafdb77bae2f7abe3cf5e05ac5c"}
        };
    }
}
