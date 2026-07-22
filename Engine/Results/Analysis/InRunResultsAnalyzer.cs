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
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Runs a reduced suite of backtest diagnostic tests periodically while the backtest is still running,
    /// against a snapshot of the current intermediate results.
    /// </summary>
    public class InRunResultsAnalyzer : ResultsAnalyzer
    {
        /// <summary>
        /// Analyses that read the current backtest state (statistics, orders, charts) instead of scanning
        /// the append-only order event and log streams. They must run against the full current state on
        /// every run, and their previous findings are replaced instead of accumulated.
        /// </summary>
        private static readonly HashSet<string> StateBasedAnalyses = new()
        {
            nameof(PortfolioValueIsNotPositiveAnalysis),
            nameof(TakeProfitAndStopLossOrdersAnalysis),
            nameof(PortfolioMarginUsageAnalysis),
        };

        private readonly Dictionary<string, QuantConnect.Analysis> _findings = new();

        /// <summary>
        /// The number of order events already consumed by previous runs. The order events
        /// in the result passed to <see cref="Run(Result, IReadOnlyList{string}, int, int)"/>
        /// are expected to start at this position.
        /// </summary>
        public int OrderEventsPosition { get; private set; }

        /// <summary>
        /// The number of log entries already consumed by previous runs. The logs passed to
        /// <see cref="Run(Result, IReadOnlyList{string}, int, int)"/> are expected to start
        /// at this position.
        /// </summary>
        public int LogsPosition { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InRunResultsAnalyzer"/> class.
        /// The instance is expected to be kept alive for the duration of the backtest,
        /// receiving fresh data on each <see cref="Run(Result, IReadOnlyList{string}, int, int)"/> call.
        /// </summary>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        public InRunResultsAnalyzer(QCAlgorithm algorithm, Language language)
            : base(null, algorithm, language, null)
        {
        }

        /// <summary>
        /// Runs the analyses incrementally: <paramref name="result"/> and <paramref name="logs"/> are
        /// expected to contain only the order events and log lines produced since the previous run
        /// (per <see cref="OrderEventsPosition"/> and <see cref="LogsPosition"/>), and the returned
        /// findings are the merge of this run's findings into the ones accumulated by previous runs.
        /// Findings from analyses scanning the order event and log streams are accumulated
        /// (first sample kept, counts totaled), while findings from state-based analyses are
        /// replaced on every run.
        /// </summary>
        /// <param name="result">A snapshot of the current intermediate backtest result, holding only new order events.</param>
        /// <param name="logs">The log lines produced since the previous run.</param>
        /// <param name="timeLimitSeconds">Wall-clock seconds allowed for the full chain before early exit.</param>
        /// <param name="maxFailedAnalyses">Maximum number of failing analyses to return.</param>
        /// <returns>The accumulated findings, ranked by analysis weight.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(Result result, IReadOnlyList<string> logs, int timeLimitSeconds = 1, int maxFailedAnalyses = 10)
        {
            SetAnalysisData(result, logs);
            var newFindings = Run(timeLimitSeconds, maxFailedAnalyses);

            // The positions are advanced even when the time limit truncates a run, so the analyses that
            // didn't get to run miss this delta until the final analysis re-scans the complete streams.
            // Stress tests show runs complete in a fraction of the time limit, but if its trace message
            // starts showing up in logs, revisit this (e.g. track per-analysis positions).
            OrderEventsPosition += result.OrderEvents?.Count ?? 0;
            LogsPosition += logs?.Count ?? 0;

            // State-based analyses are recomputed from scratch each run: remove their previous
            // findings so they are replaced, or dropped if they no longer fail
            foreach (var name in _findings.Keys.Where(IsStateBased).ToList())
            {
                _findings.Remove(name);
            }

            foreach (var finding in newFindings)
            {
                if (!IsStateBased(finding.Name) && _findings.TryGetValue(finding.Name, out var previous))
                {
                    // This run only saw new order events and logs: keep the first sample and total the counts.
                    // A null count means a single occurrence
                    finding.Sample = previous.Sample;
                    finding.Count = (previous.Count ?? 1) + (finding.Count ?? 1);
                }
                _findings[finding.Name] = finding;
            }

            var weights = GetAnalyses().ToDictionary(analysis => analysis.GetType().Name, analysis => analysis.Weight);
            return _findings.Values
                .OrderByDescending(finding => weights.GetValueOrDefault(BaseAnalysisName(finding.Name)))
                .Take(maxFailedAnalyses)
                .ToList();
        }

        /// <summary>
        /// Determines whether the given finding was produced by a state-based analysis.
        /// </summary>
        private static bool IsStateBased(string findingName) => StateBasedAnalyses.Contains(BaseAnalysisName(findingName));

        /// <summary>
        /// Gets the analysis class name from a finding name, which aggregated
        /// analyses suffix with the sub-analysis name.
        /// </summary>
        private static string BaseAnalysisName(string findingName)
        {
            var separatorIndex = findingName.IndexOf(" / ", StringComparison.Ordinal);
            return separatorIndex < 0 ? findingName : findingName[..separatorIndex];
        }

        /// <summary>
        /// The equity and benchmark curves are not built for in-run analysis:
        /// none of the in-run analyses read them, and building them would issue
        /// a benchmark history request on every run.
        /// </summary>
        protected override bool RequiresEquityCurves => false;

        /// <summary>
        /// Creates the set of diagnostic analyses to run while the backtest is in progress.
        /// Only analyses that read the result snapshot (logs, orders, order events, charts) are
        /// included: they are cheap, thread-safe, and detect error conditions whose findings
        /// don't depend on the backtest being complete. Curve and statistics based analyses are
        /// left to the final analysis, since partial-period statistics are noisy and require
        /// history requests.
        /// </summary>
        protected override IReadOnlyCollection<BaseResultsAnalysis> GetAnalyses() => new BaseResultsAnalysis[]
        {
            new PortfolioValueIsNotPositiveAnalysis(),
            new InsufficientBuyingPowerOrderResponseErrorAnalysis(),
            new MarginCallsAnalysis(),
            new ExceedsShortableQuantityOrderResponseErrorAnalysis(),
            new SecurityPriceZeroOrderResponseErrorAnalysis(),
            new OrderQuantityZeroOrderResponseErrorAnalysis(),
            new NonTradableSecurityOrderResponseErrorAnalysis(),
            new BrokerageModelRefusedToSubmitOrderOrderResponseErrorAnalysis(),
            new BrokerageModelRefusedToUpdateOrderOrderResponseErrorAnalysis(),
            new TakeProfitAndStopLossOrdersAnalysis(),
            new StaleOrderFillsAnalysis(),
            new AlgorithmWarmingUpOrderResponseErrorAnalysis(),
            new ExchangeNotOpenOrderResponseErrorAnalysis(),
            new ForexConversionRateZeroOrderResponseErrorAnalysis(),
            new ExceededMaximumOrdersOrderResponseErrorAnalysis(),
            new UnsupportedOptionShortPositionExerciseAnalysis(),
            new UnsupportedOptionExerciseQuantityAnalysis(),
            new EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis(),
            new OptionOrderOnStockSplitOrderResponseErrorAnalysis(),
            new MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis(),
            new OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis(),
            new InsightsEmittedForDelistedSecuritiesAnalysis(),
            new PortfolioMarginUsageAnalysis(),
        };
    }
}
