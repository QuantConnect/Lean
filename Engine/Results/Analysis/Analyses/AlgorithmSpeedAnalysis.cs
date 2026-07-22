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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// In-run analysis that tracks the algorithm's execution speed so the user can decide to stop
    /// a slow backtest early. It reads the throughput and progress metrics accumulated by
    /// <see cref="AlgorithmSpeedTracker"/> and reports slow processing speed, a long projected
    /// remaining runtime, degrading throughput, and history-request-dominated data loads.
    /// Benchmark speeds: https://www.quantconnect.com/performance
    /// </summary>
    public class AlgorithmSpeedAnalysis : BaseResultsAnalysis
    {
        /// <summary>
        /// The data points per second under which execution is reported as slow,
        /// matching the threshold used by <see cref="ExecutionSpeedAnalysis"/> on completed backtests.
        /// </summary>
        public const int SlowDataPointsPerSecond = 40_000;

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
        /// Gets the description of the slow algorithm issue.
        /// </summary>
        public override string Issue { get; } = "The algorithm is running slowly.";

        /// <summary>
        /// Gets the severity weight for the algorithm speed analysis.
        /// </summary>
        public override int Weight { get; } = 77;

        /// <summary>
        /// Runs the algorithm speed analysis against the speed metrics tracked for the running backtest.
        /// </summary>
        public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Speed);

        /// <summary>
        /// Runs the algorithm speed analysis against the given speed metrics.
        /// Each detected condition is reported as its own sub-finding. Every condition must hold for
        /// both the current recent window and the window as of the previous run, so a single noisy
        /// sample doesn't flag or clear a finding.
        /// </summary>
        /// <param name="speed">The speed metrics tracked for the running backtest, or null when not tracked.</param>
        /// <returns>The failed sub-findings, or empty when speed is not tracked or still within the warm-up span.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(AlgorithmSpeedTracker speed)
        {
            if (speed == null || speed.SampledSpan < MinimumSampledSpan)
            {
                return [];
            }

            var findings = new List<QuantConnect.Analysis>();
            AddSlowExecution(speed, findings);
            AddLongProjectedRuntime(speed, findings);
            AddThroughputDegradation(speed, findings);
            AddHistoryRequestLoad(speed, findings);
            return CreateAggregatedResponse(findings);
        }

        /// <summary>
        /// Reports slow execution when the recent data points per second are below the platform benchmark.
        /// </summary>
        private static void AddSlowExecution(AlgorithmSpeedTracker speed, List<QuantConnect.Analysis> findings)
        {
            if (!speed.HasDataPointCounts)
            {
                return;
            }

            var recent = speed.RecentDataPointsPerSecond();
            var previous = speed.RecentDataPointsPerSecond(skipLast: 1);
            if (recent is null or >= SlowDataPointsPerSecond || previous is null or >= SlowDataPointsPerSecond)
            {
                return;
            }

            var average = speed.DataPointsPerSecond ?? 0;
            var remaining = speed.EstimatedRemainingTime();
            var projection = remaining.HasValue
                ? Invariant($"about {FormatDuration(remaining.Value)} remaining at the recent pace")
                : "the remaining time cannot be estimated yet";
            var sample = Invariant($"Processing {FormatRate(recent.Value)} data points per second recently ") +
                Invariant($"({FormatRate(average)} average); {speed.Progress * 100:F0}% complete after ") +
                Invariant($"{FormatDuration(speed.Elapsed)}, {projection}.");

            findings.Add(new(SlowExecutionName,
                Invariant($"The algorithm is running below {SlowDataPointsPerSecond / 1000}k data points per second."),
                sample,
                null,
                [
                    "Review the algorithm code for inefficiencies.",

                    "If there is a universe, reduce its size.",

                    "Reduce the data resolution.",

                    "If the algorithm is training a model, reduce the amount of training data or reduce the number of epochs in the training process.",

                    "If the projected runtime is not acceptable, stop the backtest, apply the changes above, and run it again.",
                ]));
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
        private static void AddThroughputDegradation(AlgorithmSpeedTracker speed, List<QuantConnect.Analysis> findings)
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
