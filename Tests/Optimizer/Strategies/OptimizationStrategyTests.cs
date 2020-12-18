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

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    public abstract class OptimizationStrategyTests
    {
        protected IOptimizationStrategy Strategy;
        protected static Func<ParameterSet, decimal> _profit = parameterSet => parameterSet.Value.Where(pair => pair.Key != "skipFromResultSum").Sum(arg => arg.Value.ToDecimal());
        protected static Func<ParameterSet, decimal> _drawdown = parameterSet => parameterSet.Value.Where(pair => pair.Key != "skipFromResultSum").Sum(arg => arg.Value.ToDecimal()) / 100.0m;
        protected static Func<string, string, decimal> _parse = (dump, parameter) => JObject.Parse(dump).SelectToken($"Statistics.{parameter}").Value<decimal>();
        protected static Func<decimal, decimal, string> _stringify = (profit, drawdown) => BacktestResult.Create(profit, drawdown).ToJson();

        private Queue<OptimizationResult> _pendingOptimizationResults;

        [SetUp]
        public void Init()
        {
            _pendingOptimizationResults = new Queue<OptimizationResult>();
            Strategy = CreateStrategy();

            Strategy.NewParameterSet += (s, parameterSet) =>
            {
                _pendingOptimizationResults.Enqueue(new OptimizationResult(_stringify(_profit(parameterSet), _drawdown(parameterSet)), parameterSet, ""));
            };
        }

        [Test]
        public void ThrowOnReinitialization()
        {
            int nextId = 1;
            Strategy.NewParameterSet += (s, parameterSet) =>
            {
                Assert.AreEqual(nextId++, parameterSet.Id);
            };

            var set1 = new HashSet<OptimizationParameter>()
            {
                new OptimizationStepParameter("ema-fast", 10, 100, 1)
            };
            Strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), set1, CreateSettings());

            Strategy.PushNewResults(OptimizationResult.Initial);
            Assert.Greater(nextId, 1);

            var set2 = new HashSet<OptimizationParameter>()
            {
                new OptimizationStepParameter("ema-fast", 10, 100, 1),
                new OptimizationStepParameter("ema-slow", 10, 100, 2)
            };
            Assert.Throws<InvalidOperationException>(() =>
            {
                Strategy.Initialize(new Target("Profit", new Minimization(), null), null, set2, CreateSettings());
            });
        }

        [Test]
        public void ThrowIfNotInitialized()
        {
            var strategy = new GridSearchOptimizationStrategy();
            Assert.Throws<InvalidOperationException>(() =>
            {
                strategy.PushNewResults(OptimizationResult.Initial);
            });
        }

        protected static HashSet<OptimizationParameter> OptimizationStepParameters = new HashSet<OptimizationParameter>
        {
            new OptimizationStepParameter("ema-slow", 1, 5, 1, 0.1m),
            new OptimizationStepParameter("ema-fast", 3, 6, 2,0.1m)
        };

        protected static HashSet<OptimizationParameter> OptimizationMixedParameters = new HashSet<OptimizationParameter>
        {
            new OptimizationStepParameter("ema-slow", 1, 5, 1),
            new OptimizationStepParameter("ema-fast", 3, 6.75m, 2,0.1m),
            new StaticOptimizationParameter("skipFromResultSum", "SPY")
        };

        public virtual void StepInsideNoTargetNoConstraints(Extremum extremum, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            Strategy.Initialize(
                new Target("Profit", extremum, null),
                null,
                optimizationParameters,
                CreateSettings());

            Strategy.PushNewResults(OptimizationResult.Initial);

            while (_pendingOptimizationResults.Count > 0)
            {
                Strategy.PushNewResults(_pendingOptimizationResults.Dequeue());
            }

            Assert.AreEqual(_profit(solution), _parse(Strategy.Solution.JsonBacktestResult, "Profit"));
            foreach (var arg in Strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        public virtual void StepInsideWithConstraints(decimal drawdown, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            Strategy.Initialize(
                new Target("Profit", new Maximization(), null),
                new List<Constraint> { new Constraint("Drawdown", ComparisonOperatorTypes.Less, drawdown) },
                optimizationParameters,
                CreateSettings());

            Strategy.PushNewResults(OptimizationResult.Initial);

            while (_pendingOptimizationResults.Count > 0)
            {
                Strategy.PushNewResults(_pendingOptimizationResults.Dequeue());
            }

            Assert.AreEqual(_profit(solution), _parse(Strategy.Solution.JsonBacktestResult, "Profit"));
            Assert.AreEqual(_drawdown(solution), _parse(Strategy.Solution.JsonBacktestResult, "Drawdown"));
            foreach (var arg in Strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        public virtual void StepInsideWithTarget(decimal targetValue, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            bool reached = false;
            var target = new Target("Profit", new Maximization(), targetValue);
            target.Reached += (s, e) =>
            {
                reached = true;
            };

            Strategy.Initialize(
                target,
                null,
                optimizationParameters,
                CreateSettings());


            Strategy.PushNewResults(OptimizationResult.Initial);

            while (!reached && _pendingOptimizationResults.Count > 0)
            {
                Strategy.PushNewResults(_pendingOptimizationResults.Dequeue());
            }

            Assert.IsTrue(reached);
            Assert.AreEqual(_profit(solution), _parse(Strategy.Solution.JsonBacktestResult, "Profit"));
            foreach (var arg in Strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        public virtual void TargetNotReached(decimal targetValue, HashSet<OptimizationParameter> optimizationParameters, ParameterSet solution)
        {
            bool reached = false;
            var target = new Target("Profit", new Maximization(), targetValue);
            target.Reached += (s, e) =>
            {
                reached = true;
            };

            Strategy.Initialize(
                target,
                null,
                optimizationParameters,
                CreateSettings());


            Strategy.PushNewResults(OptimizationResult.Initial);

            while (!reached && _pendingOptimizationResults.Count > 0)
            {
                Strategy.PushNewResults(_pendingOptimizationResults.Dequeue());
            }

            Assert.IsFalse(reached);
            Assert.AreEqual(_profit(solution), _parse(Strategy.Solution.JsonBacktestResult, "Profit"));
            foreach (var arg in Strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        protected static TestCaseData[] Estimations => new[]
        {
            new TestCaseData(OptimizationStepParameters, 10),
            new TestCaseData(OptimizationMixedParameters,10)
        };
        [Test, TestCaseSource(nameof(Estimations))]
        public virtual void Estimate(HashSet<OptimizationParameter> optimizationParameters, int expected)
        {
            Strategy.Initialize(
                new Target("Profit", new Maximization(), null),
                null,
                optimizationParameters,
                CreateSettings());

            Assert.AreEqual(expected, Strategy.GetTotalBacktestEstimate());
        }

        protected abstract IOptimizationStrategy CreateStrategy();
        protected abstract OptimizationStrategySettings CreateSettings();
    }
}
