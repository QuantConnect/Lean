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
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Analysis;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
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
            var sharpes = new decimal[,]
            {
                { 0.10m, 0.20m, 0.30m },
                { 0.15m, 0.25m, 0.35m },
                { 0.18m, 0.28m, 0.38m }
            };

            var backtests = BuildGridBacktests(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 3, yCount: 3);
            var analyzer = new OptimizationAnalyzer();

            var analysis = analyzer.Run(new OptimizationAnalysisRunParameters(backtests, parameters));

            Assert.NotNull(analysis);
            Assert.AreEqual(9, analysis.BacktestCountUsed);
            Assert.AreEqual(9, analysis.BacktestCountTotal);

            // Mean = average of {0.10..0.38}.
            Assert.That(analysis.OverallSharpe.Mean, Is.EqualTo(0.2433m).Within(0.001m));
            Assert.AreEqual(0.10m, analysis.OverallSharpe.Min);
            Assert.AreEqual(0.38m, analysis.OverallSharpe.Max);
        }

        [Test]
        public void Run_BestBacktestIsArgmaxSharpe()
        {
            var sharpes = new decimal[,]
            {
                { 0.10m, 0.20m, 0.30m },
                { 0.15m, 0.25m, 0.35m },
                { 0.18m, 0.28m, 0.99m } // peak at (2, 2)
            };

            var backtests = BuildGridBacktests(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 3, yCount: 3);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(backtests, parameters));

            Assert.NotNull(analysis.Best);
            Assert.AreEqual(0.99m, analysis.Best.SharpeRatio);
            // Parameters at (xIndex=2, yIndex=2). Grid x: {1,2,3}; y: {10,20,30}.
            Assert.AreEqual(3m, analysis.Best.Parameters["x"]);
            Assert.AreEqual(30m, analysis.Best.Parameters["y"]);
        }

        [Test]
        public void Run_FindsInteriorMode()
        {
            // 3x3 with a single interior peak at (1, 1): should produce one mode with 4 neighbors.
            var sharpes = new decimal[,]
            {
                { 0.10m, 0.20m, 0.10m },
                { 0.20m, 0.99m, 0.20m },
                { 0.10m, 0.20m, 0.10m }
            };

            var backtests = BuildGridBacktests(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 3, yCount: 3);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(backtests, parameters));

            Assert.AreEqual(1, analysis.Modes.Count);
            Assert.AreEqual(0.99m, analysis.Modes[0].SharpeRatio);
            Assert.AreEqual(4, analysis.Modes[0].NeighborCount);
        }

        [Test]
        public void Run_ClusterCountRespectsSqrtCap()
        {
            // 4 backtests -> ceil(sqrt(4)) = 2 -> max 2 clusters.
            var sharpes = new decimal[,]
            {
                { 0.10m, 0.20m },
                { 0.30m, 0.40m }
            };

            var backtests = BuildGridBacktests(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 2, yCount: 2);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(backtests, parameters));

            Assert.LessOrEqual(analysis.Clusters.Count, 2);
        }

        [Test]
        public void Run_BuildsFailedBacktestSummary_FromZeroOrderBacktests()
        {
            // 2x2 grid; every backtest has zero orders and carries known analysis tags.
            var sharpes = new decimal[,]
            {
                { 0m, 0m },
                { 0m, 0m }
            };

            var backtests = BuildGridBacktests(
                sharpes,
                totalOrders: 0,
                analysisNames: new[] { "FlatEquityCurveAnalysis", "ExecutionSpeedAnalysis" });
            var parameters = BuildGridParameters(xCount: 2, yCount: 2);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(backtests, parameters));

            Assert.NotNull(analysis.FailedBacktests);
            Assert.AreEqual(4, analysis.FailedBacktests.ZeroOrderCount);
            Assert.AreEqual(4, analysis.FailedBacktests.InspectedCount);
            Assert.AreEqual(4, analysis.FailedBacktests.AnalysisNameCounts["FlatEquityCurveAnalysis"]);
            Assert.AreEqual(4, analysis.FailedBacktests.AnalysisNameCounts["ExecutionSpeedAnalysis"]);
        }

        [Test]
        public void Run_OmitsFailedBacktestSummary_WhenAllBacktestsTrade()
        {
            var sharpes = new decimal[,]
            {
                { 0.10m, 0.20m },
                { 0.30m, 0.40m }
            };

            var backtests = BuildGridBacktests(sharpes, totalOrders: 5);
            var parameters = BuildGridParameters(xCount: 2, yCount: 2);
            var analysis = new OptimizationAnalyzer().Run(new OptimizationAnalysisRunParameters(backtests, parameters));

            Assert.IsNull(analysis.FailedBacktests);
        }

        [Test]
        public void ExtractFrom_ParsesSharpeAndAnalysisNamesFromBacktestJson()
        {
            var parameterSet = new ParameterSet(0, new Dictionary<string, string> { ["x"] = "1", ["y"] = "10" });
            var json = BuildBacktestJson(0.75m, totalOrders: 12, new[] { "FlatEquityCurveAnalysis" });

            var metrics = OptimizationBacktestMetrics.ExtractFrom("bt-0", parameterSet, json);

            Assert.NotNull(metrics);
            Assert.NotNull(metrics.TotalPerformance?.PortfolioStatistics);
            Assert.AreEqual(0.75m, metrics.SharpeRatio);
            Assert.AreEqual(0.75m, metrics.TotalPerformance.PortfolioStatistics.SharpeRatio);
            Assert.AreEqual(12, metrics.TotalOrders);
            CollectionAssert.AreEqual(new[] { "FlatEquityCurveAnalysis" }, metrics.AnalysisNames.ToArray());
            Assert.AreEqual(1m, metrics.Parameters["x"]);
            Assert.AreEqual(10m, metrics.Parameters["y"]);
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static List<OptimizationBacktestMetrics> BuildGridBacktests(
            decimal[,] sharpes,
            int totalOrders,
            string[] analysisNames = null)
        {
            var result = new List<OptimizationBacktestMetrics>();
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
                    var json = BuildBacktestJson(sharpes[i, j], totalOrders, analysisNames);
                    result.Add(OptimizationBacktestMetrics.ExtractFrom($"backtest-{id}", paramSet, json));
                    id++;
                }
            }
            return result;
        }

        private static string BuildBacktestJson(decimal sharpe, int totalOrders, string[] analysisNames)
        {
            // Build a real BacktestResult and serialize through the LEAN-wide JsonSerializer
            // (CamelCaseNamingStrategy) so the JSON shape matches what BacktestingResultHandler
            // produces in production — which is what OptimizationBacktestMetrics.ExtractFrom
            // round-trips through DeserializeJson<BacktestResult>.
            var result = new QuantConnect.Packets.BacktestResult
            {
                TotalPerformance = new AlgorithmPerformance(),
                Orders = Enumerable.Range(1, totalOrders).ToDictionary(i => i, i => (Order)new MarketOrder()),
                Analysis = (analysisNames ?? System.Array.Empty<string>())
                    .Select(n => new QuantConnect.Analysis(n, "issue", null, null, System.Array.Empty<string>()))
                    .ToList()
            };
            result.TotalPerformance.PortfolioStatistics.SharpeRatio = sharpe;
            return result.SerializeJsonToString();
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
