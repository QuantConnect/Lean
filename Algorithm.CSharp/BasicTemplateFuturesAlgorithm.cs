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
    /// This example demonstrates how to add futures for a given underlying asset.
    /// It also shows how you can prefilter contracts easily based on expirations, and how you
    /// can inspect the futures chain to pick a specific contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contractSymbol;

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
            SetEndDate(2013, 10, 10);
            SetCash(1000000);

            var futureSP500 = AddFuture(RootSP500);
            var futureGold = AddFuture(RootGold);

            // set our expiry filter for this futures chain
            // SetFilter method accepts TimeSpan objects or integer for days.
            // The following statements yield the same filtering criteria
            futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
            futureGold.SetFilter(0, 182);

            var benchmark = AddEquity("SPY");
            SetBenchmark(benchmark.Symbol);

            var seeder = new FuncSecuritySeeder(GetLastKnownPrices);
            SetSecurityInitializer(security => seeder.SeedSecurity(security));
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                Debug($"{Time} - SymbolChanged event: {changedEvent}");
                if (Time.TimeOfDay != TimeSpan.Zero)
                {
                    throw new RegressionTestException($"{Time} unexpected symbol changed event {changedEvent}!");
                }
            }

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

                    // if found, trade it
                    if (contract != null)
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
        }

        public override void OnEndOfAlgorithm()
        {
            // Get the margin requirements
            var buyingPowerModel = Securities[_contractSymbol].BuyingPowerModel;
            var futureMarginModel = buyingPowerModel as FutureMarginModel;
            if (buyingPowerModel == null)
            {
                throw new RegressionTestException($"Invalid buying power model. Found: {buyingPowerModel.GetType().Name}. Expected: {nameof(FutureMarginModel)}");
            }
            var initialOvernight = futureMarginModel.InitialOvernightMarginRequirement;
            var maintenanceOvernight = futureMarginModel.MaintenanceOvernightMarginRequirement;
            var initialIntraday = futureMarginModel.InitialIntradayMarginRequirement;
            var maintenanceIntraday = futureMarginModel.MaintenanceIntradayMarginRequirement;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (addedSecurity.Symbol.SecurityType == SecurityType.Future
                    && !addedSecurity.Symbol.IsCanonical()
                    && !addedSecurity.HasData)
                {
                    throw new RegressionTestException($"Future contracts did not work up as expected: {addedSecurity.Symbol}");
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
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 40308;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 350;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2700"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-99.597%"},
            {"Drawdown", "4.400%"},
            {"Expectancy", "-0.724"},
            {"Start Equity", "1000000"},
            {"End Equity", "955700.5"},
            {"Net Profit", "-4.430%"},
            {"Sharpe Ratio", "-31.63"},
            {"Sortino Ratio", "-31.63"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "83%"},
            {"Win Rate", "17%"},
            {"Profit-Loss Ratio", "0.65"},
            {"Alpha", "-3.065"},
            {"Beta", "0.128"},
            {"Annual Standard Deviation", "0.031"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-81.232"},
            {"Tracking Error", "0.212"},
            {"Treynor Ratio", "-7.677"},
            {"Total Fees", "$6237.00"},
            {"Estimated Strategy Capacity", "$14000.00"},
            {"Lowest Capacity Asset", "GC VOFJUCDY9XNH"},
            {"Portfolio Turnover", "9912.69%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "6e0f767a46a54365287801295cf7bb75"}
        };
    }
}
