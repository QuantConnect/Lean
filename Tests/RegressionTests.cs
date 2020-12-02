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
using System.IO;
using NUnit.Framework;
using System.Linq;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Tests
{
    [TestFixture, Category("TravisExclude")]
    public class RegressionTests
    {
        [Test, TestCaseSource(nameof(GetRegressionTestParameters))]
        public void AlgorithmStatisticsRegression(AlgorithmStatisticsTestParameters parameters)
        {
            // ensure we start with a fresh config every time when running multiple tests
            Config.Reset();

            Config.Set("quandl-auth-token", "WyAazVXnq7ATy_fefTqm");
            Config.Set("forward-console-messages", "false");

            if (parameters.Algorithm == "OptionChainConsistencyRegressionAlgorithm")
            {
                // special arrangement for consistency test - we check if limits work fine
                Config.Set("symbol-minute-limit", "100");
                Config.Set("symbol-second-limit", "100");
                Config.Set("symbol-tick-limit", "100");
            }

            if (parameters.Algorithm == "TrainingInitializeRegressionAlgorithm" ||
                parameters.Algorithm == "TrainingOnDataRegressionAlgorithm")
            {
                // limit time loop to 90 seconds and set leaky bucket capacity to one minute w/ zero refill
                Config.Set("algorithm-manager-time-loop-maximum", "1.5");
                Config.Set("scheduled-event-leaky-bucket-capacity", "1");
                Config.Set("scheduled-event-leaky-bucket-refill-amount", "0");
            }

            var algorithmManager = AlgorithmRunner.RunLocalBacktest(
                parameters.Algorithm,
                parameters.Statistics,
                parameters.AlphaStatistics,
                parameters.Language,
                parameters.ExpectedFinalStatus
            ).AlgorithmManager;

            if (parameters.Algorithm == "TrainingOnDataRegressionAlgorithm")
            {
                // this training algorithm should have consumed the only minute available in the bucket
                Assert.AreEqual(0, algorithmManager.TimeLimit.AdditionalTimeBucket.AvailableTokens);
            }
        }

        private static TestCaseData[] GetRegressionTestParameters()
        {
            // since these are static test cases, they are executed before test setup
            AssemblyInitialize.AdjustCurrentDirectory();

            var nonDefaultStatuses = new Dictionary<string, AlgorithmStatus>
            {
                {"TrainingInitializeRegressionAlgorithm", AlgorithmStatus.RuntimeError},
                {"OnOrderEventExceptionRegression", AlgorithmStatus.RuntimeError},
                {"WarmUpAfterIntializeRegression", AlgorithmStatus.RuntimeError }
            };

            // find all regression algorithms in Algorithm.CSharp
            return (
                from type in typeof(BasicTemplateAlgorithm).Assembly.GetTypes()
                where typeof(IRegressionAlgorithmDefinition).IsAssignableFrom(type)
                where !type.IsAbstract                          // non-abstract
                where type.GetConstructor(new Type[0]) != null  // has default ctor
                let instance = (IRegressionAlgorithmDefinition) Activator.CreateInstance(type)
                let status = nonDefaultStatuses.GetValueOrDefault(type.Name, AlgorithmStatus.Completed)
                where instance.CanRunLocally                   // open source has data to run this algorithm
                from language in instance.Languages
                select new AlgorithmStatisticsTestParameters(type.Name, instance.ExpectedStatistics, language, status)
            )
            .OrderBy(x => x.Language).ThenBy(x => x.Algorithm)
            // generate test cases from test parameters
            .Select(x => new TestCaseData(x).SetName(x.Language + "/" + x.Algorithm))
            .ToArray();
        }

        public class AlgorithmStatisticsTestParameters
        {
            public readonly string Algorithm;
            public readonly Dictionary<string, string> Statistics;
            public readonly AlphaRuntimeStatistics AlphaStatistics;
            public readonly Language Language;
            public readonly AlgorithmStatus ExpectedFinalStatus;

            public AlgorithmStatisticsTestParameters(
                string algorithm,
                Dictionary<string, string> statistics,
                Language language,
                AlgorithmStatus expectedFinalStatus
                )
            {
                Algorithm = algorithm;
                Statistics = statistics;
                Language = language;
                ExpectedFinalStatus = expectedFinalStatus;
            }
        }
    }
}
