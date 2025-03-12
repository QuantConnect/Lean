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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression test ensures that the Stochastic indicator and its sub-indicators  
    /// are properly initialized, warmed up, and returning meaningful values.  
    /// It verifies that they do not return zero after warm-up.
    /// </summary>
    public class StochasticIndicatorAndSubIndicatorsWarmUpRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _dataPointsReceived;
        private Symbol _spy;
        private Stochastic _stochasticIndicator;
        private Stochastic _stochasticHistory;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 2, 1);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            var dailyConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            _stochasticIndicator = new Stochastic("FIRST", 14, 3, 3);
            RegisterIndicator(_spy, _stochasticIndicator, dailyConsolidator);

            WarmUpIndicator(_spy, _stochasticIndicator, TimeSpan.FromDays(1));

            _stochasticHistory = new Stochastic("SECOND", 14, 3, 3);
            RegisterIndicator(_spy, _stochasticHistory, dailyConsolidator);

            // The warm-up period for the Stochastic indicator is calculated as:
            // period + kPeriod + dPeriod - 2 = 14 + 3 + 3 - 2 = 18
            // To ensure the indicator is fully warmed up, we request a history length
            // significantly greater than 18.
            var periods = 50;
            // Get historical data for warming up the stochasticHistory
            var history = History(_spy, periods, Resolution.Daily);

            // Warm up STO indicator
            foreach (var bar in history)
            {
                _stochasticHistory.Update(bar);
            }

            var indicators = new List<IIndicator>() { _stochasticIndicator, _stochasticHistory };

            // Ensure both indicators are ready
            foreach (var indicator in indicators)
            {
                if (!indicator.IsReady)
                {
                    throw new RegressionTestException($"{indicator.Name} should be ready, but it is not. Number of samples: {indicator.Samples}");
                }
            }

        }

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp) return;

            if (slice.ContainsKey(_spy))
            {
                _dataPointsReceived = true;
                if (_stochasticIndicator.StochK.Current.Value == decimal.Zero || _stochasticHistory.StochK.Current.Value == decimal.Zero || _stochasticIndicator.FastStoch.Current.Value == decimal.Zero)
                {
                    throw new RegressionTestException("The stochastic indicators should be ready by now and start returning values different from zero.");
                }

                if (_stochasticIndicator.StochK.Current.Value != _stochasticHistory.StochK.Current.Value)
                {
                    throw new RegressionTestException($"Stoch K values of indicators differ: {_stochasticIndicator.Name}.StochK: {_stochasticIndicator.StochK.Current.Value} | {_stochasticHistory.Name}.StochK: {_stochasticHistory.StochK.Current.Value}");
                }

                if (_stochasticIndicator.StochD.Current.Value != _stochasticHistory.StochD.Current.Value)
                {
                    throw new RegressionTestException($"Stoch D values of indicators differ: {_stochasticIndicator.Name}.StochD: {_stochasticIndicator.StochD.Current.Value} | {_stochasticHistory.Name}.StochD: {_stochasticHistory.StochD.Current.Value}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Ensure that at least one data point was received
            if (!_dataPointsReceived)
            {
                throw new RegressionTestException("No data points received");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 302;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 68;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.016"},
            {"Tracking Error", "0.101"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
