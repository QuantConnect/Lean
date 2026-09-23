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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Results.Analysis;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Engine.Results
{
    /// <summary>
    /// Tests the in-run mode of <see cref="ResultsAnalyzer"/>, driven through intermediate
    /// results carrying truncated order and order event windows plus the accumulated logs.
    /// The core, mode-independent behavior is covered by <see cref="ResultsAnalyzerTests"/>.
    /// </summary>
    [TestFixture]
    public class ResultsAnalyzerInRunTests
    {
        private static readonly IReadOnlyList<string> SomeSolutions = new[] { "A solution" };

        [Test]
        public void OrderEventAndLogStreamsAreConsumedIncrementally()
        {
            var seenOrderEvents = new List<OrderEvent>();
            var seenLogs = new List<string>();
            var fake = new FakeAnalysisA(10)
            {
                OnParameters = parameters =>
                {
                    seenOrderEvents.AddRange(parameters.Result.OrderEvents);
                    seenLogs.AddRange(parameters.Logs);
                }
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            // The order event windows fully overlap (every event fits in the window), but the
            // watermark dedupes them, and the full logs are sliced from the consumed position:
            // each event and log line is analyzed exactly once, in chronological order
            analyzer.Run(3, new[] { "log 1", "log 2" });
            analyzer.Run(5, new[] { "log 3" });
            // Runs without new order events or logs produce empty deltas
            analyzer.Run(0, null);
            analyzer.Run(0, null);

            CollectionAssert.AreEqual(analyzer.OrderEventStream, seenOrderEvents);
            CollectionAssert.AreEqual(new[] { "log 1", "log 2", "log 3" }, seenLogs);
        }

        [Test]
        public void OrderEventsEvictedFromTheTruncatedWindowAreMissed()
        {
            var seenOrderEvents = new List<OrderEvent>();
            var fake = new FakeAnalysisA(10) { OnParameters = parameters => seenOrderEvents.AddRange(parameters.Result.OrderEvents) };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            // 3 new events, but only the newest 2 fit the window
            analyzer.Run(3, null, orderEventsWindowSize: 2);
            // 4 more events: the watermark is not in the window, so the whole window is new
            // and the evicted events in between are missed
            analyzer.Run(4, null, orderEventsWindowSize: 2);

            var stream = analyzer.OrderEventStream;
            CollectionAssert.AreEqual(new[] { stream[1], stream[2], stream[5], stream[6] }, seenOrderEvents);
        }

        [Test]
        public void StreamPositionsAdvanceEvenWhenTheTimeLimitTruncatesTheRun()
        {
            var seenOrderEventCounts = new List<int>();
            var seenLogCounts = new List<int>();
            var truncatedRan = false;
            // The slow analysis has the higher weight so it runs first and exhausts the time limit
            var slow = new FakeAnalysisA(20)
            {
                OnParameters = parameters =>
                {
                    seenOrderEventCounts.Add(parameters.Result.OrderEvents.Count);
                    seenLogCounts.Add(parameters.Logs.Count);
                },
                OnRun = () => Thread.Sleep(1100)
            };
            var truncated = new FakeAnalysisB(10) { OnRun = () => truncatedRan = true };
            var analyzer = new TestInRunResultsAnalyzer(slow, truncated);

            analyzer.Run(4, new[] { "log 1" }, timeLimitSeconds: 1);
            Assert.IsFalse(truncatedRan);

            // The next run still resumes after the consumed order events and logs
            slow.OnRun = null;
            analyzer.Run(0, null);
            CollectionAssert.AreEqual(new[] { 4, 0 }, seenOrderEventCounts);
            CollectionAssert.AreEqual(new[] { 1, 0 }, seenLogCounts);
        }

        [Test]
        public void StreamBasedFindingsAccumulateAcrossRuns()
        {
            var fake = new FakeAnalysisA(10);
            var analyzer = new TestInRunResultsAnalyzer(fake);

            fake.Findings = () => MakeFindings(nameof(FakeAnalysisA), "first sample", 3);
            analyzer.Run(1, new[] { "log" });

            fake.Findings = () => MakeFindings(nameof(FakeAnalysisA), "second sample", 2);
            var findings = analyzer.Run(1, new[] { "log" });

            var finding = findings.Single();
            Assert.AreEqual("first sample", finding.Sample);
            Assert.AreEqual(5, finding.Count);
        }

        [Test]
        public void StreamBasedFindingsWithNullCountsCountSingleOccurrences()
        {
            var fake = new FakeAnalysisA(10)
            {
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", null)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            analyzer.Run(1, new[] { "log" });
            var findings = analyzer.Run(1, new[] { "log" });

            Assert.AreEqual(2, findings.Single().Count);
        }

        [Test]
        public void StreamBasedFindingsPersistWhenNotReemitted()
        {
            var fake = new FakeAnalysisA(10)
            {
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", 4)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            analyzer.Run(1, new[] { "log" });

            // The next delta produces no new occurrences: the accumulated finding is still reported
            fake.Findings = () => new List<QuantConnect.Analysis>();
            var findings = analyzer.Run(1, new[] { "log" });

            var finding = findings.Single();
            Assert.AreEqual("sample", finding.Sample);
            Assert.AreEqual(4, finding.Count);
        }

        [Test]
        public void FindingsAreMutedOnceReturnedTheMaxNumberOfTimes()
        {
            // A finding is returned 3 times and then muted, regardless of its occurrence counts
            var fake = new FakeAnalysisA(10)
            {
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", 100)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            for (var report = 1; report <= 3; report++)
            {
                Assert.AreEqual(100 * report, analyzer.Run(1, new[] { "log" }).Single().Count);
            }
            // Muted from the fourth run on, even though the finding keeps accumulating
            Assert.IsEmpty(analyzer.Run(1, new[] { "log" }));
            fake.Findings = () => new List<QuantConnect.Analysis>();
            Assert.IsEmpty(analyzer.Run(1, new[] { "log" }));
        }

        [Test]
        public void StateBasedFindingsAreMutedOnceReturnedTheMaxNumberOfTimes()
        {
            var fake = new FakeAnalysisA(10)
            {
                StateBased = true,
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", 100)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            for (var report = 1; report <= 3; report++)
            {
                Assert.AreEqual(100, analyzer.Run(1, new[] { "log" }).Single().Count);
            }
            // Muted from the fourth run on, even though the analysis still fails
            Assert.IsEmpty(analyzer.Run(1, new[] { "log" }));
        }

        [Test]
        public void MutedStateBasedFindingsStayMutedWhenTheyClearAndFailAgain()
        {
            var fake = new FakeAnalysisA(10)
            {
                StateBased = true,
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", 2)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            for (var run = 0; run < 3; run++)
            {
                Assert.IsNotEmpty(analyzer.Run(1, new[] { "log" }));
            }

            // Clears, then fails again: the reported runs are not reset
            fake.Findings = () => new List<QuantConnect.Analysis>();
            Assert.IsEmpty(analyzer.Run(1, new[] { "log" }));
            fake.Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", 2);
            Assert.IsEmpty(analyzer.Run(1, new[] { "log" }));
        }

        [Test]
        public void StateBasedFindingsAreReplacedOnEveryRun()
        {
            var fake = new FakeAnalysisA(10)
            {
                StateBased = true,
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "old sample", 2)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            analyzer.Run(1, new[] { "log" });

            fake.Findings = () => MakeFindings(nameof(FakeAnalysisA), "new sample", 3);
            var findings = analyzer.Run(1, new[] { "log" });

            // Replaced, not accumulated: latest sample and count win
            var finding = findings.Single();
            Assert.AreEqual("new sample", finding.Sample);
            Assert.AreEqual(3, finding.Count);
        }

        [Test]
        public void StateBasedFindingsAreDroppedWhenTheyNoLongerFail()
        {
            var fake = new FakeAnalysisA(10)
            {
                StateBased = true,
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample", 2)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            Assert.IsNotEmpty(analyzer.Run(1, new[] { "log" }));

            fake.Findings = () => new List<QuantConnect.Analysis>();
            var findings = analyzer.Run(1, new[] { "log" });

            Assert.IsEmpty(findings);
        }

        [Test]
        public void AggregatedStateBasedFindingsAreReplacedByFullName()
        {
            // Aggregated analyses emit "AnalysisClass / SubAnalysis" finding names: state-based
            // behavior is determined by the base analysis name, replacement is keyed by the full name
            var stateBasedName = nameof(FakeAnalysisA);
            var fake = new FakeAnalysisA(10)
            {
                StateBased = true,
                Findings = () => MakeFindings($"{stateBasedName} / SubA", "sample a", 1)
                    .Concat(MakeFindings($"{stateBasedName} / SubB", "sample b", 1))
                    .ToList()
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            Assert.AreEqual(2, analyzer.Run(1, new[] { "log" }).Count);

            fake.Findings = () => MakeFindings($"{stateBasedName} / SubA", "new sample a", 2);
            var findings = analyzer.Run(1, new[] { "log" });

            // SubB no longer fails and is dropped; SubA is replaced with the fresh finding
            var finding = findings.Single();
            Assert.AreEqual($"{stateBasedName} / SubA", finding.Name);
            Assert.AreEqual("new sample a", finding.Sample);
            Assert.AreEqual(2, finding.Count);
        }

        [Test]
        public void SpeedSamplesAreTrackedOnlyWhenTheyCanBeTaken()
        {
            AlgorithmSpeedTracker speed = null;
            var fake = new FakeAnalysisA(10) { OnParameters = parameters => speed = parameters.Speed };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            analyzer.Run(1, new[] { "log" });
            Assert.IsNotNull(speed);
            Assert.AreEqual(0, speed.SampleCount);

            analyzer.Run(1, new[] { "log" }, new AlgorithmSpeedSample(TimeSpan.FromSeconds(30), 100, 0, 1, 10));
            Assert.AreEqual(1, speed.SampleCount);

            // No sample taken (e.g. while the algorithm warms up): the tracker is left untouched
            analyzer.Run(1, new[] { "log" });
            Assert.AreEqual(1, speed.SampleCount);
        }

        [Test]
        public void CompletedSpeedTrackingAddsAFinalSampleAndReturnsTheTracker()
        {
            AlgorithmSpeedTracker speed = null;
            var fake = new FakeAnalysisA(10) { OnParameters = parameters => speed = parameters.Speed };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            analyzer.Run(1, new[] { "log" }, new AlgorithmSpeedSample(TimeSpan.FromSeconds(30), 100, 0, 1, 10));

            analyzer.NextSpeedSample = new AlgorithmSpeedSample(TimeSpan.FromSeconds(60), 200, 0, 2, 10);
            var tracker = analyzer.CompleteSpeedTracking();

            // The final analysis receives the same tracker the in-run analyses saw, with the final sample added
            Assert.AreSame(speed, tracker);
            Assert.AreEqual(2, tracker.SampleCount);

            // Without a final sample (e.g. the algorithm never left warm-up), the tracker is left untouched
            analyzer.NextSpeedSample = null;
            Assert.AreEqual(2, analyzer.CompleteSpeedTracking().SampleCount);
        }

        [Test]
        public void SnapshotIsBuiltFromTheIntermediateResult()
        {
            ResultsAnalysisRunParameters seenParameters = null;
            var fake = new FakeAnalysisA(10) { OnParameters = parameters => seenParameters = parameters };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            var charts = new Dictionary<string, Chart> { ["a chart"] = new Chart("a chart") };
            var orders = new Dictionary<int, Order> { [1] = new MarketOrder() };

            analyzer.Run(2, new[] { "log" }, orders: orders, charts: charts);

            // The charts, orders and order events come from the intermediate result
            Assert.AreSame(charts, seenParameters.Result.Charts);
            Assert.AreSame(orders, seenParameters.Result.Orders);
            Assert.AreEqual(2, seenParameters.Result.OrderEvents.Count);
            CollectionAssert.AreEqual(new[] { "log" }, seenParameters.Logs);
        }

        [Test]
        public void StatisticsAreWithheldUntilEquityHasSamples()
        {
            BacktestResult seenResult = null;
            var fake = new FakeAnalysisA(10) { OnParameters = parameters => seenResult = (BacktestResult)parameters.Result };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            var performance = new AlgorithmPerformance();

            // The equity chart has no samples yet: the all-zero default statistics are withheld
            analyzer.Run(new BacktestResult(), logs: null, totalPerformance: performance);
            Assert.IsNull(seenResult.TotalPerformance);

            var equitySeries = new Series(BaseResultsHandler.EquityKey);
            equitySeries.AddPoint(new DateTime(2024, 01, 02), 100000m);
            var equityChart = new Chart(BaseResultsHandler.StrategyEquityKey);
            equityChart.AddSeries(equitySeries);
            var result = new BacktestResult
            {
                Charts = new Dictionary<string, Chart> { [equityChart.Name] = equityChart }
            };

            analyzer.Run(result, logs: null, totalPerformance: performance);
            Assert.AreSame(performance, seenResult.TotalPerformance);
        }

        [Test]
        public void FindingsAreRankedByAnalysisWeightAndCapped()
        {
            var lowWeight = new FakeAnalysisA(10)
            {
                Findings = () => MakeFindings(nameof(FakeAnalysisA), "sample a", 1)
            };
            // Aggregated finding names rank by their base analysis' weight
            var midWeight = new FakeAnalysisB(20)
            {
                Findings = () => MakeFindings($"{nameof(FakeAnalysisB)} / Sub", "sample b", 1)
            };
            var highWeight = new FakeAnalysisC(30)
            {
                Findings = () => MakeFindings(nameof(FakeAnalysisC), "sample c", 1)
            };
            var analyzer = new TestInRunResultsAnalyzer(lowWeight, midWeight, highWeight);

            var findings = analyzer.Run(1, new[] { "log" });
            CollectionAssert.AreEqual(
                new[] { nameof(FakeAnalysisC), $"{nameof(FakeAnalysisB)} / Sub", nameof(FakeAnalysisA) },
                findings.Select(finding => finding.Name));

            // The accumulated findings are capped to the top weighted ones
            lowWeight.Findings = midWeight.Findings = highWeight.Findings = () => new List<QuantConnect.Analysis>();
            findings = analyzer.Run(1, new[] { "log" }, maxFailedAnalyses: 2);
            CollectionAssert.AreEqual(
                new[] { nameof(FakeAnalysisC), $"{nameof(FakeAnalysisB)} / Sub" },
                findings.Select(finding => finding.Name));
        }

        [Test]
        public void DefaultAnalysisSetIsTheInRunCapableSubsetOfTheFinalSet()
        {
            var analyses = new DefaultSetInRunResultsAnalyzer().DefaultAnalyses;

            Assert.IsNotEmpty(analyses);
            Assert.IsTrue(analyses.All(analysis => analysis.RunsInRun));
            // Representative membership checks: state-based and stream-based in-run analyses
            // are included, final-only ones are not
            Assert.IsTrue(analyses.Any(analysis => analysis is AlgorithmSpeedAnalysis));
            Assert.IsTrue(analyses.Any(analysis => analysis is MarginCallsAnalysis));
            Assert.IsFalse(analyses.Any(analysis => analysis is MonteCarloPercentileAnalysis));
        }

        [Test]
        public void AnalysesAreCreatedOnceAndReusedAcrossRuns()
        {
            var analyzer = new TestInRunResultsAnalyzer(new FakeAnalysisA(10));

            analyzer.Run(1, new[] { "log" });
            analyzer.Run(1, new[] { "log" });

            // Both the analysis chain and the findings ranking read the cached set
            Assert.AreEqual(1, analyzer.GetAnalysesCallCount);
        }

        private static List<QuantConnect.Analysis> MakeFindings(string name, string sample, int? count)
        {
            return new List<QuantConnect.Analysis> { new(name, "An issue", sample, count, SomeSolutions) };
        }

        private class TestInRunResultsAnalyzer : ResultsAnalyzer
        {
            private readonly IReadOnlyCollection<BaseResultsAnalysis> _analyses;

            /// <summary>
            /// The full, chronological order event stream of the simulated backtest, from which the
            /// intermediate results' truncated windows are built.
            /// </summary>
            public List<OrderEvent> OrderEventStream { get; } = new();

            public List<string> Logs { get; } = new();

            public AlgorithmSpeedSample? NextSpeedSample { get; set; }

            public int GetAnalysesCallCount { get; private set; }

            public TestInRunResultsAnalyzer(params BaseResultsAnalysis[] analyses)
                : base(null, Language.CSharp, default, null, null)
            {
                _analyses = analyses;
            }

            /// <summary>
            /// Appends the new order events and logs to the backtest's streams and runs the analyzer
            /// against an intermediate result carrying the truncated, newest-first order events
            /// window the backtesting result handler builds, plus the full accumulated logs.
            /// </summary>
            public IReadOnlyList<QuantConnect.Analysis> Run(int newOrderEventsCount, string[] newLogs,
                AlgorithmSpeedSample? speedSample = null, int timeLimitSeconds = 1, int maxFailedAnalyses = 10,
                int orderEventsWindowSize = 100, Dictionary<int, Order> orders = null, Dictionary<string, Chart> charts = null)
            {
                for (var i = 0; i < newOrderEventsCount; i++)
                {
                    // One event per order, so the (order id, per-order event id) pairs stay unique
                    OrderEventStream.Add(new OrderEvent { OrderId = OrderEventStream.Count + 1, Id = 1 });
                }
                Logs.AddRange(newLogs ?? Array.Empty<string>());
                NextSpeedSample = speedSample;

                var result = new BacktestResult
                {
                    Charts = charts ?? new Dictionary<string, Chart>(),
                    Orders = orders ?? new Dictionary<int, Order>(),
                    OrderEvents = Enumerable.Reverse(OrderEventStream).Take(orderEventsWindowSize).ToList()
                };
                return Run(result, Logs, totalPerformance: null, timeLimitSeconds, maxFailedAnalyses);
            }

            protected override AlgorithmSpeedSample? TakeSpeedSample() => NextSpeedSample;

            protected override IReadOnlyCollection<BaseResultsAnalysis> GetAnalyses()
            {
                GetAnalysesCallCount++;
                return _analyses;
            }
        }

        private sealed class DefaultSetInRunResultsAnalyzer : ResultsAnalyzer
        {
            public DefaultSetInRunResultsAnalyzer()
                : base(null, Language.CSharp, default, null, null)
            {
            }

            public IReadOnlyCollection<BaseResultsAnalysis> DefaultAnalyses => Analyses;
        }

        private class FakeAnalysis : BaseResultsAnalysis
        {
            private readonly int _weight;

            public override string Issue => "A fake issue";

            public override int Weight => _weight;

            public override bool IsStateBased => StateBased;

            public bool StateBased { get; set; }

            public Func<IReadOnlyList<QuantConnect.Analysis>> Findings { get; set; } = () => new List<QuantConnect.Analysis>();

            public Action OnRun { get; set; }

            public Action<ResultsAnalysisRunParameters> OnParameters { get; set; }

            protected FakeAnalysis(int weight)
            {
                _weight = weight;
            }

            public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters)
            {
                OnRun?.Invoke();
                OnParameters?.Invoke(parameters);
                return Findings();
            }
        }

        private sealed class FakeAnalysisA : FakeAnalysis
        {
            public FakeAnalysisA(int weight) : base(weight) { }
        }

        private sealed class FakeAnalysisB : FakeAnalysis
        {
            public FakeAnalysisB(int weight) : base(weight) { }
        }

        private sealed class FakeAnalysisC : FakeAnalysis
        {
            public FakeAnalysisC(int weight) : base(weight) { }
        }
    }
}
