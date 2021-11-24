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
        public override Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "9"},
            {"Average Win", "0%"},
            {"Average Loss", "-80.0%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "80.0%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-80.0%"},
            {"Sharpe Ratio", "-0.465"},
            {"Probabilistic Sharpe Ratio", "0.012%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-1.083"},
            {"Beta", "1.393"},
            {"Annual Standard Deviation", "2.15"},
            {"Annual Variance", "4.625"},
            {"Information Ratio", "-0.495"},
            {"Tracking Error", "2.143"},
            {"Treynor Ratio", "-0.718"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$5000.00"},
            {"Lowest Capacity Asset", "SPX XL80P59H5E6M|SPX 31"},
            {"Fitness Score", "0.019"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.334"},
            {"Return Over Maximum Drawdown", "-1.249"},
            {"Portfolio Turnover", "0.051"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "31e5d533a2a1c5cf7300ebd6f776df80"}
        };
    }
}
