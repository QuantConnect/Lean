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
    public class CustomDataUnderlyingOptionSymbolMappingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _initialSymbolChangedEvent;

        // Option to add custom data with as Symbol
        private Symbol _optionSymbol;

        // Custom data that was added with option ticker
        private Symbol _customDataOptionSymbol;


        /// <summary>
        /// Adds option NWSA -&gt; FOXA so that we can test if mapping occurs to the underlying symbols in the custom data subscription
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 6, 28);
            SetEndDate(2013, 7, 02);
            SetCash(100000);

            _optionSymbol = AddOption("FOXA", Resolution.Daily).Symbol;
            _customDataOptionSymbol = AddData<SECReport10K>(_optionSymbol).Symbol;
        }

        /// <summary>
        /// Checks that custom data underlying symbols match the expected symbols and contains chain of custom -&gt; option -&gt equity
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
                if (data.SymbolChangedEvents.ContainsKey(_customDataOptionSymbol) && data.SymbolChangedEvents.ContainsKey(_optionSymbol))
                {
                    var expectedUnderlying = "?FOXA";
                    var underlying = data.SymbolChangedEvents.Keys.Where(x => x.SecurityType == SecurityType.Base && x == _customDataOptionSymbol).Single().Underlying;
                    var symbol = data.SymbolChangedEvents.Keys.Where(x => x.SecurityType == SecurityType.Equity && x == _optionSymbol).Single();

                    if (SubscriptionManager.Subscriptions.Where(x => (x.SecurityType == SecurityType.Base || x.SecurityType == SecurityType.Option || x.SecurityType == SecurityType.Equity) && x.MappedSymbol == expectedUnderlying).Count() != 3)
                    {
                        throw new Exception($"Subscription mapped symbols were not updated to {expectedUnderlying}");
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
                    if (underlying.Underlying.Value != expectedUnderlying)
                    {
                        throw new Exception($"Custom data symbol value does not match expected value. Expected {expectedUnderlying}, found {underlying.Underlying.Value}");
                    }

                    SetHoldings(underlying.Underlying, 0.5);
                }
                else
                {
                    throw new Exception("Received unknown symbol changed event");
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
