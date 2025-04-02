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
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm used for regression tests purposes
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class RegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SetCash(10000000);

            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Tick);
            AddSecurity(SecurityType.Equity, "BAC", Resolution.Minute);
            AddSecurity(SecurityType.Equity, "AIG", Resolution.Hour);
            AddSecurity(SecurityType.Equity, "IBM", Resolution.Daily);
        }

        private DateTime lastTradeTradeBars;
        private TimeSpan tradeEvery = TimeSpan.FromMinutes(1);
        public override void OnData(Slice slice)
        {
            if (Time - lastTradeTradeBars < tradeEvery) return;
            lastTradeTradeBars = Time;

            foreach (var kvp in slice.Bars)
            {
                var symbol = kvp.Key;
                var bar = kvp.Value;

                if (bar.IsFillForward)
                {
                    // only trade on new data
                    continue;
                }

                var holdings = Portfolio[symbol];
                if (!holdings.Invested)
                {
                    MarketOrder(symbol, 10);
                }
                else
                {
                    MarketOrder(symbol, -holdings.Quantity);
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
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 16896623;

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
            {"Total Orders", "583"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.451%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-0.964"},
            {"Start Equity", "10000000"},
            {"End Equity", "9999422.04"},
            {"Net Profit", "-0.006%"},
            {"Sharpe Ratio", "-33.13"},
            {"Sortino Ratio", "-33.13"},
            {"Probabilistic Sharpe Ratio", "0.023%"},
            {"Loss Rate", "99%"},
            {"Win Rate", "1%"},
            {"Profit-Loss Ratio", "4.14"},
            {"Alpha", "-0.01"},
            {"Beta", "-0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.922"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "95.517"},
            {"Total Fees", "$580.00"},
            {"Estimated Strategy Capacity", "$39000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.16%"},
            {"OrderListHash", "f63bd4cd4e69db4d2b3ba5cc0e3bd284"}
        };
    }
}
