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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm that asserts Stochastic indicator, registered with a different resolution consolidator,
    /// is warmed up properly by calling QCAlgorithm.WarmUpIndicator
    /// </summary>
    public class ConsolidateHourBarsIntoDailyBarsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private RelativeStrengthIndex _rsi;
        private RelativeStrengthIndex _rsiTimeDelta;
        private Dictionary<DateTime, decimal> _values = new();
        private int _count;

        public override void Initialize()
        {
            SetStartDate(2020, 5, 1);
            SetEndDate(2020, 6, 5);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            _rsi = new RelativeStrengthIndex("FIRST", 15, MovingAverageType.Wilders);
            RegisterIndicator(_spy, _rsi, Resolution.Daily);

            _rsiTimeDelta = new RelativeStrengthIndex("SECOND" ,15, MovingAverageType.Wilders);
        }

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp) return;

            if (slice.ContainsKey(_spy) && slice[_spy] != null)
            {
                if (Time.Month == EndDate.Month)
                {
                    var history = History(_spy, _count, Resolution.Daily);
                    foreach (var bar in history)
                    {
                        _rsiTimeDelta.Update(bar.EndTime, bar.Close);
                        var time = bar.EndTime.Date;
                        if (_rsiTimeDelta.Current.Value != _values[time])
                        {
                            throw new Exception($"Both {_rsi.Name} and {_rsiTimeDelta.Name} should have the same values, but they differ. {_rsi.Name}: {_values[time]} | {_rsiTimeDelta.Name}: {_rsiTimeDelta.Current.Value}");
                        }
                    }
                    Quit();
                }
                else
                {
                    _values[Time.Date] = _rsi.Current.Value;
                    if (Time.Hour == 16)
                    {
                        _count++;
                    }
                }
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
        public long DataPoints => 290;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 20;

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
            {"Information Ratio", "-5.215"},
            {"Tracking Error", "0.159"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
