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
using System.Globalization;
using System.Text.RegularExpressions;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Tracks the algorithm's execution speed from the throughput and progress metrics accumulated
    /// by <see cref="AlgorithmSpeedTracker"/>, reporting slow processing speed, a long projected
    /// remaining runtime, degrading throughput, and history-request-dominated data loads.
    /// It runs periodically while the backtest is in progress, so the user can decide to stop a
    /// slow backtest early, and again on the final analysis against the whole run's metrics.
    /// When the tracked metrics cannot measure the processing speed, the engine's completion log
    /// line is parsed for the whole-run average rate as a fallback; the line only exists once the
    /// backtest ends, so the fallback can only fire on the final analysis.
    /// Benchmark speeds: https://www.quantconnect.com/performance
    /// </summary>
    public class AlgorithmSpeedAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// Matches the engine's completion log line, capturing the execution time and the data
        /// points per second (in thousands). Example match: "Algorithm Id:(Foo) completed in
        /// 25.68 seconds at 85k data points per second." gives seconds=25.68, rate=85.
        /// </summary>
        private static readonly Regex CompletionLogLineRegex = new(
            @"Algorithm Id:\([^)]+\) completed in ([\d.]+) seconds at (\d+)k data points per second\. Processing total of [\d,]+ data points\.",
            RegexOptions.Compiled);

        /// <summary>
        /// The data points per second under which execution is reported as slow, from the platform benchmarks.
        /// </summary>
        public const int SlowDataPointsPerSecond = 40_000;

        /// <summary>
        /// The minimum runtime a completed backtest must have for its whole-run average rate,
        /// parsed from the completion log line, to be worth reporting as slow.
        /// </summary>
        public const int MinimumCompletedRuntimeSeconds = 10;

        /// <summary>
        /// The recent-to-initial throughput ratio under which throughput is reported as degrading.
        /// </summary>
        public const double DegradationRatio = 0.5;

        /// <summary>
        /// The share of recently processed data points served by the history provider
        /// over which the data load is reported as history-request dominated.
        /// </summary>
        public const double HighHistoryDataPointsShare = 0.5;

        /// <summary>
        /// The minimum number of history data points in the recent window for the
        /// history-request load to be worth reporting.
        /// </summary>
        public const long MinimumRecentHistoryDataPoints = 10_000;

        /// <summary>
        /// The minimum wall-clock span the metrics must cover before any finding is reported,
        /// so early warm-up noise doesn't produce false positives.
        /// </summary>
        public static readonly TimeSpan MinimumSampledSpan = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The projected remaining runtime over which the backtest is reported as long-running.
        /// </summary>
        public static readonly TimeSpan LongProjectedRemainingTime = TimeSpan.FromHours(1);

        /// <summary>
        /// The name of the slow execution sub-finding.
        /// </summary>
        public const string SlowExecutionName = "SlowExecution";

        /// <summary>
        /// The name of the long projected runtime sub-finding.
        /// </summary>
        public const string LongProjectedRuntimeName = "LongProjectedRuntime";

        /// <summary>
        /// The name of the degrading throughput sub-finding.
        /// </summary>
        public const string ThroughputDegradationName = "ThroughputDegradation";

        /// <summary>
        /// The name of the history-request load sub-finding.
        /// </summary>
        public const string HistoryRequestLoadName = "HistoryRequestLoad";

        /// <summary>
        /// This analysis reads the current speed metrics instead of scanning the order event
        /// and log streams, so its in-run findings are replaced on every run.
        /// </summary>
        public override bool IsStateBased { get; } = true;

        /// <summary>
        /// Gets the description of the slow algorithm issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm is running slowly.";

        /// <summary>
        /// Gets the severity weight for the algorithm speed analysis. High enough to run before the
        /// order-response error analyses in the in-run chain: this analysis drives the user's decision
        /// to stop a slow backtest, and it is one of the cheapest in the set, so it should not be the
        /// one skipped when the time limit or the failed-analyses cap truncates a run.
        /// </summary>
        public override int Weight { get; } = 96;

        /// <summary>
        /// Runs the algorithm speed analysis against the speed metrics tracked for the backtest,
        /// falling back to the completion log line when they cannot measure the speed.
        /// </summary>
        public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters)
            => Run(parameters.Speed, parameters.Logs, parameters.Language,
                performanceTrackingEnabled: parameters.Algorithm?.Settings.PerformanceSamplePeriod > TimeSpan.Zero);

        /// <summary>
        /// Runs the algorithm speed analysis against the given speed metrics.
        /// Each detected condition is reported as its own sub-finding. Every condition must hold for
        /// both the current recent window and the window as of the previous run, so a single noisy
        /// sample doesn't flag or clear a finding.
        /// When the metrics cannot measure the processing speed — the tracker isn't wired in, the
        /// backtest finished before it got enough samples, or the data point counters aren't fed —
        /// the completion log line's whole-run average is used to detect slow execution instead.
        /// </summary>
        /// <param name="speed">The speed metrics tracked for the running backtest, or null when not tracked.</param>
        /// <param name="logs">The log lines to search for the completion line, or null when not available.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="performanceTrackingEnabled">Whether the algorithm already has performance tracking enabled,
        /// so the findings don't suggest enabling it again.</param>
        /// <returns>The failed sub-findings, or empty when no speed condition failed or none could be measured.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(AlgorithmSpeedTracker speed, IReadOnlyList<string> logs = null,
            Language language = Language.CSharp, bool performanceTrackingEnabled = false)
        {
            var findings = new List<QuantConnect.Analysis>();
            var speedMeasured = false;
            if (speed != null && speed.SampledSpan >= MinimumSampledSpan)
            {
                speedMeasured = AddSlowExecution(speed, findings, language, performanceTrackingEnabled);
                AddLongProjectedRuntime(speed, findings);
                AddThroughputDegradation(speed, findings, language, performanceTrackingEnabled);
                AddHistoryRequestLoad(speed, findings);
            }

            if (!speedMeasured)
            {
                AddSlowExecutionFromCompletionLog(logs, findings, language, performanceTrackingEnabled);
            }

            return CreateAggregatedResponse(findings);
        }

        /// <summary>
        /// Reports slow execution when the recent data points per second are below the platform benchmark.
        /// </summary>
        /// <returns>Whether the speed could be measured, regardless of it being slow or not.</returns>
        private static bool AddSlowExecution(AlgorithmSpeedTracker speed, List<QuantConnect.Analysis> findings,
            Language language, bool performanceTrackingEnabled)
        {
            if (!speed.HasDataPointCounts)
            {
                return false;
            }

            var recent = speed.RecentDataPointsPerSecond();
            var previous = speed.RecentDataPointsPerSecond(skipLast: 1);
            if (recent == null || previous == null)
            {
                return false;
            }
            if (recent >= SlowDataPointsPerSecond || previous >= SlowDataPointsPerSecond)
            {
                return true;
            }

            var average = speed.DataPointsPerSecond ?? 0;
            var sample = Invariant($"Processing {FormatRate(recent.Value)} data points per second recently ") +
                Invariant($"({FormatRate(average)} average); {speed.Progress * 100:F0}% complete after ") +
                Invariant($"{FormatDuration(speed.Elapsed)}");

            // No remaining time to project on the final analysis of a backtest that reached its end date
            var remaining = speed.EstimatedRemainingTime();
            if (remaining == null)
            {
                sample += ", the remaining time cannot be estimated yet.";
            }
            else if (remaining.Value > TimeSpan.Zero)
            {
                sample += Invariant($", about {FormatDuration(remaining.Value)} remaining at the recent pace.");
            }
            else
            {
                sample += ".";
            }

            findings.Add(new(SlowExecutionName,
                Invariant($"The algorithm is running below {SlowDataPointsPerSecond / 1000}k data points per second."),
                sample,
                null,
                [
                    "Review the algorithm code for inefficiencies.",

                    .. PerformanceTrackingSolutions(language, performanceTrackingEnabled),

                    "If there is a universe, reduce its size.",

                    "Reduce the data resolution.",

                    "If the algorithm is training a model, reduce the amount of training data or reduce the number of epochs in the training process.",

                    "If the projected runtime is not acceptable, stop the backtest, apply the changes above, and run it again.",
                ]));
            return true;
        }

        /// <summary>
        /// Fallback slow-execution detection for when the tracked metrics cannot measure the speed:
        /// parses the engine's completion log line for the whole-run average rate. The line is only
        /// logged once the backtest ends, so in-run log deltas never match and the fallback can
        /// only fire on the final analysis.
        /// </summary>
        private static void AddSlowExecutionFromCompletionLog(IReadOnlyList<string> logs, List<QuantConnect.Analysis> findings,
            Language language, bool performanceTrackingEnabled)
        {
            for (var i = (logs?.Count ?? 0) - 1; i >= 0; i--)
            {
                var match = CompletionLogLineRegex.Match(logs[i]);
                if (!match.Success)
                {
                    continue;
                }

                var timeInSeconds = double.Parse(match.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                var dataPointsPerSecond = int.Parse(match.Groups[2].Value, NumberFormatInfo.InvariantInfo);
                if (timeInSeconds >= MinimumCompletedRuntimeSeconds && dataPointsPerSecond < SlowDataPointsPerSecond / 1000)
                {
                    findings.Add(new(SlowExecutionName,
                        Invariant($"The algorithm is running below {SlowDataPointsPerSecond / 1000}k data points per second."),
                        Invariant($"The algorithm executed at only {dataPointsPerSecond}k data points per second ") +
                            Invariant($"over the whole {FormatDuration(TimeSpan.FromSeconds(timeInSeconds))} run."),
                        null,
                        [
                            "Review the algorithm code for inefficiencies.",

                            .. PerformanceTrackingSolutions(language, performanceTrackingEnabled),

                            "If there is a universe, reduce its size.",

                            "Reduce the data resolution.",

                            "If the algorithm is training a model, reduce the amount of training data or reduce the number of epochs in the training process.",
                        ]));
                }
                return;
            }
        }

        /// <summary>
        /// Reports a long projected runtime when, at the recent pace, the backtest needs more than
        /// <see cref="LongProjectedRemainingTime"/> to complete, or when it has stopped making
        /// backtest-time progress altogether.
        /// </summary>
        private static void AddLongProjectedRuntime(AlgorithmSpeedTracker speed, List<QuantConnect.Analysis> findings)
        {
            if (speed.TotalDays <= 0 || speed.ProcessedDays >= speed.TotalDays)
            {
                return;
            }

            var daysPerSecond = speed.RecentDaysPerSecond();
            var previousDaysPerSecond = speed.RecentDaysPerSecond(skipLast: 1);

            string sample = null;
            if (daysPerSecond is 0 && previousDaysPerSecond is 0)
            {
                sample = Invariant($"The backtest has made no backtest-time progress recently: ") +
                    Invariant($"still {speed.Progress * 100:F0}% complete after {FormatDuration(speed.Elapsed)}.");
            }
            else
            {
                var remaining = speed.EstimatedRemainingTime();
                var previousRemaining = speed.EstimatedRemainingTime(skipLast: 1);
                if (remaining > LongProjectedRemainingTime && previousRemaining > LongProjectedRemainingTime)
                {
                    sample = Invariant($"About {FormatDuration(remaining.Value)} of backtest remain at the recent pace ") +
                        Invariant($"({speed.Progress * 100:F0}% complete after {FormatDuration(speed.Elapsed)}).");
                }
            }

            if (sample == null)
            {
                return;
            }

            findings.Add(new(LongProjectedRuntimeName,
                "The backtest is projected to take a long time to complete.",
                sample,
                null,
                [
                    "Reduce the backtest period.",

                    "Reduce the data resolution or the universe size.",

                    "Review the algorithm code for inefficiencies.",

                    "If the projected runtime is not acceptable, stop the backtest, apply the changes above, and run it again.",
                ]));
        }

        /// <summary>
        /// Reports degrading throughput when the recent data points per second dropped below
        /// <see cref="DegradationRatio"/> of the early-run baseline. Requires enough samples for the
        /// baseline and recent windows to not overlap.
        /// </summary>
        private static void AddThroughputDegradation(AlgorithmSpeedTracker speed, List<QuantConnect.Analysis> findings,
            Language language, bool performanceTrackingEnabled)
        {
            if (!speed.HasDataPointCounts || speed.SampleCount < 2 * AlgorithmSpeedTracker.RecentWindowSamples + 1)
            {
                return;
            }

            var initial = speed.InitialDataPointsPerSecond;
            var recent = speed.RecentDataPointsPerSecond();
            var previous = speed.RecentDataPointsPerSecond(skipLast: 1);
            if (initial is null or <= 0 || recent == null || previous == null ||
                recent >= DegradationRatio * initial || previous >= DegradationRatio * initial)
            {
                return;
            }

            findings.Add(new(ThroughputDegradationName,
                "The algorithm's processing speed is degrading as the backtest progresses.",
                Invariant($"Throughput dropped from {FormatRate(initial.Value)} data points per second early in the run ") +
                    Invariant($"to {FormatRate(recent.Value)} recently."),
                null,
                [
                    "Check for collections that grow unboundedly as the backtest progresses, like lists of past data points; use rolling windows with a fixed size instead.",

                    "Check for history requests whose range grows as the backtest progresses, like requests from the algorithm start date to the current time.",

                    "If there is a universe, check whether the number of selected securities keeps growing; remove securities that are no longer used.",

                    "Check the algorithm's memory usage: sustained growth causes garbage collection pressure that slows the whole run down.",

                    .. PerformanceTrackingSolutions(language, performanceTrackingEnabled),
                ]));
        }

        /// <summary>
        /// Reports a history-request-dominated data load when most of the recently processed data
        /// points were served by the history provider.
        /// </summary>
        private static void AddHistoryRequestLoad(AlgorithmSpeedTracker speed, List<QuantConnect.Analysis> findings)
        {
            var share = speed.RecentHistoryDataPointsShare();
            var previousShare = speed.RecentHistoryDataPointsShare(skipLast: 1);
            if (share is null or <= HighHistoryDataPointsShare || previousShare is null or <= HighHistoryDataPointsShare ||
                speed.RecentHistoryDataPoints() < MinimumRecentHistoryDataPoints)
            {
                return;
            }

            findings.Add(new(HistoryRequestLoadName,
                "Most of the data being processed comes from history requests.",
                Invariant($"{share.Value * 100:F0}% of the data points processed recently were served by history requests."),
                null,
                [
                    "Avoid issuing history requests on every data update; maintain the data incrementally with rolling windows or consolidators instead.",

                    "Warm up indicators with the automatic indicator warm-up or the algorithm warm-up period instead of history requests.",

                    "Reduce the period or resolution of the history requests.",
                ]));
        }

        /// <summary>
        /// The performance tracking suggestion shared by the findings whose diagnosis needs to locate
        /// where the execution time is spent: setting <see cref="AlgorithmSettings.PerformanceSamplePeriod"/>
        /// adds a "Performance" chart with the engine's time breakdown on the next run.
        /// Empty when the algorithm already has performance tracking enabled.
        /// </summary>
        private static IEnumerable<string> PerformanceTrackingSolutions(Language language, bool performanceTrackingEnabled)
        {
            if (performanceTrackingEnabled)
            {
                yield break;
            }

            yield return $"To see where the execution time is spent, set the " +
                $"`{FormatCode(nameof(AlgorithmSettings.PerformanceSamplePeriod), language)}` setting, like " +
                (language == Language.Python
                    ? "`self.settings.performance_sample_period = timedelta(days=1)`"
                    : "`Settings.PerformanceSamplePeriod = TimeSpan.FromDays(1);`") +
                ", and rerun to get a \"Performance\" time-breakdown chart.";
        }

        /// <summary>
        /// Formats a data points per second rate compactly: in thousands like "12.5k" when at least
        /// one thousand, as a raw count like "340" below that, so very slow rates don't read as "0.0k".
        /// </summary>
        private static string FormatRate(double dataPointsPerSecond)
        {
            return dataPointsPerSecond >= 1000
                ? Invariant($"{dataPointsPerSecond / 1000:F1}k")
                : Invariant($"{dataPointsPerSecond:F0}");
        }

        /// <summary>
        /// Formats a duration as a compact human-readable string, like "2h 5m", "12m" or "45s".
        /// </summary>
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return Invariant($"{(int)duration.TotalHours}h {duration.Minutes}m");
            }
            if (duration.TotalMinutes >= 1)
            {
                return Invariant($"{(int)duration.TotalMinutes}m");
            }
            return Invariant($"{(int)duration.TotalSeconds}s");
        }
    }
}
