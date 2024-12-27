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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #7158 where we would get future contracts which were internal
    /// </summary>
    public class FutureChainInternalSubscriptionsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            AddFuture(Futures.Indices.SP500EMini).SetFilter(0, 45);
            AddFuture(Futures.Metals.Gold).SetFilter(0, 45);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            var trade = !Portfolio.Invested;
            foreach (var chain in slice.FutureChains)
            {
                if (trade)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contractToTrade = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        select futuresContract
                    ).FirstOrDefault();

                    // if found, trade it
                    if (contractToTrade != null)
                    {
                        MarketOrder(contractToTrade.Symbol, 1);
                    }
                }

                foreach (var contract in chain.Value)
                {
                    var subscriptions = SubscriptionManager.Subscriptions.Where(x => x.Symbol == contract.Symbol).ToList();
                    if (subscriptions.Count == 0)
                    {
                        throw new RegressionTestException($"Failed to find valid subscription for {contract.Symbol} at {Time}");
                    }

                    var openInterest = Securities[contract.Symbol].OpenInterest;
                    if(openInterest == 0)
                    {
                        throw new RegressionTestException($"Open interest is 0 for {contract.Symbol} at {Time}");
                    }
                }
            }
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
        public long DataPoints => 19043;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-98.880%"},
            {"Drawdown", "4.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "96375.06"},
            {"Net Profit", "-3.625%"},
            {"Sharpe Ratio", "-16.733"},
            {"Sortino Ratio", "-16.733"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "2.959"},
            {"Beta", "-0.244"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-56.943"},
            {"Tracking Error", "0.302"},
            {"Treynor Ratio", "4.061"},
            {"Total Fees", "$2.47"},
            {"Estimated Strategy Capacity", "$2200000.00"},
            {"Lowest Capacity Asset", "GC VL5E74HP3EE5"},
            {"Portfolio Turnover", "44.33%"},
            {"OrderListHash", "6d4d3664d887d00b8222eb731f298cd8"}
        };
    }
}
