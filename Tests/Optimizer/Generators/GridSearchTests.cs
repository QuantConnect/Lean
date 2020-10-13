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
using NUnit.Framework;
using QuantConnect.Optimizer;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Optimizer.Generators
{
    [TestFixture]
    public class GridSearchTests
    {
        private IOptimizationParameterSetGenerator _strategy;

        public GridSearchTests()
        {
            this._strategy = new GridSearch();
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
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                var parameterSet = enumerator.Current;
                Assert.AreEqual("0", parameterSet.Value["ema-fast"]);
                Assert.AreEqual("0", parameterSet.Value["ema-slow"]);
                Assert.AreEqual("1", parameterSet.Value["ema-custom"]);
                Assert.IsFalse(enumerator.MoveNext());
            }
        }

        [TestCase(-10, 0, -1)]
        [TestCase(-10, 10.5, -0.5)]
        [TestCase(10, 100, 1)]
        [TestCase(10, 100, 500)]
        public void Step1D(decimal min, decimal max, decimal step)
        {
            var param = new OptimizationParameter("ema-fast", min, max, step);
            var set = new HashSet<OptimizationParameter>() { param };
            var counter = 0;
            using (var enumerator = _strategy.Step(null, set).GetEnumerator())
            {
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

                Assert.IsFalse(enumerator.MoveNext());
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
            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
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

                Assert.IsFalse(enumerator.MoveNext());
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
            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
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

                Assert.IsFalse(enumerator.MoveNext());
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

            var counter = 0;
            foreach (var parameterSet in _strategy.Step(null, args))
            {
                counter++;

                Assert.AreEqual(depth, parameterSet.Value.Count);
                if (counter == 10000)
                {
                    break;
                }
            }

            Assert.AreEqual(10000, counter);
        }

        [Test]
        public void IncrementParameterSetId()
        {
            int nextId = 1;

            var set1 = new HashSet<OptimizationParameter>()
            {
                new OptimizationParameter("ema-fast", 10, 100, 1)
            };
            foreach (var parameterSet in _strategy.Step(null, set1))
            {
                Assert.AreEqual(nextId++, parameterSet.Id);
            }

            var set2 = new HashSet<OptimizationParameter>()
            {
                new OptimizationParameter("ema-fast", 10, 100, 1),
                new OptimizationParameter("ema-slow", 1, 50, 0.5m)
            };
            foreach (var parameterSet in _strategy.Step(null, set2))
            {
                Assert.AreEqual(nextId++, parameterSet.Id);
            }

            Assert.Greater(nextId, 1);
        }
    }
}
