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
        private IOptimizationParameterSetGenerator _strategy;

        public GridSearchTests()
        {
            this._strategy = new GridSearch();
        }

        [Test]
        public void Step1D()
        {
            var param = new OptimizationParameter("ema-fast", 10, 100, 1);
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
                    Assert.IsTrue(suggestion.Keys.All(s => set.Any(arg => arg.Name == s)));
                    Assert.AreEqual(1, suggestion.Keys.Count());
                    Assert.AreEqual(v, suggestion["ema-fast"]);
                }
            }

            Assert.AreEqual((param.MaxValue - param.MinValue) / param.Step + 1, counter);
        }

        [Test]
        public void Step2D()
        {
            var args = new HashSet<OptimizationParameter>()
            {
                new OptimizationParameter ("ema-fast", 10,100, 1),
                new OptimizationParameter ("ema-slow", 20, 200, 1)
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
                        Assert.IsTrue(suggestion.Keys.All(s => args.Any(arg => arg.Name == s)));
                        Assert.AreEqual(2, suggestion.Keys.Count());
                        Assert.AreEqual(fast, suggestion["ema-fast"]);
                        Assert.AreEqual(slow, suggestion["ema-slow"]);
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
                            Assert.IsTrue(suggestion.Keys.All(s => args.Any(arg => arg.Name == s)));
                            Assert.AreEqual(3, suggestion.Keys.Count());
                            Assert.AreEqual(fast, suggestion["ema-fast"]);
                            Assert.AreEqual(slow, suggestion["ema-slow"]);
                            Assert.AreEqual(custom, suggestion["ema-custom"]);
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
