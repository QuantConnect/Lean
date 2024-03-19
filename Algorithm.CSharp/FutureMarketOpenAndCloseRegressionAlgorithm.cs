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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to check we are getting the correct market open and close times
    /// </summary>
    public class FutureMarketOpenAndCloseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual bool ExtendedMarketHours => false;

        protected virtual List<DateTime> AfterMarketOpen => new List<DateTime>() {
            new DateTime(2020, 02, 04, 9, 30, 0),
            new DateTime(2020, 02, 05, 9, 30, 0),
            new DateTime(2020, 02, 06, 9, 30, 0),
            new DateTime(2020, 02, 07, 9, 30, 0),
            new DateTime(2020, 02, 10, 9, 30, 0),
            new DateTime(2020, 02, 11, 9, 30, 0)
        };
        protected virtual List<DateTime> BeforeMarketClose => new List<DateTime>()
        {
            new DateTime(2020, 02, 04, 17, 0, 0),
            new DateTime(2020, 02, 05, 17, 0, 0),
            new DateTime(2020, 02, 06, 17, 0, 0),
            new DateTime(2020, 02, 07, 17, 0, 0),
            new DateTime(2020, 02, 10, 17, 0, 0),
            new DateTime(2020, 02, 11, 17, 0, 0)
        };
        private Queue<DateTime> _afterMarketOpenQueue;
        private Queue<DateTime> _beforeMarketCloseQueue;

        public override void Initialize()
        {
            SetStartDate(2020, 02, 04);
            SetEndDate(2020, 02, 11);
            var esFuture = AddFuture("ES", extendedMarketHours: ExtendedMarketHours).Symbol;

            _afterMarketOpenQueue = new Queue<DateTime>(AfterMarketOpen);
            _beforeMarketCloseQueue = new Queue<DateTime>(BeforeMarketClose);

            Schedule.On(DateRules.EveryDay(esFuture),
                TimeRules.AfterMarketOpen(esFuture, extendedMarketOpen: ExtendedMarketHours),
                EveryDayAfterMarketOpen);

            Schedule.On(DateRules.EveryDay(esFuture),
                TimeRules.BeforeMarketClose(esFuture, extendedMarketClose: ExtendedMarketHours),
                EveryDayBeforeMarketClose);
        }

        public void EveryDayBeforeMarketClose()
        {
            var expectedMarketClose = _beforeMarketCloseQueue.Dequeue();
            if (Time != expectedMarketClose)
            {
                throw new Exception($"Expected market close date was {expectedMarketClose} but received {Time}");
            }
        }

        public void EveryDayAfterMarketOpen()
        {
            var expectedMarketOpen = _afterMarketOpenQueue.Dequeue();
            if (Time != expectedMarketOpen)
            {
                throw new Exception($"Expected market open date was {expectedMarketOpen} but received {Time}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_afterMarketOpenQueue.Any() || _beforeMarketCloseQueue.Any())
            {
                throw new Exception($"_afterMarketOpenQueue and _beforeMarketCloseQueue should be empty");
            }
        }
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 13587;

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
            {"Information Ratio", "-11.049"},
            {"Tracking Error", "0.087"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
