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
 *
*/

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that futures and future option contracts added via universe selection
    /// get automatically seeded by default
    /// </summary>
    public class FuturesAutomaticSeedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _futureContractsAdded;
        private bool _fopsContractsAdded;

        public override void Initialize()
        {
            SetStartDate(2020, 01, 07);
            SetEndDate(2020, 01, 07);
            SetCash(100000);

            var futures = AddFuture(Futures.Indices.SP500EMini);
            futures.SetFilter(0, 365);

            AddFutureOption(futures.Symbol, universe => universe.Strikes(-5, +5));
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            var gotTrades = false;
            var gotQuotes = false;

            foreach (var addedSecurity in changes.AddedSecurities.Where(x => !x.Symbol.IsCanonical()))
            {
                if (addedSecurity.Price == 0)
                {
                    throw new RegressionTestException("Security was not seeded");
                }

                var hasTrades = addedSecurity.Cache.GetData<TradeBar>() != null;
                var hasQuotes = addedSecurity.Cache.GetData<QuoteBar>() != null;

                if (!hasTrades && !hasQuotes)
                {
                    throw new RegressionTestException("Security does not have TradeBar or QuoteBar data");
                }

                gotTrades |= hasTrades;
                gotQuotes |= hasQuotes;

                // Just making sure we had the data to select and seed futures and future options
                _futureContractsAdded |= addedSecurity.Symbol.SecurityType == SecurityType.Future;
                _fopsContractsAdded |= addedSecurity.Symbol.SecurityType == SecurityType.FutureOption;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_futureContractsAdded)
            {
                throw new RegressionTestException("No option contracts were added");
            }

            if (!_fopsContractsAdded)
            {
                throw new RegressionTestException("No future option contracts were added");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 448;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 444;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
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
