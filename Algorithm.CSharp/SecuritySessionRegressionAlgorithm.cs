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
using Common.Data.Market;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to validate Security.Session functionality.
    /// Verifies that daily session bars (Open, High, Low, Close, Volume) are correctly
    /// </summary>
    public class SecuritySessionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected bool SecurityWasRemoved { get; set; }
        protected decimal Open { get; set; }
        protected decimal High { get; set; }
        protected decimal Low { get; set; }
        protected decimal Close { get; set; }
        protected decimal Volume { get; set; }
        protected Equity Equity { get; set; }
        protected virtual Resolution Resolution => Resolution.Hour;
        protected virtual bool ExtendedMarketHours => false;
        private Symbol _symbol;
        private SessionBar _previousSessionBar;
        private DateTime _currentDate;

        /// <summary>
        /// Initialise the data
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            Equity = AddEquity("SPY", Resolution, extendedMarketHours: ExtendedMarketHours);
            _symbol = Equity.Symbol;
            Open = Close = High = Volume = 0;
            Low = decimal.MaxValue;
            _currentDate = StartDate;
            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketClose(_symbol, 1), ValidateSessionBars);
        }

        private void ValidateSessionBars()
        {
            var session = Equity.Session;
            // At this point the data was consolidated (market close)

            // Save previous session bar
            _previousSessionBar = new SessionBar(_currentDate, Open, High, Low, Close, Volume, 0);

            if (SecurityWasRemoved)
            {
                _previousSessionBar = null;
                SecurityWasRemoved = false;
                return;
            }

            // Check current session values
            if (session.Open != Open
                || session.High != High
                || session.Low != Low
                || session.Close != Close
                || session.Volume != Volume)
            {
                throw new RegressionTestException("Mismatch in current session bar (OHLCV)");
            }
        }

        public override void OnData(Slice slice)
        {
            if (!Equity.Exchange.Hours.IsOpen(slice.Time.AddTicks(-1), false))
            {
                return;
            }

            if (_currentDate.Date == slice.Time.Date)
            {
                // Same trading day → update ongoing session
                if (Open == 0)
                {
                    Open = slice[_symbol].Open;
                }
                High = Math.Max(High, slice[_symbol].High);
                Low = Math.Min(Low, slice[_symbol].Low);
                Close = slice[_symbol].Close;
                Volume += slice[_symbol].Volume;
            }
            else
            {
                // New trading day

                if (_previousSessionBar != null)
                {
                    var session = Equity.Session;
                    if (_previousSessionBar.Open != session[1].Open
                        || _previousSessionBar.High != session[1].High
                        || _previousSessionBar.Low != session[1].Low
                        || _previousSessionBar.Close != session[1].Close
                        || _previousSessionBar.Volume != session[1].Volume)
                    {
                        throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                    }
                }

                // This is the first data point of the new session
                Open = slice[_symbol].Open;
                Close = slice[_symbol].Close;
                High = slice[_symbol].High;
                Low = slice[_symbol].Low;
                Volume = slice[_symbol].Volume;
                _currentDate = slice.Time;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 78;

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
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
