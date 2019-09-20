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
using QuantConnect.Data.Custom.USTreasury;
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
    public class CustomDataAddDataCoarseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _customSymbols = new List<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));
        }

        public IEnumerable<Symbol> CoarseSelector(IEnumerable<CoarseFundamental> coarse)
        {
            var symbols = coarse.Where(x => x.HasFundamentalData && x.DollarVolume > 500000000)
                .Select(x => x.Symbol);

            _customSymbols.Clear();

            foreach (var symbol in symbols)
            {
                _customSymbols.Add(AddData<SECReport8K>(symbol, Resolution.Daily).Symbol);
            }

            return symbols;
        }

        public override void OnData(Slice data)
        {
            foreach (var customSymbol in _customSymbols)
            {
                if (!ActiveSecurities.ContainsKey(customSymbol.Underlying))
                {
                    throw new Exception($"Custom data underlying ({customSymbol.Underlying}) Symbol was not found in active securities");
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
