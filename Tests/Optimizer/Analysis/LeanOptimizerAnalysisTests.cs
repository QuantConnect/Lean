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
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Optimizer.Analysis
{
    /// <summary>
    /// End-to-end tests for <see cref="LeanOptimizer"/>'s analyzer wiring via the <see cref="LeanOptimizer.Ended"/> event.
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Self)]
    public class LeanOptimizerAnalysisTests
    {
        [Test]
        public void Ended_AttachesAnalysis_WhenBacktestsCarrySharpeRatios()
        {
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("x", 1, 4, 1),
                    new OptimizationStepParameter("y", 10, 40, 10)
                },
                MaximumConcurrentBacktests = 8
            };
            using var optimizer = new SharpeEmittingFakeLeanOptimizer(packet);

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();
            resetEvent.WaitOne();

            Assert.NotNull(result);
            Assert.NotNull(result.Analysis, "Analysis should be populated when backtests have Sharpe ratios");
            Assert.Greater(result.Analysis.BacktestCountUsed, 0);
            Assert.NotNull(result.Analysis.Best);
            Assert.NotNull(result.Analysis.OverallSharpe);
            Assert.AreEqual(2, result.Analysis.Parameters.Count);
        }

        [Test]
        public void Ended_LeavesAnalysisNull_WhenNoBacktestCarriesSharpe()
        {
            // FakeLeanOptimizer's payload carries no Sharpe; analyzer must safely skip.
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 5, 1),
                    new OptimizationStepParameter("ema-fast", 10, 50, 10)
                },
                MaximumConcurrentBacktests = 8
            };
            using var optimizer = new FakeLeanOptimizer(packet);

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();
            resetEvent.WaitOne();

            Assert.NotNull(result, "Ended must still fire even with no analyzable backtests");
            Assert.IsNull(result.Analysis, "Analysis should be null when no backtest carries a Sharpe ratio");
        }

        /// <summary>
        /// <see cref="LeanOptimizer"/> fake that emits backtest JSON shaped like a real one, with a deterministic Sharpe.
        /// </summary>
        private sealed class SharpeEmittingFakeLeanOptimizer : LeanOptimizer
        {
            public SharpeEmittingFakeLeanOptimizer(OptimizationNodePacket nodePacket) : base(nodePacket)
            {
            }

            protected override string RunLean(ParameterSet parameterSet, string backtestName)
            {
                var id = Guid.NewGuid().ToString();
                Task.Delay(10).ContinueWith(_ =>
                {
                    var x = parameterSet.Value.TryGetValue("x", out var xs) && decimal.TryParse(xs, NumberStyles.Any, CultureInfo.InvariantCulture, out var xv) ? xv : 0m;
                    var y = parameterSet.Value.TryGetValue("y", out var ys) && decimal.TryParse(ys, NumberStyles.Any, CultureInfo.InvariantCulture, out var yv) ? yv : 0m;
                    // Math.Pow is double-only; cross into double for the surface and back.
                    var sharpe = (decimal)(1.0 - 0.05 * Math.Pow((double)x - 3, 2) - 0.0005 * Math.Pow((double)y - 25, 2));
                    // Build a real BacktestResult and serialize via the LEAN-wide JsonSerializer
                    // so the JSON shape matches what BacktestingResultHandler produces.
                    var result = new QuantConnect.Packets.BacktestResult
                    {
                        // Statistics dict is what the optimizer's Criterion targets (e.g. "Statistics.Profit").
                        Statistics = new Dictionary<string, string>
                        {
                            ["Profit"] = (x + y).ToString(CultureInfo.InvariantCulture)
                        },
                        // Typed TotalPerformance.PortfolioStatistics is what the analyzer reads.
                        TotalPerformance = new AlgorithmPerformance(),
                        Orders = Enumerable.Range(1, 10).ToDictionary(i => i, i => (Order)new MarketOrder()),
                        Analysis = Array.Empty<QuantConnect.Analysis>()
                    };
                    result.TotalPerformance.PortfolioStatistics.SharpeRatio = sharpe;
                    NewResult(result.SerializeJsonToString(), id);
                });
                return id;
            }

            protected override void AbortLean(string backtestId) { }
            protected override void SendUpdate() { }
        }
    }
}
