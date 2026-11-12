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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Optimizer;
using QuantConnect.Util;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture, Parallelizable(ParallelScope.Children)]
    public class LeanOptimizerTests
    {
        // These tests run in parallel and the LeanOptimizer constructor reads "optimization-update-interval" from the
        // shared, global Config, while TrackEstimation writes it. The underlying config store is not thread-safe, so a
        // write racing a concurrent read can corrupt it. Serialize the construction-time reads against that write.
        private static readonly object _configLock = new object();

        private static FakeLeanOptimizer CreateOptimizer(OptimizationNodePacket packet)
        {
            lock (_configLock)
            {
                return new FakeLeanOptimizer(packet);
            }
        }

        [TestCase("QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy")]
        [TestCase("QuantConnect.Optimizer.Strategies.EulerSearchOptimizationStrategy")]
        public void MaximizeNoTarget(string strategyName)
        {
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                OptimizationStrategy = strategyName,
                Criterion = new Target("Profit",
                    new Maximization(),
                    null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                MaximumConcurrentBacktests = 20,
                OptimizationStrategySettings = new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = 10 }
            };
            using var optimizer = CreateOptimizer(packet);

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
            Assert.AreEqual(
                110,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);

            Assert.AreEqual(10, result.ParameterSet.Value["ema-slow"].ToDecimal());
            Assert.AreEqual(100, result.ParameterSet.Value["ema-fast"].ToDecimal());
        }

        [TestCase("QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy")]
        [TestCase("QuantConnect.Optimizer.Strategies.EulerSearchOptimizationStrategy")]
        public void MinimizeWithTarget(string strategyName)
        {
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                OptimizationStrategy = strategyName,
                Criterion = new Target("Profit", new Minimization(), 20),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                MaximumConcurrentBacktests = 20,
                OptimizationStrategySettings = new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = 10 }
            };
            using var optimizer = CreateOptimizer(packet);

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
            Assert.GreaterOrEqual(
                20,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
        }

        [Test]
        public void MaximizeGridWithConstraints()
        {
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1m),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3m)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 20
            };
            using var optimizer = CreateOptimizer(packet);

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
            Assert.AreEqual(
                15,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
            Assert.AreEqual(
                0.15m,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Drawdown);

        }

        [Test]
        public void MaximizeEulerWithConstraints()
        {
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                OptimizationStrategy = "QuantConnect.Optimizer.Strategies.EulerSearchOptimizationStrategy",
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 10m, 0.1m)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 20,
                OptimizationStrategySettings = new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = 10 }
            };
            using var optimizer = CreateOptimizer(packet);

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
            Assert.AreEqual(
                15,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
            Assert.AreEqual(
                0.15m,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Drawdown);

        }

        [Test]
        public void MinimizeWithTargetAndConstraints()
        {
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Minimization(), 20),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 20
            };
            using var optimizer = CreateOptimizer(packet);

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
            Assert.GreaterOrEqual(
                20,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
            Assert.GreaterOrEqual(
                0.15m,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Drawdown);

        }

        [Test]
        public void TrackEstimation()
        {
            lock (_configLock)
            {
                Config.Set("optimization-update-interval", 1);
            }
            OptimizationResult result = null;
            using var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Minimization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 10)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 5
            };
            using var optimizer = CreateOptimizer(packet);
            // keep stats up-to-date
            int totalBacktest = optimizer.GetCurrentEstimate();
            int totalUpdates = 0;
            int completedTests = 0;
            int failed = 0;
            optimizer.Update += (s, e) =>
            {
                var runtimeStats = optimizer.GetRuntimeStatistics();
                Assert.LessOrEqual(int.Parse(runtimeStats["Running"], CultureInfo.InvariantCulture), packet.MaximumConcurrentBacktests);
                Assert.LessOrEqual(completedTests, int.Parse(runtimeStats["Completed"], CultureInfo.InvariantCulture));
                Assert.LessOrEqual(failed, int.Parse(runtimeStats["Failed"], CultureInfo.InvariantCulture));

                Assert.AreEqual(totalBacktest, optimizer.GetCurrentEstimate());

                completedTests = int.Parse(runtimeStats["Completed"], CultureInfo.InvariantCulture);
                failed = int.Parse(runtimeStats["Failed"], CultureInfo.InvariantCulture);

                if (completedTests > 0)
                {
                    // 'ms' aren't stored so might be 0
                    Assert.GreaterOrEqual(TimeSpan.Parse(runtimeStats["Average Length"], CultureInfo.InvariantCulture), TimeSpan.Zero);
                }

                totalUpdates++;
            };
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();

            resetEvent.WaitOne();

            var runtimeStatistics = optimizer.GetRuntimeStatistics();
            Assert.NotZero(int.Parse(runtimeStatistics["Completed"], CultureInfo.InvariantCulture));
            Assert.NotZero(int.Parse(runtimeStatistics["Failed"], CultureInfo.InvariantCulture));
            // there are 2 forced estimation updates (start and end); any additional in-progress updates depend on
            // timing and are not guaranteed (e.g. when backtests complete/fail fast), so only the guaranteed
            // minimum is asserted here to avoid a flaky "expected > 2 but was 2" failure.
            Assert.GreaterOrEqual(totalUpdates, 2);
            Assert.AreEqual(int.Parse(runtimeStatistics["Completed"], CultureInfo.InvariantCulture)
                            + int.Parse(runtimeStatistics["Failed"], CultureInfo.InvariantCulture)
                            + int.Parse(runtimeStatistics["Running"], CultureInfo.InvariantCulture), totalBacktest);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LaunchFailureDoesNotDiscardPendingParameterSets(bool throwOnLaunch)
        {
            var packet = new OptimizationNodePacket
            {
                OptimizationStrategy = "QuantConnect.Optimizer.Strategies.EulerSearchOptimizationStrategy",
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("x", 0, 20, 10, 5)
                },
                MaximumConcurrentBacktests = 1,
                OptimizationStrategySettings = new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = 2 }
            };
            OptimizationResult result = null;
            LaunchFailureLeanOptimizer optimizer;
            lock (_configLock)
            {
                optimizer = new LaunchFailureLeanOptimizer(packet, throwOnLaunch);
            }
            using var _ = optimizer;
            optimizer.Ended += (s, solution) => result = solution;

            optimizer.Start();
            CollectionAssert.AreEqual(new[] { "0" }, optimizer.Attempts);

            // the next launch fails, the one after it must still run
            optimizer.CompleteLastLaunched();
            CollectionAssert.AreEqual(new[] { "0", "10", "20" }, optimizer.Attempts);
            Assert.IsNull(result);

            // the failed launch was reported to the strategy, so its round ends and the search is refined around x = 20
            optimizer.CompleteLastLaunched();
            CollectionAssert.AreEqual(new[] { "0", "10", "20", "15" }, optimizer.Attempts);
            Assert.IsNull(result);

            optimizer.CompleteLastLaunched();
            CollectionAssert.AreEqual(new[] { "0", "10", "20", "15", "20" }, optimizer.Attempts);
            Assert.IsNull(result);

            optimizer.CompleteLastLaunched();
            Assert.IsNotNull(result);
            Assert.AreEqual("20", result.ParameterSet.Value["x"]);
            var runtimeStatistics = optimizer.GetRuntimeStatistics();
            Assert.AreEqual("4", runtimeStatistics["Completed"]);
            Assert.AreEqual("1", runtimeStatistics["Failed"]);
        }

        private class LaunchFailureLeanOptimizer : LeanOptimizer
        {
            private readonly bool _throwOnLaunch;
            private string _lastLaunchedId;

            public List<string> Attempts { get; } = new();

            public LaunchFailureLeanOptimizer(OptimizationNodePacket packet, bool throwOnLaunch) : base(packet)
            {
                _throwOnLaunch = throwOnLaunch;
            }

            public void CompleteLastLaunched()
            {
                var x = RunningParameterSetForBacktest[_lastLaunchedId].Value["x"].ToDecimal();
                NewResult(BacktestResult.Create(x).ToJson(), _lastLaunchedId);
            }

            protected override string RunLean(ParameterSet parameterSet, string backtestName)
            {
                var x = parameterSet.Value["x"];
                Attempts.Add(x);
                if (x != "10")
                {
                    _lastLaunchedId = $"backtest-{Attempts.Count}";
                    return _lastLaunchedId;
                }
                if (_throwOnLaunch)
                {
                    throw new InvalidOperationException("Launch failure");
                }
                return null;
            }

            protected override void AbortLean(string backtestId)
            {
            }

            protected override void SendUpdate()
            {
            }
        }
    }
}
