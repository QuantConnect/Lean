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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing that all virtual methods in FundamentalUniverseSelectionModel 
    /// can be properly overridden and called from both C# and Python implementations.
    /// </summary>
    public class FundamentalUniverseSelectionWithOverrideMethodsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private AllMethodsUniverseSelectionModel _model;

        /// <summary>
        /// Initialize the algorithm
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 10);

            _model = new AllMethodsUniverseSelectionModel();
            AddUniverseSelection(_model);
        }

        /// <summary>
        /// OnEndOfAlgorithm event
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            // Select method will be called multiple times automatically
            // The other methods must be invoked manually to ensure their overridden implementations are executed

            // Test SelectCoarse
            var coarseData = new[] { new CoarseFundamental() };
            _model.SelectCoarse(this, coarseData);

            // Test SelectFine
            var fineData = new[] { new FineFundamental() };
            _model.SelectFine(this, fineData);

            // Test CreateCoarseFundamentalUniverse
            _model.CreateCoarseFundamentalUniverse(this);

            if (_model.SelectCallCount == 0)
            {
                throw new RegressionTestException("Expected Select to be called at least once");
            }
            if (_model.SelectCoarseCallCount == 0)
            {
                throw new RegressionTestException("Expected SelectCoarse to be called at least once");
            }
            if (_model.SelectFineCallCount == 0)
            {
                throw new RegressionTestException("Expected SelectFine to be called at least once");
            }
            if (_model.CreateCoarseCallCount == 0)
            {
                throw new RegressionTestException("Expected CreateCoarseFundamentalUniverse to be called at least once");
            }
        }

        /// <summary>
        /// Test universe selection model that overrides all virtual methods
        /// </summary>
        private class AllMethodsUniverseSelectionModel : FundamentalUniverseSelectionModel
        {
            public int SelectCallCount { get; private set; }
            public int SelectCoarseCallCount { get; private set; }
            public int SelectFineCallCount { get; private set; }
            public int CreateCoarseCallCount { get; private set; }

            public AllMethodsUniverseSelectionModel()
                : base()
            {
            }

            public override IEnumerable<Symbol> Select(QCAlgorithm algorithm, IEnumerable<Fundamental> fundamental)
            {
                SelectCallCount++;
                return [];
            }

            public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
            {
                SelectCoarseCallCount++;
                var filtered = coarse.Where(c => c.Price > 10);
                return coarse.Take(2).Select(c => c.Symbol);
            }

            public override IEnumerable<Symbol> SelectFine(QCAlgorithm algorithm, IEnumerable<FineFundamental> fine)
            {
                SelectFineCallCount++;
                return fine.Take(2).Select(f => f.Symbol);
            }

            public override Universe CreateCoarseFundamentalUniverse(QCAlgorithm algorithm)
            {
                CreateCoarseCallCount++;
                return new CoarseFundamentalUniverse(
                    algorithm.UniverseSettings,
                    coarse => CustomCoarseSelector(coarse)
                );
            }

            private static IEnumerable<Symbol> CustomCoarseSelector(IEnumerable<CoarseFundamental> coarse)
            {
                var filtered = coarse.Where(c => c.HasFundamentalData);
                return filtered.Take(5).Select(c => c.Symbol);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 56;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Information Ratio", "-7.477"},
            {"Tracking Error", "0.234"},
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
