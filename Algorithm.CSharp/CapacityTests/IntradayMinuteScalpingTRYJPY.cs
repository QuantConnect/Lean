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
    /// Scalps TRYJPY using an EMA cross strategy at minute resolution.
    /// This tests FOREX strategies that trade at a higher frequency, which
    /// should have a reduced capacity estimate as a result. This tests that
    /// currency conversions are applied properly to the capacity estimate calculation.
    /// </summary>
    public class IntradayMinuteScalpingTRYJPY : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _tryjpy;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;


        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 30);
            SetCash(100000);
            SetWarmup(100);

            _tryjpy = AddForex("TRYJPY", Resolution.Minute, Market.Oanda).Symbol;
            _fast = EMA(_tryjpy, 20);
            _slow = EMA(_tryjpy, 40);
        }

        public override void OnData(Slice data)
        {
            if (Portfolio[_tryjpy].Quantity <= 0 && _fast > _slow)
            {
                SetHoldings(_tryjpy, 1);
            }
            else if (Portfolio[_tryjpy].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(_tryjpy, -1);
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
            {"Total Orders", "603"},
            {"Average Win", "0.20%"},
            {"Average Loss", "-0.26%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "73.200%"},
            {"Expectancy", "-0.849"},
            {"Net Profit", "-73.118%"},
            {"Sharpe Ratio", "-2.046"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "91%"},
            {"Win Rate", "9%"},
            {"Profit-Loss Ratio", "0.75"},
            {"Alpha", "-0.95"},
            {"Beta", "0.541"},
            {"Annual Standard Deviation", "0.489"},
            {"Annual Variance", "0.239"},
            {"Information Ratio", "-1.863"},
            {"Tracking Error", "0.487"},
            {"Treynor Ratio", "-1.849"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$4400000.00"},
            {"Fitness Score", "0.259"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-2.135"},
            {"Return Over Maximum Drawdown", "-1.389"},
            {"Portfolio Turnover", "49.501"},
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
            {"OrderListHash", "4eb4d703a9f200b6bb3d8b0ebbc9db7f"}
        };
    }
}
