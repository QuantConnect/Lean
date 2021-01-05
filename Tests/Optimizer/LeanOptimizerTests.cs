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
        [TestCase("QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy")]
        [TestCase("QuantConnect.Optimizer.Strategies.EulerSearchOptimizationStrategy")]
        public void MaximizeNoTarget(string strategyName)
        {
            var resetEvent = new ManualResetEvent(false);
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
            var optimizer = new FakeLeanOptimizer(packet);

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
            var resetEvent = new ManualResetEvent(false);
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
            var optimizer = new FakeLeanOptimizer(packet);

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
            var resetEvent = new ManualResetEvent(false);
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
            var optimizer = new FakeLeanOptimizer(packet);

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
            var resetEvent = new ManualResetEvent(false);
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
            var optimizer = new FakeLeanOptimizer(packet);

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
            var resetEvent = new ManualResetEvent(false);
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
            var optimizer = new FakeLeanOptimizer(packet);

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
            Config.Set("optimization-update-interval", 1);
            OptimizationResult result = null;
            var resetEvent = new ManualResetEvent(false);
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
            var optimizer = new FakeLeanOptimizer(packet);
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
            // we have 2 force updates at least, expect a few more over it.
            Assert.Greater(totalUpdates, 2);
            Assert.AreEqual(int.Parse(runtimeStatistics["Completed"], CultureInfo.InvariantCulture)
                            + int.Parse(runtimeStatistics["Failed"], CultureInfo.InvariantCulture)
                            + int.Parse(runtimeStatistics["Running"], CultureInfo.InvariantCulture), totalBacktest);
        }
    }
}
