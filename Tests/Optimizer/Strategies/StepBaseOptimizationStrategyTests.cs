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
using OptimizationParameter = QuantConnect.Optimizer.Parameters.OptimizationParameter;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class StepBaseOptimizationStrategyTests
    {
        private GridSearchOptimizationStrategy _strategy;

        [SetUp]
        public void Init()
        {
            this._strategy = new GridSearchOptimizationStrategy();
        }

        [Test]
        public void ThrowIfNoStepProvidedWhenNoSegmentValue()
        {
            var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100);
            Assert.Throws<ArgumentException>(() =>
            {
                _strategy.Initialize(
                    new Target("Profit", new Maximization(), null),
                    new List<Constraint>(),
                    new HashSet<OptimizationParameter> { optimizationParameter },
                    new StepBaseOptimizationStrategySettings());
            });
        }

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(10)]
        public void CalculateStep(int numberOfSegments)
        {
            var set = new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("ema-fast", 1, 100),
                new OptimizationStepParameter("ema-slow", -10, -10),
                new OptimizationStepParameter("ema-custom", -100, -1)
            };

            _strategy.Initialize(
                new Target("Profit", new Maximization(), null),
                new List<Constraint>(),
                set,
                new StepBaseOptimizationStrategySettings { DefaultSegmentAmount = numberOfSegments });

            foreach (var parameter in set)
            {
                var stepParameter = parameter as OptimizationStepParameter;
                Assert.NotNull(stepParameter);
                var actual = Math.Abs(stepParameter.MaxValue - stepParameter.MinValue) /
                    numberOfSegments;
                Assert.AreEqual(actual, stepParameter.Step);
                Assert.AreEqual(actual / 10, stepParameter.MinStep);
            }
        }

        private static TestCaseData[] StepBaseSettings => new[]
        {
            new TestCaseData(new StepBaseOptimizationStrategySettings {DefaultSegmentAmount = 0}),
            new TestCaseData(new StepBaseOptimizationStrategySettings {DefaultSegmentAmount = -1}),
            new TestCaseData(null),
            new TestCaseData(new StepBaseOptimizationStrategySettings()),
            new TestCaseData(new OptimizationStrategySettings())
        };

        [Test, TestCaseSource(nameof(StepBaseSettings))]
        public void ThrowExceptionIfCantCalculateStep(OptimizationStrategySettings settings)
        {
            var set = new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("ema-fast", 1, 100),
                new OptimizationStepParameter("ema-slow", -10, -10),
                new OptimizationStepParameter("ema-custom", -100, -1)
            };

            foreach (var parameter in set)
            {
                var stepParameter = parameter as OptimizationStepParameter;
                Assert.NotNull(stepParameter);
                Assert.Null(stepParameter.Step);
                Assert.Null(stepParameter.MinStep);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                _strategy.Initialize(
                    new Target("Profit", new Maximization(), null),
                    new List<Constraint>(),
                    set,
                    settings);
            });
        }

    }
}
