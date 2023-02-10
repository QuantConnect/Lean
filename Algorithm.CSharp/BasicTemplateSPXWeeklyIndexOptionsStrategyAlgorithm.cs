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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add and trade SPX index weekly option strategy
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="indexes" />
    public class BasicTemplateSPXWeeklyIndexOptionsStrategyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spxOption;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 10);
            SetCash(1000000);

            var spx = AddIndex("SPX").Symbol;

            // weekly option SPX contracts
            var spxw = AddIndexOption(spx, "SPXW");
            spxw.SetFilter(u => u.Strikes(0, 1)
                 // single week ahead since there are many SPXW contracts and we want to preserve performance
                 .Expiration(0, 7)
                 .IncludeWeeklys());

            _spxOption = spxw.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested)
            {
                return;
            }

            OptionChain chain;
            if (slice.OptionChains.TryGetValue(_spxOption, out chain))
            {
                // we find the first expiration group of call options and order them in ascending strike 
                var contracts = chain
                    .Where(x => x.Right == OptionRight.Call)
                    .OrderBy(x => x.Expiry)
                    .GroupBy(x => x.Expiry)
                    .First()
                    .OrderBy(x => x.Strike)
                    .ToList();

                if (contracts.Count > 1)
                {
                    var smallerStrike = contracts[0];
                    var higherStrike = contracts[1];

                    // if found, buy until it expires
                    var optionStrategy = OptionStrategies.BearCallSpread(_spxOption, smallerStrike.Strike, higherStrike.Strike, smallerStrike.Expiry);
                    Buy(optionStrategy, 1);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(orderEvent.ToString());
            if (orderEvent.Symbol.ID.Symbol != "SPXW")
            {
                throw new Exception("Unexpected order event symbol!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 35091;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0.46%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "41.434%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "115.281"},
            {"Net Profit", "0.445%"},
            {"Sharpe Ratio", "7.984"},
            {"Probabilistic Sharpe Ratio", "95.977%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "231.56"},
            {"Alpha", "0.046"},
            {"Beta", "-0.005"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-102.372"},
            {"Tracking Error", "0.07"},
            {"Treynor Ratio", "-2.467"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$4100000.00"},
            {"Lowest Capacity Asset", "SPXW XKX6S2GM9PGU|SPX 31"},
            {"Fitness Score", "0.001"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.001"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "3571ecc4b2274878146f15167fcc683c"}
        };
    }
}
