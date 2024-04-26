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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Show cases how to use the <see cref="CompositeRiskManagementModel"/> to define
    /// </summary>
    public class CompositeRiskManagementModelFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // set algorithm framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, System.TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            // define risk management model as a composite of several risk management models
            SetRiskManagement(new CompositeRiskManagementModel(
                new MaximumUnrealizedProfitPercentPerSecurity(0.01m),
                new MaximumDrawdownPercentPerSecurity(0.01m)
            ));
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "7"},
            {"Average Win", "1.05%"},
            {"Average Loss", "-1.01%"},
            {"Compounding Annual Return", "227.385%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0.361"},
            {"Start Equity", "100000"},
            {"End Equity", "101527.86"},
            {"Net Profit", "1.528%"},
            {"Sharpe Ratio", "7.572"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "65.639%"},
            {"Loss Rate", "33%"},
            {"Win Rate", "67%"},
            {"Profit-Loss Ratio", "1.04"},
            {"Alpha", "-0.288"},
            {"Beta", "0.994"},
            {"Annual Standard Deviation", "0.221"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-46.455"},
            {"Tracking Error", "0.006"},
            {"Treynor Ratio", "1.686"},
            {"Total Fees", "$24.08"},
            {"Estimated Strategy Capacity", "$23000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "139.03%"},
            {"OrderListHash", "fa7c51aaf284cdc29cb4c0ac8ebd5356"}
        };
    }
}
