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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Scalps BTCETH using an EMA cross strategy at minute resolution.
    /// This tests crypto strategies that trade at a higher frequency, which
    /// should have a reduced capacity estimate as a result. This also tests
    /// that currency conversions are handled properly in the strategy capacity
    /// calculation class.
    /// </summary>
    public class IntradayMinuteScalpingBTCETH : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _ethbtc;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;


        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 30);
            SetCash(100000);
            SetWarmup(100);

            var ethbtc = AddCrypto("ETHBTC", Resolution.Minute, Market.GDAX);
            ethbtc.BuyingPowerModel = new BuyingPowerModel();
            _ethbtc = ethbtc.Symbol;

            _fast = EMA(_ethbtc, 20);
            _slow = EMA(_ethbtc, 40);
        }

        public override void OnData(Slice data)
        {
            if (Portfolio[_ethbtc].Quantity <= 0 && _fast > _slow)
            {
                SetHoldings(_ethbtc, 1);
            }
            else if (Portfolio[_ethbtc].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(_ethbtc, -1);
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
            {"Total Orders", "1005"},
            {"Average Win", "0.96%"},
            {"Average Loss", "-0.33%"},
            {"Compounding Annual Return", "76.267%"},
            {"Drawdown", "77.100%"},
            {"Expectancy", "-0.012"},
            {"Net Profit", "4.768%"},
            {"Sharpe Ratio", "1.01909630017278E+24"},
            {"Probabilistic Sharpe Ratio", "93.814%"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "2.95"},
            {"Alpha", "1.3466330963256E+25"},
            {"Beta", "25.59"},
            {"Annual Standard Deviation", "13.214"},
            {"Annual Variance", "174.61"},
            {"Information Ratio", "1.02164274756513E+24"},
            {"Tracking Error", "13.181"},
            {"Treynor Ratio", "5.2622435344112E+23"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$1300000.00"},
            {"Fitness Score", "0.38"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.239"},
            {"Return Over Maximum Drawdown", "-1.385"},
            {"Portfolio Turnover", "81.433"},
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
            {"OrderListHash", "6a779e7a8d12b4808845c75b88d43b3a"}
        };
    }
}
