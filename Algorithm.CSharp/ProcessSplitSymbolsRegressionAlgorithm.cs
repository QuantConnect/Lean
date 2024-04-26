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
    /// This test algorithm reproduces GH issue 2848 where an exception is thrown
    /// in the AlgorithmManager.ProcessSplitSymbols when removing the equity having a split
    /// </summary>
    public class ProcessSplitSymbolsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _aapl;
        private Security _goog;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);  //Set Start Date
            SetEndDate(2014, 06, 09);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            _aapl = AddEquity("AAPL", Resolution.Daily);
            _goog = AddEquity("GOOG", Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.Time == new DateTime(2014, 06, 06))
            {
                RemoveSecurity(_aapl.Symbol);
            }
            if (!Portfolio.Invested)
            {
                SetHoldings(_goog.Symbol, 1);
                Debug("Purchased Stock");
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
        public long DataPoints => 36;

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
            {"Compounding Annual Return", "808.402%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "103068.77"},
            {"Net Profit", "3.069%"},
            {"Sharpe Ratio", "19.483"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "100.000%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.289"},
            {"Beta", "0.537"},
            {"Annual Standard Deviation", "0.078"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "14.15"},
            {"Tracking Error", "0.077"},
            {"Treynor Ratio", "2.823"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$37000000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "19.60%"},
            {"OrderListHash", "6f3468b3a384444ee29d33ff85456660"}
        };
    }
}
