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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to validate <see cref="SecurityCache.Session"/> functionality.
    /// Verifies that daily session bars (Open, High, Low, Close, Volume) are correctly
    /// </summary>
    public class SecurityCacheSessionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private decimal _open = 0;
        private decimal _high = 0;
        private decimal _low = 0;
        private decimal _close = 0;
        private decimal _volume = 0;
        private Equity _equity;
        private Symbol _symbol;
        private SessionBar _sessionBar;
        private SessionBar _previousSessionBar;
        private DateTime _currentDate;

        /// <summary>
        /// Initialise the data
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            _equity = AddEquity("SPY", Resolution.Hour);
            _symbol = _equity.Symbol;
            _open = _close = _high = _volume = 0;
            _low = decimal.MaxValue;
            _currentDate = StartDate;
        }

        public override void OnData(Slice slice)
        {
            if (_currentDate.Date == slice.Time.Date)
            {
                // Same trading day → update ongoing session
                if (_open == 0)
                {
                    _open = slice[_symbol].Open;
                }
                _high = Math.Max(_high, slice[_symbol].High);
                _low = Math.Min(_low, slice[_symbol].Low);
                _close = slice[_symbol].Close;
                _volume += slice[_symbol].Volume;
            }
            else
            {
                // New trading day

                // Save previous session bar
                _previousSessionBar = _sessionBar;

                // Create new session bar
                var session = _equity.Session;
                _sessionBar = new SessionBar(_currentDate, _open, _high, _low, _close, _volume, 0);

                // This is the first data point of the new session
                _open = slice[_symbol].Open;
                _close = slice[_symbol].Close;
                _high = slice[_symbol].High;
                _low = slice[_symbol].Low;
                _volume = slice[_symbol].Volume;
                _currentDate = slice.Time;
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            var session = _equity.Session;
            if (session.IsTradingDayDataReady)
            {
                if (session.Open != _open
                    || session.High != _high
                    || session.Low != _low
                    || session.Close != _close
                    || session.Volume != _volume)
                {
                    throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
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
        public long DataPoints => 78;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
