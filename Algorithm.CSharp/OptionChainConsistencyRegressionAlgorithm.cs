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
    /// This regression algorithm checks if all the option chain data coming to the algo is consistent with current securities manager state
    /// </summary>
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="filter selection" />
    public class OptionChainConsistencyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        public readonly Symbol Underlying = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        public readonly Symbol OptionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(10000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);

            // set our strike/expiry filter for this option chain
            option.SetFilter(u => u.IncludeWeeklys()
                                    .Strikes(-2, +2)
                                    .Expiration(TimeSpan.Zero, TimeSpan.FromDays(10)));

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
                if (slice.OptionChains.TryGetValue(OptionSymbol, out chain))
                {
                    // check if data is consistent
                    foreach (var o in chain)
                    {
                        if (!Securities.ContainsKey(o.Symbol))
                        {
                            // inconsistency found: option chains contains contract information that is not available in securities manager and not available for trading
                            throw new Exception("inconsistency found: option chains contains contract " +
                                $"{o.Symbol.Value} that is not available in securities manager and not available for trading"
                            );
                        }
                    }

                    // trade
                    var contract = (
                        from optionContract in chain.OrderByDescending(x => x.Strike)
                        where optionContract.Right == OptionRight.Call
                        where optionContract.Expiry == Time.Date
                        where optionContract.Strike < chain.Underlying.Price
                        select optionContract
                        ).Skip(2).FirstOrDefault();

                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                        MarketOnCloseOrder(contract.Symbol, -1);
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 470437;

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
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "10000"},
            {"End Equity", "9613"},
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
            {"Estimated Strategy Capacity", "$5000.00"},
            {"Lowest Capacity Asset", "GOOCV W6NBKPFL0ACM|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "9.93%"},
            {"OrderListHash", "8887ac32d29175b21e40f335437cee61"}
        };
    }
}
