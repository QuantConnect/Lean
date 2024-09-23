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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This is an option split regression algorithm
    /// </summary>
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="regression test" />
    public class OptionRenameDailyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private Symbol _contractSymbol;
        private Symbol _underlyingSymbol;

        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock rename (NWSA->FOXA), dividends, and closes adjusted position on the third day
            SetStartDate(2013, 06, 27);
            SetEndDate(2013, 07, 02);
            SetCash(1000000);

            var option = AddOption("NWSA", Resolution.Daily);
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(-1, +1, TimeSpan.Zero, TimeSpan.MaxValue);

            // use the underlying equity as the benchmark
            SetBenchmark("NWSA");
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            foreach (var dividend in slice.Dividends.Values)
            {
                if (dividend.ReferencePrice != 32.6m || dividend.Distribution != 3.82m)
                {
                    throw new RegressionTestException($"{Time} - Invalid dividend {dividend}");
                }
            }
            if (!Portfolio.Invested)
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
                        _contractSymbol = contract.Symbol;
                        Buy(_contractSymbol, 1);

                        // Buying the underlying stock
                        _underlyingSymbol = contract.Symbol.Underlying;
                        Buy(_underlyingSymbol, 100);

                        // Check
                        if (slice.Time != new DateTime(2013, 6, 27, 16, 0, 0))
                        {
                            throw new RegressionTestException($"Received first contract at {slice.Time}; Expected at 6/28/2013 12AM.");
                        }

                        if (contract.AskPrice != 1.15m)
                        {
                            throw new RegressionTestException("Current ask price was not loaded from NWSA backtest file and is not $1.1");
                        }

                        if (contract.UnderlyingSymbol.Value != "NWSA")
                        {
                            throw new RegressionTestException("Contract underlying symbol was not NWSA as expected");
                        }
                    }
                }
            }
            else if (slice.Time == new DateTime(2013, 7, 2, 16, 0, 0)) // The end
            {
                // selling positions
                Liquidate();

                // checks
                OptionChain chain;
                if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    if (chain.Underlying.Symbol.Value != "FOXA")
                    {
                        throw new RegressionTestException("Chain underlying symbol was not FOXA as expected");
                    }

                    var contract =
                        chain.OrderBy(x => x.Expiry)
                        .Where(x => x.Right == OptionRight.Call && x.Strike == 33 && x.Expiry.Date == new DateTime(2013, 08, 17))
                        .FirstOrDefault();

                    if (contract.BidPrice != 0.05m)
                    {
                        throw new RegressionTestException("Current bid price was not loaded from FOXA file and is not $0.05");
                    }
                }
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
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
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 871;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.289%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999955"},
            {"Net Profit", "-0.004%"},
            {"Sharpe Ratio", "-9.76"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "32.662%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.264"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "NWSA VJ5IKAXU7WBQ|NWSA T3MO1488O0H1"},
            {"Portfolio Turnover", "0.06%"},
            {"OrderListHash", "4dc221b1c1461ada80a8d494dd8f2610"}
        };
    }
}
