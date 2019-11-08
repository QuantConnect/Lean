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
    /// Regression algorithm demonstrating use of map files with custom data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="rename event" />
    /// <meta name="tag" content="map" />
    /// <meta name="tag" content="mapping" />
    /// <meta name="tag" content="map files" />
    public class CustomDataUsingMapFileRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private bool _initialMapping;
        private bool _executionMapping;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 06, 27);
            SetEndDate(2013, 07, 02);

            var foxa = QuantConnect.Symbol.Create("FOXA", SecurityType.Equity, Market.USA);
            _symbol = AddData<CustomDataUsingMapping>(foxa).Symbol;

            foreach (var config in SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_symbol))
            {
                if (config.Resolution != Resolution.Minute)
                {
                    throw new Exception("Expected resolution to be set to Minute");
                }
            }
        }

        /// <summary>
        /// Checks to see if the stock has been renamed, and places an order once the symbol has changed
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (slice.SymbolChangedEvents.ContainsKey(_symbol))
            {
                var mappingEvent = slice.SymbolChangedEvents.Single(x => x.Key.SecurityType == SecurityType.Base).Value;
                Log($"{Time} - Ticker changed from: {mappingEvent.OldSymbol} to {mappingEvent.NewSymbol}");
                if (Time.Date == new DateTime(2013, 06, 27))
                {
                    // we should Not receive the initial mapping event
                    if (mappingEvent.NewSymbol != "NWSA"
                        || mappingEvent.OldSymbol != "FOXA")
                    {
                        throw new Exception($"Unexpected mapping event {mappingEvent}");
                    }
                    _initialMapping = true;
                }
                else if (Time.Date == new DateTime(2013, 06, 29))
                {
                    if (mappingEvent.NewSymbol != "FOXA"
                        || mappingEvent.OldSymbol != "NWSA")
                    {
                        throw new Exception($"Unexpected mapping event {mappingEvent}");
                    }

                    _executionMapping = true;
                    SetHoldings(_symbol, 1);
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
            {"Compounding Annual Return", "-99.882%"},
            {"Drawdown", "52.600%"},
            {"Expectancy", "0"},
            {"Net Profit", "-10.486%"},
            {"Sharpe Ratio", "-8.145"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-4.408"},
            {"Beta", "-0.257"},
            {"Annual Standard Deviation", "0.55"},
            {"Annual Variance", "0.302"},
            {"Information Ratio", "-8.51"},
            {"Tracking Error", "0.558"},
            {"Treynor Ratio", "17.411"},
            {"Total Fees", "$0.00"},
        };

        /// <summary>
        /// Test example custom data showing how to enable the use of mapping.
        /// Implemented as a wrapper of existing NWSA->FOXA equity
        /// </summary>
        private class CustomDataUsingMapping : TradeBar
        {
            /// <summary>
            /// Indicates if there is support for mapping
            /// </summary>
            /// <returns>True indicates mapping should be done</returns>
            public override bool RequiresMapping()
            {
                return true;
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return base.GetSource(new SubscriptionDataConfig(config,
                        typeof(CustomDataUsingMapping),
                    // create a new symbol as equity so we find the existing data files
                    Symbol.Create(config.MappedSymbol, SecurityType.Equity, config.Market)),
                    date,
                    isLiveMode);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                return ParseEquity(config, line, date);
            }

            /// <summary>
            /// Gets the default resolution for this data and security type
            /// </summary>
            /// <remarks>This is a method and not a property so that python
            /// custom data types can override it</remarks>
            public override Resolution DefaultResolution()
            {
                return Resolution.Minute;
            }

            /// <summary>
            /// Gets the supported resolution for this data and security type
            /// </summary>
            /// <remarks>This is a method and not a property so that python
            /// custom data types can override it</remarks>
            public override List<Resolution> SupportedResolutions()
            {
                return new List<Resolution> { Resolution.Minute };
            }
        }
    }
}
