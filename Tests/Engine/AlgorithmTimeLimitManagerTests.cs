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
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class AlgorithmTimeLimitManagerTests
    {
        [OneTimeSetUp]
        public void TearUp()
        {
            // clear the config
            Config.Reset();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // clear the config
            Config.Reset();
        }

        [Test]
        public void StopsAlgorithm()
        {
            Config.Set("algorithm-manager-time-loop-maximum", "0.05");
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(TrainingInitializeRegressionAlgorithm),
                new Dictionary<string, string>(),
                Language.CSharp,
                AlgorithmStatus.RuntimeError);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        [Test]
        public void RaceCondition()
        {
            var timeManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.FromMinutes(1));

            const int loops = 1000000;
            var task = Task.Factory.StartNew(() =>
            {
                var count = 0;
                while (count++ < loops)
                {
                    var result = timeManager.IsWithinLimit();
                    Assert.IsTrue(result.IsWithinCustomLimits, result.ErrorMessage);
                }
            });
            var task2 = Task.Factory.StartNew(() =>
            {
                var count = 0;
                while (count++ < loops)
                {
                    timeManager.StartNewTimeStep();
                }
            });

            Task.WaitAll(task, task2);
        }
    }
}
