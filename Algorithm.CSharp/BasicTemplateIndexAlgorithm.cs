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
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add index asset types.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="indexes" />
    public class BasicTemplateIndexAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spx;
        private Symbol _spy;
        private ExponentialMovingAverage _emaSlow;
        private ExponentialMovingAverage _emaFast;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 15);
            SetCash(1000000);

            // Use indicator for signal; but it cannot be traded
            _spx = AddIndex("SPX", Resolution.Minute).Symbol;
            _emaSlow = EMA(_spx, 80);
            _emaFast = EMA(_spx, 200);

            // Trade on SPY
            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// Index EMA Cross trading underlying.
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (!slice.Bars.ContainsKey(_spx) || !slice.Bars.ContainsKey(_spy))
            {
                return;
            }

            // Warm up indicators
            if (!_emaSlow.IsReady)
            {
                return;
            }

            if (_emaFast > _emaSlow)
            {
                SetHoldings(_spy, 1);
            }
            else
            {
                Liquidate();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio[_spx].TotalSaleVolume > 0)
            {
                throw new Exception("Index is not tradable.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.52%"},
            {"Compounding Annual Return", "50.775%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "-1"},
            {"Net Profit", "1.323%"},
            {"Sharpe Ratio", "6.851"},
            {"Probabilistic Sharpe Ratio", "94.289%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.099"},
            {"Annual Variance", "0.01"},
            {"Information Ratio", "6.851"},
            {"Tracking Error", "0.099"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$40.32"},
            {"Estimated Strategy Capacity", "$35000000.00"},
            {"Fitness Score", "0.33"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "132.4"},
            {"Portfolio Turnover", "0.33"},
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
            {"OrderListHash", "922dbc98edeb8e91b65c36c3582a61e7"}
        };
    }
}
