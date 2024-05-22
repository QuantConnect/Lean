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
        public override void OnData(Slice data)
        {
            if (Time - lastTradeTradeBars < tradeEvery) return;
            lastTradeTradeBars = Time;

            foreach (var kvp in data.Bars)
            {
                var symbol = kvp.Key;
                var bar = kvp.Value;

                if (bar.Time.RoundDown(bar.Period) != bar.Time)
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 16896627;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1593"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-1.158%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-0.989"},
            {"Start Equity", "10000000"},
            {"End Equity", "9998403.92"},
            {"Net Profit", "-0.016%"},
            {"Sharpe Ratio", "-18.339"},
            {"Sortino Ratio", "-18.339"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "1.81"},
            {"Alpha", "-0.015"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.943"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "27.123"},
            {"Total Fees", "$1589.00"},
            {"Estimated Strategy Capacity", "$67000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "1.87%"},
            {"OrderListHash", "2efcb91611db79040f62f0f5c36280c9"}
        };
    }
}
