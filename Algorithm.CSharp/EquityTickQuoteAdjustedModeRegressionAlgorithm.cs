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
    /// Checks that the Tick BidPrice and AskPrices are adjusted like Value.
    /// </summary>
    public class EquityTickQuoteAdjustedModeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _ibm;
        private bool _bought;
        private bool _sold;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _ibm = AddEquity("IBM", Resolution.Tick).Symbol;
        }

        public override void OnData(Slice data)
        {
            if (!data.Ticks.ContainsKey(_ibm))
            {
                return;
            }

            var security = Securities[_ibm];
            if (!security.HasData)
            {
                return;
            }

            foreach (var tick in data.Ticks[_ibm])
            {
                if (tick.BidPrice != 0 && !_bought && ((tick.Value - tick.BidPrice) <= 0.05m))
                {
                    SetHoldings(_ibm, 1);
                    _bought = true;
                    return;
                }
                if (tick.AskPrice != 0 && _bought && !_sold && Math.Abs((double)tick.Value - (double)tick.AskPrice) <= 0.05)
                {
                    Liquidate(_ibm);
                    _sold = true;
                    return;
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 694806;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.12%"},
            {"Compounding Annual Return", "-9.135%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99877.60"},
            {"Net Profit", "-0.122%"},
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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$7.34"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "39.89%"},
            {"OrderListHash", "32f56b0f4e9300ef1a34464e8083c7e7"}
        };
    }
}
