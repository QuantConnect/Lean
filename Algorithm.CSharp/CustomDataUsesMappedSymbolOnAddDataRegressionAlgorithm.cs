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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm ensures that mapped symbols can be used with AddData to correctly assign an equity to custom data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="rename event" />
    /// <meta name="tag" content="map" />
    /// <meta name="tag" content="mapping" />
    /// <meta name="tag" content="map files" />
    public class CustomDataUsesMappedSymbolOnAddDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _addedAol;
        private Symbol _twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA);
        private Security _twxCustom;

        /// <summary>
        /// Adds stock TWX from Universe and add to custom data subscription to link custom data -> equity via underlying
        /// </summary>
        public override void Initialize()
        {
            // We set the end date to 2015 so that we have the chance to load the "AOL" ticker that began trading on
            // 2009-12-10. With this way, we can know for certain that we're applying a mapped symbol to custom data
            // instead of assuming that we're supplying the last known ticker to AddData. So, instead of mapping AOL
            // from 2009-12-10 to 2015-06-24, we're actually subscribing to TWX's previous ticker, not the new AOL
            SetStartDate(2003, 10, 14);
            SetEndDate(2009, 12, 31);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverse(CoarseSelection, FineSelection);
        }

        public IEnumerable<Symbol> CoarseSelection(IEnumerable<CoarseFundamental> coarse)
        {
            var aol = coarse.Where(x => x.Symbol == _twx)
                .Select(x => x.Symbol)
                .Single();

            if (!_addedAol)
            {
                // Should map the underlying Symbol to AOL, which will in turn become TWX in the future
                _twxCustom = AddData<SECReport10K>(aol);
                _addedAol = true;
            }

            yield return aol;
        }

        public IEnumerable<Symbol> FineSelection(IEnumerable<FineFundamental> fine)
        {
            return fine.Select(x => x.Symbol);
        }

        /// <summary>
        /// Checks that custom data matches our expectation that it will be subscribed to the "*TWX -> AOL" data
        /// and not "*AOL" where "*TICKER" is the most recent-day ticker
        /// </summary>
        /// <param name="data"></param>
        public override void OnData(Slice data)
        {
            string expectedTicker;
            Symbol symbol = data[_twxCustom.Symbol].Symbol;

            if (Time < new DateTime(2003, 10, 16) && data.ContainsKey(_twxCustom.Symbol))
            {
                expectedTicker = "AOL";
            }
            else if (Time >= new DateTime(2003, 10, 16) && data.ContainsKey(_twxCustom.Symbol))
            {
                expectedTicker = "TWX";
            }
            else
            {
                return;
            }

            if (!symbol.HasUnderlying)
            {
                throw new Exception("Custom data symbol has no underlying");
            }
            if (symbol.Value != expectedTicker)
            {
                throw new Exception($"Custom data symbol value does not match expected value. Expected {expectedTicker}, found {symbol.Value}");
            }
            // TODO: maybe we don't need this since we have CustomDataUnderlyingSymbolMappingRegressionAlgorithm?
            if (symbol.Underlying.Value != expectedTicker)
            {
                throw new Exception($"Custom data underlying {symbol.Underlying.Value} was not mapped to {expectedTicker}");
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
