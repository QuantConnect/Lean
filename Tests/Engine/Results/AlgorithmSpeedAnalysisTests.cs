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
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results.Analysis;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class AlgorithmSpeedAnalysisTests
    {
        private const string SlowCompletionLogLine =
            "Algorithm Id:(BasicTemplateAlgorithm) completed in 120.50 seconds at 10k data points per second. Processing total of 1,205,000 data points.";

        [Test]
        public void NoFindingsWithoutSpeedMetrics()
        {
            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run((AlgorithmSpeedTracker)null));

            var parameters = new ResultsAnalysisRunParameters(null, null, Language.CSharp, null, null, null);
            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run(parameters));
        }

        [Test]
        public void NoFindingsBeforeTheMinimumSampledSpan()
        {
            // Very slow, but only 30s of samples: still within the warm-up grace period
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 2, stepSeconds: 30,
                dataPointsPerStep: 100, historyDataPointsPerStep: 0, daysPerStep: 0, totalDays: 0);

            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run(tracker));
        }

        [Test]
        public void FlagsSlowExecutionWithProgressAndProjection()
        {
            // 10k data points per second, 1 backtest day every 30s with only 4 days left: slow, but not long-running
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 300_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.SlowExecutionName}", finding.Name);
            var sample = (string)finding.Sample;
            StringAssert.Contains("10.0k data points per second", sample);
            StringAssert.Contains("60% complete", sample);
            StringAssert.Contains("remaining at the recent pace", sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void SlowExecutionOmitsTheProjectionWhenTheBacktestReachedItsEndDate()
        {
            // Slow throughout, but all the backtest days are processed, like on the final analysis
            // of a completed backtest: no remaining time is projected and no long-runtime finding fails
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 300_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 6);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.SlowExecutionName}", finding.Name);
            var sample = (string)finding.Sample;
            StringAssert.Contains("100% complete", sample);
            StringAssert.DoesNotContain("remaining", sample);
        }

        [Test]
        public void FormatsRatesBelowOneThousandAsRawCounts()
        {
            // 100 data points per second: formatted as a raw count instead of "0.1k"
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 3_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single(x => x.Name.EndsWith(AlgorithmSpeedAnalysis.SlowExecutionName, StringComparison.Ordinal));
            StringAssert.Contains("Processing 100 data points per second recently (100 average)", (string)finding.Sample);
        }

        [Test]
        public void DoesNotFlagFastExecution()
        {
            // 100k data points per second and a short remaining runtime
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 3_000_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run(tracker));
        }

        [Test]
        public void SlowExecutionRequiresTwoConsecutiveSlowWindows()
        {
            // A fast run whose very last window stalls: the previous window is still fast, so no flag yet
            var tracker = new AlgorithmSpeedTracker();
            for (var i = 0; i < 6; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(30 * i), 3_000_000L * i, 0, 0, 0));
            }
            tracker.AddSample(new(TimeSpan.FromSeconds(750), 15_000_000, 0, 0, 0));

            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run(tracker));

            // One more stalled window and both recent windows are slow: now it flags
            tracker.AddSample(new(TimeSpan.FromSeconds(1350), 15_000_000, 0, 0, 0));

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);
            Assert.IsTrue(findings.Any(finding => finding.Name.EndsWith(AlgorithmSpeedAnalysis.SlowExecutionName, StringComparison.Ordinal)));
        }

        [Test]
        public void CompletionLogLineIsTheSlowExecutionFallbackWhenSpeedIsNotTracked()
        {
            var logs = new[] { "Some log line", SlowCompletionLogLine, "Another log line" };

            var findings = new AlgorithmSpeedAnalysis().Run(null, logs);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.SlowExecutionName}", finding.Name);
            StringAssert.Contains("10k data points per second", (string)finding.Sample);
            StringAssert.Contains("whole", (string)finding.Sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [TestCase("Algorithm Id:(BasicTemplateAlgorithm) completed in 120.50 seconds at 85k data points per second. Processing total of 10,242,500 data points.",
            TestName = "CompletionLogFallbackDoesNotFlagFastRuns")]
        [TestCase("Algorithm Id:(BasicTemplateAlgorithm) completed in 5.20 seconds at 5k data points per second. Processing total of 26,000 data points.",
            TestName = "CompletionLogFallbackDoesNotFlagVeryShortRuns")]
        [TestCase("A log line without a completion line, like the deltas the in-run analysis cycles see",
            TestName = "CompletionLogFallbackNeedsACompletionLine")]
        public void CompletionLogFallbackOnlyFlagsSlowCompletedRuns(string logLine)
        {
            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run(null, new[] { logLine }));
        }

        [Test]
        public void TrackedMetricsSupersedeTheCompletionLogFallback()
        {
            // Fast per the tracked metrics: the slow whole-run average in the log is not reported
            var fastTracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 3_000_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            Assert.IsEmpty(new AlgorithmSpeedAnalysis().Run(fastTracker, new[] { SlowCompletionLogLine }));

            // Slow per the tracked metrics: a single metric-based finding, not the log-based one
            var slowTracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 300_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            var findings = new AlgorithmSpeedAnalysis().Run(slowTracker, new[] { SlowCompletionLogLine });

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.SlowExecutionName}", finding.Name);
            StringAssert.Contains("recently", (string)finding.Sample);
        }

        [Test]
        public void CompletionLogFallbackFiresWhenTheDataPointCountersAreNotWiredIn()
        {
            // The tracker samples calendar progress but the data point counters are always zero:
            // the metrics cannot measure the speed, so the completion line's average is used
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 0, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker, new[] { SlowCompletionLogLine });

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.SlowExecutionName}", finding.Name);
            StringAssert.Contains("whole", (string)finding.Sample);
        }

        [Test]
        public void MissingDataPointCountsSuppressDataPointBasedFindings()
        {
            // The data point counters are not wired in (always zero), but calendar progress is glacial:
            // only the projected runtime finding should be reported
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 12, stepSeconds: 30,
                dataPointsPerStep: 0, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 100_000);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.LongProjectedRuntimeName}", finding.Name);
        }

        [Test]
        public void FlagsLongProjectedRuntime()
        {
            // Fast processing, but ~35 wall-clock days of backtest left at one backtest day per 30s
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 3_000_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 100_000);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.LongProjectedRuntimeName}", finding.Name);
            StringAssert.Contains("remain at the recent pace", (string)finding.Sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void FlagsStalledCalendarProgressAsLongProjectedRuntime()
        {
            // Fast processing but the backtest time is not advancing at all
            var tracker = new AlgorithmSpeedTracker();
            for (var i = 0; i < 7; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(30 * i), 3_000_000L * i, 0, 3, 10));
            }

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.LongProjectedRuntimeName}", finding.Name);
            StringAssert.Contains("no backtest-time progress", (string)finding.Sample);
        }

        [Test]
        public void FlagsThroughputDegradation()
        {
            // 100k data points per second for the first 6 intervals, 10k for the last 5
            var tracker = new AlgorithmSpeedTracker();
            var dataPoints = 0L;
            for (var i = 0; i < 12; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(30 * i), dataPoints, 0, 0, 0));
                dataPoints += i < 6 ? 3_000_000 : 300_000;
            }

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var degradation = findings.Single(finding =>
                finding.Name.EndsWith(AlgorithmSpeedAnalysis.ThroughputDegradationName, StringComparison.Ordinal));
            StringAssert.Contains("100.0k data points per second early in the run", (string)degradation.Sample);
            StringAssert.Contains("10.0k recently", (string)degradation.Sample);
            // The recent pace is also below the absolute threshold, so slow execution is reported too
            Assert.IsTrue(findings.Any(finding => finding.Name.EndsWith(AlgorithmSpeedAnalysis.SlowExecutionName, StringComparison.Ordinal)));
        }

        [Test]
        public void NoDegradationFindingBeforeTheBaselineAndRecentWindowsAreDisjoint()
        {
            var tracker = new AlgorithmSpeedTracker();
            var dataPoints = 0L;
            for (var i = 0; i < 9; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(30 * i), dataPoints, 0, 0, 0));
                dataPoints += i < 3 ? 3_000_000 : 300_000;
            }

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            Assert.IsFalse(findings.Any(finding =>
                finding.Name.EndsWith(AlgorithmSpeedAnalysis.ThroughputDegradationName, StringComparison.Ordinal)));
        }

        [Test]
        public void FlagsHistoryRequestDominatedLoad()
        {
            // Fast overall, but 75% of the data points come from history requests
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 1_000_000, historyDataPointsPerStep: 3_000_000, daysPerStep: 1, totalDays: 10);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            var finding = findings.Single();
            Assert.AreEqual($"{nameof(AlgorithmSpeedAnalysis)} / {AlgorithmSpeedAnalysis.HistoryRequestLoadName}", finding.Name);
            StringAssert.Contains("75% of the data points", (string)finding.Sample);
        }

        [Test]
        public void SlowFindingsSuggestEnablingPerformanceTracking()
        {
            // Both slow and degrading: the slow execution and the degradation findings carry the suggestion
            var tracker = new AlgorithmSpeedTracker();
            var dataPoints = 0L;
            for (var i = 0; i < 12; i++)
            {
                tracker.AddSample(new(TimeSpan.FromSeconds(30 * i), dataPoints, 0, 0, 0));
                dataPoints += i < 6 ? 3_000_000 : 300_000;
            }

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            Assert.AreEqual(2, findings.Count);
            foreach (var finding in findings)
            {
                Assert.IsTrue(finding.Solutions.Any(solution =>
                    solution.Contains(nameof(AlgorithmSettings.PerformanceSamplePeriod), StringComparison.Ordinal)));
            }

            // The completion log fallback carries it too
            var fallbackFinding = new AlgorithmSpeedAnalysis().Run(null, new[] { SlowCompletionLogLine }).Single();
            Assert.IsTrue(fallbackFinding.Solutions.Any(solution =>
                solution.Contains(nameof(AlgorithmSettings.PerformanceSamplePeriod), StringComparison.Ordinal)));
        }

        [Test]
        public void PerformanceTrackingSuggestionFollowsTheAlgorithmLanguage()
        {
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 300_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            var finding = new AlgorithmSpeedAnalysis().Run(tracker, language: Language.Python).Single();

            var solution = finding.Solutions.Single(solution => solution.Contains("performance_sample_period", StringComparison.Ordinal));
            StringAssert.Contains("self.settings.performance_sample_period", solution);
        }

        [Test]
        public void NoPerformanceTrackingSuggestionWhenAlreadyEnabled()
        {
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 300_000, historyDataPointsPerStep: 0, daysPerStep: 1, totalDays: 10);

            var finding = new AlgorithmSpeedAnalysis().Run(tracker, performanceTrackingEnabled: true).Single();

            Assert.IsFalse(finding.Solutions.Any(solution =>
                solution.Contains(nameof(AlgorithmSettings.PerformanceSamplePeriod), StringComparison.Ordinal)));

            var fallbackFinding = new AlgorithmSpeedAnalysis()
                .Run(null, new[] { SlowCompletionLogLine }, performanceTrackingEnabled: true).Single();
            Assert.IsFalse(fallbackFinding.Solutions.Any(solution =>
                solution.Contains(nameof(AlgorithmSettings.PerformanceSamplePeriod), StringComparison.Ordinal)));
        }

        [Test]
        public void NoHistoryLoadFindingBelowTheMinimumHistoryDataPointCount()
        {
            // 60% history share, but only a few hundred history data points in the window
            var tracker = AlgorithmSpeedTrackerTests.BuildUniformTracker(samples: 7, stepSeconds: 30,
                dataPointsPerStep: 40, historyDataPointsPerStep: 60, daysPerStep: 1, totalDays: 10);

            var findings = new AlgorithmSpeedAnalysis().Run(tracker);

            Assert.IsFalse(findings.Any(finding =>
                finding.Name.EndsWith(AlgorithmSpeedAnalysis.HistoryRequestLoadName, StringComparison.Ordinal)));
        }
    }
}
