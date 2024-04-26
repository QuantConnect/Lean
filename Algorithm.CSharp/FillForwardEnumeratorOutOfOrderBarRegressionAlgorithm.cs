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
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test algorithm simply fetch and compare data of minute resolution around daylight saving period
    /// reproduces issue reported in GB issue GH issue https://github.com/QuantConnect/Lean/issues/4925
    /// related issues https://github.com/QuantConnect/Lean/issues/3707; https://github.com/QuantConnect/Lean/issues/4630
    /// </summary>
    public class FillForwardEnumeratorOutOfOrderBarRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private decimal _exptectedClose = 84.09m;
        private DateTime _exptectedTime = new DateTime(2008, 3, 10, 9, 30, 0);
        private Symbol _shy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2008, 3, 7);
            SetEndDate(2008, 3, 10);
            _shy = AddEquity("SHY", Resolution.Minute).Symbol;
            // just to make debugging easier, less subscriptions
            SetBenchmark(time => 1);
        }

        public override void OnData(Slice slice)
        {
            var trackingBar = slice.Bars.Values.FirstOrDefault(s => s.Time.Equals(_exptectedTime));

            if (trackingBar != null)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_shy, 1);
                }

                if (trackingBar.Close != _exptectedClose)
                {
                    throw new Exception(
                        $"Bar at {_exptectedTime.ToStringInvariant()} closed at price {trackingBar.Close.ToStringInvariant()}; expected {_exptectedClose.ToStringInvariant()}");
                }
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
        public long DataPoints => 1561;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "16.086%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100148.25"},
            {"Net Profit", "0.148%"},
            {"Sharpe Ratio", "7.182"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.014"},
            {"Annual Variance", "0"},
            {"Information Ratio", "9.758"},
            {"Tracking Error", "0.014"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$5.93"},
            {"Estimated Strategy Capacity", "$150000.00"},
            {"Lowest Capacity Asset", "SHY 2T"},
            {"Portfolio Turnover", "24.91%"},
            {"OrderListHash", "9d00701591b363edda102536ec5e75e0"}
        };
    }
}
