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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which reproduces GH issue 4131, we assert order events are executed in order
    /// event outside market ours
    /// </summary>
    public class ScheduledEventsOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _scheduledEventCount;
        private Symbol _spy;
        private DateTime _lastTime = DateTime.MinValue;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            _spy = AddEquity("SPY").Symbol;

            var test = 0;
            var dateRule = DateRules.EveryDay(_spy);

            var aEventCount = 0;
            var bEventCount = 0;
            var cEventCount = 0;

            // we add each twice and assert the order in which they are added is also respected for events at the same time
            for (var i = 0; i < 2; i++)
            {
                var id = i;
                Schedule.On(dateRule, TimeRules.At(9, 25), (name, time) =>
                {
                    // for id 0 event count should always be 0, for id 1 should be 1
                    if (aEventCount != id)
                    {
                        throw new Exception($"Scheduled event triggered out of order: {Time} expected id {id} but was {aEventCount}");
                    }
                    aEventCount++;
                    // goes from 0 to 1
                    aEventCount %= 2;
                    AssertScheduledEventTime();
                    Debug($"{Time} :: Test: {test}"); test++;
                });
                Schedule.On(dateRule, TimeRules.BeforeMarketClose(_spy, 5), (name, time) =>
                {
                    // for id 0 event count should always be 0, for id 1 should be 1
                    if (bEventCount != id)
                    {
                        throw new Exception($"Scheduled event triggered out of order: {Time} expected id {id} but was {bEventCount}");
                    }
                    bEventCount++;
                    // goes from 0 to 1
                    bEventCount %= 2;
                    AssertScheduledEventTime();
                    Debug($"{Time} :: Test: {test}"); test++;
                });
                Schedule.On(dateRule, TimeRules.At(16, 5), (name, time) =>
                {
                    // for id 0 event count should always be 0, for id 1 should be 1
                    if (cEventCount != id)
                    {
                        throw new Exception($"Scheduled event triggered out of order: {Time} expected id {id} but was {cEventCount}");
                    }
                    cEventCount++;
                    // goes from 0 to 1
                    cEventCount %= 2;
                    AssertScheduledEventTime();
                    Debug($"{Time} :: Test: {test}"); test = 0;
                });
            }
        }

        private void AssertScheduledEventTime()
        {
            if (_lastTime > Time)
            {
                throw new Exception($"Scheduled event time shouldn't go backwards, last time {_lastTime}, current {Time}");
            }
            _lastTime = Time;
            _scheduledEventCount++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_scheduledEventCount != 28)
            {
                throw new Exception($"OnEndOfAlgorithm expected scheduled events but was {_scheduledEventCount}");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "264.819%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.668%"},
            {"Sharpe Ratio", "8.749"},
            {"Probabilistic Sharpe Ratio", "67.311%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.005"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.219"},
            {"Annual Variance", "0.048"},
            {"Information Ratio", "-14.189"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.922"},
            {"Total Fees", "$3.26"},
            {"Fitness Score", "0.248"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "93.761"},
            {"Portfolio Turnover", "0.248"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "25885f979ca8c7b44f5d0f7daf00b241"}
        };
    }
}
