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
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Runs the suite of backtest diagnostic tests against a single backtest, in one of two modes
    /// depending on how the instance is created:
    /// a <b>final analysis</b> instance is created with the completed result and logs, and runs the
    /// full analysis set once; an <b>in-run analysis</b> instance is created with the engine's
    /// speed counters, is kept alive for the duration of the backtest, and periodically runs the
    /// in-run capable analyses incrementally against the intermediate results and logs.
    /// </summary>
    public class ResultsAnalyzer
    {
        private readonly QCAlgorithm _algorithm;
        private readonly Language _language;
        /// <summary>
        /// The number of times an in-run finding is returned before it is muted and no longer
        /// reported, so a recurring finding is not re-sent to consumers (like LLMs) on every update.
        /// </summary>
        private const int MaxFindingReports = 3;

        private readonly bool _isInRun;
        private readonly DateTime _startTime;
        private readonly PerformanceTrackingTool _performanceTrackingTool;
        private readonly BacktestProgressMonitor _progressMonitor;
        private readonly Dictionary<string, QuantConnect.Analysis> _findings = new();

        /// <summary>
        /// The number of times each finding has been returned, muting it once it reaches
        /// <see cref="MaxFindingReports"/>. Never reset: a muted finding that clears and later
        /// fails again stays muted.
        /// </summary>
        private readonly Dictionary<string, int> _findingReportCounts = new();
        private IReadOnlyList<string> _logs;
        private SortedList<DateTime, decimal> _equityCurve;
        private SortedList<DateTime, decimal> _benchmarkEquityCurve;
        private Result _result;
        private IReadOnlyCollection<BaseResultsAnalysis> _analyses;

        /// <summary>
        /// The names of the analyses in the in-run set that declare themselves state-based
        /// (see <see cref="BaseResultsAnalysis.IsStateBased"/>), whose findings are replaced
        /// on every run instead of accumulated.
        /// </summary>
        private HashSet<string> _stateBasedAnalyses;

        /// <summary>
        /// The weight of each analysis by name, for ranking the findings.
        /// </summary>
        private Dictionary<string, int> _analysisWeights;

        /// <summary>
        /// The identity of the newest order event analyzed by previous in-run runs: the order id
        /// and the per-order event id. The intermediate results carry a truncated, newest-first
        /// window of the order events, so each run consumes the window until it finds this
        /// watermark. When the watermark is not in the window the whole window is new, and any
        /// events already evicted from it are missed until the final analysis re-scans the
        /// complete stream.
        /// </summary>
        private (int OrderId, int Id)? _lastAnalyzedOrderEvent;

        /// <summary>
        /// The number of log entries already consumed by previous in-run runs, from which the
        /// next run resumes reading the log stream.
        /// </summary>
        private int _logsPosition;

        /// <summary>
        /// Whether this instance was created for in-run analysis of a backtest still in progress
        /// (see <see cref="CreateForInRunAnalysis"/>), as opposed to the final analysis of a
        /// completed backtest.
        /// </summary>
        protected bool IsInRun => _isInRun;

        /// <summary>
        /// The diagnostic analyses to run, in execution order: descending by weight, so changing an
        /// analysis weight automatically reorders execution. Created once and reused across runs,
        /// since the analyses are stateless and their weights are constant. In-run instances filter
        /// the set to the analyses that declare they can run while the backtest is in progress
        /// (see <see cref="BaseResultsAnalysis.RunsInRun"/>).
        /// </summary>
        protected IReadOnlyCollection<BaseResultsAnalysis> Analyses => _analyses ??= (IsInRun
                ? GetAnalyses().Where(analysis => analysis.RunsInRun)
                : GetAnalyses())
            .OrderByDescending(analysis => analysis.Weight)
            .ToList();

        /// <summary>
        /// Whether the equity and benchmark curves should be built before running the analyses.
        /// Building them requires a benchmark history request, so in-run instances skip it:
        /// none of the in-run analyses read the curves, and building them would issue the
        /// history request on every run.
        /// </summary>
        protected virtual bool RequiresEquityCurves => !IsInRun;

        /// <summary>
        /// The speed metrics tracked for the running backtest, made available to the analyses
        /// through <see cref="ResultsAnalysisRunParameters.Speed"/>. An in-run instance owns its
        /// tracker and feeds it a sample on each run; the final analysis instance receives the same
        /// tracker so the speed analysis also runs against the full-run metrics. Null when speed is
        /// not tracked.
        /// </summary>
        protected AlgorithmSpeedTracker SpeedTracker { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalyzer"/> class for the final
        /// analysis of a completed backtest. Use <see cref="CreateForFinalAnalysis"/> or
        /// <see cref="CreateForInRunAnalysis"/> to create instances.
        /// </summary>
        /// <param name="result">The backtest result to analyze.</param>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="logs">The full list of log lines produced by the backtest.</param>
        /// <param name="speedTracker">The speed metrics tracked for the backtest, or null when speed is not tracked.</param>
        protected ResultsAnalyzer(Result result, QCAlgorithm algorithm, Language language, IReadOnlyList<string> logs,
            AlgorithmSpeedTracker speedTracker = null)
        {
            _result = result;
            _algorithm = algorithm;
            _language = language;
            _logs = logs;
            SpeedTracker = speedTracker;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalyzer"/> class for in-run analysis
        /// of a backtest still in progress. Use <see cref="CreateForInRunAnalysis"/> to create instances.
        /// </summary>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="startTime">The UTC time the backtest started, for the speed samples' elapsed time.</param>
        /// <param name="performanceTrackingTool">The engine's data point counters, for the speed samples.</param>
        /// <param name="progressMonitor">The backtest day-progress monitor, for the speed samples.</param>
        protected ResultsAnalyzer(QCAlgorithm algorithm, Language language, DateTime startTime,
            PerformanceTrackingTool performanceTrackingTool, BacktestProgressMonitor progressMonitor)
            : this(null, algorithm, language, null, new AlgorithmSpeedTracker())
        {
            _isInRun = true;
            _startTime = startTime;
            _performanceTrackingTool = performanceTrackingTool;
            _progressMonitor = progressMonitor;
        }

        /// <summary>
        /// Creates an analyzer for the final analysis of a completed backtest, running the full
        /// analysis set once through <see cref="Run(int, int)"/>.
        /// </summary>
        /// <param name="result">The backtest result to analyze.</param>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="logs">The full list of log lines produced by the backtest.</param>
        /// <param name="speedTracker">The speed metrics tracked for the backtest, typically completed by the
        /// in-run analyzer through <see cref="CompleteSpeedTracking"/>, or null when speed is not tracked.</param>
        /// <returns>The final analysis instance.</returns>
        public static ResultsAnalyzer CreateForFinalAnalysis(Result result, QCAlgorithm algorithm, Language language,
            IReadOnlyList<string> logs, AlgorithmSpeedTracker speedTracker = null)
        {
            return new ResultsAnalyzer(result, algorithm, language, logs, speedTracker);
        }

        /// <summary>
        /// Creates an analyzer for in-run analysis of a backtest still in progress. The instance is
        /// expected to be kept alive for the duration of the backtest, sampling the given engine
        /// speed counters on each <see cref="Run(BacktestResult, IReadOnlyList{string}, AlgorithmPerformance, int, int)"/> call.
        /// </summary>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="startTime">The UTC time the backtest started, for the speed samples' elapsed time.</param>
        /// <param name="performanceTrackingTool">The engine's data point counters, for the speed samples.</param>
        /// <param name="progressMonitor">The backtest day-progress monitor, for the speed samples.</param>
        /// <returns>The in-run analysis instance.</returns>
        public static ResultsAnalyzer CreateForInRunAnalysis(QCAlgorithm algorithm, Language language,
            DateTime startTime, PerformanceTrackingTool performanceTrackingTool, BacktestProgressMonitor progressMonitor)
        {
            return new ResultsAnalyzer(algorithm, language, startTime, performanceTrackingTool, progressMonitor);
        }

        // ── Test chain ────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs all registered diagnostic checks against the backtest in weight order,
        /// stopping early when the time limit or maximum failure count is reached.
        /// </summary>
        /// <param name="timeLimitSeconds">Wall-clock seconds allowed for the full chain before early exit.</param>
        /// <param name="maxFailedAnalyses">Maximum number of failing analyses to collect before stopping; also the max returned.</param>
        /// <returns>Up to <paramref name="maxFailedAnalyses"/> <see cref="QuantConnect.Analysis"/> entries with solutions, ranked by weight.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(int timeLimitSeconds = 5, int maxFailedAnalyses = 10)
        {
            var analyses = Analyses;
            if (analyses.Count == 0)
            {
                return [];
            }

            _equityCurve = new();
            _benchmarkEquityCurve = new();
            if (RequiresEquityCurves)
            {
                (_equityCurve, _benchmarkEquityCurve) = ReadEquityCurve(_result, _algorithm);
            }

            var parameters = new ResultsAnalysisRunParameters(_result, _algorithm, _language, _logs, _equityCurve, _benchmarkEquityCurve, SpeedTracker);

            var responses = new List<QuantConnect.Analysis>();
            var timer = Stopwatch.StartNew();
            var timeLimit = TimeSpan.FromSeconds(timeLimitSeconds);

            foreach (var analysis in analyses)
            {
                if (responses.Count >= maxFailedAnalyses)
                {
                    Log.Trace($"ResultsAnalyzer: reached maximum failed analyses count of {maxFailedAnalyses}, stopping analysis chain.");
                    break;
                }
                if (timer.Elapsed >= timeLimit)
                {
                    Log.Trace($"ResultsAnalyzer: reached time limit of {timeLimitSeconds} seconds (current running time: {timer.Elapsed.TotalSeconds}s), stopping analysis chain.");
                    break;
                }

                foreach (var result in analysis.Run(parameters))
                {
                    if (result.Solutions.Count > 0)
                    {
                        responses.Add(result);
                    }
                }
            }

            return responses;
        }

        /// <summary>
        /// Runs the in-run analyses against the given intermediate backtest result and the log
        /// lines produced since the previous run. The returned findings are the merge of this
        /// run's findings into the ones accumulated by previous runs: findings from analyses
        /// scanning the order event and log streams are accumulated (first sample kept, counts
        /// totaled), while findings from state-based analyses are replaced on every run.
        /// </summary>
        /// <param name="result">The current intermediate backtest result. Its orders and order events
        /// are windows truncated to the most recent ones, so the in-run analyses can miss orders and
        /// events already evicted from them; the final analysis re-scans the complete data. Its charts
        /// are the handler's live ones, read without synchronization: a torn read while the algorithm
        /// thread updates them can fail a run, which the handler catches, and the next run retries.</param>
        /// <param name="logs">The full list of log lines produced so far; the analyzer analyzes the
        /// lines past the ones consumed by previous runs.</param>
        /// <param name="totalPerformance">The current total algorithm performance, for analyses that read
        /// portfolio statistics. Withheld from the analyses until the first equity sample exists, since
        /// the statistics are all-zero defaults before that.</param>
        /// <param name="timeLimitSeconds">Wall-clock seconds allowed for the full chain before early exit.
        /// The default is small because the analysis runs on the result handler thread, delaying message
        /// processing while it runs.</param>
        /// <param name="maxFailedAnalyses">Maximum number of failing analyses to return.</param>
        /// <returns>The accumulated findings, ranked by analysis weight.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(BacktestResult result, IReadOnlyList<string> logs,
            AlgorithmPerformance totalPerformance, int timeLimitSeconds = 1, int maxFailedAnalyses = 10)
        {
            ThrowIfNotInRunInstance();

            // Equity is not sampled while the algorithm warms up, so until the first sample exists
            // the generated statistics are all-zero defaults that would flag a false non-positive
            // portfolio value finding. Withhold them so the analyses reading them skip instead
            if (_algorithm?.IsWarmingUp == true || !HasEquitySamples(result))
            {
                totalPerformance = null;
            }

            var snapshot = new BacktestResult(new BacktestResultParameters(
                result?.Charts ?? new Dictionary<string, Chart>(),
                result?.Orders ?? new Dictionary<int, Order>(),
                result?.ProfitLoss ?? new Dictionary<DateTime, decimal>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, AlgorithmPerformance>(),
                ExtractNewOrderEvents(result?.OrderEvents),
                totalPerformance));
            var newLogs = logs?.Skip(_logsPosition).ToList();

            // The analyses run during warm-up too (the speed sample is null then), so conditions like
            // orders submitted while the algorithm warms up surface without waiting for warm-up to end
            return Run(snapshot, newLogs, TakeSpeedSample(), timeLimitSeconds, maxFailedAnalyses);
        }

        /// <summary>
        /// Takes a sample of the engine speed counters for the algorithm speed analysis.
        /// Null while the algorithm warms up, since the warm-up pace would skew the speed metrics.
        /// </summary>
        protected virtual AlgorithmSpeedSample? TakeSpeedSample()
        {
            if (_algorithm == null || _algorithm.IsWarmingUp)
            {
                return null;
            }

            return new AlgorithmSpeedSample(
                DateTime.UtcNow - _startTime,
                _performanceTrackingTool?.DataPoints ?? 0,
                _performanceTrackingTool?.HistoryDataPoints ?? 0,
                _progressMonitor?.ProcessedDays ?? 0,
                _progressMonitor?.TotalDays ?? 0);
        }

        /// <summary>
        /// Whether the strategy equity chart in the given result has samples yet. Until the first
        /// sample exists, the generated statistics are all-zero defaults.
        /// </summary>
        private static bool HasEquitySamples(Result result)
        {
            return result?.Charts != null &&
                result.Charts.TryGetValue(BaseResultsHandler.StrategyEquityKey, out var equityChart) &&
                equityChart.Series.TryGetValue(BaseResultsHandler.EquityKey, out var equitySeries) &&
                equitySeries.Values.Count > 0;
        }

        /// <summary>
        /// Extracts the order events not yet analyzed from the newest-first, truncated order events
        /// window the intermediate results carry, returned in chronological order, and advances the
        /// watermark to the newest one. The watermark advances even if the time limit later
        /// truncates the run, so analyses that didn't get to run miss this delta until the final
        /// analysis re-scans the complete stream — same trade-off as the log position advancement.
        /// </summary>
        private List<OrderEvent> ExtractNewOrderEvents(IReadOnlyList<OrderEvent> orderEvents)
        {
            var newOrderEvents = new List<OrderEvent>();
            for (var i = 0; i < (orderEvents?.Count ?? 0); i++)
            {
                var orderEvent = orderEvents[i];
                if (orderEvent.OrderId == _lastAnalyzedOrderEvent?.OrderId && orderEvent.Id == _lastAnalyzedOrderEvent?.Id)
                {
                    break;
                }
                newOrderEvents.Add(orderEvent);
            }

            if (newOrderEvents.Count > 0)
            {
                newOrderEvents.Reverse();
                var newest = newOrderEvents[^1];
                _lastAnalyzedOrderEvent = (newest.OrderId, newest.Id);
            }

            return newOrderEvents;
        }

        /// <summary>
        /// Completes the speed metrics with one final sample so they cover the backtest through
        /// its end, and returns the tracker for the final analysis to reuse. The tracker is left
        /// untouched when no sample can be taken, like when the algorithm never left warm-up.
        /// </summary>
        public AlgorithmSpeedTracker CompleteSpeedTracking()
        {
            ThrowIfNotInRunInstance();

            var speedSample = TakeSpeedSample();
            if (speedSample.HasValue)
            {
                SpeedTracker.AddSample(speedSample.Value);
            }
            return SpeedTracker;
        }

        /// <summary>
        /// Sets the backtest data to analyze. Used by in-run instances, which are kept alive
        /// and run multiple times against fresh data.
        /// </summary>
        /// <param name="result">The backtest result to analyze.</param>
        /// <param name="logs">The list of log lines to analyze.</param>
        protected void SetAnalysisData(Result result, IReadOnlyList<string> logs)
        {
            _result = result;
            _logs = logs;
        }

        /// <summary>
        /// Creates the full set of diagnostic analyses to run against the backtest.
        /// Each analysis declares through <see cref="BaseResultsAnalysis.RunsInRun"/> whether it can
        /// also run while the backtest is in progress, which in-run instances filter this set by.
        /// </summary>
        protected virtual IReadOnlyCollection<BaseResultsAnalysis> GetAnalyses() =>
        [
            new SingleTimeLoopTimeoutRuntimeErrorAnalysis(),
            new PortfolioValueIsNotPositiveAnalysis(),
            new FlatEquityCurveAnalysis(),
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
            new OrderFillsDuringExtendedMarketHoursAnalysis(),
            new ExceededMaximumOrdersOrderResponseErrorAnalysis(),
            new UnsupportedOptionShortPositionExerciseAnalysis(),
            new UnsupportedOptionExerciseQuantityAnalysis(),
            new EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis(),
            new OptionOrderOnStockSplitOrderResponseErrorAnalysis(),
            new MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis(),
            new MarketOnCloseOrderTooLateOrderResponseErrorAnalysis(),
            new OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis(),
            new InsightsEmittedForDelistedSecuritiesAnalysis(),
            new StatisticalSignificanceOfDailyReturnsAnalysis(),
            new PerformanceRelativeToBenchmarkAnalysis(),
            new CrisisEventsAnalysis(),
            new AlgorithmSpeedAnalysis(),
            new PortfolioMarginUsageAnalysis(),
            new ParameterCountAnalysis(),
            new MonteCarloPercentileAnalysis(),
        ];

        /// <summary>
        /// Runs the in-run analyses incrementally: <paramref name="result"/> and <paramref name="logs"/>
        /// are expected to contain only the order events and log lines produced since the previous run,
        /// and the returned findings are the merge of this run's findings into the ones accumulated
        /// by previous runs.
        /// </summary>
        /// <param name="result">A snapshot of the current intermediate backtest result, holding only new order events.</param>
        /// <param name="logs">The log lines produced since the previous run.</param>
        /// <param name="speedSample">A sample of the engine speed counters for the algorithm speed analysis.
        /// Null when the counters should not be sampled, like while the algorithm warms up.</param>
        /// <param name="timeLimitSeconds">Wall-clock seconds allowed for the full chain before early exit.</param>
        /// <param name="maxFailedAnalyses">Maximum number of failing analyses to return.</param>
        /// <returns>The accumulated findings, ranked by analysis weight.</returns>
        private IReadOnlyList<QuantConnect.Analysis> Run(Result result, IReadOnlyList<string> logs, AlgorithmSpeedSample? speedSample,
            int timeLimitSeconds, int maxFailedAnalyses)
        {
            SetAnalysisData(result, logs);
            if (speedSample.HasValue)
            {
                SpeedTracker.AddSample(speedSample.Value);
            }
            var newFindings = Run(timeLimitSeconds, maxFailedAnalyses);

            // The log position is advanced (like the order event watermark was on extraction) even
            // when the time limit truncates a run, so the analyses that didn't get to run miss this
            // delta until the final analysis re-scans the complete streams. Stress tests show runs
            // complete in a fraction of the time limit, but if its trace message starts showing up
            // in logs, revisit this (e.g. track per-analysis positions).
            _logsPosition += logs?.Count ?? 0;

            // State-based analyses are recomputed from scratch each run: remove their previous
            // findings so they are replaced, or dropped if they no longer fail. If a time-limit
            // truncated run skipped one of them, its finding drops until a run reaches it again —
            // same trade-off as the positions advancement above
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

            return RankFindings(maxFailedAnalyses);
        }

        /// <summary>
        /// Ranks the accumulated findings by their analysis weight, capped to the given maximum.
        /// Findings already returned <see cref="MaxFindingReports"/> times are muted and left out,
        /// and each returned finding counts one report toward that cap.
        /// </summary>
        private IReadOnlyList<QuantConnect.Analysis> RankFindings(int maxFailedAnalyses)
        {
            _analysisWeights ??= Analyses.ToDictionary(analysis => analysis.GetType().Name, analysis => analysis.Weight);
            var findings = _findings.Values
                .Where(finding => _findingReportCounts.GetValueOrDefault(finding.Name) < MaxFindingReports)
                .OrderByDescending(finding => _analysisWeights.GetValueOrDefault(BaseAnalysisName(finding.Name)))
                .Take(maxFailedAnalyses)
                .ToList();

            // Only findings that are actually returned count toward muting, so a finding a
            // truncated run never got to report is not silenced before it is seen
            foreach (var finding in findings)
            {
                _findingReportCounts[finding.Name] = _findingReportCounts.GetValueOrDefault(finding.Name) + 1;
            }

            return findings;
        }

        /// <summary>
        /// Determines whether the given finding was produced by a state-based analysis.
        /// </summary>
        private bool IsStateBased(string findingName)
        {
            _stateBasedAnalyses ??= Analyses
                .Where(analysis => analysis.IsStateBased)
                .Select(analysis => analysis.GetType().Name)
                .ToHashSet();
            return _stateBasedAnalyses.Contains(BaseAnalysisName(findingName));
        }

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
        /// Throws when this instance was not created for in-run analysis, guarding the members
        /// that track incremental state across runs.
        /// </summary>
        private void ThrowIfNotInRunInstance()
        {
            if (!IsInRun)
            {
                throw new InvalidOperationException("This operation requires an instance created for in-run analysis.");
            }
        }

        /// <summary>
        /// Reads the backtest's "Strategy Equity" chart and fetches SPY daily history to build
        /// two time-aligned equity curves: one for the backtest and one for the benchmark.
        /// </summary>
        /// <param name="result">The backtest result containing the charts.</param>
        /// <param name="algorithm">The algorithm instance used to retrieve SPY history.</param>
        /// <returns>
        /// A tuple of two <see cref="SortedList{TKey,TValue}"/> instances sharing the same timestamp keys:
        /// the first is the backtest equity curve, the second is the SPY benchmark curve.
        /// </returns>
        private static (SortedList<DateTime, decimal> BacktestEquity, SortedList<DateTime, decimal> BenchmarkEquity) ReadEquityCurve(Result result, QCAlgorithm algorithm)
        {
            // ── 1. backtest equity from "Strategy Equity" chart ──────────────────
            BaseSeries equitySeries;
            if (result.Charts.TryGetValue("Strategy Equity", out var chart) &&
                chart.Series.TryGetValue("Equity", out var series) && series.Values.Count > 0)
            {
                equitySeries = series;
            }
            else
            {
                return (new(), new());
            }

            // ── 2. Benchmark from SPY history ─────────────────────────────────────
            var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var exchangeTimeZone = MarketHoursDatabase.FromDataFolder()
                .GetExchangeHours(spy.ID.Market, spy, spy.ID.SecurityType)
                .TimeZone;

            var benchmarkSeries = new SortedList<DateTime, decimal>();
            var historyStart = algorithm.StartDate - TimeSpan.FromDays(3);
            var historyEnd = algorithm.EndDate + TimeSpan.FromDays(1);
            foreach (var bar in algorithm.History(spy, historyStart, historyEnd, Resolution.Daily))
            {
                var time = algorithm.Settings.DailyPreciseEndTime ? bar.EndTime.AddDays(1).Date : bar.EndTime;
                benchmarkSeries.Add(time.Date.ConvertToUtc(exchangeTimeZone), bar.Close);
            }

            if (benchmarkSeries.Count == 0)
            {
                return (new(), new());
            }

            // ── 3. Resample both to daily data points ────────────────────────────
            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var start = new DateTime(
                Math.Max(equitySeries.Values.First().Time.Ticks, benchmarkSeries.Keys.First().Ticks));
            var stop = new DateTime(
                Math.Min(equitySeries.Values.Last().Time.Ticks, benchmarkSeries.Keys.Last().Ticks));

            var sampledEquity = sampler.Sample(equitySeries, start, stop);

            // ── 4. Align the two curves on the same timestamps ───────────────────
            var equityByTime = new SortedList<DateTime, decimal>(
                sampledEquity.Values.ToDictionary(
                    p => p.Time,
                    p => p is Candlestick c ? c.Close ?? 0m : ((ChartPoint)p).Y ?? 0m));

            var commonKeys = equityByTime.Keys.Intersect(benchmarkSeries.Keys).ToHashSet();
            var alignedEquity = new SortedList<DateTime, decimal>(
                equityByTime.Where(kv => commonKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
            var alignedBenchmark = new SortedList<DateTime, decimal>(
                benchmarkSeries.Where(kv => commonKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

            return (alignedEquity, alignedBenchmark);
        }
    }
}
