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
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm illustrating the usage of the <see cref="QCAlgorithm.FuturesChain(Symbol, bool)"/>
    /// method to get a future chain.
    /// </summary>
    public class FuturesChainFullDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _futureContract;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            var future = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute).Symbol;

            var chain = FuturesChain(future);
            foreach (var contract in chain)
            {
                if (contract.BidPrice == 0 && contract.AskPrice == 0 && contract.Volume == 0)
                {
                    throw new RegressionTestException("FuturesChain() returned contract with no data.");
                }
            }

            _futureContract = chain
                // Get contracts expiring within 6 months
                .Where(contractData => contractData.Expiry - Time <= TimeSpan.FromDays(180))
                // Get the contract with the latest expiration date, and lowest price
                .OrderByDescending(x => x.Expiry)
                .ThenBy(x => x.LastPrice)
                .First();

            AddFutureContract(_futureContract);
        }

        public override void OnData(Slice slice)
        {
            // Do some trading with the selected contract for sample purposes
            if (!Portfolio.Invested)
            {
                SetHoldings(_futureContract, 0.5);
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
        public long DataPoints => 4083;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "450"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "50272.1"},
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
            {"Total Fees", "$8290.40"},
            {"Estimated Strategy Capacity", "$13000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "639698.49%"},
            {"OrderListHash", "312461917700d86df1b5c43e1e7ec0eb"}
        };
    }
}
