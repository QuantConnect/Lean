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

        public override void OnData(Slice data)
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
                throw new Exception($"OnSecuritiesChanged() method was never called!");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 35417;

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
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-3.587%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-0.439"},
            {"Start Equity", "100000"},
            {"End Equity", "99949.98"},
            {"Net Profit", "-0.050%"},
            {"Sharpe Ratio", "-17.344"},
            {"Sortino Ratio", "-17.344"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.12"},
            {"Alpha", "-0.039"},
            {"Beta", "0.017"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.747"},
            {"Tracking Error", "0.092"},
            {"Treynor Ratio", "-2.271"},
            {"Total Fees", "$7.00"},
            {"Estimated Strategy Capacity", "$1200000000.00"},
            {"Lowest Capacity Asset", "QQQ RIWIV7K5Z9LX"},
            {"Portfolio Turnover", "1.06%"},
            {"OrderListHash", "b5cba1a9956c6c86d56aa603f71326a1"}
        };
    }
}
