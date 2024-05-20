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
 *
*/

using System;
using System.Linq;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an Index algorithm with Daily data
    /// </summary>
    public class BasicTemplateIndexDailyAlgorithm : BasicTemplateIndexAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;
        protected override int StartDay => 1;

        // two complete weeks starting from the 5th. The 18th bar is not included since it is a holiday
        protected virtual int ExpectedBarCount => 2 * 5;
        protected int BarCounter = 0;

        /// <summary>
        /// Purchase a contract when we are not invested, liquidate otherwise
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // SPX Index is not tradable, but we can trade an option
                MarketOrder(SpxOption, 1);
            }
            else
            {
                Liquidate();
            }

            // Count how many slices we receive with SPX data in it to assert later
            if (slice.ContainsKey(Spx))
            {
                BarCounter++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (BarCounter != ExpectedBarCount)
            {
                throw new ArgumentException($"Bar Count {BarCounter} is not expected count of {ExpectedBarCount}");
            }

            if (Resolution != Resolution.Daily)
            {
                return;
            }

            var openInterest = Securities[SpxOption].Cache.GetAll<OpenInterest>();
            if (openInterest.Single().EndTime != new DateTime(2021, 1, 15, 23, 0, 0))
            {
                throw new ArgumentException($"Unexpected open interest time: {openInterest.Single().EndTime}");
            }

            foreach (var symbol in new[] { SpxOption, Spx })
            {
                var history = History(symbol, 10).ToList();
                if (history.Count != 10)
                {
                    throw new Exception($"Unexpected history count: {history.Count}");
                }
                if (history.Any(x => x.Time.TimeOfDay != new TimeSpan(8, 30, 0)))
                {
                    throw new Exception($"Unexpected history data start time");
                }
                if (history.Any(x => x.EndTime.TimeOfDay != new TimeSpan(15, 15, 0)))
                {
                    throw new Exception($"Unexpected history data end time");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 121;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 30;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "11"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "621.484%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1084600"},
            {"Net Profit", "8.460%"},
            {"Sharpe Ratio", "9.923"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "93.682%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "3.61"},
            {"Beta", "-0.513"},
            {"Annual Standard Deviation", "0.359"},
            {"Annual Variance", "0.129"},
            {"Information Ratio", "8.836"},
            {"Tracking Error", "0.392"},
            {"Treynor Ratio", "-6.937"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Portfolio Turnover", "2.42%"},
            {"OrderListHash", "61e8517ac3da6bed414ef23d26736fef"}
        };
    }
}
