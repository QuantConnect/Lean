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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that consolidators expose a built-in rolling window
    /// </summary>
    public class ConsolidatorRollingWindowRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private TradeBarConsolidator _consolidator;
        private int _consolidationCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            AddEquity("SPY", Resolution.Minute);

            _consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(10));
            _consolidator.DataConsolidated += OnDataConsolidated;
            SubscriptionManager.AddConsolidator("SPY", _consolidator);
        }

        private void OnDataConsolidated(object sender, TradeBar bar)
        {
            _consolidationCount++;

            // Window[0] must always be the bar just consolidated
            var currentBar = (TradeBar)_consolidator[0];
            if (currentBar.Time != bar.Time)
            {
                throw new RegressionTestException($"Expected consolidator[0].Time == {bar.Time} but was {currentBar.Time}");
            }
            if (currentBar.Close != bar.Close)
            {
                throw new RegressionTestException($"Expected consolidator[0].Close == {bar.Close} but was {currentBar.Close}");
            }

            // After the second consolidation the previous bar must be accessible at index 1
            if (_consolidator.Window.Count >= 2)
            {
                var previous = (TradeBar)_consolidator[1];
                if (previous.Time >= bar.Time)
                {
                    throw new RegressionTestException($"consolidator[1].Time ({previous.Time}) should be earlier than consolidator[0].Time ({bar.Time})");
                }
                if (previous.Close <= 0)
                {
                    throw new RegressionTestException("consolidator[1].Close should be greater than zero");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_consolidationCount == 0)
            {
                throw new RegressionTestException("Expected at least one consolidation but got zero");
            }

            // Default window size is 2, it must be full
            if (_consolidator.Window.Count != 2)
            {
                throw new RegressionTestException(
                    $"Expected window count of 2 but was {_consolidator.Window.Count}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

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
