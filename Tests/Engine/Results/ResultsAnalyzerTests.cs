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
using QuantConnect.Lean.Engine.Results.Analysis;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class ResultsAnalyzerTests
    {
        private static readonly IReadOnlyList<string> SomeSolutions = new[] { "A solution" };

        [Test]
        public void RunsTheOverriddenAnalysisSetAndKeepsFindingsWithSolutions()
        {
            var withSolutions = new FakeAnalysisA(10)
            {
                Findings = () => new List<QuantConnect.Analysis>
                {
                    new(nameof(FakeAnalysisA), "An issue", "sample", null, SomeSolutions)
                }
            };
            var withoutSolutions = new FakeAnalysisB(20)
            {
                Findings = () => new List<QuantConnect.Analysis>
                {
                    new(nameof(FakeAnalysisB), "An issue", "sample", null, new List<string>())
                }
            };
            var analyzer = new TestResultsAnalyzer(false, withSolutions, withoutSolutions);

            var findings = analyzer.Run();

            // Findings without solutions are not reported
            Assert.AreEqual(nameof(FakeAnalysisA), findings.Single().Name);
        }

        [Test]
        public void EmptyAnalysisSetProducesNoFindings()
        {
            var analyzer = new TestResultsAnalyzer(false);
            Assert.IsEmpty(analyzer.Run());
        }

        [Test]
        public void SkipsEquityCurveConstructionWhenNotRequired()
        {
            ResultsAnalysisRunParameters seenParameters = null;
            var fake = new FakeAnalysisA(10) { OnRun = parameters => seenParameters = parameters };
            // Null result and algorithm: building the curves would throw, so a successful
            // run proves the equity curves were skipped
            var analyzer = new TestResultsAnalyzer(false, fake);

            Assert.DoesNotThrow(() => analyzer.Run());

            Assert.IsNotNull(seenParameters);
            Assert.IsNotNull(seenParameters.EquityCurve);
            Assert.IsEmpty(seenParameters.EquityCurve);
            Assert.IsNotNull(seenParameters.BenchmarkEquityCurve);
            Assert.IsEmpty(seenParameters.BenchmarkEquityCurve);
        }

        [Test]
        public void AnalysesRunInDescendingWeightOrder()
        {
            var runOrder = new List<string>();
            var analyzer = new TestResultsAnalyzer(false,
                new FakeAnalysisA(10) { OnRun = _ => runOrder.Add(nameof(FakeAnalysisA)) },
                new FakeAnalysisB(30) { OnRun = _ => runOrder.Add(nameof(FakeAnalysisB)) },
                new FakeAnalysisC(20) { OnRun = _ => runOrder.Add(nameof(FakeAnalysisC)) });

            analyzer.Run();

            CollectionAssert.AreEqual(
                new[] { nameof(FakeAnalysisB), nameof(FakeAnalysisC), nameof(FakeAnalysisA) },
                runOrder);
        }

        [Test]
        public void TimeLimitStopsTheAnalysisChain()
        {
            var truncatedRan = false;
            // The slow analysis has the higher weight so it runs first and exhausts the time limit
            var slow = new FakeAnalysisA(20) { OnRun = _ => Thread.Sleep(1100) };
            var truncated = new FakeAnalysisB(10) { OnRun = _ => truncatedRan = true };
            var analyzer = new TestResultsAnalyzer(false, slow, truncated);

            analyzer.Run(timeLimitSeconds: 1);

            Assert.IsFalse(truncatedRan);
        }

        [Test]
        public void MaxFailedAnalysesStopsTheAnalysisChain()
        {
            var skippedRan = false;
            var failing = new FakeAnalysisA(20)
            {
                Findings = () => new List<QuantConnect.Analysis>
                {
                    new(nameof(FakeAnalysisA), "An issue", "sample", null, SomeSolutions)
                }
            };
            var skipped = new FakeAnalysisB(10) { OnRun = _ => skippedRan = true };
            var analyzer = new TestResultsAnalyzer(false, failing, skipped);

            var findings = analyzer.Run(maxFailedAnalyses: 1);

            Assert.IsFalse(skippedRan);
            Assert.AreEqual(1, findings.Count);
        }

        [Test]
        public void AnalysesAreCreatedOnceAndReusedAcrossRuns()
        {
            var analyzer = new TestResultsAnalyzer(false, new FakeAnalysisA(10));

            analyzer.Run();
            analyzer.Run();

            Assert.AreEqual(1, analyzer.GetAnalysesCallCount);
        }

        [Test]
        public void SpeedTrackerIsPassedToTheAnalyses()
        {
            var tracker = new AlgorithmSpeedTracker();
            ResultsAnalysisRunParameters seenParameters = null;
            var fake = new FakeAnalysisA(10) { OnRun = parameters => seenParameters = parameters };
            var analyzer = new TestResultsAnalyzer(false, tracker, fake);

            analyzer.Run();

            Assert.AreSame(tracker, seenParameters.Speed);
        }

        [Test]
        public void DefaultAnalysisSetIncludesTheAlgorithmSpeedAndRuntimeErrorAnalyses()
        {
            var analyses = new DefaultSetResultsAnalyzer().DefaultAnalyses;

            Assert.IsTrue(analyses.Any(analysis => analysis is AlgorithmSpeedAnalysis));
            Assert.IsTrue(analyses.Any(analysis => analysis is SingleTimeLoopTimeoutRuntimeErrorAnalysis));
        }

        [Test]
        public void DefaultAnalysisSetIncludesTheMarketOnCloseOrderTooLateAnalysis()
        {
            var analyses = new DefaultSetResultsAnalyzer().DefaultAnalyses;

            Assert.IsTrue(analyses.Any(analysis => analysis is MarketOnCloseOrderTooLateOrderResponseErrorAnalysis));
        }

        [Test]
        public void DefaultAnalysisSetDeclaresTheFinalOnlyAnalyses()
        {
            var finalOnly = new DefaultSetResultsAnalyzer().DefaultAnalyses
                .Where(analysis => !analysis.RunsInRun)
                .Select(analysis => analysis.GetType().Name);

            // These need the completed run (runtime errors, equity curves, final statistics,
            // completion logs) or read algorithm state that is not safe to access while it runs
            CollectionAssert.AreEquivalent(new[]
            {
                nameof(SingleTimeLoopTimeoutRuntimeErrorAnalysis),
                nameof(FlatEquityCurveAnalysis),
                nameof(OrderFillsDuringExtendedMarketHoursAnalysis),
                nameof(StatisticalSignificanceOfDailyReturnsAnalysis),
                nameof(PerformanceRelativeToBenchmarkAnalysis),
                nameof(CrisisEventsAnalysis),
                nameof(ParameterCountAnalysis),
                nameof(MonteCarloPercentileAnalysis),
            }, finalOnly);
        }

        [Test]
        public void DefaultAnalysisSetDeclaresTheStateBasedAnalyses()
        {
            var stateBased = new DefaultSetResultsAnalyzer().DefaultAnalyses
                .Where(analysis => analysis.IsStateBased)
                .Select(analysis => analysis.GetType().Name);

            CollectionAssert.AreEquivalent(new[]
            {
                nameof(PortfolioValueIsNotPositiveAnalysis),
                nameof(TakeProfitAndStopLossOrdersAnalysis),
                nameof(PortfolioMarginUsageAnalysis),
                nameof(AlgorithmSpeedAnalysis),
            }, stateBased);
        }

        [Test]
        public void InRunOperationsRequireAnInRunInstance()
        {
            // This is a final-analysis instance: the in-run entry points must throw
            var analyzer = new TestResultsAnalyzer(false, new FakeAnalysisA(10));

            Assert.Throws<InvalidOperationException>(() => analyzer.Run(result: null, logs: null, totalPerformance: null));
            Assert.Throws<InvalidOperationException>(() => analyzer.CompleteSpeedTracking());
        }

        private sealed class DefaultSetResultsAnalyzer : ResultsAnalyzer
        {
            public DefaultSetResultsAnalyzer()
                : base(null, null, Language.CSharp, null)
            {
            }

            public IReadOnlyCollection<BaseResultsAnalysis> DefaultAnalyses => GetAnalyses();
        }

        private class TestResultsAnalyzer : ResultsAnalyzer
        {
            private readonly bool _requiresEquityCurves;
            private readonly IReadOnlyCollection<BaseResultsAnalysis> _analyses;

            public TestResultsAnalyzer(bool requiresEquityCurves, params BaseResultsAnalysis[] analyses)
                : this(requiresEquityCurves, null, analyses)
            {
            }

            public TestResultsAnalyzer(bool requiresEquityCurves, AlgorithmSpeedTracker speedTracker, params BaseResultsAnalysis[] analyses)
                : base(null, null, Language.CSharp, null, speedTracker)
            {
                _requiresEquityCurves = requiresEquityCurves;
                _analyses = analyses;
            }

            protected override bool RequiresEquityCurves => _requiresEquityCurves;

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

            public Action<ResultsAnalysisRunParameters> OnRun { get; set; }

            protected FakeAnalysis(int weight)
            {
                _weight = weight;
            }

            public override IReadOnlyList<QuantConnect.Analysis> Run(ResultsAnalysisRunParameters parameters)
            {
                OnRun?.Invoke(parameters);
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
