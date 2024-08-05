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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using <see cref="QCAlgorithm.AddUniverseSelection(IUniverseSelectionModel)"/>
    /// </summary>
    public class AddUniverseSelectionModelCoarseAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 07);
            SetCash(100000);

            // set algorithm framework models
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            SetUniverseSelection(new CoarseFundamentalUniverseSelectionModel(
                enumerable => enumerable
                    .Select(fundamental => fundamental.Symbol)
                    .Where(symbol => symbol.Value == "AAPL")));
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(
                enumerable => enumerable
                    .Select(fundamental => fundamental.Symbol)
                    .Where(symbol => symbol.Value == "SPY")));
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(
                enumerable => enumerable
                    .Select(fundamental => fundamental.Symbol)
                    .Where(symbol => symbol.Value == "FB")));
        }

        public override void OnEndOfAlgorithm()
        {
            if (UniverseManager.Count != 3)
            {
                throw new RegressionTestException("Unexpected universe count");
            }
            if (UniverseManager.ActiveSecurities.Count != 3
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "SPY")
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "AAPL")
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "FB"))
            {
                throw new RegressionTestException("Unexpected active securities");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 234015;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "21"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-77.566%"},
            {"Drawdown", "6.000%"},
            {"Expectancy", "-0.811"},
            {"Start Equity", "100000"},
            {"End Equity", "94042.73"},
            {"Net Profit", "-5.957%"},
            {"Sharpe Ratio", "-3.345"},
            {"Sortino Ratio", "-3.766"},
            {"Probabilistic Sharpe Ratio", "4.557%"},
            {"Loss Rate", "89%"},
            {"Win Rate", "11%"},
            {"Profit-Loss Ratio", "0.70"},
            {"Alpha", "-0.519"},
            {"Beta", "1.491"},
            {"Annual Standard Deviation", "0.2"},
            {"Annual Variance", "0.04"},
            {"Information Ratio", "-3.878"},
            {"Tracking Error", "0.147"},
            {"Treynor Ratio", "-0.449"},
            {"Total Fees", "$29.11"},
            {"Estimated Strategy Capacity", "$680000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "7.48%"},
            {"OrderListHash", "2c814c55e7d7c56482411c065b861b33"}
        };
    }
}
