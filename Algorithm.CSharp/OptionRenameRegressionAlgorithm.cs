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
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This is an option split regression algorithm
    /// </summary>
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="regression test" />
    public class OptionRenameRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock rename (NWSA->FOXA), dividends, and closes adjusted position on the third day
            SetStartDate(2013, 06, 28);
            SetEndDate(2013, 07, 02);
            SetCash(1000000);

            var option = AddOption("TFCFA");
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(-1, +1, TimeSpan.Zero, TimeSpan.MaxValue);

            // use the underlying equity as the benchmark
            SetBenchmark("TFCFA");
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            foreach (var dividend in slice.Dividends.Values)
            {
                if (dividend.ReferencePrice != 32.59m || dividend.Distribution != 3.82m)
                {
                    throw new Exception($"{Time} - Invalid dividend {dividend}");
                }
            }
            if (!Portfolio.Invested)
            {
                if (Time.Day == 28 && Time.Hour > 9 && Time.Minute > 0)
                {
                    OptionChain chain;
                    if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                    {
                        var contract =
                            chain.OrderBy(x => x.Expiry)
                            .Where(x => x.Right == OptionRight.Call && x.Strike == 33 && x.Expiry.Date == new DateTime(2013, 08, 17))
                            .FirstOrDefault();

                        if (contract != null)
                        {
                            // Buying option
                            Buy(contract.Symbol, 1);

                            // Buying the underlying stock
                            var underlyingSymbol = contract.Symbol.Underlying;
                            Buy(underlyingSymbol, 100);

                            // checks
                            if (contract.AskPrice != 1.1m)
                            {
                                throw new Exception("Regression test failed: current ask price was not loaded from NWSA backtest file and is not $1.1");
                            }
                        }
                    }
                }
            }
            else
            {
                if (Time.Day == 2 && Time.Hour > 14 && Time.Minute > 0)
                {
                    // selling positions
                    Liquidate();

                    // checks
                    OptionChain chain;
                    if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                    {
                        var contract =
                            chain.OrderBy(x => x.Expiry)
                            .Where(x => x.Right == OptionRight.Call && x.Strike == 33 && x.Expiry.Date == new DateTime(2013, 08, 17))
                            .FirstOrDefault();

                        if (contract.BidPrice != 0.05m)
                        {
                            throw new Exception("Regression test failed: current bid price was not loaded from FOXA file and is not $0.05");
                        }
                    }
                }
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-0.492%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.006%"},
            {"Sharpe Ratio", "-3.943"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.943"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$4.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-2.808"},
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
            {"OrderListHash", "bc96f0b98a0865ad88b6b995a26bfd9b"}
        };
    }
}
