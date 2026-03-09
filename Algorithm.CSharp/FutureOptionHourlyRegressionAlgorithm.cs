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
using QuantConnect.Data;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests using FutureOptions hourly resolution
    /// </summary>
    public class FutureOptionHourlyRegressionAlgorithm : FutureOptionDailyRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;

        protected override void ScheduleBuySell()
        {
            // Schedule a purchase of this contract at Noon
            Schedule.On(DateRules.Today, TimeRules.Noon, () =>
            {
                Ticket = MarketOrder(ESOption, 1);
            });

            // Schedule liquidation at 2PM when the market is open
            Schedule.On(DateRules.Today, TimeRules.At(17,0,0), () =>
            {
                Liquidate();
            });
        }

        public override void OnData(Slice slice)
        {
            // Assert we are only getting data only hourly intervals
            if (slice.Time.Minute != 0)
            {
                throw new ArgumentException($"Expected data only on hourly intervals; instead was {slice.Time}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 55;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99672.16"},
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
            {"Total Fees", "$2.84"},
            {"Estimated Strategy Capacity", "$3000.00"},
            {"Lowest Capacity Asset", "ES XCZJLCEYO5XG|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "4.90%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "10661c6d84f71ca7e07e2fdf5b79851b"}
        };
    }
}

