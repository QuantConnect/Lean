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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Rebalances ultra-liquid stocks monthly, testing
    /// bursts of orders centered around the start of the month at Daily resolution
    /// </summary>
    public class MonthlyRebalanceDaily : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2019, 12, 31);
            SetEndDate(2020, 4, 5);
            SetCash(100000);

            var spy = AddEquity("SPY", Resolution.Daily).Symbol;
            AddEquity("GE", Resolution.Daily);
            AddEquity("FB", Resolution.Daily);
            AddEquity("DIS", Resolution.Daily);
            AddEquity("CSCO", Resolution.Daily);
            AddEquity("CRM", Resolution.Daily);
            AddEquity("C", Resolution.Daily);
            AddEquity("BAC", Resolution.Daily);
            AddEquity("BABA", Resolution.Daily);
            AddEquity("AAPL", Resolution.Daily);

            Schedule.On(DateRules.MonthStart(spy), TimeRules.Noon, () =>
            {
                foreach (var symbol in Securities.Keys)
                {
                    SetHoldings(symbol, 0.10);
                }
            });
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "35"},
            {"Average Win", "0.07%"},
            {"Average Loss", "-0.07%"},
            {"Compounding Annual Return", "-68.407%"},
            {"Drawdown", "32.400%"},
            {"Expectancy", "-0.309"},
            {"Net Profit", "-25.901%"},
            {"Sharpe Ratio", "-1.503"},
            {"Probabilistic Sharpe Ratio", "2.878%"},
            {"Loss Rate", "64%"},
            {"Win Rate", "36%"},
            {"Profit-Loss Ratio", "0.90"},
            {"Alpha", "-0.7"},
            {"Beta", "-0.238"},
            {"Annual Standard Deviation", "0.386"},
            {"Annual Variance", "0.149"},
            {"Information Ratio", "-0.11"},
            {"Tracking Error", "0.712"},
            {"Treynor Ratio", "2.442"},
            {"Total Fees", "$38.99"},
            {"Estimated Strategy Capacity", "$19000000.00"},
            {"Fitness Score", "0.003"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-2.021"},
            {"Return Over Maximum Drawdown", "-2.113"},
            {"Portfolio Turnover", "0.014"},
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
            {"OrderListHash", "76d8164a3c0d4a7d45e94367c4ba5be1"}
        };
    }
}
