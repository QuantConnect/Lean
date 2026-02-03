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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class DailyConsolidationExtendedMarketHoursWarningRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private TradeBar _lastBar;
        private int _mismatchCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 31);

            _spy = AddEquity("SPY", Resolution.Hour, extendedMarketHours: true).Symbol;

            // Daily consolidator that excludes extended market hours
            // Requires both the subscription and the algorithm setting to enable them
            // the subscription has ExtendedMarketHours=true, but the setting is false by default
            Consolidate(_spy, Resolution.Daily, OnNormalMarketHours); // This will show a warning

            // Daily consolidator that includes extended market hours,
            // since both the subscription and the algorithm setting are enabled
            Settings.DailyConsolidationUseExtendedMarketHours = true;
            Consolidate(_spy, Resolution.Daily, OnExtendedMarketHours);
        }

        private void OnNormalMarketHours(TradeBar dailyBar)
        {
            // Save the last consolidated bar for comparison
            _lastBar = dailyBar;
        }
        private void OnExtendedMarketHours(TradeBar dailyBar)
        {
            if (dailyBar.Open != _lastBar.Open || dailyBar.High != _lastBar.High || dailyBar.Low != _lastBar.Low || dailyBar.Close != _lastBar.Close)
            {
                // Track bar mismatches between normal and extended market hours
                _mismatchCount++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_mismatchCount == 0)
            {
                throw new RegressionTestException("Expected differences between daily consolidations with and without extended market hours.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 440;

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
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-6.224"},
            {"Tracking Error", "0.108"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}