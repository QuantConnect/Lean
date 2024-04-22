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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to define a universe using the fundamental data
    /// </summary>
    public class FundamentalRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbolsFundamental = 2;

        private SecurityChanges _changes = SecurityChanges.None;
        private Universe _universe;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 26);
            SetEndDate(2014, 04, 07);

            _universe = AddUniverse(FundamentalSelectionFunction);

            // before we add any symbol
            AssertFundamentalUniverseData();

            AddEquity("SPY");
            AddEquity("AAPL");

            // Request fundamental data for symbols at current algorithm time
            var ibm = QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var ibmFundamental = Fundamentals(ibm);
            if (Time != StartDate || Time != ibmFundamental.EndTime)
            {
                throw new Exception($"Unexpected {nameof(Fundamental)} time {ibmFundamental.EndTime}");
            }
            if (ibmFundamental.Price == 0)
            {
                throw new Exception($"Unexpected {nameof(Fundamental)} IBM price!");
            }

            var nb = QuantConnect.Symbol.Create("NB", SecurityType.Equity, Market.USA);
            var fundamentals = Fundamentals(new List<Symbol>{ nb, ibm }).ToList();
            if (fundamentals.Count != 2)
            {
                throw new Exception($"Unexpected {nameof(Fundamental)} count {fundamentals.Count}! Expected 2");
            }

            // Request historical fundamental data for symbols
            var history = History<Fundamental>(Securities.Keys, new TimeSpan(2, 0, 0, 0)).ToList();
            if(history.Count != 2)
            {
                throw new Exception($"Unexpected {nameof(Fundamental)} history count {history.Count}! Expected 2");
            }

            if (history[0].Values.Count != 2)
            {
                throw new Exception($"Unexpected {nameof(Fundamental)} data count {history[0].Values.Count}, expected 2!");
            }

            foreach (var ticker in new[] {"AAPL", "SPY"})
            {
                if (!history[0].TryGetValue(ticker, out var fundamental) || fundamental.Price == 0)
                {
                    throw new Exception($"Unexpected {ticker} fundamental data");
                }
            }
            AssertFundamentalUniverseData();
        }

        private void AssertFundamentalUniverseData()
        {
            // we run it twice just to match the history request data point count with the python version which has 1 extra different api test/assert
            for (var i = 0; i < 2; i++)
            {
                // Request historical fundamental data for all symbols, passing the universe instance
                var universeDataPerTime = History(_universe, new TimeSpan(2, 0, 0, 0)).ToList();
                if (universeDataPerTime.Count != 2)
                {
                    throw new Exception($"Unexpected {nameof(Fundamentals)} history count {universeDataPerTime.Count}! Expected 1");
                }

                foreach (var universeDataCollection in universeDataPerTime)
                {
                    AssertFundamentalEnumerator(universeDataCollection, "1");
                }
            }

            // Passing through the unvierse type and symbol
            var enumerableOfDataDictionary = History<FundamentalUniverse>(new[] { _universe.Symbol }, 100);
            foreach (var selectionCollectionForADay in enumerableOfDataDictionary)
            {
                AssertFundamentalEnumerator(selectionCollectionForADay[_universe.Symbol], "2");
            }
        }

        private void AssertFundamentalEnumerator(IEnumerable<BaseData> enumerable, string caseName)
        {
            var dataPointCount = 0;
            // note we need to cast to Fundamental type
            foreach (Fundamental fundamental in enumerable)
            {
                dataPointCount++;
            }
            if (dataPointCount < 7000)
            {
                throw new Exception($"Unexpected historical {nameof(Fundamentals)} data count {dataPointCount} case {caseName}! Expected > 7000");
            }
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbolsCoarse'
        public IEnumerable<Symbol> FundamentalSelectionFunction(IEnumerable<Fundamental> fundamental)
        {
            // select only symbols with fundamental data and sort descending by daily dollar volume
            var sortedByDollarVolume = fundamental
                .Where(x => x.Price > 1)
                .OrderByDescending(x => x.DollarVolume);

            // sort descending by P/E ratio
            var sortedByPeRatio = sortedByDollarVolume.OrderByDescending(x => x.ValuationRatios.PERatio);

            // take the top entries from our sorted collection
            var topFine = sortedByPeRatio.Take(NumberOfSymbolsFundamental);

            // we need to return only the symbol objects
            return topFine.Select(x => x.Symbol);
        }

        public override void OnData(Slice slice)
        {
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

            // we want allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.02m);
            }

            _changes = SecurityChanges.None;
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
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
        public long DataPoints => 77967;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 16;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-2.572%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99907.23"},
            {"Net Profit", "-0.093%"},
            {"Sharpe Ratio", "-4.883"},
            {"Sortino Ratio", "-6.653"},
            {"Probabilistic Sharpe Ratio", "22.758%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.014"},
            {"Beta", "0.023"},
            {"Annual Standard Deviation", "0.003"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0.597"},
            {"Tracking Error", "0.095"},
            {"Treynor Ratio", "-0.694"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$1500000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.30%"},
            {"OrderListHash", "0cf47831afc5b90519f77d5f7c4ecfa2"}
        };
    }
}
