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
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests that splits do not cause the algorithm to report capacity estimates
    /// above or below the actual capacity due to splits. The stock HTGM is illiquid,
    /// trading only $1.2 Million per day on average with sparse trade frequencies.
    /// </summary>
    public class SplitTestingStrategy : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _htgm;

        public override void Initialize()
        {
            SetStartDate(2020, 11, 1);
            SetEndDate(2020, 12, 5);
            SetCash(100000);

            var htgm = AddEquity("HTGM", Resolution.Hour);
            htgm.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _htgm = htgm.Symbol;
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_htgm, 1);
            }
            else
            {
                SetHoldings(_htgm, -1);
            }
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
            {"Total Orders", "162"},
            {"Average Win", "0.10%"},
            {"Average Loss", "-0.35%"},
            {"Compounding Annual Return", "-94.432%"},
            {"Drawdown", "30.400%"},
            {"Expectancy", "-0.564"},
            {"Net Profit", "-23.412%"},
            {"Sharpe Ratio", "-1.041"},
            {"Probabilistic Sharpe Ratio", "12.971%"},
            {"Loss Rate", "66%"},
            {"Win Rate", "34%"},
            {"Profit-Loss Ratio", "0.29"},
            {"Alpha", "-4.827"},
            {"Beta", "1.43"},
            {"Annual Standard Deviation", "0.876"},
            {"Annual Variance", "0.767"},
            {"Information Ratio", "-4.288"},
            {"Tracking Error", "0.851"},
            {"Treynor Ratio", "-0.637"},
            {"Total Fees", "$2655.91"},
            {"Estimated Strategy Capacity", "$11000.00"},
            {"Fitness Score", "0.052"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-2.2"},
            {"Return Over Maximum Drawdown", "-3.481"},
            {"Portfolio Turnover", "0.307"},
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
            {"OrderListHash", "54f571c11525656e9b383e235e77002e"}
        };
    }
}
