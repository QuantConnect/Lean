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

        // Custom data that was added with Symbol
        private Symbol _customDataSymbol;

        // Option to add custom data with as Symbol
        private Symbol _optionSymbol;

        // Custom data that was added with option ticker
        private Symbol _customDataOptionSymbol;


        /// <summary>
        /// Adds stocks and options TWX + BAC so that we can test if mapping occurs to the underlying symbol in the custom data subscription
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2003, 10, 14);
            SetEndDate(2014, 4, 9);
            SetCash(100000);

            // Renames on 2003-10-16 from AOL to TWX
            _equitySymbol = AddEquity("GOOGL", Resolution.Daily).Symbol;
            _customDataSymbol = AddData<SECReport10K>(_equitySymbol).Symbol;

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
                string expectedUnderlying;

                if (data.SymbolChangedEvents.ContainsKey(_customDataSymbol) && data.SymbolChangedEvents.ContainsKey(_equitySymbol))
                {
                    expectedUnderlying = "GOOGL";
                    underlying = data.SymbolChangedEvents[_customDataSymbol].Symbol.Underlying;
                    symbol = data.SymbolChangedEvents[_equitySymbol].Symbol;
                }
                // For options, handle the case a bit differently
                else if (data.SymbolChangedEvents.ContainsKey(_customDataOptionSymbol) && data.SymbolChangedEvents.ContainsKey(_optionSymbol))
                {
                    expectedUnderlying = "?TWX";
                    underlying = data.SymbolChangedEvents[_customDataOptionSymbol].Symbol.Underlying;
                    symbol = data.SymbolChangedEvents[_optionSymbol].Symbol;

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
                    if (underlying.Underlying.Value != expectedUnderlying)
                    {
                        throw new Exception($"Custom data symbol value does not match expected value. Expected {expectedUnderlying}, found {underlying.Underlying.Value}");
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
                if (underlying.Value != expectedUnderlying)
                {
                    throw new Exception($"Underlying equity symbol value from chained custom data does not match expected value. Expected {symbol.Underlying.Value}, found {underlying.Underlying.Value}");
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
