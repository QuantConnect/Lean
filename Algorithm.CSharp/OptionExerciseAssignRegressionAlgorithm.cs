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
    /// This regression algorithm tests option exercise and assignment functionality
    /// We open two positions and go with them into expiration. We expect to see our long position exercised and short position assigned.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    public class OptionExerciseAssignRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        private readonly Symbol _optionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);
        private bool _assignedOption = false;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);

            // set our strike/expiry filter for this option chain
            option.SetFilter(u => u.IncludeWeeklys()
                                   .Strikes(-2, +2)
                                   .Expiration(TimeSpan.Zero, TimeSpan.FromDays(10)));

            // use the underlying equity as the benchmark
            SetBenchmark(equity.Symbol);
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_assignedOption)
            {
                throw new RegressionTestException("In the end, short ITM option position was not assigned.");
            }
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
                    // find the second call strike under market price expiring today
                    var contracts = (
                        from optionContract in chain.OrderByDescending(x => x.Strike)
                        where optionContract.Right == OptionRight.Call
                        where optionContract.Expiry == Time.Date
                        where optionContract.Strike < chain.Underlying.Price
                        select optionContract
                        ).Take(2);

                    if (contracts.Any())
                    {
                        MarketOrder(contracts.FirstOrDefault().Symbol, 1);
                        MarketOrder(contracts.Skip(1).FirstOrDefault().Symbol, -1);
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
        public override void OnAssignmentOrderEvent(OrderEvent assignmentEvent)
        {
            Log(assignmentEvent.ToString());
            _assignedOption = true;
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
        public long DataPoints => 26483;

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
            {"Average Win", "0.30%"},
            {"Average Loss", "-0.32%"},
            {"Compounding Annual Return", "-22.695%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99648"},
            {"Net Profit", "-0.352%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0.92"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "30.10%"},
            {"OrderListHash", "c32a840b8e572bce151e319354df0723"}
        };
    }
}
