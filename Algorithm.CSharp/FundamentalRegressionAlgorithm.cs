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
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
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

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 25);
            SetEndDate(2014, 04, 07);

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
            var history = History<Fundamental>(Securities.Keys, new TimeSpan(1, 0, 0, 0)).ToList();
            if(history.Count != 1)
            {
                throw new Exception($"Unexpected {nameof(Fundamental)} history count {history.Count}! Expected 1");
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

            // Request historical fundamental data for all symbols
            var history2 = History<Fundamentals>(new TimeSpan(1, 0, 0, 0)).ToList();
            if (history2.Count != 1)
            {
                throw new Exception($"Unexpected {nameof(Fundamentals)} history count {history.Count}! Expected 1");
            }
            if (history2[0].Single().Value.Data.Count < 7000)
            {
                throw new Exception($"Unexpected {nameof(Fundamentals)} data count {history.Count}! Expected > 7000");
            }
            if (history2[0].Single().Value.Data.Any(x => x.GetType() != typeof(Fundamental)))
            {
                throw new Exception($"Unexpected {nameof(Fundamentals)} data type!");
            }

            AddUniverse(FundamentalSelectionFunction);
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbolsCoarse'
        public IEnumerable<Symbol> FundamentalSelectionFunction(IEnumerable<Fundamental> fundamental)
        {
            // select only symbols with fundamental data and sort descending by daily dollar volume
            var sortedByDollarVolume = fundamental
                .Where(x => x.Price > 1 && x.HasFundamentalData)
                .OrderByDescending(x => x.DollarVolume);

            // sort descending by P/E ratio
            var sortedByPeRatio = sortedByDollarVolume.OrderByDescending(x => x.ValuationRatios.PERatio);

            // take the top entries from our sorted collection
            var topFine = sortedByPeRatio.Take(NumberOfSymbolsFundamental);

            // we need to return only the symbol objects
            return topFine.Select(x => x.Symbol);
        }

        //Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        public void OnData(TradeBars data)
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
        public long DataPoints => 85867;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 3;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.223%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.009%"},
            {"Sharpe Ratio", "-6.313"},
            {"Probabilistic Sharpe Ratio", "12.055%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.019"},
            {"Beta", "0.027"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.749"},
            {"Tracking Error", "0.095"},
            {"Treynor Ratio", "-0.876"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$2200000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.28%"},
            {"OrderListHash", "34bb9933f9d242713c0ec14c4ee586b6"}
        };
    }
}
