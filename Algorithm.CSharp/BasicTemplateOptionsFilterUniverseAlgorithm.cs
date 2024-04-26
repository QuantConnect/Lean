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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add options for a given underlying equity security.
    /// It also shows how you can prefilter contracts easily based on strikes and expirations.
    /// It also shows how you can inspect the option chain to pick a specific option contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="filter selection" />
    public class BasicTemplateOptionsFilterUniverseAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        public Symbol OptionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);
            OptionSymbol = option.Symbol;

            // Set our custom universe filter, Expires today, is a call, and is within 10 dollars of the current price
            option.SetFilter(universe => from symbol in universe.WeeklysOnly().Expiration(0, 1)
                                         where symbol.ID.OptionRight != OptionRight.Put &&
                                              -10 < universe.Underlying.Price - symbol.ID.StrikePrice &&
                                              universe.Underlying.Price - symbol.ID.StrikePrice < 10
                                         select symbol);

            // use the underlying equity as the benchmark
            SetBenchmark(equity.Symbol);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (slice.OptionChains.TryGetValue(OptionSymbol, out chain))
                {
                    // Get the first ITM call expiring today
                    var contract = (
                        from optionContract in chain.OrderByDescending(x => x.Strike)
                        where optionContract.Expiry == Time.Date
                        where optionContract.Strike < chain.Underlying.Price
                        select optionContract
                        ).FirstOrDefault();

                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1252633;

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
            {"Average Loss", "-0.40%"},
            {"Compounding Annual Return", "-21.622%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99689"},
            {"Net Profit", "-0.311%"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "15.08%"},
            {"OrderListHash", "db6a1134ad325bce31c2bdd2e87ff5f4"}
        };
    }
}
