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

        //Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        public void OnData(TradeBars data)
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "11"},
            {"Average Win", "0.51%"},
            {"Average Loss", "-0.33%"},
            {"Compounding Annual Return", "-31.082%"},
            {"Drawdown", "2.700%"},
            {"Expectancy", "0.263"},
            {"Net Profit", "-1.518%"},
            {"Sharpe Ratio", "-2.118"},
            {"Probabilistic Sharpe Ratio", "23.259%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.53"},
            {"Alpha", "-0.208"},
            {"Beta", "0.415"},
            {"Annual Standard Deviation", "0.119"},
            {"Annual Variance", "0.014"},
            {"Information Ratio", "-1.167"},
            {"Tracking Error", "0.126"},
            {"Treynor Ratio", "-0.607"},
            {"Total Fees", "$11.63"},
            {"Fitness Score", "0.013"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-5.1"},
            {"Return Over Maximum Drawdown", "-11.717"},
            {"Portfolio Turnover", "0.282"},
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
            {"OrderListHash", "3d1ae61492b34c39115b76757510c058"}
        };
    }
}
