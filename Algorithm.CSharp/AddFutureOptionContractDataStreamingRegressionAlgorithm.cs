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
    /// This regression algorithm tests that we receive the expected data when
    /// we add future option contracts individually using <see cref="AddFutureOptionContract"/>
    /// </summary>
    public class AddFutureOptionContractDataStreamingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _onDataReached;
        private bool _invested;
        private Symbol _es20h20;
        private Symbol _es19m20;

        private readonly HashSet<Symbol> _symbolsReceived = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _expectedSymbolsReceived = new HashSet<Symbol>();
        private readonly Dictionary<Symbol, List<QuoteBar>> _dataReceived = new Dictionary<Symbol, List<QuoteBar>>();

        public override void Initialize()
        {
            SetStartDate(2020, 1, 4);
            SetEndDate(2020, 1, 8);

            _es20h20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 3, 20)),
                Resolution.Minute).Symbol;

            _es19m20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 6, 19)),
                Resolution.Minute).Symbol;

            // Get option contract lists for 2020/01/05 (Time.AddDays(1)) because Lean has local data for that date
            var optionChains = OptionChainProvider.GetOptionContractList(_es20h20, Time.AddDays(1))
                .Concat(OptionChainProvider.GetOptionContractList(_es19m20, Time.AddDays(1)));

            foreach (var optionContract in optionChains)
            {
                _expectedSymbolsReceived.Add(AddFutureOptionContract(optionContract, Resolution.Minute).Symbol);
            }

            if (_expectedSymbolsReceived.Count == 0)
            {
                throw new InvalidOperationException("Expected Symbols receive count is 0, expected >0");
            }
        }

        public override void OnData(Slice data)
        {
            if (!data.HasData)
            {
                return;
            }

            _onDataReached = true;

            var hasOptionQuoteBars = false;
            foreach (var qb in data.QuoteBars.Values)
            {
                if (qb.Symbol.SecurityType != SecurityType.FutureOption)
                {
                    continue;
                }

                hasOptionQuoteBars = true;

                _symbolsReceived.Add(qb.Symbol);
                if (!_dataReceived.ContainsKey(qb.Symbol))
                {
                    _dataReceived[qb.Symbol] = new List<QuoteBar>();
                }

                _dataReceived[qb.Symbol].Add(qb);
            }

            if (_invested || !hasOptionQuoteBars)
            {
                return;
            }

            if (data.ContainsKey(_es20h20) && data.ContainsKey(_es19m20))
            {
                SetHoldings(_es20h20, 0.2);
                SetHoldings(_es19m20, 0.2);

                _invested = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();

            if (!_onDataReached)
            {
                throw new Exception("OnData() was never called.");
            }
            if (_symbolsReceived.Count != _expectedSymbolsReceived.Count)
            {
                throw new AggregateException($"Expected {_expectedSymbolsReceived.Count} option contracts Symbols, found {_symbolsReceived.Count}");
            }

            var missingSymbols = new List<Symbol>();
            foreach (var expectedSymbol in _expectedSymbolsReceived)
            {
                if (!_symbolsReceived.Contains(expectedSymbol))
                {
                    missingSymbols.Add(expectedSymbol);
                }
            }

            if (missingSymbols.Count > 0)
            {
                throw new Exception($"Symbols: \"{string.Join(", ", missingSymbols)}\" were not found in OnData");
            }

            foreach (var expectedSymbol in _expectedSymbolsReceived)
            {
                var data = _dataReceived[expectedSymbol];
                var nonDupeDataCount = data.Select(x =>
                {
                    x.EndTime = default(DateTime);
                    return x;
                }).Distinct().Count();

                if (nonDupeDataCount < 1000)
                {
                    throw new Exception($"Received too few data points. Expected >=1000, found {nonDupeDataCount} for {expectedSymbol}");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 311879;

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
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "5512.811%"},
            {"Drawdown", "1.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "105332.8"},
            {"Net Profit", "5.333%"},
            {"Sharpe Ratio", "64.084"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.977%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "25.763"},
            {"Beta", "2.914"},
            {"Annual Standard Deviation", "0.423"},
            {"Annual Variance", "0.179"},
            {"Information Ratio", "66.11"},
            {"Tracking Error", "0.403"},
            {"Treynor Ratio", "9.308"},
            {"Total Fees", "$8.60"},
            {"Estimated Strategy Capacity", "$22000000.00"},
            {"Lowest Capacity Asset", "ES XFH59UK0MYO1"},
            {"Portfolio Turnover", "122.11%"},
            {"OrderListHash", "d744fa8beaa60546c84924ed68d945d9"}
        };
    }
}
