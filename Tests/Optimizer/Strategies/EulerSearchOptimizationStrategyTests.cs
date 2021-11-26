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

using NUnit.Framework;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class EulerSearchOptimizationStrategyTests : OptimizationStrategyTests
    {
        [TestFixture]
        public class EulerSearchTests
        {
            private EulerSearchOptimizationStrategy _strategy;

            [SetUp]
            public void Init()
            {
                this._strategy = new EulerSearchOptimizationStrategy();
            }

            [TestCase(10)]
            [TestCase(5)]
            [TestCase(2)]
            public void Depth(int _defaultSegmentAmount)
            {
                var param = new OptimizationStepParameter("ema-fast", 10, 100, 10, 0.1m);
                var set = new HashSet<OptimizationParameter> { param, new StaticOptimizationParameter("pepe", "pipi") };
                _strategy.Initialize(
                    new Target("Profit", new Maximization(), null),
                    new List<Constraint>(),
                    set,
                    new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = _defaultSegmentAmount });
                Queue<OptimizationResult> _pendingOptimizationResults = new Queue<OptimizationResult>();
                int depth = -1;
                _strategy.NewParameterSet += (s, parameterSet) =>
                    {
                        if (_pendingOptimizationResults.Count == 0)
                        {
                            depth++;
                        }

                        _pendingOptimizationResults.Enqueue(new OptimizationResult(_stringify(_profit(parameterSet), _drawdown(parameterSet)), parameterSet, ""));
                    };

                _strategy.PushNewResults(OptimizationResult.Initial);

                while (_pendingOptimizationResults.Count > 0)
                {
                    _strategy.PushNewResults(_pendingOptimizationResults.Dequeue());
                }

                Assert.AreEqual(Math.Ceiling(Math.Log((double)(param.Step / param.MinStep), _defaultSegmentAmount)), depth);
            }

            [Test]
            public void ThrowIfNoSettingsPassed()
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    _strategy.Initialize(
                        new Target("Profit", new Maximization(), null),
                        new List<Constraint>(),
                        new HashSet<OptimizationParameter>
                        {
                            new OptimizationStepParameter("ema-fast", 10, 100, 10, 0.1m),
                            new StaticOptimizationParameter("pepe", "pipi")
                        },
                        null);
                });
            }

            [TestCase(5)]
            [TestCase(10)]
            public void Reduce(int amountOfSegments)
            {
                var param = new OptimizationStepParameter("ema-fast", 10, 100, 10, 0.1m);
                var set = new HashSet<OptimizationParameter> { param, new StaticOptimizationParameter("pepe", "pipi") };
                _strategy.Initialize(
                    new Target("Profit", new Maximization(), null),
                    new List<Constraint>(),
                    set,
                    new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = amountOfSegments });
                Queue<OptimizationResult> pendingOptimizationResults = new Queue<OptimizationResult>();
                int depth = -1;
                _strategy.NewParameterSet += (s, parameterSet) =>
                {
                    if (pendingOptimizationResults.Count == 0)
                    {
                        depth++;
                    }

                    pendingOptimizationResults.Enqueue(new OptimizationResult(_stringify(_profit(parameterSet), _drawdown(parameterSet)), parameterSet, ""));
                };

                _strategy.PushNewResults(OptimizationResult.Initial);

                var step = param.Step ?? 1;
                var datapoint = param.MinValue;
                while (pendingOptimizationResults.Count > 0)
                {
                    var count = pendingOptimizationResults.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var optimizationResult = pendingOptimizationResults.Dequeue();
                        Assert.AreEqual(datapoint + step * i,
                            optimizationResult.ParameterSet.Value["ema-fast"].ToDecimal());
                        _strategy.PushNewResults(optimizationResult);
                    }

                    step = Math.Max(param.MinStep.Value, step / amountOfSegments);
                    datapoint = param.MaxValue - step * ((decimal)amountOfSegments / 2);
                }
            }
        }

        protected override IOptimizationStrategy CreateStrategy()
        {
            return new EulerSearchOptimizationStrategy();
        }

        protected override OptimizationStrategySettings CreateSettings()
        {
            return new StepBaseOptimizationStrategySettings(){DefaultSegmentAmount = 10};
        }

        private static TestCaseData[] StrategySettings => new[]
        {
            new TestCaseData(new Maximization(), OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5.0"}, { "ema-fast" , "6.0"} })),
            new TestCaseData(new Minimization(), OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"} })),
            new TestCaseData(new Maximization(), OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5"}, { "ema-fast" , "6.0" }, { "skipFromResultSum", "SPY" } })),
            new TestCaseData(new Minimization(), OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"}, { "skipFromResultSum", "SPY" } }))
        };

        [Test, TestCaseSource(nameof(StrategySettings))]
        public override void StepInsideNoTargetNoConstraints(Extremum extremum, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            base.StepInsideNoTargetNoConstraints(extremum, optimizationParameters, solution);
        }

        private static TestCaseData[] OptimizeWithConstraint => new[]
        {
            new TestCaseData(0.05m, OptimizationStepParameters,new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1.1"}, { "ema-fast" , "3.8"} })),
            new TestCaseData(0.06m, OptimizationStepParameters,new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1.9"}, { "ema-fast" , "4.0"} })),
            new TestCaseData(0.05m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3.9" }, { "skipFromResultSum", "SPY" } })),
            new TestCaseData(0.06m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "2"}, { "ema-fast" , "3.9" }, { "skipFromResultSum", "SPY" } }))
        };
        [Test, TestCaseSource(nameof(OptimizeWithConstraint))]
        public override void StepInsideWithConstraints(decimal drawdown, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            base.StepInsideWithConstraints(drawdown, optimizationParameters, solution);
        }

        private static TestCaseData[] OptimizeWithTarget => new[]
        {
            new TestCaseData(0m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"} })),
            new TestCaseData(4m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"} })),
            new TestCaseData(5m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "5"} })),
            new TestCaseData(8m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "3"}, { "ema-fast" , "5"} })),
            new TestCaseData(0m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"}, { "skipFromResultSum", "SPY" } })),
            new TestCaseData(5m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "5"}, { "skipFromResultSum", "SPY" } })),
            new TestCaseData(8m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "3"}, { "ema-fast" , "5"}, { "skipFromResultSum", "SPY" } }))
        };
        [Test, TestCaseSource(nameof(OptimizeWithTarget))]
        public override void StepInsideWithTarget(decimal targetValue, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            base.StepInsideWithTarget(targetValue, optimizationParameters, solution);
        }

        private static TestCaseData[] OptimizeWithTargetNotReached => new[]
        {
            new TestCaseData(15m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5.0"}, { "ema-fast" , "6.0" } })),
            new TestCaseData(155m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5"}, { "ema-fast" , "6.0"}, { "skipFromResultSum", "SPY" } }))
        };

        [Test, TestCaseSource(nameof(OptimizeWithTargetNotReached))]
        public override void TargetNotReached(decimal targetValue, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            base.TargetNotReached(targetValue, optimizationParameters, solution);
        }
    }
}
