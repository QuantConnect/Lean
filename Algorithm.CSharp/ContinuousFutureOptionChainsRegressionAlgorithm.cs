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
    public class ContinuousFutureOptionChainsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _es;
        private bool _foundNonEmptyChain;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 4);
            SetEndDate(2020, 1, 8);

            _es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);

            AddFutureOption(_es.Symbol, universe => universe.Strikes(-1, 1));
        }

        public override void OnData(Slice slice)
        {
            if (!slice.HasData || Portfolio.Invested)
            {
                return;
            }

            // Retrieve the OptionChain for the mapped symbol of the continuous future
            var chain = slice.OptionChains.get(_es.Mapped);
            if (chain == null || !chain.Any())
            {
                return;
            }

            // Mark that we successfully received a non-empty OptionChain
            _foundNonEmptyChain = true;

            // Buy the first call option we find
            var call = chain.Contracts.Values.FirstOrDefault(c => c.Right == OptionRight.Call);
            if (call != null && !Portfolio.Invested)
            {
                MarketOrder(call.Symbol, 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Ensure at least one non-empty OptionChain was found during the execution
            if (!_foundNonEmptyChain)
            {
                throw new RegressionTestException("No option chain found");
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
        public long DataPoints => 17567;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "56.716%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100617.33"},
            {"Net Profit", "0.617%"},
            {"Sharpe Ratio", "9.234"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.977%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.295"},
            {"Beta", "0.337"},
            {"Annual Standard Deviation", "0.049"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-0.251"},
            {"Tracking Error", "0.059"},
            {"Treynor Ratio", "1.341"},
            {"Total Fees", "$1.42"},
            {"Estimated Strategy Capacity", "$430000.00"},
            {"Lowest Capacity Asset", "ES XCZJLDQX2SRO|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "0.78%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "2a94699b64757d2fd55a198f8c3952ef"}
        };
    }
}
