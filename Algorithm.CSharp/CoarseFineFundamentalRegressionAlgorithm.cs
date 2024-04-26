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
        public override void OnData(Slice data)
        {
            // verify we don't receive data for inactive securities
            var inactiveSymbols = data.Keys
                .Where(sym => !UniverseManager.ActiveSecurities.ContainsKey(sym))
                // on daily data we'll get the last data point and the delisting at the same time
                .Where(sym => !data.Delistings.ContainsKey(sym) || data.Delistings[sym].Type != DelistingType.Delisted)
                .ToList();
            if (inactiveSymbols.Any())
            {
                var symbols = string.Join(", ", inactiveSymbols);
                throw new Exception($"Received data for non-active security: {symbols}.");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 7244;

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
            {"Average Win", "1.16%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "32.505%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "50000"},
            {"End Equity", "50581.67"},
            {"Net Profit", "1.163%"},
            {"Sharpe Ratio", "2.666"},
            {"Sortino Ratio", "19.179"},
            {"Probabilistic Sharpe Ratio", "64.748%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.272"},
            {"Beta", "0.436"},
            {"Annual Standard Deviation", "0.086"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "3.572"},
            {"Tracking Error", "0.092"},
            {"Treynor Ratio", "0.523"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$49000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "6.64%"},
            {"OrderListHash", "69614ad86028ebc190bff5fb42795a3f"}
        };
    }
}
