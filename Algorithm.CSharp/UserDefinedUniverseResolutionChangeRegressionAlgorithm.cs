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
    /// Regression algorithm asserting that adding a new resolution to a security already in the user defined universe
    /// creates its data subscription, and that removing and re-adding a security at a new resolution in the same time step
    /// replaces the old data subscription with the new one
    /// </summary>
    public class UserDefinedUniverseResolutionChangeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aig;
        private Symbol _bac;
        private DateTime _switchTime;
        private int _aigTicks;
        private int _aigBarsAfterSwitch;
        private int _bacTicks;
        private int _bacBarsAfterSwitch;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            _aig = AddEquity("AIG", Resolution.Minute).Symbol;
            _bac = AddEquity("BAC", Resolution.Minute).Symbol;

            Schedule.On(DateRules.EveryDay(_aig), TimeRules.At(15, 0), () =>
            {
                // AIG keeps its minute subscription and adds a tick one
                AddEquity("AIG", Resolution.Tick);

                // BAC is replaced by a tick subscription in the same time step
                RemoveSecurity(_bac);
                AddEquity("BAC", Resolution.Tick);

                _switchTime = Time;
            });
        }

        public override void OnData(Slice slice)
        {
            if (_switchTime == default)
            {
                return;
            }

            if (slice.Ticks.TryGetValue(_aig, out var aigTicks))
            {
                _aigTicks += aigTicks.Count;
            }
            if (slice.Ticks.TryGetValue(_bac, out var bacTicks))
            {
                _bacTicks += bacTicks.Count;
            }

            // allow the bar emitted in the same minute as the switch
            if (Time > _switchTime.AddMinutes(1))
            {
                if (slice.Bars.ContainsKey(_aig))
                {
                    _aigBarsAfterSwitch++;
                }
                if (slice.Bars.ContainsKey(_bac))
                {
                    _bacBarsAfterSwitch++;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_switchTime == default)
            {
                throw new RegressionTestException("The resolution switch was never scheduled");
            }
            if (_aigTicks == 0)
            {
                throw new RegressionTestException("Expected AIG ticks after adding the tick resolution");
            }
            if (_aigBarsAfterSwitch == 0)
            {
                throw new RegressionTestException("Expected AIG minute bars to keep arriving after adding the tick resolution");
            }
            if (_bacTicks == 0)
            {
                throw new RegressionTestException("Expected BAC ticks after re-adding it at tick resolution");
            }
            if (_bacBarsAfterSwitch != 0)
            {
                throw new RegressionTestException($"Expected no BAC minute bars after it was replaced by ticks, got {_bacBarsAfterSwitch}");
            }
            if (!Securities[_bac].IsTradable)
            {
                throw new RegressionTestException("Expected BAC to be tradable after re-adding it");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 307906;

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
