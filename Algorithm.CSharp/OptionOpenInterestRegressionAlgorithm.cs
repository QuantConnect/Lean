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
    /// Options Open Interest data regression test.
    /// </summary>
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="regression test" />
    public class OptionOpenInterestRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 06);
            SetCash(1000000);

            var option = AddOption("TWX");

            option.SetFilter(-10, +10, TimeSpan.Zero, TimeSpan.FromDays(365 * 2));

            // use the underlying equity as the benchmark
            SetBenchmark("TWX");
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                foreach (var chain in slice.OptionChains)
                {
                    foreach (var contract in chain.Value)
                    {
                        if (contract.Symbol.ID.StrikePrice == 72.5m &&
                            contract.Symbol.ID.OptionRight == OptionRight.Call &&
                            contract.Symbol.ID.Date == new DateTime(2016, 01, 15))
                        {
                            var history = History<OpenInterest>(contract.Symbol, TimeSpan.FromDays(1)).ToList();
                            if (history.Count == 0)
                            {
                                throw new RegressionTestException("Regression test failed: open interest history request is empty");
                            }

                            var security = Securities[contract.Symbol];
                            var openInterestCache = security.Cache.GetData<OpenInterest>();
                            if (openInterestCache == null)
                            {
                                throw new RegressionTestException("Regression test failed: current open interest isn't in the security cache");
                            }

                            if (slice.Time.Date == new DateTime(2014, 06, 05) && (contract.OpenInterest != 50 || security.OpenInterest != 50))
                            {
                                throw new RegressionTestException("Regression test failed: current open interest was not correctly loaded and is not equal to 50");
                            }
                            if (slice.Time.Date == new DateTime(2014, 06, 06) && (contract.OpenInterest != 70 || security.OpenInterest != 70))
                            {
                                throw new RegressionTestException("Regression test failed: current open interest was not correctly loaded and is not equal to 70");
                            }
                            if (slice.Time.Date == new DateTime(2014, 06, 06))
                            {
                                MarketOrder(contract.Symbol, 1);
                                MarketOnCloseOrder(contract.Symbol, -1);
                            }
                        }
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
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 256364;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 77028;

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
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999898"},
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
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "AOL W78ZERDZK1QE|AOL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.07%"},
            {"OrderListHash", "58c3e82532109b692429e1eb062296b5"}
        };
    }
}
