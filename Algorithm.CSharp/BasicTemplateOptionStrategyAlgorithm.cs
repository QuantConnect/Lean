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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrate how to use Option Strategies (e.g. OptionStrategies.Straddle) helper classes to batch send orders for common strategies.
    /// It also shows how you can prefilter contracts easily based on strikes and expirations, and how you can inspect the
    /// option chain to pick a specific option contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="option strategies" />
    /// <meta name="tag" content="filter selection" />
    public class BasicTemplateOptionStrategyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(1000000);

            var option = AddOption("GOOG");
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(-2, +2, TimeSpan.Zero, TimeSpan.FromDays(180));

            // Adding this to reproduce GH issue #2314
            SetWarmup(TimeSpan.FromMinutes(1));

            // use the underlying equity as the benchmark
            SetBenchmark("GOOG");
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var atmStraddle = chain
                        .OrderBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                        .ThenByDescending(x => x.Expiry)
                        .FirstOrDefault();

                    if (atmStraddle != null)
                    {
                        Sell(OptionStrategies.Straddle(_optionSymbol, atmStraddle.Strike, atmStraddle.Expiry), 2);
                    }
                }
            }
            else
            {
                Liquidate();
            }

            foreach(var kpv in slice.Bars)
            {
                Log($"---> OnData: {Time}, {kpv.Key.Value}, {kpv.Value.Close:0.00}");
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "778"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "6.900%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-6.860%"},
            {"Sharpe Ratio", "-11.225"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-17.287"},
            {"Beta", "-26.806"},
            {"Annual Standard Deviation", "0.77"},
            {"Annual Variance", "0.593"},
            {"Information Ratio", "-10.418"},
            {"Tracking Error", "0.799"},
            {"Treynor Ratio", "0.322"},
            {"Total Fees", "$778.00"}
        };
    }
}
