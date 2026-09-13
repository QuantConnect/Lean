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
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Packets;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmWalkForwardOptimizationTests
    {
        [Test]
        public void ScheduledOptimizationAppliesWinningParameterSet()
        {
            var algorithm = CreateAlgorithm(out var realTimeHandler);
            var provider = new FakeWalkForwardOptimizationProvider
            {
                Result = new WalkForwardOptimizationResult(new ParameterSet(1, new Dictionary<string, string>
                {
                    { "ema-fast", "20" },
                    { "ema-slow", "200" }
                }))
            };
            algorithm.SetWalkForwardOptimizationProvider(provider);
            algorithm.SetParameters(new Dictionary<string, string>
            {
                { "ema-fast", "10" },
                { "ema-slow", "100" }
            });

            algorithm.Optimize(
                algorithm.DateRules.Today,
                algorithm.TimeRules.Now,
                new Target("Profit", new Maximization(), null),
                new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-fast", 10, 20, 10),
                    new OptimizationStepParameter("ema-slow", 100, 200, 100)
                });

            realTimeHandler.SetTime(algorithm.UtcTime);

            Assert.AreEqual("20", algorithm.GetParameter("ema-fast"));
            Assert.AreEqual("200", algorithm.GetParameter("ema-slow"));
            Assert.AreEqual(1, provider.Requests.Count);
            Assert.AreEqual(algorithm, provider.Requests[0].Algorithm);
            Assert.AreEqual(2, provider.Requests[0].Parameters.Count);
            Assert.IsNotNull(provider.Requests[0].Target);
        }

        [Test]
        public void SelectorOptimizationAppliesSelectedBacktestParameterSet()
        {
            var algorithm = CreateAlgorithm(out var realTimeHandler);
            var expectedParameterSet = new ParameterSet(2, new Dictionary<string, string>
            {
                { "lookback", "40" }
            });
            var provider = new FakeWalkForwardOptimizationProvider
            {
                Result = new WalkForwardOptimizationResult(new[]
                {
                    new OptimizationBacktest(new ParameterSet(1, new Dictionary<string, string>
                    {
                        { "lookback", "20" }
                    }), "backtest-1", "candidate-1"),
                    new OptimizationBacktest(expectedParameterSet, "backtest-2", "candidate-2")
                })
            };
            algorithm.SetWalkForwardOptimizationProvider(provider);
            algorithm.SetParameters(new Dictionary<string, string>
            {
                { "lookback", "20" }
            });

            algorithm.Optimize(
                algorithm.DateRules.Today,
                algorithm.TimeRules.Now,
                backtests => expectedParameterSet,
                new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("lookback", 20, 40, 20)
                });

            realTimeHandler.SetTime(algorithm.UtcTime);

            Assert.AreEqual("40", algorithm.GetParameter("lookback"));
            Assert.AreEqual(1, provider.Requests.Count);
            Assert.IsNull(provider.Requests[0].Target);
            Assert.IsNotNull(provider.Requests[0].TargetSelector);
        }

        [Test]
        public void OptimizationChildAlgorithmDoesNotStartNestedOptimization()
        {
            var algorithm = CreateAlgorithm(out var realTimeHandler);
            var provider = new FakeWalkForwardOptimizationProvider
            {
                Result = new WalkForwardOptimizationResult(new ParameterSet(1, new Dictionary<string, string>
                {
                    { "ema-fast", "20" }
                }))
            };
            algorithm.SetWalkForwardOptimizationProvider(provider);
            algorithm.SetParameters(new Dictionary<string, string>
            {
                { "ema-fast", "10" }
            });
            typeof(QCAlgorithm)
                .GetField("_algorithmMode", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(algorithm, AlgorithmMode.Optimization);

            algorithm.Optimize(
                algorithm.DateRules.Today,
                algorithm.TimeRules.Now,
                new Target("Profit", new Maximization(), null),
                new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-fast", 10, 20, 10)
                });

            realTimeHandler.SetTime(algorithm.UtcTime);

            Assert.AreEqual("10", algorithm.GetParameter("ema-fast"));
            Assert.AreEqual(0, provider.Requests.Count);
        }

        private static QCAlgorithm CreateAlgorithm(out BacktestingRealTimeHandler realTimeHandler)
        {
            var algorithm = new QCAlgorithm();
            realTimeHandler = new BacktestingRealTimeHandler();
            var timeLimitManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.MaxValue);
            realTimeHandler.Setup(algorithm, new AlgorithmNodePacket(PacketType.BacktestNode), null, null, timeLimitManager);
            algorithm.Schedule.SetEventSchedule(realTimeHandler);
            algorithm.SetDateTime(new DateTime(2024, 1, 1));
            return algorithm;
        }

        private sealed class FakeWalkForwardOptimizationProvider : IWalkForwardOptimizationProvider
        {
            public List<WalkForwardOptimizationRequest> Requests { get; } = new List<WalkForwardOptimizationRequest>();
            public WalkForwardOptimizationResult Result { get; set; } = WalkForwardOptimizationResult.Empty;

            public WalkForwardOptimizationResult Optimize(WalkForwardOptimizationRequest request)
            {
                Requests.Add(request);
                return Result;
            }
        }
    }
}
