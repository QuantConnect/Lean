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
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Futures framework algorithm that uses open interest to select the active contract.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="futures" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="filter selection" />
    public class OpenInterestFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly HashSet<DateTime> ExpectedExpiryDates = new HashSet<DateTime>
        {
            new DateTime(2013, 12, 27),
            new DateTime(2014, 02, 26)
        };

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Tick;

            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 11);
            SetCash(10000000);

            // set framework models
            SetUniverseSelection(
                new OpenInterestFutureUniverseSelectionModel(
                    this,
                    t => new[] {QuantConnect.Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX)},
                    null,
                    ExpectedExpiryDates.Count
                )
            );
        }

        public override void OnData(Slice slice)
        {
            if (Transactions.OrdersCount == 0 && slice.HasData)
            {
                var matched = slice.Keys.Where(s => !s.IsCanonical() && !ExpectedExpiryDates.Contains(s.ID.Date)).ToList();
                if (matched.Count != 0)
                {
                    throw new Exception($"{matched.Count}/{slice.Keys.Count} were unexpected expiry date(s): " + string.Join(", ", matched.Select(x => x.ID.Date)));
                }

                foreach (var symbol in slice.Keys)
                {
                    MarketOrder(symbol, 1);
                }
            }
            else if (Portfolio.Any(p => p.Value.Invested))
            {
                Liquidate();
            }
        }

        /// <summary>
        ///     This is used by the regression test system to indicate if the open source Lean repository has the required data to
        ///     run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        ///     This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = {Language.CSharp, Language.Python};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2794076;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 252;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.020%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "10000000"},
            {"End Equity", "9999980.12"},
            {"Net Profit", "0.000%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-57.739"},
            {"Tracking Error", "0.178"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$9.88"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GC VMRHKN2NLWV1"},
            {"Portfolio Turnover", "1.32%"},
            {"OrderListHash", "cc9ca77de1272050971b5438e757df61"}
        };
    }
}
