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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

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
        protected virtual Resolution Resolution => Resolution.Daily;
        protected virtual bool ExtendedMarketHours => false;

        // S&P 500 EMini futures
        private const string RootSP500 = Futures.Indices.SP500EMini;

        // Gold futures
        private const string RootGold = Futures.Metals.Gold;
        private Future _futureSP500;
        private Future _futureGold;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2014, 10, 10);
            SetCash(1000000);

            _futureSP500 = AddFuture(RootSP500, Resolution, extendedMarketHours: ExtendedMarketHours);
            _futureGold = AddFuture(RootGold, Resolution, extendedMarketHours: ExtendedMarketHours);

            // set our expiry filter for this futures chain
            // SetFilter method accepts TimeSpan objects or integer for days.
            // The following statements yield the same filtering criteria
            _futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
            _futureGold.SetFilter(0, 182);
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

                    // if found, trade it.
                    // Also check if exchange is open for regular or extended hours. Since daily data comes at 8PM, this allows us prevent the
                    // algorithm from trading on friday when there is not after-market.
                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                    }
                }
            }
            // Same as above, check for cases like trading on a friday night.
            else if (Securities.Values.Where(x => x.Invested).All(x => x.Exchange.Hours.IsOpen(Time, true)))
            {
                Liquidate();
            }

            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                if (Time.TimeOfDay != TimeSpan.Zero)
                {
                    throw new RegressionTestException($"{Time} unexpected symbol changed event {changedEvent}!");
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
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 12478;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "32"},
            {"Average Win", "0.33%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "0.110%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0.184"},
            {"Start Equity", "1000000"},
            {"End Equity", "1001108"},
            {"Net Profit", "0.111%"},
            {"Sharpe Ratio", "-1.388"},
            {"Sortino Ratio", "-0.748"},
            {"Probabilistic Sharpe Ratio", "14.051%"},
            {"Loss Rate", "88%"},
            {"Win Rate", "12%"},
            {"Profit-Loss Ratio", "8.47"},
            {"Alpha", "-0.006"},
            {"Beta", "-0.002"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.347"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "3.781"},
            {"Total Fees", "$72.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Portfolio Turnover", "0.87%"},
            {"OrderListHash", "168731c8f3a19f230cc1410818b3b573"}
        };
    }
}
