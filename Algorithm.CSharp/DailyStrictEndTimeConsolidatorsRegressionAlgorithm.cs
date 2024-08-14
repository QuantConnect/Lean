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
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting behavior of consolidators while using daily strict end time
    /// </summary>
    public class DailyStrictEndTimeConsolidatorsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _consolidatorsDataResolutionCount;
        private int _consolidatorsDataTimeSpanCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            AddEquity("SPY", Resolution.Minute);
            AddEquity("AAPL", Resolution.Daily, fillForward: false);

            Consolidate("AAPL", Resolution.Daily, AssertResolutionBasedDailyBars);
            Consolidate("SPY", Resolution.Daily, AssertResolutionBasedDailyBars);

            Consolidate("AAPL", QuantConnect.Time.OneDay, AssertTimeSpanBasedDailyBars);
            Consolidate("SPY", QuantConnect.Time.OneDay, AssertTimeSpanBasedDailyBars);
        }

        protected virtual void AssertResolutionBasedDailyBars(TradeBar bar)
        {
            Debug($"AssertResolutionBasedDailyBars({Time}): {bar}");
            _consolidatorsDataResolutionCount++;
            AssertDailyBar(bar);
        }

        protected virtual void AssertTimeSpanBasedDailyBars(TradeBar bar)
        {
            Debug($"AssertTimeSpanBasedDailyBars({Time}): {bar}");
            _consolidatorsDataTimeSpanCount++;
            if (bar.Symbol == "AAPL")
            {
                // underlying is daily, passes through, it will be daily strict end times, even if created as a timespan
                AssertDailyBar(bar);
            }
            else
            {
                if (bar.EndTime.Hour != 0 || bar.Period != QuantConnect.Time.OneDay)
                {
                    throw new RegressionTestException($"{Time}: Unexpected daily time span based bar span {bar.EndTime}!");
                }
            }
        }

        private void AssertDailyBar(TradeBar bar)
        {
            if (Settings.DailyPreciseEndTime)
            {
                if (bar.EndTime.Hour != 16 || bar.Period != TimeSpan.FromHours(6.5))
                {
                    throw new RegressionTestException($"{Time}: Unexpected daily resolution based bar span {bar.EndTime}!");
                }
            }
            else
            {
                if (bar.EndTime.Hour != 0 || bar.Period != QuantConnect.Time.OneDay)
                {
                    throw new RegressionTestException($"{Time}: Unexpected daily resolution based bar span {bar.EndTime}!");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_consolidatorsDataTimeSpanCount != 9)
            {
                throw new RegressionTestException($"Unexpected consolidator time span data count {_consolidatorsDataTimeSpanCount}!");
            }
            if (_consolidatorsDataResolutionCount != (9 + (Settings.DailyPreciseEndTime ? 1 : 0)))
            {
                throw new RegressionTestException($"Unexpected consolidator resolution data count {_consolidatorsDataResolutionCount}!");
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
        public long DataPoints => 3948;

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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
