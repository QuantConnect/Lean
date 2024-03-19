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
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue 1046. Where scheduled events wouldn't work during warmup
    /// </summary>
    public class WarmupScheduledEventsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Queue<DateTime> _onEndOfDayScheduledEvents = new(new[]
        {
            new DateTime(2013, 10, 04, 15, 50, 0),
            new DateTime(2013, 10, 07, 15, 50, 0),

            new DateTime(2013, 10, 08, 15, 50, 0),
        });

        private Queue<DateTime> _scheduledEvents = new (new[]
        {
            new DateTime(2013, 10, 04, 18, 0, 0),
            new DateTime(2013, 10, 05, 0, 0, 0),
            new DateTime(2013, 10, 05, 6, 0, 0),
            new DateTime(2013, 10, 05, 12, 0, 0),
            new DateTime(2013, 10, 05, 18, 0, 0),
            new DateTime(2013, 10, 06, 0, 0, 0),
            new DateTime(2013, 10, 06, 6, 0, 0),
            new DateTime(2013, 10, 06, 12, 0, 0),
            new DateTime(2013, 10, 06, 18, 0, 0),
            new DateTime(2013, 10, 07, 0, 0, 0),
            new DateTime(2013, 10, 07, 6, 0, 0),
            new DateTime(2013, 10, 07, 12, 0, 0),
            new DateTime(2013, 10, 07, 18, 0, 0),

            new DateTime(2013, 10, 08, 0, 0, 0),
            new DateTime(2013, 10, 08, 6, 0, 0),
            new DateTime(2013, 10, 08, 12, 0, 0)
        });

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 08);

            AddEquity("SPY", Resolution.Minute, fillForward: false);

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromHours(6)), () =>
            {
                Debug($"Scheduled event happening at {Time}. IsWarmingUp: {IsWarmingUp}");
                if (!LiveMode)
                {
                    var expected = _scheduledEvents.Dequeue();
                    if (expected != Time)
                    {
                        throw new Exception($"Unexpected scheduled event time: {Time}. Expected {expected}");
                    }

                    if (expected.Day > 7 && IsWarmingUp)
                    {
                        throw new Exception("Algorithm should be warming up on the 7th!");
                    }
                }
            });

            SetWarmUp(9, Resolution.Hour);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_scheduledEvents.Count != 0)
            {
                throw new Exception("Some scheduled event was not fired!");
            }
            if (_onEndOfDayScheduledEvents.Count != 0)
            {
                throw new Exception("Some OnEndOfDay scheduled event was not fired!");
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            Debug($"OnEndOfDay scheduled event happening at {Time}. IsWarmingUp: {IsWarmingUp}");
            var expected = _onEndOfDayScheduledEvents.Dequeue();
            if (expected != Time)
            {
                throw new Exception($"Unexpected OnEndOfDay scheduled event time: {Time}. Expected {expected}");
            }
            if (expected.Day > 7 && IsWarmingUp)
            {
                throw new Exception("Algorithm should be warming up on the 7th!");
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
        public virtual long DataPoints => 819;

        /// <summary>
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
