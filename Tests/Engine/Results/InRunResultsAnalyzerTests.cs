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

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class InRunResultsAnalyzerTests
    {
        private static readonly IReadOnlyList<string> SomeSolutions = new[] { "A solution" };

        [Test]
        public void PositionsAdvanceByTheConsumedOrderEventsAndLogs()
        {
            var analyzer = new TestInRunResultsAnalyzer(new FakeAnalysisA(10));

            analyzer.Run(MakeResult(3), new[] { "log 1", "log 2" });
            Assert.AreEqual(3, analyzer.OrderEventsPosition);
            Assert.AreEqual(2, analyzer.LogsPosition);

            analyzer.Run(MakeResult(5), new[] { "log 3" });
            Assert.AreEqual(8, analyzer.OrderEventsPosition);
            Assert.AreEqual(3, analyzer.LogsPosition);

            // Null order events and logs don't move the positions
            analyzer.Run(new BacktestResult(), null);
            Assert.AreEqual(8, analyzer.OrderEventsPosition);
            Assert.AreEqual(3, analyzer.LogsPosition);
        }

        [Test]
        public void PositionsAdvanceEvenWhenTheTimeLimitTruncatesTheRun()
        {
            var truncatedRan = false;
            // The slow analysis has the higher weight so it runs first and exhausts the time limit
            var slow = new FakeAnalysisA(20) { OnRun = () => Thread.Sleep(1100) };
            var truncated = new FakeAnalysisB(10) { OnRun = () => truncatedRan = true };
            var analyzer = new TestInRunResultsAnalyzer(slow, truncated);

            analyzer.Run(MakeResult(4), new[] { "log 1" }, timeLimitSeconds: 1);

            Assert.IsFalse(truncatedRan);
            Assert.AreEqual(4, analyzer.OrderEventsPosition);
            Assert.AreEqual(1, analyzer.LogsPosition);
        }

        [Test]
        public void StreamBasedFindingsAccumulateAcrossRuns()
        {
            var fake = new FakeAnalysisA(10);
            var analyzer = new TestInRunResultsAnalyzer(fake);

            fake.Findings = () => MakeFindings(nameof(FakeAnalysisA), "first sample", 3);
            analyzer.Run(MakeResult(1), new[] { "log" });

            fake.Findings = () => MakeFindings(nameof(FakeAnalysisA), "second sample", 2);
            var findings = analyzer.Run(MakeResult(1), new[] { "log" });

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

            analyzer.Run(MakeResult(1), new[] { "log" });
            var findings = analyzer.Run(MakeResult(1), new[] { "log" });

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
            analyzer.Run(MakeResult(1), new[] { "log" });

            // The next delta produces no new occurrences: the accumulated finding is still reported
            fake.Findings = () => new List<QuantConnect.Analysis>();
            var findings = analyzer.Run(MakeResult(1), new[] { "log" });

            var finding = findings.Single();
            Assert.AreEqual("sample", finding.Sample);
            Assert.AreEqual(4, finding.Count);
        }

        [Test]
        public void StateBasedFindingsAreReplacedOnEveryRun()
        {
            var fake = new FakeAnalysisA(10)
            {
                Findings = () => MakeFindings(nameof(PortfolioValueIsNotPositiveAnalysis), "old sample", 2)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            analyzer.Run(MakeResult(1), new[] { "log" });

            fake.Findings = () => MakeFindings(nameof(PortfolioValueIsNotPositiveAnalysis), "new sample", 3);
            var findings = analyzer.Run(MakeResult(1), new[] { "log" });

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
                Findings = () => MakeFindings(nameof(PortfolioValueIsNotPositiveAnalysis), "sample", 2)
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            Assert.IsNotEmpty(analyzer.Run(MakeResult(1), new[] { "log" }));

            fake.Findings = () => new List<QuantConnect.Analysis>();
            var findings = analyzer.Run(MakeResult(1), new[] { "log" });

            Assert.IsEmpty(findings);
        }

        [Test]
        public void AggregatedStateBasedFindingsAreReplacedByFullName()
        {
            // Aggregated analyses emit "AnalysisClass / SubAnalysis" finding names: state-based
            // behavior is determined by the base analysis name, replacement is keyed by the full name
            var stateBasedName = nameof(PortfolioValueIsNotPositiveAnalysis);
            var fake = new FakeAnalysisA(10)
            {
                Findings = () => MakeFindings($"{stateBasedName} / SubA", "sample a", 1)
                    .Concat(MakeFindings($"{stateBasedName} / SubB", "sample b", 1))
                    .ToList()
            };
            var analyzer = new TestInRunResultsAnalyzer(fake);
            Assert.AreEqual(2, analyzer.Run(MakeResult(1), new[] { "log" }).Count);

            fake.Findings = () => MakeFindings($"{stateBasedName} / SubA", "new sample a", 2);
            var findings = analyzer.Run(MakeResult(1), new[] { "log" });

            // SubB no longer fails and is dropped; SubA is replaced with the fresh finding
            var finding = findings.Single();
            Assert.AreEqual($"{stateBasedName} / SubA", finding.Name);
            Assert.AreEqual("new sample a", finding.Sample);
            Assert.AreEqual(2, finding.Count);
        }

        [Test]
        public void SpeedSamplesAreTrackedOnlyWhenProvided()
        {
            AlgorithmSpeedTracker speed = null;
            var fake = new FakeAnalysisA(10) { OnParameters = parameters => speed = parameters.Speed };
            var analyzer = new TestInRunResultsAnalyzer(fake);

            analyzer.Run(MakeResult(1), new[] { "log" });
            Assert.IsNotNull(speed);
            Assert.AreEqual(0, speed.SampleCount);

            analyzer.Run(MakeResult(1), new[] { "log" }, new AlgorithmSpeedSample(TimeSpan.FromSeconds(30), 100, 0, 1, 10));
            Assert.AreEqual(1, speed.SampleCount);

            // No sample provided (e.g. while the algorithm warms up): the tracker is left untouched
            analyzer.Run(MakeResult(1), new[] { "log" });
            Assert.AreEqual(1, speed.SampleCount);
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

            var findings = analyzer.Run(MakeResult(1), new[] { "log" });
            CollectionAssert.AreEqual(
                new[] { nameof(FakeAnalysisC), $"{nameof(FakeAnalysisB)} / Sub", nameof(FakeAnalysisA) },
                findings.Select(finding => finding.Name));

            // The accumulated findings are capped to the top weighted ones
            lowWeight.Findings = midWeight.Findings = highWeight.Findings = () => new List<QuantConnect.Analysis>();
            findings = analyzer.Run(MakeResult(1), new[] { "log" }, maxFailedAnalyses: 2);
            CollectionAssert.AreEqual(
                new[] { nameof(FakeAnalysisC), $"{nameof(FakeAnalysisB)} / Sub" },
                findings.Select(finding => finding.Name));
        }

        [Test]
        public void RequiredChartsAreTheChartsReadByTheInRunAnalyses()
        {
            // The result handler only clones these charts into the analyzed snapshot,
            // so this must stay in sync with the charts the in-run analyses read
            CollectionAssert.AreEquivalent(
                new[] { BaseResultsHandler.PortfolioMarginKey },
                InRunResultsAnalyzer.RequiredCharts);
        }

        [Test]
        public void AnalysesAreCreatedOnceAndReusedAcrossRuns()
        {
            var analyzer = new TestInRunResultsAnalyzer(new FakeAnalysisA(10));

            analyzer.Run(MakeResult(1), new[] { "log" });
            analyzer.Run(MakeResult(1), new[] { "log" });

            // Both the analysis chain and the findings ranking read the cached set
            Assert.AreEqual(1, analyzer.GetAnalysesCallCount);
        }

        private static BacktestResult MakeResult(int orderEventsCount)
        {
            return new BacktestResult
            {
                OrderEvents = Enumerable.Range(0, orderEventsCount).Select(_ => new OrderEvent()).ToList()
            };
        }

        private static List<QuantConnect.Analysis> MakeFindings(string name, string sample, int? count)
        {
            return new List<QuantConnect.Analysis> { new(name, "An issue", sample, count, SomeSolutions) };
        }

        private class TestInRunResultsAnalyzer : InRunResultsAnalyzer
        {
            private readonly IReadOnlyCollection<BaseResultsAnalysis> _analyses;

            public TestInRunResultsAnalyzer(params BaseResultsAnalysis[] analyses)
                : base(null, Language.CSharp)
            {
                _analyses = analyses;
            }

            public int GetAnalysesCallCount { get; private set; }

            protected override IReadOnlyCollection<BaseResultsAnalysis> GetAnalyses()
            {
                GetAnalysesCallCount++;
                return _analyses;
            }
        }

        private class FakeAnalysis : BaseResultsAnalysis
        {
            private readonly int _weight;

            public override string Issue => "A fake issue";

            public override int Weight => _weight;

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
