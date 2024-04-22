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
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of option warmup
    /// </summary>
    public class WarmupOptionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        private Symbol _optionSymbol;

        protected List<DateTime> OptionWarmupTimes { get; } = new();

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var option = AddOption(UnderlyingTicker);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-5, +5).Expiration(0, 180).IncludeWeeklys());
            SetWarmUp(TimeSpan.FromDays(1));
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                // we find at the money (ATM) put contract with farthest expiration
                var atmContract = chain
                    .OrderByDescending(x => x.Expiry)
                    .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                    .ThenByDescending(x => x.Right)
                    .FirstOrDefault();

                if (atmContract != null)
                {
                    // during warmup, using daily resolution (with the same TZ as the algorithm) the last bar.EndTime of warmup
                    // overlaps with the algorithm start time, considered not to be in warmup anymore.
                    // This bar would also be emitted by lean if no warmup was set and daily resolution used, see 'BasicTemplateDailyAlgorithm'
                    if (Time <= StartDate)
                    {
                        if(atmContract.LastPrice == 0)
                        {
                            throw new Exception("Contract price is not set!");
                        }
                        OptionWarmupTimes.Add(Time);
                    }
                    else if (!Portfolio.Invested && IsMarketOpen(_optionSymbol))
                    {
                        // if found, trade it
                        MarketOrder(atmContract.Symbol, 1);
                        MarketOnCloseOrder(atmContract.Symbol, -1);
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var start = new DateTime(2015, 12, 23, 9, 31, 0);
            var end = new DateTime(2015, 12, 23, 16, 0, 0);
            var count = 0;
            do
            {
                if (OptionWarmupTimes[count] != start)
                {
                    throw new Exception($"Unexpected time {OptionWarmupTimes[count]} expected {start}");
                }
                count++;
                start = start.AddMinutes(1);
            }
            while (start < end);
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
        public virtual long DataPoints => 609294;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99718"},
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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$1300000.00"},
            {"Lowest Capacity Asset", "GOOCV 30AKMEIPOSS1Y|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "10.71%"},
            {"OrderListHash", "8a36462ee0349c04d01d464e592dd347"}
        };
    }
}
