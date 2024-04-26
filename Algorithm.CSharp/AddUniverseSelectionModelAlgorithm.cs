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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 53;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-14.233%"},
            {"Drawdown", "3.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99831.88"},
            {"Net Profit", "-0.168%"},
            {"Sharpe Ratio", "62.464"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.117"},
            {"Beta", "1.19"},
            {"Annual Standard Deviation", "0.213"},
            {"Annual Variance", "0.046"},
            {"Information Ratio", "70.778"},
            {"Tracking Error", "0.043"},
            {"Treynor Ratio", "11.2"},
            {"Total Fees", "$22.21"},
            {"Estimated Strategy Capacity", "$340000000.00"},
            {"Lowest Capacity Asset", "FB V6OIPNZEM8V9"},
            {"Portfolio Turnover", "26.92%"},
            {"OrderListHash", "3832790c7dd9d50805b6206129b01110"}
        };
    }
}
