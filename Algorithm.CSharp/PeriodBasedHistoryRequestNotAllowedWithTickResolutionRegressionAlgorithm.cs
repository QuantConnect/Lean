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
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for testing that period-based history requests are not allowed with tick resolution
    /// </summary>
    public class PeriodBasedHistoryRequestNotAllowedWithTickResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 09);

            var spy = AddEquity("SPY", Resolution.Tick).Symbol;

            // Tick resolution is not allowed for period-based history requests
            AssertThatHistoryThrowsForTickResolution(() => History<Tick>(spy, 1),
                "Tick history call with implicit tick resolution");
            AssertThatHistoryThrowsForTickResolution(() => History<Tick>(spy, 1, Resolution.Tick),
                "Tick history call with explicit tick resolution");
            AssertThatHistoryThrowsForTickResolution(() => History<Tick>(new [] { spy }, 1),
                "Tick history call with symbol array with implicit tick resolution");
            AssertThatHistoryThrowsForTickResolution(() => History<Tick>(new [] { spy }, 1, Resolution.Tick),
                "Tick history call with symbol array with explicit tick resolution");

            var history = History<Tick>(spy, TimeSpan.FromHours(12));
            if (history.Count() == 0)
            {
                throw new Exception("On history call with implicit tick resolution: history returned no results");
            }

            history = History<Tick>(spy, TimeSpan.FromHours(12), Resolution.Tick);
            if (history.Count() == 0)
            {
                throw new Exception("On history call with explicit tick resolution: history returned no results");
            }
        }

        private void AssertThatHistoryThrowsForTickResolution(Action historyCall, string historyCallDescription)
        {
            try
            {
                historyCall();
                throw new Exception($"{historyCallDescription}: expected an exception to be thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 7682413;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 2736238;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
