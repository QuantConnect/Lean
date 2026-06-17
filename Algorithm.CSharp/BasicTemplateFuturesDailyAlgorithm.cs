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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
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

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.RemovedSecurities.Count > 0 &&
                Portfolio.Invested &&
                Securities.Values.Where(x => x.Invested).All(x => x.Exchange.Hours.IsOpen(Time, true)))
            {
                Liquidate();
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
        public virtual long DataPoints => 5874;

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
            {"Total Orders", "22"},
            {"Average Win", "0.61%"},
            {"Average Loss", "-0.49%"},
            {"Compounding Annual Return", "0.117%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0.124"},
            {"Start Equity", "1000000"},
            {"End Equity", "1001178.23"},
            {"Net Profit", "0.118%"},
            {"Sharpe Ratio", "-1.834"},
            {"Sortino Ratio", "-0.52"},
            {"Probabilistic Sharpe Ratio", "15.902%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.25"},
            {"Alpha", "-0.007"},
            {"Beta", "0.002"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.352"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "-4.239"},
            {"Total Fees", "$6.77"},
            {"Estimated Strategy Capacity", "$290000000.00"},
            {"Lowest Capacity Asset", "GC VOFJUCDY9XNH"},
            {"Portfolio Turnover", "0.12%"},
            {"Drawdown Recovery", "69"},
            {"OrderListHash", "064f8f56389ceecb333b5a4bb79622c1"}
        };
    }
}
