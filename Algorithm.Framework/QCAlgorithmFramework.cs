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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework
{
    /// <summary>
    /// Algorithm framework base class that enforces a modular approach to algorithm development
    /// </summary>
    public partial class QCAlgorithmFramework : QCAlgorithm
    {
        private readonly ISecurityValuesProvider _securityValuesProvider;

        /// <summary>
        /// Enables additional logging of framework models including:
        /// All insights, portfolio targets, order events, and any risk management altered targets
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// Returns true since algorithms derived from this use the framework
        /// </summary>
        public override bool IsFrameworkAlgorithm => true;

        /// <summary>
        /// Gets or sets the universe selection model.
        /// </summary>
        public IUniverseSelectionModel UniverseSelection { get; set; }

        /// <summary>
        /// Gets or sets the alpha model
        /// </summary>
        public IAlphaModel Alpha { get; set; }

        /// <summary>
        /// Gets or sets the portoflio construction model
        /// </summary>
        public IPortfolioConstructionModel PortfolioConstruction { get; set; }

        /// <summary>
        /// Gets or sets the execution model
        /// </summary>
        public IExecutionModel Execution { get; set; }

        /// <summary>
        /// Gets or sets the risk management model
        /// </summary>
        public IRiskManagementModel RiskManagement { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QCAlgorithmFramework"/> class
        /// </summary>
        public QCAlgorithmFramework()
        {
            _securityValuesProvider = new AlgorithmSecurityValuesProvider(this);

            // set model defaults
            Execution = new ImmediateExecutionModel();
            RiskManagement = new NullRiskManagementModel();
        }

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        public override void PostInitialize()
        {
            CheckModels();

            foreach (var universe in UniverseSelection.CreateUniverses(this))
            {
                AddUniverse(universe);
            }

            if (DebugMode)
            {
                InsightsGenerated += (algorithm, data) => Log($"{Time}: {string.Join(" | ", data.Insights.OrderBy(i => i.Symbol.ToString()))}");
            }

            // emit warning message about using the framework with cash modelling
            if (BrokerageModel.AccountType == AccountType.Cash)
            {
                Error("These models are currently unsuitable for Cash Modeled brokerages (e.g. GDAX) and may result in unexpected trades."
                    + " To prevent possible user error we've restricted them to Margin trading. You can select margin account types with"
                    + " SetBrokerage( ... AccountType.Margin)");
            }

            base.PostInitialize();
        }

        /// <summary>
        /// Used to send data updates to algorithm framework models
        /// </summary>
        /// <param name="slice">The current data slice</param>
        public sealed override void OnFrameworkData(Slice slice)
        {
            // generate, timestamp and emit insights
            var insights = Alpha.Update(this, slice)
                .Select(SetGeneratedAndClosedTimes)
                .ToArray();

            // only fire insights generated event if we actually have insights
            if (insights.Length != 0)
            {
                // debug printing of generated insights
                if (DebugMode)
                {
                    Log($"{Time}: ALPHA: {string.Join(" | ", insights.Select(i => i.ToString()).OrderBy(i => i))}");
                }

                OnInsightsGenerated(insights);
            }

            // construct portfolio targets from insights
            var targets = PortfolioConstruction.CreateTargets(this, insights).ToArray();

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

            var riskTargetOverrides = RiskManagement.ManageRisk(this, targets).ToArray();

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

            // execute on the targets, overriding targets for symbols w/ risk targets
            var riskAdjustedTargets = riskTargetOverrides.Concat(targets).DistinctBy(pt => pt.Symbol).ToArray();

            if (DebugMode && riskAdjustedTargets.Length > 0)
            {
                Log($"{Time}: RISK ADJUSTED TARGETS: {string.Join(" | ", riskTargetOverrides.Select(t => t.ToString()).OrderBy(t => t))}");
            }

            Execution.Execute(this, riskAdjustedTargets);
        }

        /// <summary>
        /// Used to send security changes to algorithm framework models
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public sealed override void OnFrameworkSecuritiesChanged(SecurityChanges changes)
        {
            if (DebugMode)
            {
                Log($"{Time}: {changes}");
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
        public void SetUniverseSelection(IUniverseSelectionModel universeSelection)
        {
            UniverseSelection = universeSelection;
        }

        /// <summary>
        /// Sets the alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha</param>
        public void SetAlpha(IAlphaModel alpha)
        {
            Alpha = alpha;
        }

        /// <summary>
        /// Sets the portfolio construction model
        /// </summary>
        /// <param name="portfolioConstruction">Model defining how to build a portoflio from insights</param>
        public void SetPortfolioConstruction(IPortfolioConstructionModel portfolioConstruction)
        {
            PortfolioConstruction = portfolioConstruction;
        }

        /// <summary>
        /// Sets the execution model
        /// </summary>
        /// <param name="execution">Model defining how to execute trades to reach a portfolio target</param>
        public void SetExecution(IExecutionModel execution)
        {
            Execution = execution;
        }

        /// <summary>
        /// Sets the risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining </param>
        public void SetRiskManagement(IRiskManagementModel riskManagement)
        {
            RiskManagement = riskManagement;
        }

        private Insight SetGeneratedAndClosedTimes(Insight insight)
        {
            insight.GeneratedTimeUtc = UtcTime;
            insight.ReferenceValue = _securityValuesProvider.GetValues(insight.Symbol).Get(insight.Type);

            TimeSpan barSize;
            Security security;
            SecurityExchangeHours exchangeHours;
            if (Securities.TryGetValue(insight.Symbol, out security))
            {
                exchangeHours = security.Exchange.Hours;
                barSize = security.Resolution.ToTimeSpan();
            }
            else
            {
                barSize = insight.Period.ToHigherResolutionEquivalent(false).ToTimeSpan();
                exchangeHours = MarketHoursDatabase.GetExchangeHours(insight.Symbol.ID.Market, insight.Symbol, insight.Symbol.SecurityType);
            }

            var localStart = UtcTime.ConvertFromUtc(exchangeHours.TimeZone);
            barSize = QuantConnect.Time.Max(barSize, QuantConnect.Time.OneMinute);
            var barCount = (int) (insight.Period.Ticks / barSize.Ticks);

            insight.CloseTimeUtc = QuantConnect.Time.GetEndTimeForTradeBars(exchangeHours, localStart, barSize, barCount, false).ConvertToUtc(exchangeHours.TimeZone);

            return insight;
        }

        private void CheckModels()
        {
            if (UniverseSelection == null)
            {
                throw new Exception($"Framework algorithms must specify a portfolio selection model using the '{nameof(UniverseSelection)}' property.");
            }
            if (Alpha == null)
            {
                throw new Exception($"Framework algorithms must specify a alpha model using the '{nameof(Alpha)}' property.");
            }
            if (PortfolioConstruction == null)
            {
                throw new Exception($"Framework algorithms must specify a portfolio construction model using the '{nameof(PortfolioConstruction)}' property");
            }
            if (Execution == null)
            {
                throw new Exception($"Framework algorithms must specify an execution model using the '{nameof(Execution)}' property.");
            }
            if (RiskManagement == null)
            {
                throw new Exception($"Framework algorithms must specify an risk management model using the '{nameof(RiskManagement)}' property.");
            }
        }
    }
}
