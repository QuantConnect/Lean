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

using QuantConnect.Algorithm.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test the OptionChainedUniverseSelectionModel class
    /// </summary>
    public class OptionChainedUniverseSelectionModelRegressionAlgorithm: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;
            SetStartDate(2014, 03, 22);
            SetEndDate(2014, 04, 07);
            SetCash(100000);

            AddSecurity(SecurityType.Equity, "GOOG", Resolution.Daily);

            var universe = AddUniverse(coarse =>
            {
                // select the various google symbols over the period
                return from c in coarse
                       let sym = c.Symbol.Value
                       where sym == "GOOG" || sym == "GOOCV" || sym == "GOOAV"
                       select c.Symbol;

                // Before March 28th 2014:
                // - Only GOOG  T1AZ164W5VTX existed
                // On March 28th 2014
                // - GOOAV VP83T1ZUHROL and GOOCV VP83T1ZUHROL are listed
                // On April 02nd 2014
                // - GOOAV VP83T1ZUHROL is delisted
                // - GOOG  T1AZ164W5VTX becomes GOOGL
                // - GOOCV VP83T1ZUHROL becomes GOOG
            });

            AddUniverseSelection(new OptionChainedUniverseSelectionModel(universe,
                option_filter_universe => option_filter_universe));
        }

        public override void OnEndOfAlgorithm()
        {
            if (!UniverseManager.ContainsKey("?GOOCV"))
            {
                throw new Exception("Option chain {?GOOCV} should have been in the universe but it was not");
            }

            if (!UniverseManager.ContainsKey("?GOOG"))
            {
                throw new Exception("Option chain {?GOOG} should have been in the universe but it was not");
            }

            if (!UniverseManager.ContainsKey("?GOOAV"))
            {
                throw new Exception("Option chain {?GOOAV} should have been in the universe but it was not");
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
        public long DataPoints => 78080;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.552"},
            {"Tracking Error", "0.092"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
