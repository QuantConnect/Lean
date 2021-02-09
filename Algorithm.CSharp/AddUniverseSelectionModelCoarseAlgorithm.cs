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
                throw new Exception("Unexpected universe count");
            }
            if (UniverseManager.ActiveSecurities.Count != 3
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "SPY")
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "AAPL")
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "FB"))
            {
                throw new Exception("Unexpected active securities");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "23"},
            {"Average Win", "0.00%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-75.307%"},
            {"Drawdown", "5.800%"},
            {"Expectancy", "-0.859"},
            {"Net Profit", "-5.586%"},
            {"Sharpe Ratio", "-3.257"},
            {"Probabilistic Sharpe Ratio", "5.931%"},
            {"Loss Rate", "92%"},
            {"Win Rate", "8%"},
            {"Profit-Loss Ratio", "0.70"},
            {"Alpha", "-0.593"},
            {"Beta", "0.692"},
            {"Annual Standard Deviation", "0.204"},
            {"Annual Variance", "0.042"},
            {"Information Ratio", "-2.884"},
            {"Tracking Error", "0.194"},
            {"Treynor Ratio", "-0.962"},
            {"Total Fees", "$25.92"},
            {"Fitness Score", "0.004"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "-4.462"},
            {"Return Over Maximum Drawdown", "-13.032"},
            {"Portfolio Turnover", "0.083"},
            {"Total Insights Generated", "33"},
            {"Total Insights Closed", "30"},
            {"Total Insights Analysis Completed", "30"},
            {"Long Insight Count", "33"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "ce59e51c8e404b5dbbc02911473aed1c"}
        };
    }
}
