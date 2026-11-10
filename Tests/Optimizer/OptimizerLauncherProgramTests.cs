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
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using Program = QuantConnect.Optimizer.Launcher.Program;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture, NonParallelizable]
    public class OptimizerLauncherProgramTests
    {
        private ILogHandler _logHandler;
        private string _logFilePath;
        private bool _debuggingEnabled;

        [SetUp]
        public void SetUp()
        {
            _logHandler = Log.LogHandler;
            _logFilePath = Log.FilePath;
            _debuggingEnabled = Log.DebuggingEnabled;
            FailingStartLeanOptimizer.Created = false;
            FailingStartLeanOptimizer.Disposed = false;

            Config.Set("log-handler", "ConsoleLogHandler");
            Config.Set("optimization-launcher", nameof(FailingStartLeanOptimizer));
            Config.Set("optimization-strategy", "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy");
            Config.Set("optimization-strategy-settings",
                "{\"$type\":\"QuantConnect.Optimizer.Strategies.StepBaseOptimizationStrategySettings, QuantConnect.Optimizer\",\"default-segment-amount\":10}");
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            Log.LogHandler = _logHandler;
            Log.FilePath = _logFilePath;
            Log.DebuggingEnabled = _debuggingEnabled;
        }

        [Test]
        public void ExitsWithFailureWhenStartupFailsBeforeCreatingTheOptimizer()
        {
            Config.Set("estimate", "true");
            Config.Set("parameters", "[{\"name\":\"x\",\"min\":0,\"max\":1,\"step\":0}]");

            Assert.AreEqual(1, RunMain());
        }

        [Test]
        public void ExitsWithFailureAndDisposesTheOptimizerWhenEstimateFails()
        {
            Config.Set("estimate", "true");
            Config.Set("optimization-strategy", typeof(EstimateFailingOptimizationStrategy).AssemblyQualifiedName);
            Config.Set("parameters", "[{\"name\":\"x\",\"min\":0,\"max\":1,\"step\":1}]");

            Assert.AreEqual(1, RunMain());
            Assert.IsTrue(FailingStartLeanOptimizer.Disposed);
        }

        [Test]
        public void ExitsWithFailureAndDisposesTheOptimizerWhenStartFails()
        {
            Config.Set("parameters", "[{\"name\":\"x\",\"min\":0,\"max\":1,\"step\":1}]");

            Assert.AreEqual(1, RunMain());
            Assert.IsTrue(FailingStartLeanOptimizer.Disposed);
        }

        [Test]
        public void ExitsWithSuccessAfterEstimating()
        {
            Config.Set("estimate", "true");
            Config.Set("parameters", "[{\"name\":\"x\",\"min\":0,\"max\":1,\"step\":1}]");

            Assert.AreEqual(0, RunMain());
            Assert.IsTrue(FailingStartLeanOptimizer.Disposed);
        }

        [TestCase("--help")]
        [TestCase("-h")]
        [TestCase("-?")]
        [TestCase("--version")]
        [TestCase("-V")]
        public void ExitsWithSuccessWithoutCreatingTheOptimizerWhenHelpOrVersionIsRequested(string argument)
        {
            Config.Set("estimate", "true");
            Config.Set("parameters", "[{\"name\":\"x\",\"min\":0,\"max\":1,\"step\":1}]");

            Assert.AreEqual(0, RunMain(argument));
            Assert.IsFalse(FailingStartLeanOptimizer.Created);
        }

        [Test]
        public void EstimatesWhenRequestedFromTheCommandLine()
        {
            Config.Set("parameters", "[{\"name\":\"x\",\"min\":0,\"max\":1,\"step\":1}]");

            Assert.AreEqual(0, RunMain("--estimate"));
            Assert.IsTrue(FailingStartLeanOptimizer.Created);
            Assert.IsTrue(FailingStartLeanOptimizer.Disposed);
        }

        private static int RunMain(params string[] args)
        {
            var main = Task.Run(() => Program.Main(args));
            Assert.IsTrue(main.Wait(TimeSpan.FromSeconds(30)), "The optimizer launcher did not exit");
            return main.Result;
        }

        public class FailingStartLeanOptimizer : LeanOptimizer
        {
            public static bool Created { get; set; }

            public static bool Disposed { get; set; }

            public FailingStartLeanOptimizer(OptimizationNodePacket nodePacket)
                : base(nodePacket)
            {
                Created = true;
            }

            protected override string RunLean(ParameterSet parameterSet, string backtestName)
            {
                throw new InvalidOperationException("Lean could not be launched");
            }

            protected override void AbortLean(string backtestId)
            {
            }

            protected override void SendUpdate()
            {
            }

            public override void Dispose()
            {
                Disposed = true;
                base.Dispose();
            }
        }

        public class EstimateFailingOptimizationStrategy : IOptimizationStrategy
        {
            public event EventHandler<ParameterSet> NewParameterSet { add { } remove { } }

            public OptimizationResult Solution => null;

            public void Initialize(Target target, IReadOnlyList<Constraint> constraints, HashSet<OptimizationParameter> parameters,
                OptimizationStrategySettings settings)
            {
            }

            public void PushNewResults(OptimizationResult result)
            {
            }

            public int GetTotalBacktestEstimate()
            {
                throw new InvalidOperationException("The backtests could not be estimated");
            }
        }
    }
}
