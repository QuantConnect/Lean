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
            var args = new Dictionary<string, OptimizationParameter>()
            {
                {"ema-fast", new OptimizationParameter { MinValue = 10, MaxValue = 100, Step = 1}}
            };

            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
                for (var v = args["ema-fast"].MinValue; v <= args["ema-fast"].MaxValue; v += args["ema-fast"].Step)
                {
                    counter++;
                    Assert.IsTrue(enumerator.MoveNext());

                    var suggestion = enumerator.Current;

                    Assert.IsNotNull(suggestion);
                    Assert.IsTrue(suggestion.Arguments.All(s => args.ContainsKey(s.Key)));
                    Assert.AreEqual(1, suggestion.Arguments.Count());
                    Assert.AreEqual(v, suggestion.Arguments.First(s => s.Key == "ema-fast").Value);
                }
            }

            Assert.AreEqual((args["ema-fast"].MaxValue - args["ema-fast"].MinValue) / args["ema-fast"].Step + 1, counter);
        }

        [Test]
        public void Step2D()
        {
            var args = new Dictionary<string, OptimizationParameter>()
            {
                { "ema-fast", new OptimizationParameter {MinValue = 10, MaxValue = 100, Step = 1}},
                { "ema-slow", new OptimizationParameter {MinValue = 20, MaxValue = 200, Step = 1}}
            };
            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
                for (var fast = args["ema-fast"].MinValue; fast <= args["ema-fast"].MaxValue; fast += args["ema-fast"].Step)
                {
                    for (var slow = args["ema-slow"].MinValue; slow <= args["ema-slow"].MaxValue; slow += args["ema-slow"].Step)
                    {
                        counter++;
                        Assert.IsTrue(enumerator.MoveNext());

                        var suggestion = enumerator.Current;

                        Assert.IsNotNull(suggestion);
                        Assert.IsTrue(suggestion.Arguments.All(s => args.ContainsKey(s.Key)));
                        Assert.AreEqual(2, suggestion.Arguments.Count());
                        Assert.AreEqual(fast, suggestion.Arguments.First(s => s.Key == "ema-fast").Value);
                        Assert.AreEqual(slow, suggestion.Arguments.First(s => s.Key == "ema-slow").Value);
                    }
                }
            }

            var total = 1m;
            foreach (var arg in args.Values)
            {
                total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
            }

            Assert.AreEqual(total, counter);
        }

        [Test]
        public void Step3D()
        {
            var args = new Dictionary<string, OptimizationParameter>()
            {
                {"ema-fast", new OptimizationParameter {MinValue = 10, MaxValue = 100, Step = 1}},
                {"ema-slow",new OptimizationParameter {MinValue = 20, MaxValue = 200, Step = 4}},
                {"ema-custom",new OptimizationParameter {MinValue = 30, MaxValue = 300, Step = 2}},
            };
            var counter = 0;
            using (var enumerator = _strategy.Step(null, args).GetEnumerator())
            {
                for (var fast = args["ema-fast"].MinValue; fast <= args["ema-fast"].MaxValue; fast += args["ema-fast"].Step)
                {
                    for (var slow = args["ema-slow"].MinValue; slow <= args["ema-slow"].MaxValue; slow += args["ema-slow"].Step)
                    {
                        for (var custom = args["ema-custom"].MinValue; custom <= args["ema-custom"].MaxValue; custom += args["ema-custom"].Step)
                        {
                            counter++;
                            Assert.IsTrue(enumerator.MoveNext());

                            var suggestion = enumerator.Current;

                            Assert.IsNotNull(suggestion);
                            Assert.IsTrue(suggestion.Arguments.All(s => args.ContainsKey(s.Key)));
                            Assert.AreEqual(3, suggestion.Arguments.Count());
                            Assert.AreEqual(fast, suggestion.Arguments.First(s => s.Key == "ema-fast").Value);
                            Assert.AreEqual(slow, suggestion.Arguments.First(s => s.Key == "ema-slow").Value);
                            Assert.AreEqual(custom, suggestion.Arguments.First(s => s.Key == "ema-custom").Value);
                        }
                    }
                }
            }

            var total = 1m;
            foreach (var arg in args.Values)
            {
                total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
            }

            Assert.AreEqual(total, counter);
        }
    }
}
