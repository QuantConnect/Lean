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
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm shows some of the various helper methods available when defining universes
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    public class UniverseSelectionDefinitionsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SecurityChanges _changes = SecurityChanges.None;
        private bool _onSecuritiesChangedWasCalled;

        public override void Initialize()
        {
            // subscriptions added via universe selection will have this resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 03, 28);
            SetCash(100*1000);

            // add universe for the top 3 stocks by dollar volume
            AddUniverse(Universe.Top(3));
        }

        public override void OnData(Slice slice)
        {
            if (_changes == SecurityChanges.None) return;

            // liquidate securities that fell out of our universe
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // invest in securities just added to our universe
            foreach (var security in _changes.AddedSecurities)
            {
                if (!security.Invested)
                {
                    MarketOrder(security.Symbol, 10);
                }
            }

            _changes = SecurityChanges.None;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_onSecuritiesChangedWasCalled)
            {
                throw new RegressionTestException($"OnSecuritiesChanged() method was never called!");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _onSecuritiesChangedWasCalled = true;
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
        public long DataPoints => 35413;

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
            {"Total Orders", "7"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-5.668%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99920.10"},
            {"Net Profit", "-0.080%"},
            {"Sharpe Ratio", "-12.528"},
            {"Sortino Ratio", "-11.575"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.058"},
            {"Beta", "0.042"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.968"},
            {"Tracking Error", "0.09"},
            {"Treynor Ratio", "-1.342"},
            {"Total Fees", "$7.00"},
            {"Estimated Strategy Capacity", "$3700000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "1.06%"},
            {"OrderListHash", "4b589eb854896e3516fb9ebcde6fd6c1"}
        };
    }
}
