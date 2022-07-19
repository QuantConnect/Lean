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
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add futures with daily resolution.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesDailyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contractSymbol;
        protected virtual Resolution Resolution => Resolution.Daily;

        // S&P 500 EMini futures
        private const string RootSP500 = Futures.Indices.SP500EMini;

        // Gold futures
        private const string RootGold = Futures.Metals.Gold;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2014, 10, 10);
            SetCash(1000000);

            var futureSP500 = AddFuture(RootSP500, Resolution);
            var futureGold = AddFuture(RootGold, Resolution);

            // set our expiry filter for this futures chain
            // SetFilter method accepts TimeSpan objects or integer for days.
            // The following statements yield the same filtering criteria
            futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
            futureGold.SetFilter(0, 182);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                foreach(var chain in slice.FutureChains)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contract = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        where futuresContract.Expiry > Time.Date.AddDays(90)
                        select futuresContract
                    ).FirstOrDefault();

                    // if found and exchange is open, trade it. Exchange could be closed, for example for a bar after 6:00pm on a friday, when futures
                    // markets are closed.
                    if (contract != null && Securities[contract.Symbol].Exchange.ExchangeOpen)
                    {
                        _contractSymbol = contract.Symbol;
                        MarketOrder(_contractSymbol, 1);
                    }
                }
            }
            else
            {
                Liquidate();
            }

            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                if (Time.TimeOfDay != TimeSpan.Zero)
                {
                    throw new Exception($"{Time} unexpected symbol changed event {changedEvent}!");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 13559;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "152"},
            {"Average Win", "0.09%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.638%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "-0.871"},
            {"Net Profit", "-0.643%"},
            {"Sharpe Ratio", "-2.323"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "99%"},
            {"Win Rate", "1%"},
            {"Profit-Loss Ratio", "8.83"},
            {"Alpha", "-0.004"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.408"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "3.612"},
            {"Total Fees", "$281.20"},
            {"Estimated Strategy Capacity", "$1000.00"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Fitness Score", "0.013"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-1.45"},
            {"Return Over Maximum Drawdown", "-0.992"},
            {"Portfolio Turnover", "0.04"},
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
            {"OrderListHash", "48bfc4d255420cb589e00cf582554e0a"}
        };
    }
}
