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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Abstract regression framework algorithm for multiple framework regression tests
    /// </summary>
    public abstract class BaseFrameworkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2014, 6, 1);
            SetEndDate(2014, 6, 30);

            UniverseSettings.Resolution = Resolution.Hour;
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            var symbols = new[] { "AAPL", "AIG", "BAC", "SPY" }
                .Select(ticker => QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA))
                .ToList();

            // Manually add AAPL and AIG when the algorithm starts
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols.Take(2)));

            // At midnight, add all securities every day except on the last data
            // With this procedure, the Alpha Model will experience multiple universe changes
            AddUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(), TimeRules.Midnight,
                dt => dt < EndDate.AddDays(-1) ? symbols : Enumerable.Empty<Symbol>()));

            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(31), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        public override void OnEndOfAlgorithm()
        {
            // The base implementation checks for active insights
            var insightsCount = Insights.GetInsights(insight => insight.IsActive(UtcTime)).Count;
            if (insightsCount != 0)
            {
                throw new RegressionTestException($"The number of active insights should be 0. Actual: {insightsCount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 765;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}
