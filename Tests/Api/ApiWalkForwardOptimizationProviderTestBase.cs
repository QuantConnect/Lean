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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Statistics;
using QuantConnect.Util;
using ApiOptimization = QuantConnect.Api.Optimization;
using ApiOptimizationBacktest = QuantConnect.Api.OptimizationBacktest;

namespace QuantConnect.Tests.API
{
    public abstract class ApiWalkForwardOptimizationProviderTestBase
    {
        protected const string CompileId = "compile-1";

        [SetUp]
        public void SetUp()
        {
            Config.Set("walk-forward-optimization-compile-id", CompileId);
            Config.Set("walk-forward-optimization-estimated-cost", "0.25");
            Config.Set("walk-forward-optimization-node-type", QuantConnect.Api.OptimizationNodes.O2_8);
            Config.Set("walk-forward-optimization-parallel-nodes", "2");
            Config.Set("walk-forward-optimization-api-poll-interval-seconds", "0");
            Config.Set("walk-forward-optimization-api-timeout-minutes", "1");
            Config.Set("walk-forward-optimization-strategy", typeof(GridSearchOptimizationStrategy).FullName);
        }

        [TearDown]
        public void TearDown()
        {
            SetUp();
        }

        protected static WalkForwardOptimizationRequest CreateTargetRequest(Target target = null)
        {
            return new WalkForwardOptimizationRequest(
                CreateAlgorithm(),
                new DateTime(2026, 1, 1),
                target ?? new Target(PerformanceMetrics.NetProfit, new Maximization(), null),
                CreateParameters());
        }

        protected static WalkForwardOptimizationRequest CreateSelectorRequest()
        {
            return new WalkForwardOptimizationRequest(
                CreateAlgorithm(),
                new DateTime(2026, 1, 1),
                backtests => backtests
                    .OrderByDescending(backtest => backtest.Statistics[PerformanceMetrics.NetProfit].ToDecimal())
                    .First()
                    .ParameterSet,
                CreateParameters());
        }

        protected static HashSet<OptimizationParameter> CreateParameters()
        {
            return new HashSet<OptimizationParameter>
            {
                new OptimizationStepParameter("ema-fast", 10, 20, 10)
            };
        }

        protected static ApiOptimization CreateCompletedOptimization(
            string statistic = PerformanceMetrics.NetProfit,
            decimal firstValue = 10,
            decimal secondValue = 20)
        {
            return new ApiOptimization
            {
                Status = OptimizationStatus.Completed,
                Backtests = new Dictionary<string, ApiOptimizationBacktest>
                {
                    { "backtest-1", CreateBacktest(1, "10", statistic, firstValue) },
                    { "backtest-2", CreateBacktest(2, "20", statistic, secondValue) }
                }
            };
        }

        private static QCAlgorithm CreateAlgorithm()
        {
            return new QCAlgorithm { ProjectId = 123 };
        }

        private static ApiOptimizationBacktest CreateBacktest(
            int id,
            string parameterValue,
            string statistic,
            decimal statisticValue)
        {
            return new ApiOptimizationBacktest(
                new ParameterSet(id, new Dictionary<string, string> { { "ema-fast", parameterValue } }),
                $"backtest-{id}",
                $"candidate-{id}")
            {
                Statistics = new Dictionary<string, string>
                {
                    { statistic, statisticValue.ToStringInvariant() }
                }
            };
        }
    }
}
