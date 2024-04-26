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

using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
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
            UniverseSettings.Resolution = Resolution.Minute;
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 6);
            SetCash(100000);

            var universe = AddUniverse("my-minute-universe-name", time => new List<string> { "AAPL", "TWX" });

            AddUniverseSelection(new OptionChainedUniverseSelectionModel(universe, u => u.Strikes(-2, +2)
                                   // Expiration method accepts TimeSpan objects or integer for days.
                                   // The following statements yield the same filtering criteria
                                   .Expiration(0, 180)));
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && IsMarketOpen("AAPL") && IsMarketOpen("TWX"))
            {
                var values = slice.OptionChains.Where(x => (x.Key == "?AAPL" || x.Key == "?TWX")).Select(x => x.Value);

                foreach (var chain in values)
                {
                    // we find at the money (ATM) put contract with farthest expiration
                    var atmContract = chain
                        .OrderByDescending(x => x.Expiry)
                        .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                        .ThenByDescending(x => x.Right)
                        .FirstOrDefault();

                    if (atmContract != null)
                    {
                        // if found, trade it
                        MarketOrder(atmContract.Symbol, 1);
                        MarketOnCloseOrder(atmContract.Symbol, -1);
                    }
                }
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
        public long DataPoints => 1033404;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100051"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$110000.00"},
            {"Lowest Capacity Asset", "AAPL 2ZTXYLO9EQPZA|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "8.85%"},
            {"OrderListHash", "a542a51c6e634f2ddd9a97ce208d5a67"}
        };
    }
}
