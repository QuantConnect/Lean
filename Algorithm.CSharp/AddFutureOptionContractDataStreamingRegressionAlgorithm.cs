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
using QuantConnect.Securities.Future;

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
        private Symbol _es18z20;
        private Symbol _es19h21;

        private readonly HashSet<Symbol> _symbolsReceived = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _expectedSymbolsReceived = new HashSet<Symbol>();
        private readonly Dictionary<Symbol, List<QuoteBar>> _dataReceived = new Dictionary<Symbol, List<QuoteBar>>();

        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            SetEndDate(2020, 9, 23);

            _es18z20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 12, 18)),
                Resolution.Minute).Symbol;

            _es19h21 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2021, 3, 19)),
                Resolution.Minute).Symbol;

            var optionChains = OptionChainProvider.GetOptionContractList(_es18z20, Time)
                .Concat(OptionChainProvider.GetOptionContractList(_es19h21, Time));

            foreach (var optionContract in optionChains)
            {
                _expectedSymbolsReceived.Add(AddFutureOptionContract(optionContract, Resolution.Minute).Symbol);
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
                if (qb.Symbol.SecurityType != SecurityType.Option)
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

            if (data.ContainsKey(_es18z20) && data.ContainsKey(_es19h21))
            {
                SetHoldings(_es18z20, 0.2);
                SetHoldings(_es19h21, 0.2);

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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            { "Total Trades", "2" },
            { "Average Win", "0%" },
            { "Average Loss", "0%" },
            { "Compounding Annual Return", "21631.964%" },
            { "Drawdown", "2.500%" },
            { "Expectancy", "0" },
            { "Net Profit", "2.993%" },
            { "Sharpe Ratio", "11.754" },
            { "Probabilistic Sharpe Ratio", "0%" },
            { "Loss Rate", "0%" },
            { "Win Rate", "0%" },
            { "Profit-Loss Ratio", "0" },
            { "Alpha", "0" },
            { "Beta", "0" },
            { "Annual Standard Deviation", "0.008" },
            { "Annual Variance", "0" },
            { "Information Ratio", "11.754" },
            { "Tracking Error", "0.008" },
            { "Treynor Ratio", "0" },
            { "Total Fees", "$3.70" },
            { "Fitness Score", "1" },
            { "Kelly Criterion Estimate", "0" },
            { "Kelly Criterion Probability Value", "0" },
            { "Sortino Ratio", "79228162514264337593543950335" },
            { "Return Over Maximum Drawdown", "79228162514264337593543950335" },
            { "Portfolio Turnover", "1.586" },
            { "Total Insights Generated", "0" },
            { "Total Insights Closed", "0" },
            { "Total Insights Analysis Completed", "0" },
            { "Long Insight Count", "0" },
            { "Short Insight Count", "0" },
            { "Long/Short Ratio", "100%" },
            { "Estimated Monthly Alpha Value", "$0" },
            { "Total Accumulated Estimated Alpha Value", "$0" },
            { "Mean Population Estimated Insight Value", "$0" },
            { "Mean Population Direction", "0%" },
            { "Mean Population Magnitude", "0%" },
            { "Rolling Averaged Population Direction", "0%" },
            { "Rolling Averaged Population Magnitude", "0%" },
            { "OrderListHash", "-1899680538" }
        };
    }
}
