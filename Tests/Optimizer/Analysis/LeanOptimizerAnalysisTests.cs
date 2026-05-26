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
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
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
    /// End-to-end tests that drive a fake <see cref="LeanOptimizer"/> through a real
    /// <c>Start → NewResult → TriggerOnEndEvent</c> cycle and assert what
    /// <c>OptimizationResult.Analysis</c> ends up carrying when the <c>Ended</c> event fires.
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Self)]
    public class LeanOptimizerAnalysisTests
    {
        [Test]
        public void Ended_AttachesAnalysis_WhenTrialsCarrySharpeRatios()
        {
            // Drive a 4×4 grid of trials, each emitting a backtest payload with a Sharpe ratio
            // and Total Orders. The analyzer should populate the Analysis on the Ended event.
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
            // Property-inject the Engine-side analyzer — the same wiring Optimizer.Launcher
            // does. LeanOptimizer skips analysis silently when Analyzer is null, so without
            // this line the happy-path assertion below would never get a populated Analysis.
            optimizer.Analyzer = new OptimizationAnalyzer();

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
            Assert.NotNull(result.Analysis, "Analysis should be populated when trials have Sharpe ratios");
            Assert.Greater(result.Analysis.TrialCountUsed, 0);
            Assert.NotNull(result.Analysis.Best);
            Assert.NotNull(result.Analysis.OverallSharpe);
            // Per-parameter reports should exist for both grid axes.
            Assert.AreEqual(2, result.Analysis.Parameters.Count);
        }

        [Test]
        public void Ended_LeavesAnalysisNull_WhenNoTrialCarriesSharpe()
        {
            // Use the stock FakeLeanOptimizer, which emits a Statistics payload with Profit and
            // Drawdown but no Sharpe Ratio. The analyzer should safely skip and Ended should
            // still fire with Analysis == null.
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
            // Wire the analyzer the same way the launcher does. With no Sharpe in any trial,
            // the analyzer's own safe-skip path returns null — verifying that null travels
            // through TriggerOnEndEvent without breaking the Ended event.
            optimizer.Analyzer = new OptimizationAnalyzer();

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();
            resetEvent.WaitOne();

            Assert.NotNull(result, "Ended must still fire even with no analyzable trials");
            Assert.IsNull(result.Analysis, "Analysis should be null when no trial carries a Sharpe ratio");
        }

        // ── fake that emits Sharpe-bearing backtest JSON ───────────────────────────

        /// <summary>
        /// LeanOptimizer fake whose RunLean produces a backtest result payload shaped like a
        /// real one: a Statistics dictionary with "Sharpe Ratio" and "Total Orders" keys, plus
        /// an Analysis array. Sharpe is deterministically derived from the parameter values
        /// so the analyzer has a real signal to chew on.
        /// </summary>
        private sealed class SharpeEmittingFakeLeanOptimizer : LeanOptimizer
        {
            public SharpeEmittingFakeLeanOptimizer(OptimizationNodePacket nodePacket) : base(nodePacket)
            {
            }

            protected override string RunLean(ParameterSet parameterSet, string backtestName)
            {
                var id = Guid.NewGuid().ToString();
                // Stagger replies a touch so we exercise the concurrent NewResult path.
                Task.Delay(10).ContinueWith(_ =>
                {
                    var x = parameterSet.Value.TryGetValue("x", out var xs) && double.TryParse(xs, NumberStyles.Any, CultureInfo.InvariantCulture, out var xv) ? xv : 0;
                    var y = parameterSet.Value.TryGetValue("y", out var ys) && double.TryParse(ys, NumberStyles.Any, CultureInfo.InvariantCulture, out var yv) ? yv : 0;
                    // Smooth quadratic-ish surface so there's a real best and real sensitivity.
                    var sharpe = 1.0 - 0.05 * Math.Pow(x - 3, 2) - 0.0005 * Math.Pow(y - 25, 2);
                    var payload = new
                    {
                        Statistics = new Dictionary<string, string>
                        {
                            ["Sharpe Ratio"] = sharpe.ToString("R", CultureInfo.InvariantCulture),
                            ["Total Orders"] = "10",
                            ["Profit"] = (x + y).ToString(CultureInfo.InvariantCulture)
                        },
                        Analysis = Array.Empty<QuantConnect.Analysis>()
                    };
                    NewResult(JsonConvert.SerializeObject(payload), id);
                });
                return id;
            }

            protected override void AbortLean(string backtestId) { }
            protected override void SendUpdate() { }
        }
    }
}
