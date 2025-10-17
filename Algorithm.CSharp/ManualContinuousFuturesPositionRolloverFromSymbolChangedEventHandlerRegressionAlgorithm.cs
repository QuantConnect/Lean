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

using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the new symbol, on a security changed event,
    /// is added to the securities collection and is tradable.
    /// This specific algorithm tests the manual rollover with the symbol changed event
    /// that is received in the <see cref="OnSymbolChangedEvents(SymbolChangedEvents)"/> handler.
    /// </summary>
    public class ManualContinuousFuturesPositionRolloverFromSymbolChangedEventHandlerRegressionAlgorithm
        : ManualContinuousFuturesPositionRolloverRegressionAlgorithm
    {
        public override void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged)
        {
            if (!Portfolio.Invested)
            {
                return;
            }

            ManualPositionsRollover(symbolsChanged);
        }

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 885;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "7.01%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "15.724%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "107628.9"},
            {"Net Profit", "7.629%"},
            {"Sharpe Ratio", "1.716"},
            {"Sortino Ratio", "1.088"},
            {"Probabilistic Sharpe Ratio", "89.188%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.08"},
            {"Beta", "0.095"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-1.238"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "1.06"},
            {"Total Fees", "$6.45"},
            {"Estimated Strategy Capacity", "$2900000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "1.37%"},
            {"Drawdown Recovery", "16"},
            {"OrderListHash", "3c6520fb6ffd031c1632680d6f7c3f75"}
        };
    }
}
