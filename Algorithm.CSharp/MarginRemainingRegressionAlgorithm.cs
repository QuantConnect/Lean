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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm reproduces GH issue 3763 (performing just 1 trade)
    /// </summary>
    public class MarginRemainingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Security _appl;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2007, 1, 1);
            SetEndDate(2010, 1, 1);

            _spy = AddEquity("SPY", Resolution.Daily, leverage: 1).Symbol;
            _appl = AddEquity("AAPL", Resolution.Daily, leverage: 1);

            Schedule.On(DateRules.EveryDay(), TimeRules.Noon, () =>
            {
                Plot("Info", "Portfolio.MarginRemaining", Portfolio.MarginRemaining);
                Plot("Info", "Portfolio.Cash", Portfolio.Cash);
            });
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // 70% SPY
                SetHoldings(_spy, 0.7);
                Debug("Purchased Stock SPY");
            }

            if (Portfolio.MarginRemaining <= 0)
            {
                throw new RegressionTestException($"Unexpected margin remaining value {Portfolio.MarginRemaining}");
            }

            // in the 2009 dip buy AAPL
            if (Time.Year == 2009 && !_appl.Invested)
            {
                // 30% SPY
                SetHoldings(_appl.Symbol, 0.3);
                Debug("Purchased Stock AAPL");
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
        public long DataPoints => 6800;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "6.056%"},
            {"Drawdown", "42.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "119303.75"},
            {"Net Profit", "19.304%"},
            {"Sharpe Ratio", "0.162"},
            {"Sortino Ratio", "0.183"},
            {"Probabilistic Sharpe Ratio", "7.738%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.057"},
            {"Beta", "0.708"},
            {"Annual Standard Deviation", "0.177"},
            {"Annual Variance", "0.031"},
            {"Information Ratio", "0.8"},
            {"Tracking Error", "0.087"},
            {"Treynor Ratio", "0.04"},
            {"Total Fees", "$45.18"},
            {"Estimated Strategy Capacity", "$410000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.09%"},
            {"OrderListHash", "39bdab2dcde5bed30c6fc3200d39e83c"}
        };
    }
}
