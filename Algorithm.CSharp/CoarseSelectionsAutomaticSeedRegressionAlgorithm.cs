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
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that securities added via coarse selection get automatically seeded by default
    /// </summary>
    public class CoarseSelectionsAutomaticSeedRegressionAlgorithm : AutomaticSeedBaseRegressionAlgorithm
    {
        private readonly Queue<List<Symbol>> _coarseSelections = new(new[] { "AAPL", "GOOG", "AIG", "BAC", "FB", "IBM" }
            .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA))
            .BatchBy(2));

        private HashSet<Symbol> _addedSecurities = new();

        protected override bool ShouldHaveTradeData => true;
        // Daily resolution, only trade data is available
        protected override bool ShouldHaveQuoteData => false;
        protected override bool ShouldHaveOpenInterestData => false;

        public override void Initialize()
        {
            SetStartDate(2015, 01, 01);
            SetEndDate(2015, 03, 01);
            SetCash(100000);

            Settings.SeedInitialPrices = true;
            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse((coarse) =>
            {
                var selection = _coarseSelections.Dequeue();
                _coarseSelections.Enqueue(selection);
                return selection;
            });
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            base.OnSecuritiesChanged(changes);

            foreach (var addedSecurity in changes.AddedSecurities.Where(x => !x.Symbol.IsCanonical()))
            {
                _addedSecurities.Add(addedSecurity.Symbol);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_coarseSelections.SelectMany(x => x).Order().SequenceEqual(_addedSecurities.Order()))
            {
                throw new RegressionTestException("Not all securities were added");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 358;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 390;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.066"},
            {"Tracking Error", "0.116"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
