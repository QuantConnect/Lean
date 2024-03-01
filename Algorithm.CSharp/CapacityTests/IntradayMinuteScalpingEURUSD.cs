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
    /// Scalps EURUSD using an EMA cross strategy at minute resolution.
    /// This tests FOREX strategies that trade at a higher frequency, which
    /// should have a reduced capacity estimate as a result.
    /// </summary>
    public class IntradayMinuteScalpingEURUSD : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _eurusd;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;


        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 30);
            SetCash(100000);
            SetWarmup(100);

            _eurusd = AddForex("EURUSD", Resolution.Minute, Market.Oanda).Symbol;
            _fast = EMA(_eurusd, 20);
            _slow = EMA(_eurusd, 40);
        }

        public override void OnData(Slice data)
        {
            if (Portfolio[_eurusd].Quantity <= 0 && _fast > _slow)
            {
                SetHoldings(_eurusd, 1);
            }
            else if (Portfolio[_eurusd].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(_eurusd, -1);
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
            {"Total Orders", "671"},
            {"Average Win", "0.07%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "-80.820%"},
            {"Drawdown", "12.200%"},
            {"Expectancy", "-0.447"},
            {"Net Profit", "-12.180%"},
            {"Sharpe Ratio", "-13.121"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "79%"},
            {"Win Rate", "21%"},
            {"Profit-Loss Ratio", "1.61"},
            {"Alpha", "-0.746"},
            {"Beta", "-0.02"},
            {"Annual Standard Deviation", "0.057"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-4.046"},
            {"Tracking Error", "0.161"},
            {"Treynor Ratio", "37.346"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$44000000.00"},
            {"Fitness Score", "0.025"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-16.609"},
            {"Return Over Maximum Drawdown", "-7.115"},
            {"Portfolio Turnover", "52.476"},
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
            {"OrderListHash", "74ee44736b9300c0262dc75c0cd140e1"}
        };
    }
}
