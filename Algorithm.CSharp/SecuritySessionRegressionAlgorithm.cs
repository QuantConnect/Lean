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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to validate Security.Session functionality.
    /// Verifies that daily session bars (Open, High, Low, Close, Volume) are correctly
    /// </summary>
    public class SecuritySessionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected int ProcessedDataCount { get; set; }
        protected bool SecurityWasRemoved { get; set; }
        protected decimal Open { get; set; }
        protected decimal High { get; set; }
        protected decimal Low { get; set; }
        protected decimal Close { get; set; }
        protected decimal Volume { get; set; }
        protected Security Security { get; set; }
        protected virtual Resolution Resolution => Resolution.Hour;
        protected virtual bool DailyPreciseEndTime => true;
        protected virtual bool ExtendedMarketHours => false;
        protected TradeBar PreviousSessionBar { get; set; }
        protected DateTime CurrentDate { get; set; }

        /// <summary>
        /// Initialise the data
        /// </summary>
        public override void Initialize()
        {
            AddSecurityInitializer((security) =>
            {
                // activate session tracking
                security.Session.Size = 3;
            });
            Settings.DailyPreciseEndTime = DailyPreciseEndTime;
            InitializeSecurity();

            // Check initial session values
            var session = Security.Session;
            if (session == null)
            {
                throw new RegressionTestException("Security.Session is null");
            }
            if (session.Open != 0
                || session.High != 0
                || session.Low != 0
                || session.Close != 0
                || session.Volume != 0
                || session.OpenInterest != 0)
            {
                throw new RegressionTestException("Session should start with all zero values.");
            }
            ProcessedDataCount = 0;
            Low = decimal.MaxValue;
            CurrentDate = StartDate;
            ConfigureSchedule();
        }

        public virtual void InitializeSecurity()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            Security = AddEquity("SPY", Resolution, extendedMarketHours: ExtendedMarketHours);
        }

        protected virtual void ConfigureSchedule()
        {
            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketClose(Security.Symbol, 1), ValidateSessionBars);
        }

        protected virtual void ValidateSessionBars()
        {
            if (ProcessedDataCount == 0)
            {
                return;
            }
            var session = Security.Session;
            // At this point the data was consolidated (market close)

            // Save previous session bar
            PreviousSessionBar = new TradeBar(CurrentDate, Security.Symbol, Open, High, Low, Close, Volume);

            if (SecurityWasRemoved)
            {
                PreviousSessionBar = null;
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

        protected virtual bool IsWithinMarketHours(DateTime currentDateTime)
        {
            var marketOpen = Security.Exchange.Hours.GetNextMarketOpen(currentDateTime.Date, false).TimeOfDay;
            var marketClose = Security.Exchange.Hours.GetNextMarketClose(currentDateTime.Date, false).TimeOfDay;
            var currentTime = currentDateTime.TimeOfDay;
            return (marketOpen < currentTime && currentTime <= marketClose) || (!DailyPreciseEndTime && Resolution == Resolution.Daily);
        }

        public override void OnData(Slice slice)
        {
            if (ProcessedDataCount == 0)
            {
                CurrentDate = slice.Time;
            }
            if (!IsWithinMarketHours(slice.Time) || !slice.ContainsKey(Security.Symbol))
            {
                // Skip data outside market hours
                return;
            }

            // Accumulate data within regular market hours
            // to later compare against the Session values
            AccumulateSessionData(slice);
            ProcessedDataCount++;
        }

        protected virtual void AccumulateSessionData(Slice slice)
        {
            var symbol = Security.Symbol;
            if (CurrentDate.Date == slice.Time.Date)
            {
                // Same trading day
                if (Open == 0)
                {
                    Open = slice[symbol].Open;
                }
                High = Math.Max(High, slice[symbol].High);
                Low = Math.Min(Low, slice[symbol].Low);
                Close = slice[symbol].Close;
                Volume += slice[symbol].Volume;
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
                        || PreviousSessionBar.Volume != session[1].Volume)
                    {
                        throw new RegressionTestException("Mismatch in previous session bar (OHLCV)");
                    }
                }

                // This is the first data point of the new session
                Open = slice[symbol].Open;
                Close = slice[symbol].Close;
                High = slice[symbol].High;
                Low = slice[symbol].Low;
                Volume = slice[symbol].Volume;
                CurrentDate = slice.Time;
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
        public virtual int AlgorithmHistoryDataPoints => 0;

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
