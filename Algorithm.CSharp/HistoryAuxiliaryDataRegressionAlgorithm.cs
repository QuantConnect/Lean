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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of auxiliary data history requests
    /// </summary>
    public class HistoryAuxiliaryDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 5);

            var aapl = AddEquity("AAPL", Resolution.Daily).Symbol;

            // multi symbol request
            var spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var multiSymbolRequest = History<Dividend>(new[] { aapl, spy }, 360, Resolution.Daily).ToList();
            if (multiSymbolRequest.Count != 12)
            {
                throw new Exception($"Unexpected multi symbol dividend count: {multiSymbolRequest.Count}");
            }

            // continuous future mapping requests
            var sp500 = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
            var continuousFutureOpenInterestMapping = History<SymbolChangedEvent>(sp500, new DateTime(2007, 1, 1), new DateTime(2012, 1, 1),
                dataMappingMode: DataMappingMode.OpenInterest).ToList();
            if (continuousFutureOpenInterestMapping.Count != 9)
            {
                throw new Exception($"Unexpected continuous future mapping event count: {continuousFutureOpenInterestMapping.Count}");
            }
            var continuousFutureLastTradingDayMapping = History<SymbolChangedEvent>(sp500, new DateTime(2007, 1, 1),new DateTime(2012, 1, 1),
                dataMappingMode: DataMappingMode.LastTradingDay).ToList();
            if (continuousFutureLastTradingDayMapping.Count != 9)
            {
                throw new Exception($"Unexpected continuous future mapping event count: {continuousFutureLastTradingDayMapping.Count}");
            }
            // mapping dates should be different
            if (Enumerable.SequenceEqual(continuousFutureOpenInterestMapping.Select(x => x.EndTime), continuousFutureLastTradingDayMapping.Select(x => x.EndTime)))
            {
                throw new Exception($"Unexpected continuous future mapping times");
            }

            var dividends = History<Dividend>(aapl, 360).ToList();
            if (dividends.Count != 6)
            {
                throw new Exception($"Unexpected dividend count: {dividends.Count}");
            }
            foreach (var dividend in dividends)
            {
                if (dividend.Distribution == 0)
                {
                    throw new Exception($"Unexpected Distribution: {dividend.Distribution}");
                }
            }

            var splits = History<Split>(aapl, 360).ToList();
            if (splits.Count != 2)
            {
                throw new Exception($"Unexpected split count: {splits.Count}");
            }
            foreach (var split in splits)
            {
                if (split.SplitFactor == 0)
                {
                    throw new Exception($"Unexpected SplitFactor: {split.SplitFactor}");
                }
            }

            var cryptoFuture = QuantConnect.Symbol.Create("BTCUSD", SecurityType.CryptoFuture, Market.Binance);
            var marginInterests = History<MarginInterestRate>(cryptoFuture, 24 * 3, Resolution.Hour).ToList();
            if (marginInterests.Count != 8)
            {
                throw new Exception($"Unexpected margin interest count: {marginInterests.Count}");
            }
            foreach (var marginInterest in marginInterests)
            {
                if (marginInterest.InterestRate == 0)
                {
                    throw new Exception($"Unexpected InterestRate: {marginInterest.InterestRate}");
                }
            }

            // last trading date on 2007-05-18
            var delistedSymbol = QuantConnect.Symbol.Create("AAA.1", SecurityType.Equity, Market.USA);
            var delistings = History<Delisting>(delistedSymbol, new DateTime(2007, 5, 15), new DateTime(2007, 5, 21)).ToList();
            if (delistings.Count != 2)
            {
                throw new Exception($"Unexpected delistings count: {delistings.Count}");
            }
            if (delistings[0].Type != DelistingType.Warning)
            {
                throw new Exception($"Unexpected delisting: {delistings[0]}");
            }
            if (delistings[1].Type != DelistingType.Delisted)
            {
                throw new Exception($"Unexpected delisting: {delistings[1]}");
            }

            // get's remapped:
            // 2008-09-30 spwr -> spwra
            // 2011-11-17 spwra -> spwr
            var remappedSymbol = QuantConnect.Symbol.Create("SPWR", SecurityType.Equity, Market.USA);
            var symbolChangedEvents = History<SymbolChangedEvent>(remappedSymbol, new DateTime(2007, 1, 1), new DateTime(2012, 1, 1)).ToList();
            if (symbolChangedEvents.Count != 2)
            {
                throw new Exception($"Unexpected SymbolChangedEvents count: {symbolChangedEvents.Count}");
            }
            if (symbolChangedEvents[0].OldSymbol != "SPWR" || symbolChangedEvents[0].NewSymbol != "SPWRA" || symbolChangedEvents[0].EndTime != new DateTime(2008, 9, 30))
            {
                throw new Exception($"Unexpected SymbolChangedEvents: {symbolChangedEvents[0]}");
            }
            if (symbolChangedEvents[1].NewSymbol != "SPWR" || symbolChangedEvents[1].OldSymbol != "SPWRA" || symbolChangedEvents[1].EndTime != new DateTime(2011, 11, 17))
            {
                throw new Exception($"Unexpected SymbolChangedEvents: {symbolChangedEvents[1]}");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("AAPL", 1);
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
        public long DataPoints => 25;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 50;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-75.686%"},
            {"Drawdown", "3.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98081.50"},
            {"Net Profit", "-1.918%"},
            {"Sharpe Ratio", "-2.361"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "25.305%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.055"},
            {"Beta", "2.161"},
            {"Annual Standard Deviation", "0.295"},
            {"Annual Variance", "0.087"},
            {"Information Ratio", "-2.188"},
            {"Tracking Error", "0.16"},
            {"Treynor Ratio", "-0.323"},
            {"Total Fees", "$3.76"},
            {"Estimated Strategy Capacity", "$680000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "20.70%"},
            {"OrderListHash", "a91397cd498dea863efb99017abdbeab"}
        };
    }
}
