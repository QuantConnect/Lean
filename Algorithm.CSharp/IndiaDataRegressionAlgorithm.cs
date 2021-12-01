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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

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
        private Symbol _symbol;
        private bool _initialMapping;
        private bool _executionMapping;
        private bool _receivedWarningEvent;
        private bool _receivedOccurredEvent;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 5, 20);  //Set Start Date
            SetEndDate(2016, 7, 26);    //Set End Date
            _symbol = AddEquity("3MINDIA", Resolution.Daily, Market.India).Symbol;
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="data">Data.</param>
        public void OnData(Dividends data) // update this to Dividends dictionary
        {
            var dividend = data["3MINDIA"];
            if (dividend.Price != 645.5700m || dividend.ReferencePrice != 645.5700m || dividend.Distribution != 645.5700m)
            {
                throw new Exception("Did not receive expected price values");
            }
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="data">Data.</param>
        public void OnData(Splits data)
        {
            var split = data["3MINDIA"];
            if (split.Type == SplitType.Warning)
            {
                _receivedWarningEvent = true;
            }
            else if (split.Type == SplitType.SplitOccurred)
            {
                _receivedOccurredEvent = true;
                if (split.Price != 645.5700m || split.ReferencePrice != 645.5700m || split.SplitFactor != 645.5700m)
                {
                    throw new Exception("Did not receive expected price values");
                }
            }
        }

        /// <summary>
        /// Checks the symbol change event
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (Time.Date == new DateTime(2016, 6, 15))
            {
            }

            if (slice.Splits.Any())
            {  
            }

            if (slice.SymbolChangedEvents.ContainsKey(_symbol))
            {
                var mappingEvent = slice.SymbolChangedEvents.Single(x => x.Key.SecurityType == SecurityType.Equity).Value;
                Log($"{Time} - Ticker changed from: {mappingEvent.OldSymbol} to {mappingEvent.NewSymbol}");
                if (Time.Date == new DateTime(1999, 01, 01))
                {
                    // we should Not receive the initial mapping event
                    {
                        throw new Exception($"Unexpected mapping event {mappingEvent}");
                    }
                    _initialMapping = true;
                }
                else if (Time.Date == new DateTime(2004, 06, 15))
                {
                    if (mappingEvent.NewSymbol != "3MINDIA"
                        || mappingEvent.OldSymbol != "BIRLA3M")
                    {
                        throw new Exception($"Unexpected mapping event {mappingEvent}");
                    }
                    _executionMapping = true;
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
                throw new Exception("The ticker generated the initial rename event");
            }
            if (!_executionMapping)
            {
                throw new Exception("The ticker did not rename throughout the course of its life even though it should have");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-99.907%"},
            {"Drawdown", "11.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "-10.343%"},
            {"Sharpe Ratio", "-1.554"},
            {"Probabilistic Sharpe Ratio", "0.001%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.91"},
            {"Beta", "-5.602"},
            {"Annual Standard Deviation", "0.643"},
            {"Annual Variance", "0.413"},
            {"Information Ratio", "-1.378"},
            {"Tracking Error", "0.736"},
            {"Treynor Ratio", "0.178"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "NWSA.CustomDataUsingMapping T3MO1488O0H0"},
            {"Fitness Score", "0.127"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-9.481"},
            {"Portfolio Turnover", "0.249"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "d4cf2839e74df7fa436e30f44be4cb57"}
        };
    }
}
