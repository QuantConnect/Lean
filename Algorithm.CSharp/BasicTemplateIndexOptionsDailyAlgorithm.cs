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

using System.Linq;
using QuantConnect.Data;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an IndexOptions algorithm with Daily data
    /// </summary>
    public class BasicTemplateIndexOptionsDailyAlgorithm : BasicTemplateIndexOptionsAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;
        protected override int StartDay => 1;
        
        /// <summary>
        /// Index EMA Cross trading index options of the index.
        /// </summary>
        public override void OnData(Slice slice)
        {
            foreach (var chain in slice.OptionChains.Values)
            {
                // Select the contract with the lowest AskPrice
                var contract = chain.Contracts.OrderBy(x => x.Value.AskPrice).FirstOrDefault().Value;

                if (contract == null)
                {
                    return;
                }
                
                if (Portfolio.Invested)
                {
                    Liquidate();
                }
                else
                {
                    MarketOrder(contract.Symbol, 1);
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 360;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "11"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.092%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "999920"},
            {"Net Profit", "-0.008%"},
            {"Sharpe Ratio", "-19.865"},
            {"Sortino Ratio", "-175397.15"},
            {"Probabilistic Sharpe Ratio", "0.013%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.003"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.454"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "-44.954"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P59H5E6M|SPX 31"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "8340619d603921c1ce261287890b9c1c"}
        };
    }
}
