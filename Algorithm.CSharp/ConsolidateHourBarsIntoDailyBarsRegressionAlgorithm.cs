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
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm asserts the consolidated US equity daily bars from the hour bars exactly matches
    /// the daily bars returned from the database
    /// </summary>
    public class ConsolidateHourBarsIntoDailyBarsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private RelativeStrengthIndex _rsi;
        private RelativeStrengthIndex _rsiTimeDelta;
        private Dictionary<DateTime, decimal> _values = new();
        private int _count;
        private bool _indicatorsCompared;

        public override void Initialize()
        {
            SetStartDate(2020, 5, 1);
            SetEndDate(2020, 6, 5);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            // We will use these two indicators to compare the daily consolidated bars equals
            // the ones returned from the database. We use this specific type of indicator as
            // it depends on its previous values. Thus, if at some point the bars received by
            // the indicators differ, so will their final values
            _rsi = new RelativeStrengthIndex("FIRST", 15, MovingAverageType.Wilders);
            RegisterIndicator(_spy, _rsi, Resolution.Daily, selector: (bar) =>
            {
                var tradeBar = (TradeBar)bar;
                return (tradeBar.Close + tradeBar.Open) / 2;
            });

            // We won't register this indicator as we will update it manually at the end of the
            // month, so that we can compare the values of the indicator that received consolidated
            // bars and the values of this one
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
                        var time = bar.EndTime.Date;
                        var average = (bar.Close + bar.Open) / 2;
                        _rsiTimeDelta.Update(bar.EndTime, average);
                        if (_rsiTimeDelta.Current.Value != _values[time])
                        {
                            throw new RegressionTestException($"Both {_rsi.Name} and {_rsiTimeDelta.Name} should have the same values, but they differ. {_rsi.Name}: {_values[time]} | {_rsiTimeDelta.Name}: {_rsiTimeDelta.Current.Value}");
                        }
                    }
                    _indicatorsCompared = true;
                    Quit();
                }
                else
                {
                    _values[Time.Date] = _rsi.Current.Value;

                    // Since the symbol resolution is hour and the symbol is equity, we know the last bar received in a day will
                    // be at the market close, this is 16h. We need to count how many daily bars were consolidated in order to know
                    // how many we need to request from the history
                    if (Time.Hour == 16)
                    {
                        _count++;
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_indicatorsCompared)
            {
                throw new RegressionTestException($"Indicators {_rsi.Name} and {_rsiTimeDelta.Name} should have been compared, but they were not. Please make sure the indicators are getting SPY data");
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
