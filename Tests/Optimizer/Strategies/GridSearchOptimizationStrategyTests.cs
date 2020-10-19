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
using System;
using System.Collections.Generic;
using System.Linq;
using Math = System.Math;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture]
    public class GridSearchOptimizationStrategyTests
    {
        private GridSearchOptimizationStrategy _strategy = new GridSearchOptimizationStrategy();
        private Func<ParameterSet, decimal> _compute = parameterSet => parameterSet.Value.Sum(arg => arg.Value.ToDecimal());
        private HashSet<OptimizationParameter> _optimizationParameters = new HashSet<OptimizationParameter>
        {
            new OptimizationParameter("ema-slow", 1, 5, 1),
            new OptimizationParameter("ema-fast", 3, 6, 2)
        };

        [SetUp]
        public void Init()
        {
            _strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                _strategy.PushNewResults(new OptimizationResult(_compute(parameterSet), parameterSet));
            };
        }

        private static TestCaseData[] StrategySettings => new[]
        {
            new TestCaseData(new Maximization(), 10),
            new TestCaseData(new Minimization(), 1)
        };

        [Test, TestCaseSource(nameof(StrategySettings))]
        public void StepInside(Extremum extremum, int bestSet)
        {
            ParameterSet solution = null;
            _strategy.Initialize(extremum, _optimizationParameters);
            _strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                if (parameterSet.Id == bestSet)
                {
                    solution = parameterSet;
                }
            };

            _strategy.PushNewResults(OptimizationResult.Empty);

            Assert.AreEqual(_compute(solution), _strategy.Solution.Target);
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        [Test, TestCaseSource(nameof(StrategySettings))]
        public void FindBest(Extremum extremum, int bestSet)
        {
            var emaSlow = _optimizationParameters.First(s => s.Name == "ema-slow");
            var emaFast = _optimizationParameters.First(s => s.Name == "ema-fast");

            _strategy.Initialize(extremum, _optimizationParameters);
            ParameterSet solution = null;
            int i = 0;
            for (var slow = emaSlow.MinValue; slow <= emaSlow.MaxValue; slow += emaSlow.Step)
            {
                for (var fast = emaFast.MinValue; fast <= emaFast.MaxValue; fast += emaFast.Step)
                {
                    var parameterSet = new ParameterSet(++i, new Dictionary<string, string>
                    {
                        {emaSlow.Name, slow.ToStringInvariant()},
                        {emaFast.Name, fast.ToStringInvariant()}
                    });
                    _strategy.PushNewResults(new OptimizationResult(_compute(parameterSet), parameterSet));
                    if (parameterSet.Id == bestSet)
                    {
                        solution = parameterSet;
                    }
                }
            }

            Assert.AreEqual(_compute(solution), _strategy.Solution.Target);
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution?.Value[arg.Key], arg.Value);
            }
        }

        [TestFixture]
        public class GridSearchTests
        {
            private GridSearchOptimizationStrategy _strategy;

            [SetUp]
            public void Init()
            {
                this._strategy = new GridSearchOptimizationStrategy();
            }

            [TestCase(0)]
            [TestCase(1)]
            [TestCase(-2.5)]
            public void SinglePoint(decimal step)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationParameter("ema-fast", 0, 0, step),
                    new OptimizationParameter("ema-slow", 0, 0, step),
                    new OptimizationParameter("ema-custom", 1, 1, step)
                };

                _strategy.Initialize(new Maximization(), args);

                _strategy.NewParameterSet += (s, e) =>
                {
                    var parameterSet = (e as OptimizationEventArgs).ParameterSet;
                    Assert.AreEqual("0", parameterSet.Value["ema-fast"]);
                    Assert.AreEqual("0", parameterSet.Value["ema-slow"]);
                    Assert.AreEqual("1", parameterSet.Value["ema-custom"]);
                };

                _strategy.PushNewResults(OptimizationResult.Empty);
            }

            [TestCase(-10, 0, -1)]
            [TestCase(-10, 10.5, -0.5)]
            [TestCase(10, 100, 1)]
            [TestCase(10, 100, 500)]
            public void Step1D(decimal min, decimal max, decimal step)
            {
                var param = new OptimizationParameter("ema-fast", min, max, step);
                var set = new HashSet<OptimizationParameter>() { param };
                _strategy.Initialize(new Maximization(), set);
                var counter = 0;

                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, e) =>
                    {
                        enumerator.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Empty);

                    for (var v = param.MinValue; v <= param.MaxValue; v += param.Step)
                    {
                        counter++;
                        Assert.IsTrue(enumerator.MoveNext());

                        var suggestion = enumerator.Current;

                        Assert.IsNotNull(suggestion);
                        Assert.IsTrue(suggestion.Value.All(s => set.Any(arg => arg.Name == s.Key)));
                        Assert.AreEqual(1, suggestion.Value.Count);
                        Assert.AreEqual(v.ToStringInvariant(), suggestion.Value["ema-fast"]);
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);
                Assert.AreEqual(Math.Floor((param.MaxValue - param.MinValue) / param.Step) + 1, counter);
            }

            private static TestCaseData[] OptimizationParameter2D =>
            new[]{
            new TestCaseData(new decimal[,] {{10, 100, 1}, {20, 200, 1}}),
            new TestCaseData(new decimal[,] {{10.5m, 100.5m, 1.5m}, { 20m, 209.9m, 3.5m}}),
            new TestCaseData(new decimal[,] {{ -10.5m, 0m, -1.5m }, { -209.9m, -20m, -3.5m } }),
            new TestCaseData(new decimal[,] {{ 10.5m, 0m, 1.5m }, { 209.9m, -20m, -3.5m } })
            };

            [Test, TestCaseSource(nameof(OptimizationParameter2D))]
            public void Step2D(decimal[,] data)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationParameter("ema-fast", data[0,0], data[0,1], data[0,2]),
                    new OptimizationParameter("ema-slow", data[1,0], data[1,1], data[1,2])
                };
                _strategy.Initialize(new Maximization(), args);
                var counter = 0;
                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, e) =>
                    {
                        enumerator.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Empty);

                    var fastParam = args.First(arg => arg.Name == "ema-fast");
                    var slowParam = args.First(arg => arg.Name == "ema-slow");
                    for (var fast = fastParam.MinValue; fast <= fastParam.MaxValue; fast += fastParam.Step)
                    {
                        for (var slow = slowParam.MinValue; slow <= slowParam.MaxValue; slow += slowParam.Step)
                        {
                            counter++;
                            Assert.IsTrue(enumerator.MoveNext());

                            var suggestion = enumerator.Current;

                            Assert.IsNotNull(suggestion);
                            Assert.IsTrue(suggestion.Value.All(s => args.Any(arg => arg.Name == s.Key)));
                            Assert.AreEqual(2, suggestion.Value.Count);
                            Assert.AreEqual(fast.ToStringInvariant(), suggestion.Value["ema-fast"]);
                            Assert.AreEqual(slow.ToStringInvariant(), suggestion.Value["ema-slow"]);
                        }
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);

                var total = 1m;
                foreach (var arg in args)
                {
                    total *= Math.Floor((arg.MaxValue - arg.MinValue) / arg.Step) + 1;
                }

                Assert.AreEqual(total, counter);
            }

            [Test]
            public void Step3D()
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationParameter("ema-fast", 10, 100, 1),
                    new OptimizationParameter("ema-slow", 20, 200, 4),
                    new OptimizationParameter("ema-custom", 30, 300, 2)
                };
                _strategy.Initialize(new Maximization(), args);
                var counter = 0;

                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, e) =>
                    {
                        enumerator.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Empty);

                    var fastParam = args.First(arg => arg.Name == "ema-fast");
                    var slowParam = args.First(arg => arg.Name == "ema-slow");
                    var customParam = args.First(arg => arg.Name == "ema-custom");
                    for (var fast = fastParam.MinValue; fast <= fastParam.MaxValue; fast += fastParam.Step)
                    {
                        for (var slow = slowParam.MinValue; slow <= slowParam.MaxValue; slow += slowParam.Step)
                        {
                            for (var custom = customParam.MinValue; custom <= customParam.MaxValue; custom += customParam.Step)
                            {
                                counter++;
                                Assert.IsTrue(enumerator.MoveNext());

                                var suggestion = enumerator.Current;

                                Assert.IsNotNull(suggestion);
                                Assert.IsTrue(suggestion.Value.All(s => args.Any(arg => arg.Name == s.Key)));
                                Assert.AreEqual(3, suggestion.Value.Count());
                                Assert.AreEqual(fast.ToStringInvariant(), suggestion.Value["ema-fast"]);
                                Assert.AreEqual(slow.ToStringInvariant(), suggestion.Value["ema-slow"]);
                                Assert.AreEqual(custom.ToStringInvariant(), suggestion.Value["ema-custom"]);
                            }
                        }
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);

                var total = 1m;
                foreach (var arg in args)
                {
                    total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
                }

                Assert.AreEqual(total, counter);
            }

            [Test]
            public void NoStackOverflowException()
            {
                var depth = 100;
                var args = new HashSet<OptimizationParameter>();

                for (int i = 0; i < depth; i++)
                {
                    args.Add(new OptimizationParameter($"ema-{i}", 10, 100, 1));
                }
                _strategy.Initialize(new Maximization(), args);

                var counter = 0;
                _strategy.NewParameterSet += (s, e) =>
                {
                    counter++;
                    Assert.AreEqual(depth, (e as OptimizationEventArgs).ParameterSet.Value.Count);
                    if (counter == 10000)
                    {
                        throw new Exception("Break loop due to large amount of data");
                    }
                };

                Assert.Throws<Exception>(() =>
                {
                    _strategy.PushNewResults(OptimizationResult.Empty);
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
                    new OptimizationParameter("ema-fast", 10, 100, 1)
                };
                _strategy.Initialize(new Maximization(), set);

                _strategy.NewParameterSet += (s, e) =>
                {
                    Assert.AreEqual(nextId++, (e as OptimizationEventArgs).ParameterSet.Id);
                };

                last = nextId;
                _strategy.PushNewResults(OptimizationResult.Empty);
                Assert.Greater(nextId, last);

                last = nextId;
                _strategy.PushNewResults(OptimizationResult.Empty);
                Assert.Greater(nextId, last);
            }

            [Test]
            public void ResetCounterAfterInitialize()
            {
                int nextId = 1;
                _strategy.NewParameterSet += (s, e) =>
                {
                    Assert.AreEqual(nextId++, (e as OptimizationEventArgs).ParameterSet.Id);
                };

                var set1 = new HashSet<OptimizationParameter>()
                {
                    new OptimizationParameter("ema-fast", 10, 100, 1)
                };
                _strategy.Initialize(new Maximization(), set1);

                _strategy.PushNewResults(OptimizationResult.Empty);
                Assert.Greater(nextId, 1);

                var set2 = new HashSet<OptimizationParameter>()
                {
                    new OptimizationParameter("ema-fast", 10, 100, 1),
                    new OptimizationParameter("ema-slow", 10, 100, 2)
                };
                _strategy.Initialize(new Minimization(), set2);
                nextId = 1;
                _strategy.PushNewResults(OptimizationResult.Empty);
                Assert.Greater(nextId, 1);
            }
        }
    }
}
