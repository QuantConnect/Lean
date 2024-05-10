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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add and trade SPX index weekly options
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="indexes" />
    public class BasicTemplateSPXWeeklyIndexOptionsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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

            // regular option SPX contracts
            var spxOptions = AddIndexOption(spx);
            spxOptions.SetFilter(u => u.Strikes(0, 1).Expiration(0, 30));

            // weekly option SPX contracts
            var spxw = AddIndexOption(spx, "SPXW");
            spxw.SetFilter(u => u.Strikes(0, 1)
                 // single week ahead since there are many SPXW contracts and we want to preserve performance
                 .Expiration(0, 7)
                 .IncludeWeeklys());

            _spxOption = spxw.Symbol;
        }

        /// <summary>
        /// Index EMA Cross trading underlying.
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested)
            {
                return;
            }

            OptionChain chain;
            if (slice.OptionChains.TryGetValue(_spxOption, out chain))
            {
                // we find at the money (ATM) put contract with closest expiration
                var atmContract = chain
                    .OrderBy(x => x.Expiry)
                    .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                    .ThenByDescending(x => x.Right)
                    .FirstOrDefault();

                if (atmContract != null)
                {
                    // if found, buy until it expires
                    MarketOrder(atmContract.Symbol, 1);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 57869;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.69%"},
            {"Compounding Annual Return", "59.804%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "-0.5"},
            {"Start Equity", "1000000"},
            {"End Equity", "1006025"},
            {"Net Profit", "0.602%"},
            {"Sharpe Ratio", "3.01"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "62.865%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.249"},
            {"Beta", "-0.033"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-99.414"},
            {"Tracking Error", "0.072"},
            {"Treynor Ratio", "-0.382"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$580000.00"},
            {"Lowest Capacity Asset", "SPXW 31K54PVWHUJHQ|SPX 31"},
            {"Portfolio Turnover", "0.48%"},
            {"OrderListHash", "07a085baedb37bb7c8d460558ea77e88"}
        };
    }
}
