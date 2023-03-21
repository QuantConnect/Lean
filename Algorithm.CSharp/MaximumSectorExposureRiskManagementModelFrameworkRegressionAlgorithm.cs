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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Show example of how to use the <see cref="MaximumSectorExposureRiskManagementModel"/> Risk Management Model
    /// </summary>
    public class MaximumSectorExposureRiskManagementModelFrameworkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 2, 1);  //Set Start Date
            SetEndDate(2014, 5, 1);    //Set End Date
            SetCash(100000);           //Set Strategy Cash

            // set algorithm framework models
            var tickers = new string[] { "AAPL", "MSFT", "GOOG", "AIG", "BAC" };
            SetUniverseSelection(new FineFundamentalUniverseSelectionModel(
                coarse => coarse.Where(x => tickers.Contains(x.Symbol.Value)).Select(x => x.Symbol),
                fine => fine.Select(x => x.Symbol)
            ));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, System.TimeSpan.FromMinutes(20)));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            // define risk management model such that maximum weight of a single sector be 50%
            SetRiskManagement(new MaximumSectorExposureRiskManagementModel(0.5m));
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
        public long DataPoints => 544;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "44"},
            {"Average Win", "0.02%"},
            {"Average Loss", "-0.16%"},
            {"Compounding Annual Return", "-12.023%"},
            {"Drawdown", "4.500%"},
            {"Expectancy", "-0.855"},
            {"Net Profit", "-3.108%"},
            {"Sharpe Ratio", "-1.445"},
            {"Probabilistic Sharpe Ratio", "4.073%"},
            {"Loss Rate", "87%"},
            {"Win Rate", "13%"},
            {"Profit-Loss Ratio", "0.11"},
            {"Alpha", "-0.125"},
            {"Beta", "0.215"},
            {"Annual Standard Deviation", "0.058"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-2.908"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "-0.39"},
            {"Total Fees", "$78.26"},
            {"Estimated Strategy Capacity", "$35000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "3.25%"},
            {"OrderListHash", "c414e8a498776b8d43eccba0b88e9061"}
        };
    }
}
