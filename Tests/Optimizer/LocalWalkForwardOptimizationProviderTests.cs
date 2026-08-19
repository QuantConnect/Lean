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
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Api;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Launcher;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture]
    public class LocalWalkForwardOptimizationProviderTests
    {
        [Test]
        public void RunsOptimizerAndReturnsWinningParameterSet()
        {
            var provider = new TestLocalWalkForwardOptimizationProvider();
            var parameters = new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("ema-fast", 10, 20, 10)
            };
            var target = new Target("Profit", new Maximization(), null);

            var result = provider.Optimize(new WalkForwardOptimizationRequest(
                new QCAlgorithm(),
                new DateTime(2026, 1, 1),
                target,
                parameters));

            Assert.AreEqual("20", result.ParameterSet.Value["ema-fast"]);
            Assert.AreSame(target, provider.Packet.Criterion);
            Assert.AreSame(parameters, provider.Packet.OptimizationParameters);
            Assert.AreEqual(2, result.Backtests.Count);
        }

        [Test]
        public void SelectorRequestsReturnBacktestsWithoutPreselectedParameterSet()
        {
            var provider = new TestLocalWalkForwardOptimizationProvider();
            var parameters = new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("ema-fast", 10, 20, 10)
            };

            var result = provider.Optimize(new WalkForwardOptimizationRequest(
                new QCAlgorithm(),
                new DateTime(2026, 1, 1),
                backtests => backtests
                    .OrderByDescending(backtest => backtest.Statistics[PerformanceMetrics.NetProfit].ToDecimal())
                    .First()
                    .ParameterSet,
                parameters));

            Assert.IsNull(result.ParameterSet);
            Assert.AreEqual(2, result.Backtests.Count);
            Assert.AreEqual("20", result.Backtests
                .OrderByDescending(backtest => backtest.Statistics[PerformanceMetrics.NetProfit].ToDecimal())
                .First()
                .ParameterSet
                .Value["ema-fast"]);
            Assert.AreEqual(
                "Statistics." + PerformanceMetrics.NetProfit,
                provider.Packet.Criterion.Target
                    .Replace("['", string.Empty, StringComparison.Ordinal)
                    .Replace("']", string.Empty, StringComparison.Ordinal));
        }

        [Test]
        public void SelectorRequestsReturnBacktestsWhenDefaultTargetDoesNotSelectSolution()
        {
            var provider = new TestLocalWalkForwardOptimizationProvider(includeNetProfit: false);
            var parameters = new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("ema-fast", 10, 20, 10)
            };

            var result = provider.Optimize(new WalkForwardOptimizationRequest(
                new QCAlgorithm(),
                new DateTime(2026, 1, 1),
                backtests => backtests
                    .OrderByDescending(backtest => backtest.Statistics["Profit"].ToDecimal())
                    .First()
                    .ParameterSet,
                parameters));

            Assert.IsNull(result.ParameterSet);
            Assert.AreEqual(2, result.Backtests.Count);
            Assert.AreEqual("20", result.Backtests
                .OrderByDescending(backtest => backtest.Statistics["Profit"].ToDecimal())
                .First()
                .ParameterSet
                .Value["ema-fast"]);
        }

        private sealed class TestLocalWalkForwardOptimizationProvider : LocalWalkForwardOptimizationProvider
        {
            private readonly bool _includeNetProfit;

            public TestLocalWalkForwardOptimizationProvider(bool includeNetProfit = true)
            {
                _includeNetProfit = includeNetProfit;
            }

            public OptimizationNodePacket Packet { get; private set; }

            protected override LeanOptimizer CreateOptimizer(OptimizationNodePacket packet)
            {
                Packet = packet;
                return new ImmediateLeanOptimizer(packet, _includeNetProfit);
            }
        }

        private sealed class ImmediateLeanOptimizer : LeanOptimizer
        {
            private readonly bool _includeNetProfit;

            public ImmediateLeanOptimizer(OptimizationNodePacket nodePacket, bool includeNetProfit)
                : base(nodePacket)
            {
                _includeNetProfit = includeNetProfit;
            }

            protected override string RunLean(ParameterSet parameterSet, string backtestName)
            {
                var backtestId = parameterSet.Id.ToStringInvariant();
                Task.Delay(10).ContinueWith(_ =>
                {
                    var netProfit = parameterSet.Value["ema-fast"].ToDecimal();
                    NewResult(CreateBacktestJson(netProfit, _includeNetProfit), backtestId);
                }, TaskScheduler.Default);
                return backtestId;
            }

            protected override void AbortLean(string backtestId)
            {
            }

            protected override void SendUpdate()
            {
            }

            private static string CreateBacktestJson(decimal netProfit, bool includeNetProfit)
            {
                var statistics = new Dictionary<string, string>
                {
                    { "Profit", netProfit.ToStringInvariant() }
                };
                if (includeNetProfit)
                {
                    statistics[PerformanceMetrics.NetProfit] = netProfit.ToStringInvariant();
                }

                return Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Statistics = statistics
                });
            }
        }
    }
}
