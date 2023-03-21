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
    /// Abstract regression Framework algorithm used by <see cref="EmaCrossAlphaModelFrameworkAlgorithm"/>.
    /// </summary>
    public abstract class BaseAlphaModelFrameworkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var symbols = new[] { "SPY", "AIG", "BAC", "IBM" }
                .Select(ticker => QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA))
                .ToList();

            // Manually add SPY and AIG when the algorithm starts
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols.Take(2)));

            // At midnight, add all securities every day except on the last data
            // With this procedure, the Alpha Model will experience multiple universe changes
            AddUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(), TimeRules.Midnight,
                dt => dt < EndDate.AddDays(-1) ? symbols : Enumerable.Empty<Symbol>()));

            SetAlpha(new NullAlphaModel());
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        public override void OnEndOfAlgorithm()
        {
            // We have removed all securities from the universe. The Alpha Model should remove the consolidator
            var consolidatorCount = SubscriptionManager.Subscriptions.Sum(s => s.Consolidators.Count);
            if (consolidatorCount > 0)
            {
                throw new Exception($"The number of consolidator is should be zero. Actual: {consolidatorCount}");
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
        public virtual long DataPoints => 14869;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 152;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}
