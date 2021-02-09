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
    public class AddUniverseSelectionModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2013, 10, 08);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // set algorithm framework models
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            AddUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)));
            AddUniverseSelection(new ManualUniverseSelectionModel(
                QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA), // duplicate will be ignored
                QuantConnect.Symbol.Create("FB", SecurityType.Equity, Market.USA)));
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-14.333%"},
            {"Drawdown", "3.300%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.169%"},
            {"Sharpe Ratio", "-0.131"},
            {"Probabilistic Sharpe Ratio", "45.057%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-3.068"},
            {"Beta", "0.595"},
            {"Annual Standard Deviation", "0.382"},
            {"Annual Variance", "0.146"},
            {"Information Ratio", "-13.618"},
            {"Tracking Error", "0.376"},
            {"Treynor Ratio", "-0.084"},
            {"Total Fees", "$13.98"},
            {"Fitness Score", "0.146"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-4.398"},
            {"Portfolio Turnover", "0.268"},
            {"Total Insights Generated", "15"},
            {"Total Insights Closed", "12"},
            {"Total Insights Analysis Completed", "12"},
            {"Long Insight Count", "15"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "8971c92ba163cec8526379865d9b9ee4"}
        };
    }
}
