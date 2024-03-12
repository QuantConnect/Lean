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
    /// Tests capacity by trading SPY (beast) alongside a small cap stock ABUS (penny)
    /// </summary>
    public class BeastVsPenny : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 3, 31);
            SetCash(10000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            var penny = AddEquity("ABUS", Resolution.Hour).Symbol;

            Schedule.On(DateRules.EveryDay(_spy), TimeRules.AfterMarketOpen(_spy, 1, false), () =>
            {
                SetHoldings(_spy, 0.5m);
                SetHoldings(penny, 0.5m);
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
            {"Total Orders", "70"},
            {"Average Win", "0.07%"},
            {"Average Loss", "-0.51%"},
            {"Compounding Annual Return", "-89.548%"},
            {"Drawdown", "49.900%"},
            {"Expectancy", "-0.514"},
            {"Net Profit", "-42.920%"},
            {"Sharpe Ratio", "-0.797"},
            {"Probabilistic Sharpe Ratio", "9.019%"},
            {"Loss Rate", "57%"},
            {"Win Rate", "43%"},
            {"Profit-Loss Ratio", "0.13"},
            {"Alpha", "-0.24"},
            {"Beta", "1.101"},
            {"Annual Standard Deviation", "1.031"},
            {"Annual Variance", "1.063"},
            {"Information Ratio", "-0.351"},
            {"Tracking Error", "0.836"},
            {"Treynor Ratio", "-0.747"},
            {"Total Fees", "$81.45"},
            {"Estimated Strategy Capacity", "$21000.00"},
            {"Fitness Score", "0.01"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-1.284"},
            {"Return Over Maximum Drawdown", "-1.789"},
            {"Portfolio Turnover", "0.038"},
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
            {"OrderListHash", "67c9083f604ed16fb68481e7c26878dc"}
        };
    }
}
