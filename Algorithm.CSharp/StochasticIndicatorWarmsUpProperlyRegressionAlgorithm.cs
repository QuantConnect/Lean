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
    /// Regression algorithm that asserts Stochastic indicator, registered with a different resolution consolidator,
    /// is warmed up properly by calling QCAlgorithm.WarmUpIndicator
    /// </summary>
    public class StochasticIndicatorWarmsUpProperlyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _dataPointsReceived;
        private Symbol _spy;
        private RelativeStrengthIndex _rsi;
        private RelativeStrengthIndex _rsiHistory;
        private Stochastic _sto;
        private Stochastic _stoHistory;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 2, 1);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            var dailyConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            _rsi = new RelativeStrengthIndex(14, MovingAverageType.Wilders);
            _sto = new Stochastic("FIRST", 10, 3, 3);
            RegisterIndicator(_spy, _rsi, dailyConsolidator);
            RegisterIndicator(_spy, _sto, dailyConsolidator);

            WarmUpIndicator(_spy, _rsi, TimeSpan.FromDays(1));
            WarmUpIndicator(_spy, _sto, TimeSpan.FromDays(1));

            _rsiHistory = new RelativeStrengthIndex(14, MovingAverageType.Wilders);
            _stoHistory = new Stochastic("SECOND", 10, 3, 3);
            RegisterIndicator(_spy, _rsiHistory, dailyConsolidator);
            RegisterIndicator(_spy, _stoHistory, dailyConsolidator);

            var history = History(_spy, Math.Max(_rsiHistory.WarmUpPeriod, _stoHistory.WarmUpPeriod), Resolution.Daily);

            // Warm up RSI indicator
            foreach (var bar in history)
            {
                _rsiHistory.Update(bar.EndTime, bar.Close);
            }

            // Warm up STO indicator
            foreach (var bar in history.TakeLast(_stoHistory.WarmUpPeriod))
            {
                _stoHistory.Update(bar);
            }

            var indicators = new List<IIndicator>() { _rsi, _sto, _rsiHistory, _stoHistory };

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

                if (_rsi.Current.Value != _rsiHistory.Current.Value)
                {
                    throw new RegressionTestException($"Values of indicators differ: {_rsi.Name}: {_rsi.Current.Value} | {_rsiHistory.Name}: {_rsiHistory.Current.Value}");
                }

                if (_sto.StochK.Current.Value != _stoHistory.StochK.Current.Value)
                {
                    throw new RegressionTestException($"Stoch K values of indicators differ: {_sto.Name}.StochK: {_sto.StochK.Current.Value} | {_stoHistory.Name}.StochK: {_stoHistory.StochK.Current.Value}");
                }

                if (_sto.StochD.Current.Value != _stoHistory.StochD.Current.Value)
                {
                    throw new RegressionTestException($"Stoch D values of indicators differ: {_sto.Name}.StochD: {_sto.StochD.Current.Value} | {_stoHistory.Name}.StochD: {_stoHistory.StochD.Current.Value}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_dataPointsReceived)
            {
                throw new Exception("No data points received");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 302;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 44;

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
