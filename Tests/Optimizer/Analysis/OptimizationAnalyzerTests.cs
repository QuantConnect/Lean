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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results.Analysis.Optimization;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Analysis;
using QuantConnect.Optimizer.Parameters;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QuantConnect.Tests.Optimizer.Analysis
{
    [TestFixture, Parallelizable(ParallelScope.Self)]
    public class OptimizationAnalyzerTests
    {
        [Test]
        public void Run_ProducesOverallSharpeStats()
        {
            // 3x3 grid of synthetic Sharpe values.
            var sharpes = new double[,]
            {
                { 0.10, 0.20, 0.30 },
                { 0.15, 0.25, 0.35 },
                { 0.18, 0.28, 0.38 }
            };

            var trials = BuildGridTrials(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 3, yCount: 3);
            var analyzer = new OptimizationAnalyzer();

            var analysis = analyzer.Run(new OptimizationAnalysisRunParameters(trials, parameters));

            Assert.NotNull(analysis);
            Assert.AreEqual(9, analysis.TrialCountUsed);
            Assert.AreEqual(9, analysis.TrialCountTotal);

            // Mean = average of {0.10..0.38} = 0.243333...
            Assert.AreEqual(0.2433, analysis.OverallSharpe.Mean, 1e-3);
            Assert.AreEqual(0.10, analysis.OverallSharpe.Min, 1e-9);
            Assert.AreEqual(0.38, analysis.OverallSharpe.Max, 1e-9);
        }

        [Test]
        public void Run_BestTrialIsArgmaxSharpe()
        {
            var sharpes = new double[,]
            {
                { 0.10, 0.20, 0.30 },
                { 0.15, 0.25, 0.35 },
                { 0.18, 0.28, 0.99 } // peak at (2, 2)
            };

            var trials = BuildGridTrials(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 3, yCount: 3);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(trials, parameters));

            Assert.NotNull(analysis.Best);
            Assert.AreEqual(0.99, analysis.Best.SharpeRatio, 1e-9);
            // Parameters at (xIndex=2, yIndex=2). Grid x: {1,2,3}; y: {10,20,30}.
            Assert.AreEqual(3.0, analysis.Best.Parameters["x"], 1e-9);
            Assert.AreEqual(30.0, analysis.Best.Parameters["y"], 1e-9);
        }

        [Test]
        public void Run_FindsInteriorMode()
        {
            // 3x3 with a single interior peak at (1, 1): should produce one mode with 4 neighbors.
            var sharpes = new double[,]
            {
                { 0.10, 0.20, 0.10 },
                { 0.20, 0.99, 0.20 },
                { 0.10, 0.20, 0.10 }
            };

            var trials = BuildGridTrials(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 3, yCount: 3);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(trials, parameters));

            Assert.AreEqual(1, analysis.Modes.Count);
            Assert.AreEqual(0.99, analysis.Modes[0].SharpeRatio, 1e-9);
            Assert.AreEqual(4, analysis.Modes[0].NeighborCount);
        }

        [Test]
        public void Run_ClusterCountRespectsSqrtCap()
        {
            // 4 trials -> ceil(sqrt(4)) = 2 -> max 2 clusters.
            var sharpes = new double[,]
            {
                { 0.10, 0.20 },
                { 0.30, 0.40 }
            };

            var trials = BuildGridTrials(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 2, yCount: 2);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(trials, parameters));

            Assert.LessOrEqual(analysis.Clusters.Count, 2);
        }

        [Test]
        public void Run_BuildsFailedBacktestSummary_FromZeroOrderTrials()
        {
            // 2x2 grid; every trial has zero orders and carries known analysis tags.
            var sharpes = new double[,]
            {
                { 0.0, 0.0 },
                { 0.0, 0.0 }
            };

            var trials = BuildGridTrials(
                sharpes,
                totalOrders: 0,
                analysisNames: new[] { "FlatEquityCurveAnalysis", "ExecutionSpeedAnalysis" });
            var parameters = BuildGridParameters(xCount: 2, yCount: 2);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(trials, parameters));

            Assert.NotNull(analysis.FailedBacktests);
            Assert.AreEqual(4, analysis.FailedBacktests.ZeroOrderCount);
            Assert.AreEqual(4, analysis.FailedBacktests.InspectedCount);
            Assert.AreEqual(4, analysis.FailedBacktests.AnalysisNameCounts["FlatEquityCurveAnalysis"]);
            Assert.AreEqual(4, analysis.FailedBacktests.AnalysisNameCounts["ExecutionSpeedAnalysis"]);
        }

        [Test]
        public void Run_OmitsFailedBacktestSummary_WhenAllTrialsTrade()
        {
            var sharpes = new double[,]
            {
                { 0.10, 0.20 },
                { 0.30, 0.40 }
            };

            var trials = BuildGridTrials(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 2, yCount: 2);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(trials, parameters));

            Assert.IsNull(analysis.FailedBacktests);
        }

        [Test]
        public void ExtractFrom_ParsesSharpeAndAnalysisNamesFromBacktestJson()
        {
            // Verifies the on-the-fly extraction path: given the same backtest-result JSON shape
            // LeanOptimizer.NewResult receives, ExtractFrom pulls out Sharpe / Total Orders /
            // Analysis tags.
            var parameterSet = new ParameterSet(0, new Dictionary<string, string> { ["x"] = "1", ["y"] = "10" });
            var json = BuildBacktestJson(0.75, totalOrders: 12, new[] { "FlatEquityCurveAnalysis" });

            var metrics = OptimizationTrialMetrics.ExtractFrom("bt-0", parameterSet, json);

            Assert.NotNull(metrics);
            Assert.IsTrue(metrics.HasSharpe);
            Assert.AreEqual(0.75, metrics.Sharpe, 1e-9);
            Assert.AreEqual(12, metrics.TotalOrders);
            CollectionAssert.AreEqual(new[] { "FlatEquityCurveAnalysis" }, metrics.AnalysisNames.ToArray());
            Assert.AreEqual(1.0, metrics.Parameters["x"], 1e-9);
            Assert.AreEqual(10.0, metrics.Parameters["y"], 1e-9);
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static List<OptimizationTrialMetrics> BuildGridTrials(
            double[,] sharpes,
            int totalOrders,
            string[] analysisNames = null)
        {
            var trials = new List<OptimizationTrialMetrics>();
            var xCount = sharpes.GetLength(0);
            var yCount = sharpes.GetLength(1);
            var id = 0;
            for (var i = 0; i < xCount; i++)
            {
                for (var j = 0; j < yCount; j++)
                {
                    var paramSet = new ParameterSet(id, new Dictionary<string, string>
                    {
                        ["x"] = (i + 1).ToString(CultureInfo.InvariantCulture),
                        ["y"] = ((j + 1) * 10).ToString(CultureInfo.InvariantCulture)
                    });
                    // Exercise the real extraction factory so the test mirrors what LeanOptimizer
                    // does at NewResult time. Drives both the JSON path and the parameter
                    // parsing in a single line, keeping the on-the-fly contract under test.
                    var json = BuildBacktestJson(sharpes[i, j], totalOrders, analysisNames);
                    trials.Add(OptimizationTrialMetrics.ExtractFrom($"backtest-{id}", paramSet, json));
                    id++;
                }
            }
            return trials;
        }

        private static string BuildBacktestJson(double sharpe, int totalOrders, string[] analysisNames)
        {
            var statistics = new Dictionary<string, string>
            {
                ["Sharpe Ratio"] = sharpe.ToString("R", CultureInfo.InvariantCulture),
                ["Total Orders"] = totalOrders.ToString(CultureInfo.InvariantCulture)
            };
            var analyses = (analysisNames ?? System.Array.Empty<string>())
                .Select(n => new QuantConnect.Analysis(n, "issue", null, null, System.Array.Empty<string>()))
                .ToList();
            return JsonConvert.SerializeObject(new
            {
                Statistics = statistics,
                Analysis = analyses
            });
        }

        private static HashSet<OptimizationParameter> BuildGridParameters(int xCount, int yCount)
        {
            return new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("x", 1, xCount, 1),
                new OptimizationStepParameter("y", 10, yCount * 10, 10)
            };
        }
    }
}
