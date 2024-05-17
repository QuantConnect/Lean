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
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm using and asserting the behavior of auxiliary Data handlers
    /// </summary>
    public class AuxiliaryDataHandlersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _onSplits;
        private bool _onDividends;
        private bool _onDelistingsCalled;
        private bool _onSymbolChangedEvents;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2007, 05, 16);
            SetEndDate(2015, 1, 1);

            UniverseSettings.Resolution = Resolution.Daily;

            // will get delisted
            AddEquity("AAA.1");

            // get's remapped
            AddEquity("SPWR");

            // has a split & dividends
            AddEquity("AAPL");
        }

        public override void OnDelistings(Delistings delistings)
        {
            if (!delistings.ContainsKey("AAA.1"))
            {
                throw new Exception("Unexpected OnDelistings call");
            }
            _onDelistingsCalled = true;
        }

        public override void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged)
        {
            if (!symbolsChanged.ContainsKey("SPWR"))
            {
                throw new Exception("Unexpected OnSymbolChangedEvents call");
            }
            _onSymbolChangedEvents = true;
        }

        public override void OnSplits(Splits splits)
        {
            if (!splits.ContainsKey("AAPL"))
            {
                throw new Exception("Unexpected OnSplits call");
            }
            _onSplits = true;
        }

        public override void OnDividends(Dividends dividends)
        {
            if (!dividends.ContainsKey("AAPL"))
            {
                throw new Exception("Unexpected OnDividends call");
            }
            _onDividends = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_onDelistingsCalled)
            {
                throw new Exception("OnDelistings was not called!");
            }
            if (!_onSymbolChangedEvents)
            {
                throw new Exception("OnSymbolChangedEvents was not called!");
            }
            if (!_onSplits)
            {
                throw new Exception("OnSplits was not called!");
            }
            if (!_onDividends)
            {
                throw new Exception("OnDividends was not called!");
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
        public long DataPoints => 126221;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "-0.332"},
            {"Tracking Error", "0.183"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
