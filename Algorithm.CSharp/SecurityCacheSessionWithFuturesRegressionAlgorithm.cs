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
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to validate <see cref="SecurityCache.Session"/> with Futures.
    /// Ensures OHLCV + OpenInterest are consistent with Tick data.
    /// </summary>
    public class SecurityCacheSessionWithFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private decimal _open;
        private decimal _high;
        private decimal _low;
        private decimal _close;
        private decimal _volume;
        private decimal _bidPrice;
        private decimal _askPrice;
        private decimal _bidHigh;
        private decimal _bidLow;
        private decimal _askLow;
        private decimal _askHigh;
        private decimal _openInterest;
        private Future _future;
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
            SetEndDate(2013, 10, 09);

            _future = AddFuture(Futures.Metals.Gold, Resolution.Tick);
            _symbol = _future.Symbol;

            _low = decimal.MaxValue;
            _bidLow = decimal.MaxValue;
            _askLow = decimal.MaxValue;
            _currentDate = StartDate;
            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketOpen(_future.Symbol, 1), ValidateSessionBars);
        }

        private void ValidateSessionBars()
        {
            var session = _future.Session;

            // Check current session values
            if (session.IsTradingDayDataReady)
            {
                if (_sessionBar == null
                    || _sessionBar.Open != session.Open
                    || _sessionBar.High != session.High
                    || _sessionBar.Low != session.Low
                    || _sessionBar.Close != session.Close
                    || _sessionBar.Volume != session.Volume
                    || _sessionBar.OpenInterest != session.OpenInterest)
                {
                    throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
                }
            }

            // Check previous session values
            if (_previousSessionBar != null)
            {
                if (_previousSessionBar.Open != session[1].Open
                    || _previousSessionBar.High != session[1].High
                    || _previousSessionBar.Low != session[1].Low
                    || _previousSessionBar.Close != session[1].Close
                    || _previousSessionBar.Volume != session[1].Volume
                    || _previousSessionBar.OpenInterest != session[1].OpenInterest)
                {
                    throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                }
            }

        }

        public override void OnData(Slice slice)
        {
            foreach (var tick in slice.Ticks[_symbol])
            {
                if (tick.TickType == TickType.Trade)
                {
                    _volume += tick.Quantity;
                } else if (tick.TickType == TickType.OpenInterest)
                {
                    _openInterest = tick.Value;
                }
                if (_currentDate.Date == tick.Time.Date)
                {
                    if (tick.BidPrice != 0)
                    {
                        _bidPrice = tick.BidPrice;
                        _bidLow = Math.Min(_bidLow, tick.BidPrice);
                        _bidHigh = Math.Max(_bidHigh, tick.BidPrice);
                    }
                    if (tick.AskPrice != 0)
                    {
                        _askPrice = tick.AskPrice;
                        _askLow = Math.Min(_askLow, tick.AskPrice);
                        _askHigh = Math.Max(_askHigh, tick.AskPrice);
                    }
                    if (_bidPrice != 0 && _askPrice != 0)
                    {
                        var midPrice = (_bidPrice + _askPrice) / 2;
                        if (_open == 0)
                        {
                            _open = midPrice;
                        }
                        _close = midPrice;
                    }
                    if (_bidHigh != 0 && _askHigh != 0)
                    {
                        _high = Math.Max(_high, (_bidHigh + _askHigh) / 2);
                    }
                    if (_bidLow != decimal.MaxValue && _askLow != decimal.MaxValue)
                    {
                        _low = Math.Min(_low, (_bidLow + _askLow) / 2);
                    }
                }
                else
                {
                    // New trading day

                    // Save previous session bar
                    _previousSessionBar = _sessionBar;

                    // Create new session bar
                    _sessionBar = new SessionBar(
                        _currentDate,
                        _open,
                        _high,
                        _low,
                        _close,
                        _volume,
                        _openInterest
                    );

                    // This is the first data point of the new session
                    _open = (_bidPrice + _askPrice) / 2;
                    _low = decimal.MaxValue;
                    _bidLow = decimal.MaxValue;
                    _askLow = decimal.MaxValue;
                    _volume = 0;
                    _currentDate = tick.Time.Date;
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
        public long DataPoints => 311432;

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
            {"Information Ratio", "5.524"},
            {"Tracking Error", "0.136"},
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
