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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Optimizer;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture]
    public class GridSearchTests
    {
        private IOptimizationStrategy _strategy;

        public GridSearchTests()
        {
            this._strategy = new GridSearch();
        }

        [Test]
        public void Step1D()
        {
            var param = new OptimizationParameter() { Name = "ema-fast", MinValue = 10, MaxValue = 100, Step = 1 };
            var counter = 0;
            using (var enumerator = _strategy.Step(null, new[] {param}).GetEnumerator())
            {
                for (var v = param.MinValue; v <= param.MaxValue; v += param.Step)
                {
                    counter++;
                    Assert.IsTrue(enumerator.MoveNext());

                    var suggestion = enumerator.Current;

                    Assert.IsNotNull(suggestion);
                    Assert.IsTrue(suggestion.Arguments.Any(s => s.Key == param.Name));
                    Assert.AreEqual(1, suggestion.Arguments.Count());
                    Assert.AreEqual(v, suggestion.Arguments.First(s => s.Key == param.Name).Value);
                }
            }

            Assert.AreEqual((param.MaxValue - param.MinValue)/param.Step + 1, counter);
        }

        [Test]
        public void Step2D()
        {
            var args = new List<OptimizationParameter>
            {
                new OptimizationParameter() {Name = "ema-fast", MinValue = 10, MaxValue = 100, Step = 1},
                new OptimizationParameter() {Name = "ema-slow", MinValue = 20, MaxValue = 200, Step = 1}
            };
            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
                for (var fast = args[0].MinValue; fast <= args[0].MaxValue; fast += args[0].Step)
                {
                    for (var slow = args[1].MinValue; slow <= args[1].MaxValue; slow += args[1].Step)
                    {
                        counter++;
                        Assert.IsTrue(enumerator.MoveNext());

                        var suggestion = enumerator.Current;

                        Assert.IsNotNull(suggestion);
                        Assert.IsTrue(suggestion.Arguments.Any(s => s.Key == args[0].Name));
                        Assert.IsTrue(suggestion.Arguments.Any(s => s.Key == args[1].Name));
                        Assert.AreEqual(2, suggestion.Arguments.Count());
                        Assert.AreEqual(fast, suggestion.Arguments.First(s => s.Key == args[0].Name).Value);
                        Assert.AreEqual(slow, suggestion.Arguments.First(s => s.Key == args[1].Name).Value);
                    }
                }
            }

            var total = 1m;
            foreach (var arg in args)
            {
                total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
            }

            Assert.AreEqual(total, counter);
        }

        [Test]
        public void Step3D()
        {
            var args = new List<OptimizationParameter>
            {
                new OptimizationParameter() {Name = "ema-fast", MinValue = 10, MaxValue = 100, Step = 1},
                new OptimizationParameter() {Name = "ema-slow", MinValue = 20, MaxValue = 200, Step = 4},
                new OptimizationParameter() {Name = "ema-custom", MinValue = 30, MaxValue = 300, Step = 2},
            };
            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
                for (var fast = args[0].MinValue; fast <= args[0].MaxValue; fast += args[0].Step)
                {
                    for (var slow = args[1].MinValue; slow <= args[1].MaxValue; slow += args[1].Step)
                    {
                        for (var custom = args[2].MinValue; custom <= args[2].MaxValue; custom += args[2].Step)
                        {
                            counter++;
                            Assert.IsTrue(enumerator.MoveNext());

                            var suggestion = enumerator.Current;

                            Assert.IsNotNull(suggestion);
                            Assert.IsTrue(suggestion.Arguments.Any(s => s.Key == args[0].Name));
                            Assert.IsTrue(suggestion.Arguments.Any(s => s.Key == args[1].Name));
                            Assert.IsTrue(suggestion.Arguments.Any(s => s.Key == args[2].Name));
                            Assert.AreEqual(3, suggestion.Arguments.Count());
                            Assert.AreEqual(fast, suggestion.Arguments.First(s => s.Key == args[0].Name).Value);
                            Assert.AreEqual(slow, suggestion.Arguments.First(s => s.Key == args[1].Name).Value);
                            Assert.AreEqual(custom, suggestion.Arguments.First(s => s.Key == args[2].Name).Value);
                        }
                    }
                }
            }

            var total = 1m;
            foreach (var arg in args)
            {
                total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
            }

            Assert.AreEqual(total, counter);
        }
    }
}
