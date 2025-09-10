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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to validate <see cref="SecurityCache.Session"/> with Futures.
    /// Ensures OHLCV are consistent with Tick data.
    /// </summary>
    public class SecuritySessionWithFuturesRegressionAlgorithm : SecuritySessionRegressionAlgorithm
    {
        private decimal _bidPrice;
        private decimal _askPrice;
        private decimal _bidHigh;
        private decimal _bidLow;
        private decimal _askLow;
        private decimal _askHigh;
        private decimal _previousOpenInterest;

        public override void InitializeSecurity()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);

            Security = AddFuture(Futures.Metals.Gold, Resolution.Tick, extendedMarketHours: ExtendedMarketHours);
            _bidLow = decimal.MaxValue;
            _askLow = decimal.MaxValue;
        }

        protected override bool IsWithinMarketHours(DateTime currentDateTime)
        {
            return Security.Exchange.Hours.IsOpen(currentDateTime, false);
        }

        protected override void AccumulateSessionData(Slice slice)
        {
            var symbol = Security.Symbol;
            foreach (var tick in slice.Ticks[symbol])
            {
                if (tick.TickType == TickType.Trade)
                {
                    Volume += tick.Quantity;
                }
                if (CurrentDate.Date == tick.Time.Date)
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
                        if (Open == 0)
                        {
                            Open = midPrice;
                        }
                        Close = midPrice;
                    }
                    if (_bidHigh != 0 && _askHigh != 0)
                    {
                        High = Math.Max(High, (_bidHigh + _askHigh) / 2);
                    }
                    if (_bidLow != decimal.MaxValue && _askLow != decimal.MaxValue)
                    {
                        Low = Math.Min(Low, (_bidLow + _askLow) / 2);
                    }
                }
                else
                {
                    // New trading day
                    if (PreviousSessionBar != null)
                    {
                        var session = Security.Session;
                        if (PreviousSessionBar.Open != session[1].Open
                            || PreviousSessionBar.High != session[1].High
                            || PreviousSessionBar.Low != session[1].Low
                            || PreviousSessionBar.Close != session[1].Close
                            || PreviousSessionBar.Volume != session[1].Volume
                            || _previousOpenInterest != session[1].OpenInterest)
                        {
                            throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                        }
                    }

                    // This is the first data point of the new session
                    Open = (_bidPrice + _askPrice) / 2;
                    Low = decimal.MaxValue;
                    _bidLow = decimal.MaxValue;
                    _askLow = decimal.MaxValue;
                    Volume = 0;
                    CurrentDate = tick.Time.Date;
                }
            }
        }

        protected override void ValidateSessionBars()
        {
            // At this point the data was consolidated
            var session = Security.Session;

            // Save previous session bar
            PreviousSessionBar = new TradeBar(CurrentDate, Security.Symbol, Open, High, Low, Close, Volume);
            _previousOpenInterest = Security.OpenInterest;

            // Check current session values
            if (session.Open != Open
                || session.High != High
                || session.Low != Low
                || session.Close != Close
                || session.Volume != Volume
                || session.OpenInterest != Security.OpenInterest)
            {
                throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 180093;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
