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
    /// Regression algorithm illustrating the usage of the <see cref="QCAlgorithm.FuturesChains(IEnumerable{Symbol}, bool)"/>
    /// method to get multiple futures chains.
    /// </summary>
    public class FuturesChainsMultipleFullDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _esFutureContract;
        private Symbol _gcFutureContract;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            var esFuture= AddFuture(Futures.Indices.SP500EMini).Symbol;
            var gcFuture = AddFuture(Futures.Metals.Gold).Symbol;

            var chains = FuturesChains([esFuture, gcFuture]);

            _esFutureContract = GetContract(chains, esFuture);
            _gcFutureContract = GetContract(chains, gcFuture);

            AddFutureContract(_esFutureContract);
            AddFutureContract(_gcFutureContract);
        }

        private Symbol GetContract(FuturesChains chains, Symbol canonical)
        {
            return chains
                .Where(kvp => kvp.Key == canonical)
                .Select(kvp => kvp.Value)
                .Single()
                // Get contracts expiring within 6 months
                .Where(contractData => contractData.Expiry - Time <= TimeSpan.FromDays(180))
                // Get the contract with the latest expiration date, and lowest price
                .OrderByDescending(x => x.Expiry)
                .ThenBy(x => x.LastPrice)
                .First();
        }

        public override void OnData(Slice slice)
        {
            // Do some trading with the selected contract for sample purposes
            if (!Portfolio.Invested)
            {
                SetHoldings(_esFutureContract, 0.25);
                SetHoldings(_gcFutureContract, 0.25);
            }
            else
            {
                Liquidate();
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 8184;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 2;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "900"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "57108.26"},
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
            {"Total Fees", "$5701.74"},
            {"Estimated Strategy Capacity", "$19000.00"},
            {"Lowest Capacity Asset", "GC VOFJUCDY9XNH"},
            {"Portfolio Turnover", "432921.78%"},
            {"OrderListHash", "c75de8eb115be82a0ec2afaec8d034ff"}
        };
    }
}
