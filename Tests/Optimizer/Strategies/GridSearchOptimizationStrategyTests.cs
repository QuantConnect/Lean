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
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using Math = System.Math;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class GridSearchOptimizationStrategyTests : OptimizationStrategyTests
    {
        [TestFixture]
        public class GridSearchTests
        {
            private GridSearchOptimizationStrategy _strategy;

            [SetUp]
            public void Init()
            {
                this._strategy = new GridSearchOptimizationStrategy();
            }

            [TestCase(1)]
            [TestCase(0.5)]
            public void SinglePoint(decimal step)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 0, 0, step),
                    new OptimizationStepParameter("ema-slow", 0, 0, step),
                    new OptimizationStepParameter("ema-custom", 1, 1, step)
                };

                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new StepBaseOptimizationStrategySettings());

                _strategy.NewParameterSet += (s, parameterSet) =>
                {
                    Assert.AreEqual(0, parameterSet.Value["ema-fast"].ToDecimal());
                    Assert.AreEqual(0, parameterSet.Value["ema-slow"].ToDecimal());
                    Assert.AreEqual(1, parameterSet.Value["ema-custom"].ToDecimal());
                };

                _strategy.PushNewResults(OptimizationResult.Initial);
            }

            [TestCase(10, 100, 1)]
            [TestCase(10, 100, 500)]
            public void Step1D(decimal min, decimal max, decimal step)
            {
                var param = new OptimizationStepParameter("ema-fast", min, max, step);
                var set = new HashSet<OptimizationParameter>() { param };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), set, new StepBaseOptimizationStrategySettings());
                var counter = 0;

                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, parameterSet) =>
                    {
                        enumerator.Enqueue(parameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Initial);

                    using (var paramEnumerator = new OptimizationStepParameterEnumerator(param))
                    {
                        while (paramEnumerator.MoveNext())
                        {
                            var value = paramEnumerator.Current;
                            counter++;
                            Assert.IsTrue(enumerator.MoveNext());

                            var suggestion = enumerator.Current;

                            Assert.IsNotNull(suggestion);
                            Assert.IsTrue(suggestion.Value.All(s => set.Any(arg => arg.Name == s.Key)));
                            Assert.AreEqual(1, suggestion.Value.Count);
                            Assert.AreEqual(value, suggestion.Value["ema-fast"]);
                        }
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);
                Assert.AreEqual(Math.Floor((param.MaxValue - param.MinValue) / param.Step.Value) + 1, counter);
            }

            [TestCase(1, 1, 1)]
            [TestCase(10, 100, 1)]
            [TestCase(10, 100, 500)]
            public void Estimate1D(decimal min, decimal max, decimal step)
            {
                var param = new OptimizationStepParameter("ema-fast", min, max, step);
                var staticParam = new StaticOptimizationParameter("pepe", "SPY");
                var set = new HashSet<OptimizationParameter> { param, staticParam };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), set, new StepBaseOptimizationStrategySettings());

                Assert.AreEqual(Math.Floor(Math.Abs(max - min) / Math.Abs(step)) + 1, _strategy.GetTotalBacktestEstimate());
            }

            private static TestCaseData[] OptimizationStepParameter2D => new[]{
                new TestCaseData(new decimal[,] {{10, 100, 1}, {20, 200, 1}}),
                new TestCaseData(new decimal[,] {{10.5m, 100.5m, 1.5m}, { 20m, 209.9m, 3.5m}})
            };

            [Test, TestCaseSource(nameof(OptimizationStepParameter2D))]
            public void Step2D(decimal[,] data)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", data[0,0], data[0,1], data[0,2]),
                    new OptimizationStepParameter("ema-slow", data[1,0], data[1,1], data[1,2])
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new StepBaseOptimizationStrategySettings());
                var counter = 0;
                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, parameterSet) =>
                    {
                        enumerator.Enqueue(parameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Initial);

                    var fastParam = args.First(arg => arg.Name == "ema-fast") as OptimizationStepParameter;
                    var slowParam = args.First(arg => arg.Name == "ema-slow") as OptimizationStepParameter;

                    using (var fastEnumerator = new OptimizationStepParameterEnumerator(fastParam))
                    {
                        using (var slowEnumerator = new OptimizationStepParameterEnumerator(slowParam))
                        {
                            while (fastEnumerator.MoveNext())
                            {
                                var fast = fastEnumerator.Current;
                                slowEnumerator.Reset();
                                while (slowEnumerator.MoveNext())
                                {
                                    var slow = slowEnumerator.Current;

                                    counter++;
                                    Assert.IsTrue(enumerator.MoveNext());

                                    var suggestion = enumerator.Current;

                                    Assert.IsNotNull(suggestion);
                                    Assert.IsTrue(suggestion.Value.All(s => args.Any(arg => arg.Name == s.Key)));
                                    Assert.AreEqual(2, suggestion.Value.Count);
                                    Assert.AreEqual(fast, suggestion.Value["ema-fast"]);
                                    Assert.AreEqual(slow, suggestion.Value["ema-slow"]);
                                }
                            }
                        }
                    }


                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);

                var total = 1m;
                foreach (var arg in args.Cast<OptimizationStepParameter>())
                {
                    total *= Math.Floor((arg.MaxValue - arg.MinValue) / arg.Step.Value) + 1;
                }

                Assert.AreEqual(total, counter);
            }

            [Test, TestCaseSource(nameof(OptimizationStepParameter2D))]
            public void Estimate2D(decimal[,] data)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", data[0,0], data[0,1], data[0,2]),
                    new OptimizationStepParameter("ema-slow", data[1,0], data[1,1], data[1,2]),
                    new StaticOptimizationParameter("pepe", "SPY")
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new StepBaseOptimizationStrategySettings());

                var total = 1m;
                foreach (var arg in args.OfType<OptimizationStepParameter>())
                {
                    total *= Math.Floor((arg.MaxValue - arg.MinValue) / arg.Step.Value) + 1;
                }

                Assert.AreEqual(total, _strategy.GetTotalBacktestEstimate());
            }

            [Test]
            public void Step3D()
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 10, 100, 1),
                    new OptimizationStepParameter("ema-slow", 20, 200, 4),
                    new OptimizationStepParameter("ema-custom", 30, 300, 2),
                    new StaticOptimizationParameter("pepe", "SPY")
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), null, args, new StepBaseOptimizationStrategySettings());
                var counter = 0;

                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, parameterSet) =>
                    {
                        enumerator.Enqueue(parameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Initial);

                    var fastParam = args.First(arg => arg.Name == "ema-fast") as OptimizationStepParameter;
                    var slowParam = args.First(arg => arg.Name == "ema-slow") as OptimizationStepParameter;
                    var customParam = args.First(arg => arg.Name == "ema-custom") as OptimizationStepParameter;
                    using (var fastEnumerator = new OptimizationStepParameterEnumerator(fastParam))
                    {
                        using (var slowEnumerator = new OptimizationStepParameterEnumerator(slowParam))
                        {
                            using (var customEnumerator = new OptimizationStepParameterEnumerator(customParam))
                            {
                                while (fastEnumerator.MoveNext())
                                {
                                    var fast = fastEnumerator.Current;
                                    slowEnumerator.Reset();

                                    while (slowEnumerator.MoveNext())
                                    {
                                        var slow = slowEnumerator.Current;
                                        customEnumerator.Reset();

                                        while (customEnumerator.MoveNext())
                                        {
                                            var custom = customEnumerator.Current;
                                            counter++;
                                            Assert.IsTrue(enumerator.MoveNext());

                                            var parameterSet = enumerator.Current;

                                            Assert.IsNotNull(parameterSet);
                                            Assert.IsTrue(parameterSet.Value.All(s =>
                                                args.Any(arg => arg.Name == s.Key)));
                                            Assert.AreEqual(4, parameterSet.Value.Count());
                                            Assert.AreEqual(fast, parameterSet.Value["ema-fast"]);
                                            Assert.AreEqual(slow, parameterSet.Value["ema-slow"]);
                                            Assert.AreEqual(custom, parameterSet.Value["ema-custom"]);
                                            Assert.AreEqual("SPY", parameterSet.Value["pepe"]);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);

                var total = 1m;
                foreach (var arg in args.OfType<OptimizationStepParameter>())
                {
                    total *= (arg.MaxValue - arg.MinValue) / arg.Step.Value + 1;
                }

                Assert.AreEqual(total, counter);
            }

            [Test]
            public void Estimate3D()
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 10, 100, 1),
                    new OptimizationStepParameter("ema-slow", 20, 200, 4),
                    new OptimizationStepParameter("ema-custom", 30, 300, 2)
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), null, args, new StepBaseOptimizationStrategySettings());

                var total = 1m;
                foreach (var arg in args.Cast<OptimizationStepParameter>())
                {
                    total *= (arg.MaxValue - arg.MinValue) / arg.Step.Value + 1;
                }

                Assert.AreEqual(total, _strategy.GetTotalBacktestEstimate());
            }

            [Test]
            public void NoStackOverflowException()
            {
                var depth = 100;
                var args = new HashSet<OptimizationParameter>();

                for (int i = 0; i < depth; i++)
                {
                    args.Add(new OptimizationStepParameter($"ema-{i}", 10, 100, 1));
                }
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new StepBaseOptimizationStrategySettings());

                var counter = 0;
                _strategy.NewParameterSet += (s, parameterSet) =>
                {
                    counter++;
                    Assert.AreEqual(depth, parameterSet.Value.Count);
                    if (counter == 10000)
                    {
                        throw new Exception("Break loop due to large amount of data");
                    }
                };

                Assert.Throws<Exception>(() =>
                {
                    _strategy.PushNewResults(OptimizationResult.Initial);
                });

                Assert.AreEqual(10000, counter);
            }

            [Test]
            public void IncrementParameterSetId()
            {
                int nextId = 1,
                    last = 1;

                var set = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 10, 100, 1)
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), null, set, new StepBaseOptimizationStrategySettings());

                _strategy.NewParameterSet += (s, parameterSet) =>
                {
                    Assert.AreEqual(nextId++, parameterSet.Id);
                };

                last = nextId;
                _strategy.PushNewResults(OptimizationResult.Initial);
                Assert.Greater(nextId, last);

                last = nextId;
                _strategy.PushNewResults(OptimizationResult.Initial);
                Assert.Greater(nextId, last);
            }
        }

        protected override IOptimizationStrategy CreateStrategy()
        {
            return new GridSearchOptimizationStrategy();
        }

        protected override OptimizationStrategySettings CreateSettings()
        {
            return new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = 10 };
        }

        private static TestCaseData[] StrategySettings => new[]
        {
            new TestCaseData(new Maximization(), OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5"}, { "ema-fast" , "5"} })),
            new TestCaseData(new Minimization(), OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"} })),
            new TestCaseData(new Maximization(), OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5"}, { "ema-fast" , "5"}, { "skipFromResultSum", "SPY" } })),
            new TestCaseData(new Minimization(), OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"}, { "skipFromResultSum", "SPY" } }))
        };

        [Test, TestCaseSource(nameof(StrategySettings))]
        public override void StepInsideNoTargetNoConstraints(Extremum extremum, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            base.StepInsideNoTargetNoConstraints(extremum, optimizationParameters, solution);
        }

        private static TestCaseData[] OptimizeWithConstraint => new[]
        {
            new TestCaseData(0.05m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"} })),
            new TestCaseData(0.06m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "2"}, { "ema-fast" , "3"} })),
            new TestCaseData(0.05m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "1"}, { "ema-fast" , "3"}, { "skipFromResultSum", "SPY" } })),
            new TestCaseData(0.06m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "2"}, { "ema-fast" , "3"}, { "skipFromResultSum", "SPY" } }))
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
            new TestCaseData(15m, OptimizationStepParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5"}, { "ema-fast" , "5"} })),
            new TestCaseData(155m, OptimizationMixedParameters, new ParameterSet(-1, new Dictionary<string, string>{{"ema-slow", "5"}, { "ema-fast" , "5"}, { "skipFromResultSum", "SPY" } }))
        };

        [Test, TestCaseSource(nameof(OptimizeWithTargetNotReached))]
        public override void TargetNotReached(decimal targetValue, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            base.TargetNotReached(targetValue, optimizationParameters, solution);
        }
    }
}
