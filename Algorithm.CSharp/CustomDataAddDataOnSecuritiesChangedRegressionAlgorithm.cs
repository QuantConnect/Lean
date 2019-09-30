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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm ensures that data added via OnSecuritiesChanged (underlying) is present in ActiveSecurities
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="regression test" />
    public class CustomDataAddDataOnSecuritiesChangedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _customSymbols = new List<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2014, 3, 24);
            SetEndDate(2014, 4, 7);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));
        }

        public IEnumerable<Symbol> CoarseSelector(IEnumerable<CoarseFundamental> coarse)
        {
            return new[]
            {
                QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("FB", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("GOOGL", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA),
            };
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && Transactions.GetOpenOrders().Count == 0)
            {
                var aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
                SetHoldings(aapl, 0.5);
            }

            foreach (var customSymbol in _customSymbols)
            {
                if (!ActiveSecurities.ContainsKey(customSymbol.Underlying))
                {
                    throw new Exception($"Custom data underlying ({customSymbol.Underlying}) Symbol was not found in active securities");
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            bool iterated = false;
            foreach (var added in changes.AddedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Equity))
            {
                if (!iterated)
                {
                    _customSymbols.Clear();
                    iterated = true;
                }
                _customSymbols.Add(AddData<SECReport8K>(added.Symbol, Resolution.Daily).Symbol);
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
            {"Compounding Annual Return", "-33.688%"},
            {"Drawdown", "2.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "-1.674%"},
            {"Sharpe Ratio", "-5.986"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.253"},
            {"Beta", "0.474"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-2.235"},
            {"Tracking Error", "0.064"},
            {"Treynor Ratio", "-0.744"},
            {"Total Fees", "$3.50"},
        };
    }
}
