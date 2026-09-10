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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
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
        public void WarnsOnceOnLongTimeStepWithoutFailing()
        {
            var previousLogHandler = Log.LogHandler;
            try
            {
                var logHandler = new QueueLogHandler();
                Log.LogHandler = logHandler;

                var userWarnings = new List<string>();
                var timeManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.FromMinutes(20),
                    timeLoopWarningThreshold: TimeSpan.FromMilliseconds(5));
                timeManager.UserWarningHandler = userWarnings.Add;
                timeManager.StartNewTimeStep();
                // the first call initializes the current time step start time
                Assert.IsTrue(timeManager.IsWithinLimit().IsWithinCustomLimits);
                Thread.Sleep(50);

                // the time step is over the warning threshold: it warns but does not fail
                var result = timeManager.IsWithinLimit();
                Assert.IsTrue(result.IsWithinCustomLimits, result.ErrorMessage);
                Assert.AreEqual(1, WarningCount(logHandler));
                Assert.AreEqual(1, userWarnings.Count(x => x.Contains("time step has been executing")));

                // only warns once per time step
                Thread.Sleep(20);
                Assert.IsTrue(timeManager.IsWithinLimit().IsWithinCustomLimits);
                Assert.AreEqual(1, WarningCount(logHandler));
                Assert.AreEqual(1, userWarnings.Count);

                // a new slow time step warns again
                timeManager.StartNewTimeStep();
                Assert.IsTrue(timeManager.IsWithinLimit().IsWithinCustomLimits);
                Thread.Sleep(50);
                Assert.IsTrue(timeManager.IsWithinLimit().IsWithinCustomLimits);
                Assert.AreEqual(2, WarningCount(logHandler));
                Assert.AreEqual(2, userWarnings.Count);
            }
            finally
            {
                Log.LogHandler = previousLogHandler;
            }
        }

        [Test]
        public void DoesNotWarnOnFastTimeStep()
        {
            var previousLogHandler = Log.LogHandler;
            try
            {
                var logHandler = new QueueLogHandler();
                Log.LogHandler = logHandler;

                // default three minute warning threshold
                var userWarnings = new List<string>();
                var timeManager = new AlgorithmTimeLimitManager(TokenBucket.Null, TimeSpan.FromMinutes(20));
                timeManager.UserWarningHandler = userWarnings.Add;
                timeManager.StartNewTimeStep();
                Assert.IsTrue(timeManager.IsWithinLimit().IsWithinCustomLimits);
                Thread.Sleep(20);
                Assert.IsTrue(timeManager.IsWithinLimit().IsWithinCustomLimits);

                Assert.AreEqual(0, WarningCount(logHandler));
                Assert.AreEqual(0, userWarnings.Count);
            }
            finally
            {
                Log.LogHandler = previousLogHandler;
            }
        }

        private static int WarningCount(QueueLogHandler logHandler)
        {
            return logHandler.Logs.Count(entry => entry.Message.Contains("time step has been executing"));
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
