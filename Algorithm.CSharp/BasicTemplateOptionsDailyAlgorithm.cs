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
    /// This example demonstrates how to add options for a given underlying equity security.
    /// It also shows how you can prefilter contracts easily based on strikes and expirations, and how you
    /// can inspect the option chain to pick a specific option contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="filter selection" />
    public class BasicTemplateOptionsDailyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "AAPL";
        private Symbol _optionSymbol;
        private bool _optionExpired;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 15);
            SetEndDate(2016, 2, 1);
            SetCash(100000);

            var equity = AddEquity(UnderlyingTicker, Resolution.Daily);
            var option = AddOption(UnderlyingTicker, Resolution.Daily);
            _optionSymbol = option.Symbol;

            option.SetFilter(x => x.CallsOnly().Expiration(0, 60));

            // use the underlying equity as the benchmark
            SetBenchmark(equity.Symbol);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    // Grab us the contract nearest expiry that is not today
                    var contractsByExpiration = chain.Where(x => x.Expiry != Time.Date).OrderBy(x => x.Expiry);
                    var contract = contractsByExpiration.FirstOrDefault();

                    if (contract != null)
                    {
                        // if found, trade it
                        MarketOrder(contract.Symbol, 1);
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

            // Check for our expected OTM option expiry
            if (orderEvent.Message.Contains("OTM", StringComparison.InvariantCulture))
            {
                // Assert it is at midnight (5AM UTC)
                if (orderEvent.UtcTime != new DateTime(2016, 1, 16, 5, 0, 0))
                {
                    throw new ArgumentException($"Expiry event was not at the correct time, {orderEvent.UtcTime}");
                }

                _optionExpired = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Assert we had our option expire and fill a liquidation order
            if (_optionExpired != true)
            {
                throw new ArgumentException("Algorithm did not process the option expiration like expected");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 308;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.16%"},
            {"Compounding Annual Return", "-8.351%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "98844"},
            {"Net Profit", "-1.156%"},
            {"Sharpe Ratio", "-4.04"},
            {"Sortino Ratio", "-2.422"},
            {"Probabilistic Sharpe Ratio", "0.099%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.058"},
            {"Beta", "0.021"},
            {"Annual Standard Deviation", "0.017"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.49"},
            {"Tracking Error", "0.289"},
            {"Treynor Ratio", "-3.212"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$72000.00"},
            {"Lowest Capacity Asset", "AAPL W78ZEO2985GM|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "5e20fad3461ac9998afe8d76ad43b25c"}
        };
    }
}
