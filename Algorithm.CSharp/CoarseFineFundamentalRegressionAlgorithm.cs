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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to define a universe
    /// as a combination of use the coarse fundamental data and fine fundamental data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    /// <meta name="tag" content="regression test" />
    public class CoarseFineFundamentalRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbolsFine = 2;

        // initialize our changes to nothing
        private SecurityChanges _changes = SecurityChanges.None;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 07);
            SetCash(50000);

            // this add universe method accepts two parameters:
            // - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
            // - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
            AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
        }

        // return a list of three fixed symbol objects
        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            if (Time.Date < new DateTime(2014, 4, 1))
            {
                return new List<Symbol>
                {
                    QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                    QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA)
                };
            }

            return new List<Symbol>
            {
                QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
        }

        // sort the data by market capitalization and take the top 'NumberOfSymbolsFine'
        public IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            // sort descending by market capitalization
            var sortedByMarketCap = fine.OrderByDescending(x => x.MarketCap);

            // take the top entries from our sorted collection
            var topFine = sortedByMarketCap.Take(NumberOfSymbolsFine);

            // we need to return only the symbol objects
            return topFine.Select(x => x.Symbol);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            // verify we don't receive data for inactive securities
            var inactiveSymbols = slice.Keys
                .Where(sym => !UniverseManager.ActiveSecurities.ContainsKey(sym))
                // on daily data we'll get the last data point and the delisting at the same time
                .Where(sym => !slice.Delistings.ContainsKey(sym) || slice.Delistings[sym].Type != DelistingType.Delisted)
                .ToList();
            if (inactiveSymbols.Any())
            {
                var symbols = string.Join(", ", inactiveSymbols);
                throw new RegressionTestException($"Received data for non-active security: {symbols}.");
            }

            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None) return;

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                    Debug("Liquidated Stock: " + security.Symbol.Value);
                }
            }

            // we want 50% allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                if (security.Fundamentals.EarningRatios.EquityPerShareGrowth.OneYear > 0.25)
                {
                    SetHoldings(security.Symbol, 0.5m);
                    Debug("Purchased Stock: " + security.Symbol.Value);
                }
            }

            _changes = SecurityChanges.None;
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;

            if (changes.AddedSecurities.Count > 0)
            {
                Debug("Securities added: " + string.Join(",", changes.AddedSecurities.Select(x => x.Symbol.Value)));
            }
            if (changes.RemovedSecurities.Count > 0)
            {
                Debug("Securities removed: " + string.Join(",", changes.RemovedSecurities.Select(x => x.Symbol.Value)));
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
        public long DataPoints => 7245;

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
            {"Average Win", "1.39%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "40.025%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "50000"},
            {"End Equity", "50696.56"},
            {"Net Profit", "1.393%"},
            {"Sharpe Ratio", "3.192"},
            {"Sortino Ratio", "4.952"},
            {"Probabilistic Sharpe Ratio", "68.664%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.328"},
            {"Beta", "0.474"},
            {"Annual Standard Deviation", "0.088"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "4.219"},
            {"Tracking Error", "0.09"},
            {"Treynor Ratio", "0.59"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$81000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "6.65%"},
            {"OrderListHash", "4eaacdd341a5be0d04cb32647d931471"}
        };
    }
}
