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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
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
        private Symbol _badEquitySymbol;

        // Custom data that was added with Symbol
        private Symbol _customDataSymbol;
        private Symbol _badCustomDataSymbol;

        /// <summary>
        /// Adds stocks GOOGL -&gt; GOOG and GOOG -&gt; GOOCV so that we can test if mapping occurs to the underlying symbol in the custom data subscription
        /// as well as testing the behavior of adding custom data that can be mapped with a ticker
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 3, 1);
            SetEndDate(2014, 4, 9);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;

            // Reset the symbol cache to test for ticker adding of custom data
            SymbolCache.Clear();
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));
        }

        public IEnumerable<Symbol> CoarseSelector(IEnumerable<CoarseFundamental> coarse)
        {
            return new[]
            {
                QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("GOOGL", SecurityType.Equity, Market.USA),
            };
        }

        /// <summary>
        /// Checks that custom data underlying symbol matches the equity symbol
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
                if (data.SymbolChangedEvents.ContainsKey(_customDataSymbol) && data.SymbolChangedEvents.ContainsKey(_equitySymbol))
                {
                    var expectedUnderlying = "GOOGL";
                    var underlying = data.SymbolChangedEvents.Keys.Where(x => x.SecurityType == SecurityType.Base && x == _customDataSymbol).Single().Underlying;
                    var symbol = data.SymbolChangedEvents.Keys.Where(x => x.SecurityType == SecurityType.Equity && x == _equitySymbol).Single();

                    if (SubscriptionManager.Subscriptions.Where(x => (x.SecurityType == SecurityType.Base || x.SecurityType == SecurityType.Equity) && x.MappedSymbol == expectedUnderlying).Count() != 2)
                    {
                        throw new Exception($"Subscription mapped symbols were not updated to {expectedUnderlying}");
                    }
                    if (underlying == null)
                    {
                        throw new Exception("Custom data Symbol for GOOGL has no underlying");
                    }
                    if (underlying != symbol)
                    {
                        throw new Exception($"Underlying custom data Symbol does not match equity Symbol after rename event. Expected {symbol.Value} - got {underlying.Value}");
                    }
                    if (underlying.Value != expectedUnderlying)
                    {
                        throw new Exception($"Underlying equity symbol value from chained custom data does not match expected value. Expected {symbol.Underlying.Value}, found {underlying.Underlying.Value}");
                    }

                    SetHoldings(symbol, 0.5);
                }
                else if (data.SymbolChangedEvents.ContainsKey(_badCustomDataSymbol) && data.SymbolChangedEvents.ContainsKey(_badEquitySymbol))
                {
                    var underlying = data.SymbolChangedEvents.Keys.Where(x => x.SecurityType == SecurityType.Base && x == _badCustomDataSymbol).Single().Underlying;
                    var symbol = data.SymbolChangedEvents.Keys.Where(x => x.SecurityType == SecurityType.Equity && x == _badEquitySymbol).Single();

                    if (underlying == null)
                    {
                        throw new Exception($"Bad custom data symbol does not have underlying");
                    }
                    if (underlying == symbol)
                    {
                        throw new Exception($"Underlying custom data Symbol is equal to bad Symbol");
                    }
                }
                else
                {
                    throw new Exception("Received unknown symbol changed event");
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Equity))
            {
                // It is in fact "GOOGL" we're catching here, and we're adding it as "GOOG" with the ticker,
                // which will resolve to GOOCV in the past if we use the ticker and not the symbol
                if (added.Symbol.Value == "GOOG")
                {
                    _badEquitySymbol = added.Symbol;
                    _badCustomDataSymbol = AddData<SECReport10K>("GOOG").Symbol;

                    _equitySymbol = added.Symbol;
                    _customDataSymbol = AddData<SECReport10K>(added.Symbol).Symbol;
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
