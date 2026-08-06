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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #9647: the algorithm holds cash in a currency other than
    /// the account currency and warms up, so the currency conversion rates are seeded at the warm-up start
    /// and drift during the warm-up replay. The starting portfolio value must be captured once warm-up
    /// brings the conversion rates up to date, which the "Start Equity" expected statistic asserts:
    /// it must match the portfolio value at the warm-up end (EURUSD ~1.20 at the start date) instead of
    /// the stale snapshot valued with the warm-up start rate (EURUSD ~1.05 a year earlier), and the
    /// algorithm must not report any net profit from the rate drift during warm-up since it never trades
    /// </summary>
    public class WarmupStartingPortfolioValueRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _warmupFinishedCalled;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetEndDate(2018, 1, 8);
            SetCash(100000);
            SetCash("EUR", 100000);

            AddForex("EURUSD", Resolution.Daily, Market.Oanda);
            SetBenchmark(time => 0m);
            SetWarmUp(TimeSpan.FromDays(365));
        }

        public override void OnWarmupFinished()
        {
            _warmupFinishedCalled = true;

            if (Portfolio.CashBook["EUR"].ConversionRate == 0)
            {
                throw new RegressionTestException("EUR conversion rate was not set once warm-up finished");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_warmupFinishedCalled)
            {
                throw new RegressionTestException($"{nameof(OnWarmupFinished)} was never called!");
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
        public long DataPoints => 321;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 5;

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
            {"Compounding Annual Return", "-13.102%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "220025.00"},
            {"End Equity", "219282"},
            {"Net Profit", "-0.338%"},
            {"Sharpe Ratio", "-3.477"},
            {"Sortino Ratio", "-12.68"},
            {"Probabilistic Sharpe Ratio", "22.877%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.035"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-2.9"},
            {"Tracking Error", "0.035"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "1"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
