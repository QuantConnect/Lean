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
using QuantConnect.Interfaces;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using a consolidator to check GetNextMarketClose() and GetNextMarketOpen()
    /// are returning the correct market close and open times
    /// </summary>
    public class FutureMarketOpenConsolidatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual bool ExtendedMarketHours => false;
        protected virtual List<DateTime> ExpectedOpens => new List<DateTime>()
        {
            new DateTime(2013, 10, 07, 9, 30, 0),
            new DateTime(2013, 10, 08, 9, 30, 0),
            new DateTime(2013, 10, 09, 9, 30, 0),
            new DateTime(2013, 10, 10, 9, 30, 0),
            new DateTime(2013, 10, 11, 9, 30, 0),
            new DateTime(2013, 10, 14, 9, 30, 0),
            new DateTime(2013, 10, 14, 9, 30, 0),
        };
        protected virtual List<DateTime> ExpectedCloses => new List<DateTime>()
        {
            new DateTime(2013, 10, 07, 17, 0, 0),
            new DateTime(2013, 10, 08, 17, 0, 0),
            new DateTime(2013, 10, 09, 17, 0, 0),
            new DateTime(2013, 10, 10, 17, 0, 0),
            new DateTime(2013, 10, 11, 17, 0, 0),
            new DateTime(2013, 10, 14, 17, 0, 0),
            new DateTime(2013, 10, 14, 17, 0, 0),
        };

        private Queue<DateTime> _expectedOpensQueue;
        private Queue<DateTime> _expectedClosesQueue;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 06);
            SetEndDate(2013, 10, 14);

            var es = AddSecurity(SecurityType.Future, "ES", extendedMarketHours: ExtendedMarketHours);

            _expectedOpensQueue = new Queue<DateTime>(ExpectedOpens);
            _expectedClosesQueue = new Queue<DateTime>(ExpectedCloses);

            Consolidate<BaseData>(es.Symbol, dataTime =>
            {
                // based on the given data time we return the start time of it's bar and the expected period size
                return LeanData.GetDailyCalendar(dataTime, es.Exchange, ExtendedMarketHours);
            }, bar => Assert(bar));
        }

        public void Assert(BaseData bar)
        {
            var open = _expectedOpensQueue.Dequeue();
            var close = _expectedClosesQueue.Dequeue();

            if (open != bar.Time || close != bar.EndTime)
            {
                throw new Exception($"Bar span was expected to be from {open} to {close}. " +
                    $"\n But was from {bar.Time} to {bar.EndTime}.");
            }

            Logging.Log.Debug($"Consolidator Event span. Start {bar.Time} End : {bar.EndTime}");
        }

        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 32073;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "-3.108"},
            {"Tracking Error", "0.163"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
