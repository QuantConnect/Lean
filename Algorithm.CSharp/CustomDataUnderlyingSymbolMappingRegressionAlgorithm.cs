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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm ensures that mapping is also applied to the underlying symbol(s) for custom data subscriptions
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="rename event" />
    /// <meta name="tag" content="map" />
    /// <meta name="tag" content="mapping" />
    /// <meta name="tag" content="map files" />
    public class CustomDataUnderlyingSymbolMappingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _initialSymbolChangedEvent;

        // Equity to add custom data with as Symbol
        private Symbol _equitySymbol;
        // Equity to add custom data with as ticker
        private Symbol _equityTicker;

        // Custom data that was added with Symbol
        private Symbol _customDataSymbol;
        // Custom data that was added with ticker
        private Symbol _customDataTicker;

        // Option to add custom data with as Symbol
        private Symbol _optionSymbol;
        // Option to add custom data with as ticker
        private Symbol _optionTicker;

        // Custom data that was added with option Symbol
        private Symbol _customDataOptionTicker;
        // Custom data that was added with option ticker
        private Symbol _customDataOptionSymbol;


        /// <summary>
        /// Adds stocks and options TWX + BAC so that we can test if mapping occurs to the underlying symbol in the custom data subscription
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(1998, 1, 1);
            SetEndDate(2004, 1, 1);
            SetCash(100000);

            // Renames on 2003-10-16 from AOL to TWX
            _equityTicker = AddEquity("TWX", Resolution.Daily).Symbol;
            _customDataTicker = AddData<SECReport10K>("TWX").Symbol;

            // Renames on 1998-09-30 from NB to BAC
            _equitySymbol = AddEquity("BAC", Resolution.Daily).Symbol;
            _customDataSymbol = AddData<SECReport10K>(_equitySymbol).Symbol;

            _optionTicker = AddOption("BAC", Resolution.Daily).Symbol;
            // TODO: Maybe this might not work? Maybe we should prefix "BAC" with "?" like option symbol values are stored in the Symbol Cache
            _customDataOptionTicker = AddData<SECReport10K>("BAC").Symbol;

            _optionSymbol = AddOption("TWX", Resolution.Daily).Symbol;
            _customDataOptionSymbol = AddData<SECReport10K>(_optionSymbol).Symbol;
        }

        /// <summary>
        /// Checks that custom data underlying symbol matches the equity symbol at the same time step
        /// </summary>
        /// <param name="data"></param>
        public override void OnData(Slice data)
        {
            if (data.SymbolChangedEvents.Any() && !_initialSymbolChangedEvent)
            {
                _initialSymbolChangedEvent = true;
                return;
            }

            if (data.SymbolChangedEvents.Any())
            {
                Symbol underlying;
                Symbol symbol;

                if (data.SymbolChangedEvents.ContainsKey(_customDataTicker) && data.SymbolChangedEvents.ContainsKey(_equityTicker) && Time.Date == new DateTime(2003, 10, 16))
                {
                    underlying = data.SymbolChangedEvents[_customDataTicker].Symbol.Underlying;
                    symbol = data.SymbolChangedEvents[_equityTicker].Symbol;
                }
                else if (data.SymbolChangedEvents.ContainsKey(_customDataSymbol) && data.SymbolChangedEvents.ContainsKey(_equitySymbol) && Time.Date == new DateTime(1998, 9, 30))
                {
                    underlying = data.SymbolChangedEvents[_customDataSymbol].Symbol.Underlying;
                    symbol = data.SymbolChangedEvents[_equitySymbol].Symbol;
                }
                // For options, handle the case a bit differently
                else if ((data.SymbolChangedEvents.ContainsKey(_customDataOptionTicker) && data.SymbolChangedEvents.ContainsKey(_optionTicker)) ||
                    (data.SymbolChangedEvents.ContainsKey(_customDataOptionSymbol) && data.SymbolChangedEvents.ContainsKey(_optionSymbol)))
                {
                    if (Time.Date == new DateTime(1998, 9, 30))
                    {
                        underlying = data.SymbolChangedEvents[_customDataOptionTicker].Symbol.Underlying;
                        symbol = data.SymbolChangedEvents[_optionTicker].Symbol;
                    }
                    else if (Time.Date == new DateTime(2003, 10, 16))
                    {
                        underlying = data.SymbolChangedEvents[_customDataOptionSymbol].Symbol.Underlying;
                        symbol = data.SymbolChangedEvents[_optionSymbol].Symbol;
                    }
                    else
                    {
                        throw new Exception("Received unknown option symbol changed event");
                    }
                    if (underlying == null)
                    {
                        throw new Exception("Custom data Symbol has no underlying");
                    }
                    if (underlying.Underlying == null)
                    {
                        throw new Exception("Custom data underlying has no underlying equity symbol");
                    }
                    if (underlying.Underlying != symbol.Underlying)
                    {
                        throw new Exception($"Custom data underlying->(2) does match option underlying (equity symbol). Expected {symbol.Underlying.Value} got {underlying.Underlying.Value}");
                    }

                    return;
                }
                else
                {
                    throw new Exception("Received unknown symbol changed event");
                }

                if (underlying != symbol)
                {
                    if (underlying == null)
                    {
                        throw new Exception("Custom data Symbol has no underlying");
                    }
                    throw new Exception($"Underlying custom data Symbol does not match equity Symbol after rename event. Expected {symbol.Value} - got {underlying.Value}");
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
