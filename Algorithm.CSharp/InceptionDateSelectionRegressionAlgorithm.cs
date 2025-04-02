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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test universe additions and removals with open positions
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class InceptionDateSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SecurityChanges _changes = SecurityChanges.None;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 1);
            SetEndDate(2013, 10, 31);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Hour;

            // select IBM once a week, empty universe the other days
            AddUniverseSelection(new CustomUniverseSelectionModel("my-custom-universe", dt => dt.Day % 7 == 0 ? new List<string> { "IBM" } : Enumerable.Empty<string>()));
            // Adds SPY 5 days after StartDate and keep it in Universe
            AddUniverseSelection(new InceptionDateUniverseSelectionModel("spy-inception", new Dictionary<string, DateTime> {{"SPY", StartDate.AddDays(5)}}));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">TradeBars dictionary object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (_changes == SecurityChanges.None) return;

            // we'll simply go long each security we added to the universe
            foreach (var security in _changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, .5);
            }

            _changes = SecurityChanges.None;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Object containing AddedSecurities and RemovedSecurities</param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            // liquidate securities removed from our universe
            foreach (var security in changes.RemovedSecurities)
            {
                Liquidate(security.Symbol, "Removed from Universe");
            }

            _changes = changes;
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 405;

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
            {"Total Orders", "9"},
            {"Average Win", "0.11%"},
            {"Average Loss", "-0.24%"},
            {"Compounding Annual Return", "28.358%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "-0.267"},
            {"Start Equity", "100000"},
            {"End Equity", "102119.68"},
            {"Net Profit", "2.120%"},
            {"Sharpe Ratio", "3.201"},
            {"Sortino Ratio", "5.22"},
            {"Probabilistic Sharpe Ratio", "76.344%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.47"},
            {"Alpha", "0.015"},
            {"Beta", "0.478"},
            {"Annual Standard Deviation", "0.058"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-2.771"},
            {"Tracking Error", "0.063"},
            {"Treynor Ratio", "0.392"},
            {"Total Fees", "$16.73"},
            {"Estimated Strategy Capacity", "$7000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.45%"},
            {"OrderListHash", "27cdeff9728c1a42239ea1b5b2c335dc"}
        };
    }
}
