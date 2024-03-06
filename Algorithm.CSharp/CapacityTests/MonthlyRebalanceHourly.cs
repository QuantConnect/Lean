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
    /// bursts of orders centered around the start of the month at Hourly resolution
    /// </summary>
    public class MonthlyRebalanceHourly : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2019, 12, 31);
            SetEndDate(2020, 4, 5);
            SetCash(100000);

            var spy = AddEquity("SPY", Resolution.Hour).Symbol;
            AddEquity("GE", Resolution.Hour);
            AddEquity("FB", Resolution.Hour);
            AddEquity("DIS", Resolution.Hour);
            AddEquity("CSCO", Resolution.Hour);
            AddEquity("CRM", Resolution.Hour);
            AddEquity("C", Resolution.Hour);
            AddEquity("BAC", Resolution.Hour);
            AddEquity("BABA", Resolution.Hour);
            AddEquity("AAPL", Resolution.Hour);

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
            {"Average Win", "0.05%"},
            {"Average Loss", "-0.10%"},
            {"Compounding Annual Return", "-72.444%"},
            {"Drawdown", "36.500%"},
            {"Expectancy", "-0.449"},
            {"Net Profit", "-28.406%"},
            {"Sharpe Ratio", "-1.369"},
            {"Probabilistic Sharpe Ratio", "4.398%"},
            {"Loss Rate", "64%"},
            {"Win Rate", "36%"},
            {"Profit-Loss Ratio", "0.51"},
            {"Alpha", "-0.175"},
            {"Beta", "0.892"},
            {"Annual Standard Deviation", "0.503"},
            {"Annual Variance", "0.253"},
            {"Information Ratio", "-0.822"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "-0.772"},
            {"Total Fees", "$38.83"},
            {"Estimated Strategy Capacity", "$6000000.00"},
            {"Fitness Score", "0.004"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-2.033"},
            {"Return Over Maximum Drawdown", "-2.079"},
            {"Portfolio Turnover", "0.018"},
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
            {"OrderListHash", "1de9bcf6cda0945af6ba1f74c4dcb22c"}
        };
    }
}
