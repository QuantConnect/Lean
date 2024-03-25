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

using System;
using System.Collections.Generic;
using System.Linq;

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the <see cref="DataNormalizationMode.ScaledRaw"/> data normalization mode is allowed history requests and
    /// that prices are adjusted to the last factor before the history end date.
    /// </summary>
    public class ScaledRawHistoryAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;

        private DateTime _lastSplitOrDividendDate;

        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(100000);
            SetBenchmark(x => 0);

            _aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (slice.Splits.ContainsKey(_aapl))
            {
                _lastSplitOrDividendDate = slice.Splits[_aapl].Time;
            }

            if (slice.Dividends.ContainsKey(_aapl))
            {
                _lastSplitOrDividendDate = slice.Dividends[_aapl].Time;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_lastSplitOrDividendDate == DateTime.MinValue)
            {
                throw new Exception("No split or dividend was found in the algorithm.");
            }

            var start = Time.AddMonths(-18);
            var end = Time;
            var rawHistory = History(new[] { _aapl }, start, end, dataNormalizationMode: DataNormalizationMode.Raw).ToList();
            var scaledRawHistory = History(new[] { _aapl }, start, end, dataNormalizationMode: DataNormalizationMode.ScaledRaw).ToList();

            if (rawHistory.Count == 0 || scaledRawHistory.Count != rawHistory.Count)
            {
                throw new Exception($@"Expected history results to not be empty and have the same count. Raw: {rawHistory.Count
                    }, ScaledRaw: {scaledRawHistory.Count}");
            }

            for (var i = 0; i < rawHistory.Count; i++)
            {
                var rawBar = rawHistory[i].Bars[_aapl];
                var scaledRawBar = scaledRawHistory[i].Bars[_aapl];

                if (rawBar.Time < _lastSplitOrDividendDate)
                {
                    if (rawBar.Open == scaledRawBar.Open || rawBar.High == scaledRawBar.High || rawBar.Low == scaledRawBar.Low || rawBar.Close == scaledRawBar.Close)
                    {
                        throw new Exception($@"Expected history results to be different at {rawBar.Time
                            } before the last split or dividend date {_lastSplitOrDividendDate}");
                    }
                }
                else if (rawBar.Open != scaledRawBar.Open || rawBar.High != scaledRawBar.High || rawBar.Low != scaledRawBar.Low || rawBar.Close != scaledRawBar.Close)
                {
                    throw new Exception($@"Expected history results to be the same at {rawBar.Time} after the last split or dividend date {_lastSplitOrDividendDate}");
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 516;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 760;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
