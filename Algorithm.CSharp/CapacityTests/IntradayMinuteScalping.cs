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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Scalps SPY using an EMA cross strategy at minute resolution.
    /// This tests equity strategies that trade at a higher frequency, which
    /// should have a reduced capacity estimate as a result.
    /// </summary>
    public class IntradayMinuteScalping : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;


        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 1, 30);
            SetCash(100000);
            SetWarmup(100);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            _fast = EMA(_spy, 20);
            _slow = EMA(_spy, 40);
        }

        public override void OnData(Slice data)
        {
            if (Portfolio[_spy].Quantity <= 0 && _fast > _slow)
            {
                SetHoldings(_spy, 1);
            }
            else if (Portfolio[_spy].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(_spy, -1);
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
            {"Total Orders", "150"},
            {"Average Win", "0.16%"},
            {"Average Loss", "-0.11%"},
            {"Compounding Annual Return", "-19.320%"},
            {"Drawdown", "3.900%"},
            {"Expectancy", "-0.193"},
            {"Net Profit", "-1.730%"},
            {"Sharpe Ratio", "-1.606"},
            {"Probabilistic Sharpe Ratio", "21.397%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "1.45"},
            {"Alpha", "-0.357"},
            {"Beta", "0.635"},
            {"Annual Standard Deviation", "0.119"},
            {"Annual Variance", "0.014"},
            {"Information Ratio", "-4.249"},
            {"Tracking Error", "0.106"},
            {"Treynor Ratio", "-0.302"},
            {"Total Fees", "$449.14"},
            {"Estimated Strategy Capacity", "$27000000.00"},
            {"Fitness Score", "0.088"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-3.259"},
            {"Return Over Maximum Drawdown", "-7.992"},
            {"Portfolio Turnover", "14.605"},
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
            {"OrderListHash", "f5a0e9547f7455004fa6c3eb136534e9"}
        };
    }
}
