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
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating use of map files with custom data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="SEC" />
    /// <meta name="tag" content="rename event" />
    /// <meta name="tag" content="map" />
    /// <meta name="tag" content="mapping" />
    /// <meta name="tag" content="map files" />
    public class CustomDataUsingMapFileRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private bool _changedSymbol;
        private bool _properSymbolBeforeRename;
        private bool _properSymbolAfterRename;

        /// <summary>
        /// Ticker we use for testing
        /// </summary>
        public const string Ticker = "TWX";

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2001, 1, 1);
            SetEndDate(2003, 12, 31);
            SetCash(100000);

            // AOL renames to TWX in 2003
            _symbol = AddData<SECReport8K>(Ticker, Resolution.Daily).Symbol;
            AddEquity(Ticker, Resolution.Daily);
        }

        /// <summary>
        /// Checks to see if the stock has been renamed, and places an order once the symbol has changed
        /// </summary>
        /// <param name="slice"></param>
        public override void OnData(Slice slice)
        {
            if (slice.SymbolChangedEvents.ContainsKey(_symbol))
            {
                // Check to see if it was renamed on the 16th
                _changedSymbol = Time.Date == new DateTime(2003, 10, 16);
                Log($"{Time} - Ticker changed from: {slice.SymbolChangedEvents[_symbol].OldSymbol} to {slice.SymbolChangedEvents[_symbol].NewSymbol}");
            }

            foreach (var report in slice.Get<SECReport8K>())
            {
                if (!_properSymbolBeforeRename)
                {
                    _properSymbolBeforeRename = report.Key.Value == "AOL" && Time < new DateTime(2003, 10, 16);
                }
                if (!_properSymbolAfterRename)
                {
                    _properSymbolAfterRename = report.Key.Value == "TWX" && Time >= new DateTime(2003, 10, 16);
                }

                Log($"{Time} - Received 8-K report for {report.Key.Value}");
            }
        }
        
        /// <summary>
        /// Final step of the algorithm
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (!_changedSymbol)
            {
                throw new Exception("The ticker did not rename throughout the course of its life even though it should have");
            }
            if (!_properSymbolBeforeRename)
            {
                throw new Exception("The SEC report data never renamed to its old ticker");
            }
            if (!_properSymbolAfterRename)
            {
                throw new Exception("The SEC report data never renamed back to its present-day ticker");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

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
        };
    }
}
