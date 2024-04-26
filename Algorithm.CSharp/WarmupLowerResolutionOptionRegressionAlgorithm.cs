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
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting warming up with a lower resolution for speed is respected using options
    /// </summary>
    public class WarmupLowerResolutionOptionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<DateTime> _optionWarmupTimes = new();
        private const string UnderlyingTicker = "AAPL";
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 09);
            SetEndDate(2014, 06, 09);

            var option = AddOption(UnderlyingTicker);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-5, +5).Expiration(0, 180).IncludeWeeklys());
            SetWarmUp(TimeSpan.FromDays(3), Resolution.Daily);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (IsWarmingUp)
            {
                foreach (var data in slice.Values)
                {
                    var dataSpan = data.EndTime - data.Time;
                    if (dataSpan != QuantConnect.Time.OneDay)
                    {
                        throw new Exception($"Unexpected bar span! {data}: {dataSpan}");
                    }
                }
            }

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
                    if (IsWarmingUp)
                    {
                        if (atmContract.LastPrice == 0)
                        {
                            throw new Exception("Contract price is not set!");
                        }
                        _optionWarmupTimes.Add(Time);
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

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Debug($"{Time}-{changes}");
        }

        public override void OnEndOfAlgorithm()
        {
            var start = new DateTime(2014, 06, 07, 0, 0, 0);
            var end = new DateTime(2014, 06, 07, 0, 0, 0);
            var count = 0;
            do
            {
                if (_optionWarmupTimes[count] != start)
                {
                    throw new Exception($"Unexpected time {_optionWarmupTimes[count]} expected {start}");
                }
                count++;
                start = start.AddDays(1);
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
        public long DataPoints => 942365;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99908"},
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
            {"Estimated Strategy Capacity", "$5000.00"},
            {"Lowest Capacity Asset", "AAPL 2ZTXYMUME0LUU|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "1.08%"},
            {"OrderListHash", "1dfc2281fd254870f2e32528e7bb7842"}
        };
    }
}
