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
    /// Scalps GBPJPY using an EMA cross strategy at minute resolution.
    /// This tests FOREX strategies that trade at a higher frequency, which
    /// should have a reduced capacity estimate as a result. This test also
    /// tests that currency conversion rates are applied and calculated correctly.
    /// </summary>
    public class IntradayMinuteScalpingGBPJPY : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _gbpjpy;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;


        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 30);
            SetCash(100000);
            SetWarmup(100);

            _gbpjpy = AddForex("GBPJPY", Resolution.Minute, Market.Oanda).Symbol;
            _fast = EMA(_gbpjpy, 20);
            _slow = EMA(_gbpjpy, 40);
        }

        public override void OnData(Slice data)
        {
            if (Portfolio[_gbpjpy].Quantity <= 0 && _fast > _slow)
            {
                SetHoldings(_gbpjpy, 1);
            }
            else if (Portfolio[_gbpjpy].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(_gbpjpy, -1);
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
            {"Total Orders", "735"},
            {"Average Win", "0.08%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "-93.946%"},
            {"Drawdown", "19.900%"},
            {"Expectancy", "-0.592"},
            {"Net Profit", "-19.794%"},
            {"Sharpe Ratio", "-10.054"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "84%"},
            {"Win Rate", "16%"},
            {"Profit-Loss Ratio", "1.56"},
            {"Alpha", "-0.895"},
            {"Beta", "0.068"},
            {"Annual Standard Deviation", "0.09"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "-4.929"},
            {"Tracking Error", "0.164"},
            {"Treynor Ratio", "-13.276"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$49000000.00"},
            {"Fitness Score", "0.049"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-10.846"},
            {"Return Over Maximum Drawdown", "-4.904"},
            {"Portfolio Turnover", "58.921"},
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
            {"OrderListHash", "66f04c9622ab242993c8ce951418e6d9"}
        };
    }
}
