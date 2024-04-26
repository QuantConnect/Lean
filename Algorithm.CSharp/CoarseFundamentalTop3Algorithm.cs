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

using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algorithm we demonstrate how to use the coarse fundamental data to
    /// define a universe as the top dollar volume
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    /// <meta name="tag" content="regression test" />
    public class CoarseFundamentalTop3Algorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbols = 3;

        // initialize our changes to nothing
        private SecurityChanges _changes = SecurityChanges.None;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 07);
            SetCash(50000);

            // this add universe method accepts a single parameter that is a function that
            // accepts an IEnumerable<CoarseFundamental> and returns IEnumerable<Symbol>
            AddUniverse(CoarseSelectionFunction);
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        public static IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            // sort descending by daily dollar volume
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);

            // take the top entries from our sorted collection
            var top = sortedByDollarVolume.Take(NumberOfSymbols);

            // we need to return only the symbol objects
            return top.Select(x => x.Symbol);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            Log($"OnData({UtcTime:o}): Keys: {string.Join(", ", data.Keys.OrderBy(x => x))}");

            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None) return;

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // we want 1/N allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 1m / NumberOfSymbols);
            }

            _changes = SecurityChanges.None;
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
            Log($"OnSecuritiesChanged({UtcTime:o}):: {changes}");
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            Log($"OnOrderEvent({UtcTime:o}):: {fill}");
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
        public long DataPoints => 78091;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "12"},
            {"Average Win", "0.55%"},
            {"Average Loss", "-0.26%"},
            {"Compounding Annual Return", "16.717%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "0.850"},
            {"Start Equity", "50000"},
            {"End Equity", "50318.65"},
            {"Net Profit", "0.637%"},
            {"Sharpe Ratio", "1.024"},
            {"Sortino Ratio", "2.169"},
            {"Probabilistic Sharpe Ratio", "50.223%"},
            {"Loss Rate", "40%"},
            {"Win Rate", "60%"},
            {"Profit-Loss Ratio", "2.08"},
            {"Alpha", "0.196"},
            {"Beta", "0.741"},
            {"Annual Standard Deviation", "0.118"},
            {"Annual Variance", "0.014"},
            {"Information Ratio", "2.294"},
            {"Tracking Error", "0.097"},
            {"Treynor Ratio", "0.163"},
            {"Total Fees", "$27.94"},
            {"Estimated Strategy Capacity", "$200000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "26.69%"},
            {"OrderListHash", "82ca991c660ecefbcbdf0b4cc90ddd67"}
        };
    }
}
