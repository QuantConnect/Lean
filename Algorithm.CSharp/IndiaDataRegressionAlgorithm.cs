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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating use of map files with India data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="India data" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="rename event" />
    /// <meta name="tag" content="map" />
    /// <meta name="tag" content="mapping" />
    /// <meta name="tag" content="map files" />
    public class IndiaDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _mappingSymbol, _splitAndDividendSymbol;
        private bool _initialMapping;
        private bool _executionMapping;
        private bool _receivedWarningEvent;
        private bool _receivedOccurredEvent;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetAccountCurrency("INR");  //Set Account Currency 
            SetStartDate(2004, 5, 20);  //Set Start Date
            SetEndDate(2016, 7, 26);    //Set End Date
            _mappingSymbol = AddEquity("3MINDIA", Resolution.Daily, Market.India).Symbol;
            _splitAndDividendSymbol = AddEquity("CCCL", Resolution.Daily, Market.India).Symbol;
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="dividends">Data.</param>
        public override void OnDividends(Dividends dividends)
        {
            if (dividends.ContainsKey(_splitAndDividendSymbol))
            {
                var dividend = dividends[_splitAndDividendSymbol];
                if (Time.Date == new DateTime(2010, 06, 15) &&
                    (dividend.Price != 0.5m || dividend.ReferencePrice != 88.8m || dividend.Distribution != 0.5m))
                {
                    throw new RegressionTestException("Did not receive expected dividend values");
                }
            }
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="splits">Splits.</param>
        public override void OnSplits(Splits splits)
        {
            if (splits.ContainsKey(_splitAndDividendSymbol))
            {
                var split = splits[_splitAndDividendSymbol];
                if (split.Type == SplitType.Warning)
                {
                    _receivedWarningEvent = true;
                }
                else if (split.Type == SplitType.SplitOccurred)
                {
                    _receivedOccurredEvent = true;
                    if (split.Price != 421m || split.ReferencePrice != 421m || split.SplitFactor != 0.2m)
                    {
                        throw new RegressionTestException("Did not receive expected split values");
                    }
                }
            }
        }

        /// <summary>
        /// Checks the symbol change event
        /// </summary>
        public override void OnSymbolChangedEvents(SymbolChangedEvents symbolsChanged)
        {
            if (symbolsChanged.ContainsKey(_mappingSymbol))
            {
                var mappingEvent = symbolsChanged.Single(x => x.Key.SecurityType == SecurityType.Equity).Value;
                Log($"{Time} - Ticker changed from: {mappingEvent.OldSymbol} to {mappingEvent.NewSymbol}");
                if (Time.Date == new DateTime(1999, 01, 01))
                {
                    _initialMapping = true;
                }
                else if (Time.Date == new DateTime(2004, 06, 15))
                {
                    if (mappingEvent.NewSymbol == "3MINDIA"
                        && mappingEvent.OldSymbol == "BIRLA3M")
                    {
                        _executionMapping = true;
                    }
                }
            }
        }

        /// <summary>
        /// Final step of the algorithm
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_initialMapping)
            {
                throw new RegressionTestException("The ticker generated the initial rename event");
            }
            if (!_executionMapping)
            {
                throw new RegressionTestException("The ticker did not rename throughout the course of its life even though it should have");
            }
            if (!_receivedOccurredEvent)
            {
                throw new RegressionTestException("Did not receive expected split event");
            }
            if (!_receivedWarningEvent)
            {
                throw new RegressionTestException("Did not receive expected split warning event");
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
        public long DataPoints => 23036;

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
            {"Total Fees", "₹0.00"},
            {"Estimated Strategy Capacity", "₹0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
