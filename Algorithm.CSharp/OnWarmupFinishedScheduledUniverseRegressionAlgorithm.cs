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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting OnWarmupFinished fires at StartDate (midnight)
    /// when using a ScheduledUniverseSelectionModel that triggers at 8 AM, skipping midnight entirely.
    /// </summary>
    public class OnWarmupFinishedScheduledUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _onWarmupFinishedCalled;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Minute;
            SetWarmup(TimeSpan.FromDays(1));

            // Universe triggers at 8 AM
            SetUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(),
                TimeRules.At(8, 0),
                _ => new[] { QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA) }
            ));
        }

        public override void OnWarmupFinished()
        {
            _onWarmupFinishedCalled = true;

            if (Time != StartDate)
            {
                throw new RegressionTestException(
                    $"Expected OnWarmupFinished to fire at StartDate ({StartDate:yyyy-MM-dd HH:mm:ss}), " +
                    $"but fired at {Time:yyyy-MM-dd HH:mm:ss}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_onWarmupFinishedCalled)
            {
                throw new RegressionTestException("OnWarmupFinished was never called");
            }
        }

        public bool CanRunLocally { get; } = true;

        public List<Language> Languages { get; } = new() { Language.CSharp };

        public long DataPoints => 3948;

        public int AlgorithmHistoryDataPoints => 0;

        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

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
            {"Information Ratio", "-31.448"},
            {"Tracking Error", "0.164"},
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
