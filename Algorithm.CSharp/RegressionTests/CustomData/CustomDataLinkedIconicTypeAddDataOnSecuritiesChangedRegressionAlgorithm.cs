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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.IconicTypes;
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
    public class CustomDataLinkedIconicTypeAddDataOnSecuritiesChangedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
            foreach (var added in changes.AddedSecurities)
            {
                if (!iterated)
                {
                    _customSymbols.Clear();
                    iterated = true;
                }
                _customSymbols.Add(AddData<LinkedData>(added.Symbol, Resolution.Daily).Symbol);
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 78123;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-33.427%"},
            {"Drawdown", "2.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98341.86"},
            {"Net Profit", "-1.658%"},
            {"Sharpe Ratio", "-4.844"},
            {"Sortino Ratio", "-5.768"},
            {"Probabilistic Sharpe Ratio", "5.401%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.215"},
            {"Beta", "0.503"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-3.027"},
            {"Tracking Error", "0.054"},
            {"Treynor Ratio", "-0.529"},
            {"Total Fees", "$14.45"},
            {"Estimated Strategy Capacity", "$370000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "3.33%"},
            {"OrderListHash", "4338282f0d992269ff5acaeba8667049"}
        };
    }
}
