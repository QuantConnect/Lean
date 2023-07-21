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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        // this is so that later during 'UniverseSelection.CreateUniverses' we wont remove the user universes from the UniverseManager
        private readonly HashSet<Symbol> _universeSelectionUniverses = new ();
        private bool _isEmitWarmupInsightWarningSent;
        private bool _isEmitDelistedInsightWarningSent;

        /// <summary>
        /// Enables additional logging of framework models including:
        /// All insights, portfolio targets, order events, and any risk management altered targets
        /// </summary>
        [DocumentationAttribute(Logging)]
        public bool DebugMode { get; set; }

        /// <summary>
        /// Gets or sets the universe selection model.
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public IUniverseSelectionModel UniverseSelection { get; set; }

        /// <summary>
        /// Gets or sets the alpha model
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public IAlphaModel Alpha { get; set; }

        /// <summary>
        /// Gets the insight manager
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public InsightManager Insights { get; private set; }

        /// <summary>
        /// Gets or sets the portfolio construction model
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public IPortfolioConstructionModel PortfolioConstruction { get; set; }

        /// <summary>
        /// Gets or sets the execution model
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public IExecutionModel Execution { get; set; }

        /// <summary>
        /// Gets or sets the risk management model
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public IRiskManagementModel RiskManagement { get; set; }

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        [DocumentationAttribute(AlgorithmFramework)]
        public void FrameworkPostInitialize()
        {
            foreach (var universe in UniverseSelection.CreateUniverses(this))
            {
                AddUniverse(universe);
                _universeSelectionUniverses.Add(universe.Configuration.Symbol);
            }

            if (DebugMode)
            {
                InsightsGenerated += (algorithm, data) => Log($"{Time}: {string.Join(" | ", data.Insights.OrderBy(i => i.Symbol.ToString()))}");
            }
        }

        /// <summary>
        /// Used to send data updates to algorithm framework models
        /// </summary>
        /// <param name="slice">The current data slice</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(HandlingData)]
        public void OnFrameworkData(Slice slice)
        {
            if (UtcTime >= UniverseSelection.GetNextRefreshTimeUtc())
            {
                // remove deselected universes by symbol before we create new universes
                foreach (var ukvp in UniverseManager.Where(kvp => kvp.Value.DisposeRequested))
                {
                    var universeSymbol = ukvp.Key;
                    // have to remove in the next loop after the universe is marked as disposed, when 'Dispose()' is called it will trigger universe selection
                    // and deselect all symbols, sending the removed security changes, which are picked up by the AlgorithmManager and tags securities
                    // as non tradable as long as they are not active in any universe (uses UniverseManager.ActiveSecurities)
                    // but they will remain tradable if a position is still being hold since they won't be remove from the UniverseManager
                    // but this last part will not happen if we remove the universe from the UniverseManager right away, since it won't be part of 'UniverseManager'.
                    // And we have to remove the universe even if it's present at 'universes' because that one is another New universe that should get added!
                    // 'UniverseManager' will skip duplicate entries getting added.
                    UniverseManager.Remove(universeSymbol);
                    _universeSelectionUniverses.Remove(universeSymbol);
                }

                var toRemove = new HashSet<Symbol>(_universeSelectionUniverses);
                foreach (var universe in UniverseSelection.CreateUniverses(this))
                {
                    // add newly selected universes
                    _universeSelectionUniverses.Add(universe.Configuration.Symbol);
                    AddUniverse(universe);

                    toRemove.Remove(universe.Configuration.Symbol);
                }

                // remove deselected universes by symbol but prevent removal of qc algorithm created user defined universes
                foreach (var universeSymbol in toRemove)
                {
                    // mark this universe as disposed to remove all child subscriptions
                    UniverseManager[universeSymbol].Dispose();
                }
            }

            // update scores
            Insights.Step(UtcTime);

            // we only want to run universe selection if there's no data available in the slice
            if (!slice.HasData)
            {
                return;
            }

            // insight timestamping handled via InsightsGenerated event handler
            var insightsEnumerable = Alpha.Update(this, slice);
            // for performance only call 'ToArray' if not empty enumerable (which is static)
            var insights = insightsEnumerable == Enumerable.Empty<Insight>()
                ? new Insight[] { } : insightsEnumerable.ToArray();

            // only fire insights generated event if we actually have insights
            if (insights.Length != 0)
            {
                insights = InitializeInsights(insights);
                OnInsightsGenerated(insights);
            }

            ProcessInsights(insights);
        }

        /// <summary>
        /// They different framework models will process the new provided insight.
        /// The <see cref="IPortfolioConstructionModel"/> will create targets,
        /// the <see cref="IRiskManagementModel"/> will adjust the targets
        /// and the <see cref="IExecutionModel"/> will execute the <see cref="IPortfolioTarget"/>
        /// </summary>
        /// <param name="insights">The insight to process</param>
        [DocumentationAttribute(AlgorithmFramework)]
        private void ProcessInsights(Insight[] insights)
        {
            // construct portfolio targets from insights
            var targetsEnumerable = PortfolioConstruction.CreateTargets(this, insights);
            // for performance only call 'ToArray' if not empty enumerable (which is static)
            var targets = targetsEnumerable == Enumerable.Empty<IPortfolioTarget>()
                ? new IPortfolioTarget[] {} : targetsEnumerable.ToArray();

            // set security targets w/ those generated via portfolio construction module
            foreach (var target in targets)
            {
                var security = Securities[target.Symbol];
                security.Holdings.Target = target;
            }

            if (DebugMode)
            {
                // debug printing of generated targets
                if (targets.Length > 0)
                {
                    Log($"{Time}: PORTFOLIO: {string.Join(" | ", targets.Select(t => t.ToString()).OrderBy(t => t))}");
                }
            }

            var riskTargetOverridesEnumerable = RiskManagement.ManageRisk(this, targets);
            // for performance only call 'ToArray' if not empty enumerable (which is static)
            var riskTargetOverrides = riskTargetOverridesEnumerable == Enumerable.Empty<IPortfolioTarget>()
                ? new IPortfolioTarget[] { } : riskTargetOverridesEnumerable.ToArray();

            // override security targets w/ those generated via risk management module
            foreach (var target in riskTargetOverrides)
            {
                var security = Securities[target.Symbol];
                security.Holdings.Target = target;
            }

            if (DebugMode)
            {
                // debug printing of generated risk target overrides
                if (riskTargetOverrides.Length > 0)
                {
                    Log($"{Time}: RISK: {string.Join(" | ", riskTargetOverrides.Select(t => t.ToString()).OrderBy(t => t))}");
                }
            }

            IPortfolioTarget[] riskAdjustedTargets;
            // for performance we check the length before
            if (riskTargetOverrides.Length != 0
                || targets.Length != 0)
            {
                // execute on the targets, overriding targets for symbols w/ risk targets
                riskAdjustedTargets = riskTargetOverrides.Concat(targets).DistinctBy(pt => pt.Symbol).ToArray();
            }
            else
            {
                riskAdjustedTargets = new IPortfolioTarget[] { };
            }

            if (DebugMode)
            {
                // only log adjusted targets if we've performed an adjustment
                if (riskTargetOverrides.Length > 0)
                {
                    Log($"{Time}: RISK ADJUSTED TARGETS: {string.Join(" | ", riskAdjustedTargets.Select(t => t.ToString()).OrderBy(t => t))}");
                }
            }

            Execution.Execute(this, riskAdjustedTargets);
        }

        /// <summary>
        /// Used to send security changes to algorithm framework models
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(Universes)]
        public void OnFrameworkSecuritiesChanged(SecurityChanges changes)
        {
            if (DebugMode)
            {
                Debug($"{Time}: {changes}");
            }

            Alpha.OnSecuritiesChanged(this, changes);
            PortfolioConstruction.OnSecuritiesChanged(this, changes);
            Execution.OnSecuritiesChanged(this, changes);
            RiskManagement.OnSecuritiesChanged(this, changes);
        }

        /// <summary>
        /// Sets the universe selection model
        /// </summary>
        /// <param name="universeSelection">Model defining universes for the algorithm</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(Universes)]
        public void SetUniverseSelection(IUniverseSelectionModel universeSelection)
        {
            UniverseSelection = universeSelection;
        }

        /// <summary>
        /// Adds a new universe selection model
        /// </summary>
        /// <param name="universeSelection">Model defining universes for the algorithm to add</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(Universes)]
        public void AddUniverseSelection(IUniverseSelectionModel universeSelection)
        {
            if (UniverseSelection.GetType() != typeof(NullUniverseSelectionModel))
            {
                var compositeUniverseSelection = UniverseSelection as CompositeUniverseSelectionModel;
                if (compositeUniverseSelection != null)
                {
                    compositeUniverseSelection.AddUniverseSelection(universeSelection);
                }
                else
                {
                    UniverseSelection = new CompositeUniverseSelectionModel(UniverseSelection, universeSelection);
                }
            }
            else
            {
                UniverseSelection = universeSelection;
            }
        }

        /// <summary>
        /// Sets the alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha</param>
        [DocumentationAttribute(AlgorithmFramework)]
        public void SetAlpha(IAlphaModel alpha)
        {
            Alpha = alpha;
        }

        /// <summary>
        /// Adds a new alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha to add</param>
        [DocumentationAttribute(AlgorithmFramework)]
        public void AddAlpha(IAlphaModel alpha)
        {
            if (Alpha.GetType() != typeof(NullAlphaModel))
            {
                var compositeAlphaModel = Alpha as CompositeAlphaModel;
                if (compositeAlphaModel != null)
                {
                    compositeAlphaModel.AddAlpha(alpha);
                }
                else
                {
                    Alpha = new CompositeAlphaModel(Alpha, alpha);
                }
            }
            else
            {
                Alpha = alpha;
            }
        }

        /// <summary>
        /// Sets the portfolio construction model
        /// </summary>
        /// <param name="portfolioConstruction">Model defining how to build a portfolio from insights</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void SetPortfolioConstruction(IPortfolioConstructionModel portfolioConstruction)
        {
            PortfolioConstruction = portfolioConstruction;
        }

        /// <summary>
        /// Sets the execution model
        /// </summary>
        /// <param name="execution">Model defining how to execute trades to reach a portfolio target</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void SetExecution(IExecutionModel execution)
        {
            Execution = execution;
        }

        /// <summary>
        /// Sets the risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining how risk is managed</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void SetRiskManagement(IRiskManagementModel riskManagement)
        {
            RiskManagement = riskManagement;
        }

        /// <summary>
        /// Adds a new risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining how risk is managed to add</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void AddRiskManagement(IRiskManagementModel riskManagement)
        {
            if (RiskManagement.GetType() != typeof(NullRiskManagementModel))
            {
                var compositeRiskModel = RiskManagement as CompositeRiskManagementModel;
                if (compositeRiskModel != null)
                {
                    compositeRiskModel.AddRiskManagement(riskManagement);
                }
                else
                {
                    RiskManagement = new CompositeRiskManagementModel(RiskManagement, riskManagement);
                }
            }
            else
            {
                RiskManagement = riskManagement;
            }
        }

        /// <summary>
        /// Manually emit insights from an algorithm.
        /// This is typically invoked before calls to submit orders in algorithms written against
        /// QCAlgorithm that have been ported into the algorithm framework.
        /// </summary>
        /// <param name="insights">The array of insights to be emitted</param>
        [DocumentationAttribute(AlgorithmFramework)]
        public void EmitInsights(params Insight[] insights)
        {
            if (IsWarmingUp)
            {
                if (!_isEmitWarmupInsightWarningSent)
                {
                    Error("Warning: insights emitted during algorithm warmup are ignored.");
                    _isEmitWarmupInsightWarningSent = true;
                }
                return;
            }

            insights = InitializeInsights(insights);
            OnInsightsGenerated(insights);
            ProcessInsights(insights);
        }

        /// <summary>
        /// Manually emit insights from an algorithm.
        /// This is typically invoked before calls to submit orders in algorithms written against
        /// QCAlgorithm that have been ported into the algorithm framework.
        /// </summary>
        /// <param name="insight">The insight to be emitted</param>
        [DocumentationAttribute(AlgorithmFramework)]
        public void EmitInsights(Insight insight)
        {
            EmitInsights(new[] { insight });
        }

        /// <summary>
        /// Helper method used to validate insights and prepare them to be emitted
        /// </summary>
        /// <param name="insights">insights preparing to be emitted</param>
        /// <returns>Validated insights</returns>
        private Insight[] InitializeInsights(Insight[] insights)
        {
            List<Insight> validInsights = null;
            for (var i = 0; i < insights.Length; i++)
            {
                var security = Securities[insights[i].Symbol];
                if (security.IsDelisted)
                {
                    if (!_isEmitDelistedInsightWarningSent)
                    {
                        Error($"QCAlgorithm.EmitInsights(): Warning: cannot emit insights for delisted securities, these will be discarded");
                        _isEmitDelistedInsightWarningSent = true;
                    }

                    // If this is our first invalid insight, create the list and fill it with previous values
                    if (validInsights == null)
                    {
                        validInsights = new List<Insight>() {};
                        for (var j = 0; j < i; j++)
                        {
                            validInsights.Add(insights[j]);
                        }
                    }
                }
                else
                {
                    // Initialize the insight fields
                    insights[i] = InitializeInsightFields(insights[i], security);

                    // If we already had an invalid insight, this will have been initialized storing the valid ones.
                    if (validInsights != null)
                    {
                        validInsights.Add(insights[i]);
                    }
                }
            }

            return validInsights == null ? insights : validInsights.ToArray();

        }

        /// <summary>
        /// Helper class used to set values not required to be set by alpha models
        /// </summary>
        /// <param name="insight">The <see cref="Insight"/> to set the values for</param>
        /// <param name="security">The <see cref="Security"/> instance associated with this insight</param>
        /// <returns>The same <see cref="Insight"/> instance with the values set</returns>
        private Insight InitializeInsightFields(Insight insight, Security security)
        {
            insight.GeneratedTimeUtc = UtcTime;
            switch (insight.Type)
            {
                case InsightType.Price:
                    insight.ReferenceValue = security.Price;
                    break;
                case InsightType.Volatility:
                    insight.ReferenceValue = security.VolatilityModel.Volatility;
                    break;
            }
            insight.SourceModel = string.IsNullOrEmpty(insight.SourceModel) ? Alpha.GetModelName() : insight.SourceModel;

            var exchangeHours = MarketHoursDatabase.GetExchangeHours(insight.Symbol.ID.Market, insight.Symbol, insight.Symbol.SecurityType);
            insight.SetPeriodAndCloseTime(exchangeHours);
            return insight;
        }
    }
}
