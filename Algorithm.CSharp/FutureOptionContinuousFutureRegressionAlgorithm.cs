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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm that validates that when using a continuous future (without a filter)
    /// the option chains are correctly populated using the mapped symbol.
    /// </summary>
    public class FutureOptionContinuousFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected Future Future { get; private set; }
        private bool _hasAnyOptionChainForMappedSymbol;
        private bool _anyContractFound;
        private readonly HashSet<Symbol> _uniqueFutureChains = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _uniqueOptionChains = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _uniqueOptionContracts = new HashSet<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2020, 1, 4);
            SetEndDate(2020, 1, 8);

            Future = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            SetFilter();

            AddFutureOption(Future.Symbol, universe => universe.Strikes(-1, 1));
        }

        public virtual void SetFilter()
        {
        }

        public override void OnData(Slice slice)
        {
            if (!slice.HasData)
            {
                return;
            }

            // FutureChains should be unique
            foreach (var futureChain in slice.FutureChains)
            {
                if (!_uniqueFutureChains.Add(futureChain.Key))
                {
                    throw new RegressionTestException($"Duplicate FutureChain found: {futureChain.Key}");
                }
            }

            // OptionChains should be unique
            foreach (var optionChain in slice.OptionChains)
            {
                if (!_uniqueOptionChains.Add(optionChain.Key))
                {
                    throw new RegressionTestException($"Duplicate OptionChain found: {optionChain.Key}");
                }
            }

            // Option contracts within chains should be unique
            foreach (var optionChain in slice.OptionChains.Values)
            {
                foreach (var contract in optionChain.Contracts.Keys)
                {
                    if (!_uniqueOptionContracts.Add(contract))
                    {
                        throw new RegressionTestException($"Duplicate OptionContract found: {contract}");
                    }
                }
            }

            _anyContractFound = _uniqueOptionContracts.Count != 0;

            // OptionChain for mapped symbol
            var canonicalSymbol = QuantConnect.Symbol.CreateCanonicalOption(Future.Mapped);
            if (!slice.OptionChains.TryGetValue(canonicalSymbol, out var chain) || chain == null || !chain.Any())
            {
                return;
            }

            // Mark that we successfully received a non-empty OptionChain for mapped symbol
            _hasAnyOptionChainForMappedSymbol = true;

            // Reset hashsets
            _uniqueFutureChains.Clear();
            _uniqueOptionChains.Clear();
            _uniqueOptionContracts.Clear();
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_hasAnyOptionChainForMappedSymbol)
            {
                throw new RegressionTestException("No non-empty option chain found for mapped symbol during algorithm execution");
            }

            if (!_anyContractFound)
            {
                throw new RegressionTestException("No option contract found during algorithm execution");
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
        public virtual long DataPoints => 12072;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.RuntimeError;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.665"},
            {"Tracking Error", "0.053"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
