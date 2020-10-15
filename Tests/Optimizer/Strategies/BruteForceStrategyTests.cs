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
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Optimizer;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture]
    public class BruteForceStrategyTests
    {
        private BruteForceStrategy _strategy = new BruteForceStrategy();
        private Func<ParameterSet, decimal> _compute = parameterSet => parameterSet.Value.Sum(arg => arg.Value.ToDecimal());
        private IEnumerable<ParameterSet> _parameterSets = new List<ParameterSet>()
        {
            new ParameterSet(1, new Dictionary<string, string>() {{"ema-fast", "1"}, { "ema-slow", "20" } }),
            new ParameterSet(2, new Dictionary<string, string>() {{"ema-fast", "8"}, { "ema-slow", "17" } }),
            new ParameterSet(3, new Dictionary<string, string>() {{"ema-fast", "2"}, { "ema-slow", "17" } }),
            new ParameterSet(4, new Dictionary<string, string>() {{"ema-fast", "4"}, { "ema-slow", "18" } })
        };
        private Mock<IOptimizationParameterSetGenerator> _mock = new Mock<IOptimizationParameterSetGenerator>();

        [SetUp]
        public void Init()
        {
            _mock
                .Setup(s => s.Step(It.IsAny<ParameterSet>(), It.IsAny<HashSet<OptimizationParameter>>()))
                .Returns(_parameterSets);

            _strategy.Initialize(_mock.Object, new Maximization(), new HashSet<OptimizationParameter>());

            _strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                _strategy.PushNewResults(new OptimizationResult(_compute(parameterSet), parameterSet));
            };
        }

        private static TestCaseData[] StrategySettings => new TestCaseData[]
        {
            new TestCaseData(new Maximization(), 2),
            new TestCaseData(new Minimization(), 3)
        };

        [Test, TestCaseSource(nameof(StrategySettings))]
        public void StepInside(Extremum extremum, int bestSet)
        {
            _strategy.Initialize(_mock.Object, extremum, new HashSet<OptimizationParameter>());

            _strategy.PushNewResults(OptimizationResult.Empty);

            var solution = _parameterSets.First(s => s.Id == bestSet);
            Assert.AreEqual(_compute(solution), _strategy.Solution.Target);
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        [Test, TestCaseSource(nameof(StrategySettings))]
        public void FindBest(Extremum extremum, int bestSet)
        {
            _strategy.Initialize(_mock.Object, extremum, new HashSet<OptimizationParameter>());

            foreach (var parameterSet in _parameterSets)
            {
                _strategy.PushNewResults(new OptimizationResult(_compute(parameterSet), parameterSet));
            }

            var solution = _parameterSets.First(s => s.Id == bestSet);
            Assert.AreEqual(_compute(solution), _strategy.Solution.Target);
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }
    }
}
