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
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    public class OptionSplitRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
            SetStartDate(2014, 06, 06);
            SetEndDate(2014, 06, 09);
            SetCash(1000000);

            var option = AddOption("AAPL");
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(u => u.IncludeWeeklys()
                       .Strikes(-2, +2)
                       .Expiration(TimeSpan.Zero, TimeSpan.FromDays(365 * 2)));

            // use the underlying equity as the benchmark
            SetBenchmark("AAPL");
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                if (Time.Hour > 9 && Time.Minute > 0)
                {
                    OptionChain chain;
                    if (slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                    {
                        var contract =
                            chain.OrderBy(x => x.Expiry)
                            .Where(x => x.Right == OptionRight.Call && x.Strike == 650)
                            .Skip(1)
                            .FirstOrDefault();

                        if (contract != null)
                        {
                            Buy(contract.Symbol, 1);
                        }
                    }
                }
            }
            else
            {
                if (Time.Day > 6 && Time.Hour > 14 && Time.Minute > 0)
                {
                    Liquidate();
                }
            }

            if (Portfolio.Invested)
            {
                var holdings = Portfolio.Securities.Where(x => x.Value.Holdings.AbsoluteQuantity != 0).First().Value.Holdings.AbsoluteQuantity;

                if (Time.Day == 6 && holdings != 1)
                {
                    throw new RegressionTestException($"Expected position quantity of 1 but was {holdings.ToStringInvariant()}");
                }
                if (Time.Day == 9 && holdings != 7)
                {
                    throw new RegressionTestException($"Expected position quantity of 7 but was {holdings.ToStringInvariant()}");
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
        public long DataPoints => 124202;

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
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-1.512%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "999833"},
            {"Net Profit", "-0.017%"},
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
            {"Information Ratio", "-19.236"},
            {"Tracking Error", "0.147"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$88000.00"},
            {"Lowest Capacity Asset", "AAPL VRCWOCTRR37Q|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.04%"},
            {"OrderListHash", "75e0d3e5d72502421287925c55de3054"}
        };
    }
}
