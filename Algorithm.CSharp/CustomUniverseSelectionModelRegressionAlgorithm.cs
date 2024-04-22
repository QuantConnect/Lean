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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm showing how to implement a custom universe selection model and asserting it's behavior
    /// </summary>
    public class CustomUniverseSelectionModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 3, 24);
            SetEndDate(2014, 4, 7);

            UniverseSettings.Resolution = Resolution.Daily;
            SetUniverseSelection(new CustomUniverseSelectionModel());
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                foreach (var kvp in ActiveSecurities)
                {
                    SetHoldings(kvp.Key, 0.1);
                }
            }
        }

        private class CustomUniverseSelectionModel : FundamentalUniverseSelectionModel
        {
            private bool _selected;
            public CustomUniverseSelectionModel(): base()
            {
            }
            public override IEnumerable<Symbol> Select(QCAlgorithm algorithm, IEnumerable<Fundamental> fundamental)
            {
                if (!_selected)
                {
                    _selected = true;
                    return new[] { QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA) };
                }
                return Data.UniverseSelection.Universe.Unchanged;
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
        public long DataPoints => 78062;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-7.765%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99668.37"},
            {"Net Profit", "-0.332%"},
            {"Sharpe Ratio", "-5.972"},
            {"Sortino Ratio", "-7.125"},
            {"Probabilistic Sharpe Ratio", "5.408%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.055"},
            {"Beta", "0.1"},
            {"Annual Standard Deviation", "0.011"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0.413"},
            {"Tracking Error", "0.087"},
            {"Treynor Ratio", "-0.653"},
            {"Total Fees", "$2.89"},
            {"Estimated Strategy Capacity", "$1600000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.67%"},
            {"OrderListHash", "d0880701c833c9b8521d634b7e1edf4d"}
        };
    }
}
