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
using NUnit.Framework;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;

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

            if (parameters.Algorithm == "BasicTemplateIntrinioEconomicData")
            {
                var parametersConfigString = Config.Get("parameters");
                var algorithmParameters = parametersConfigString != string.Empty
                    ? JsonConvert.DeserializeObject<Dictionary<string, string>>(parametersConfigString)
                    : new Dictionary<string, string>();

                algorithmParameters["intrinio-username"] = "121078c02c20a09aa5d9c541087e7fa4";
                algorithmParameters["intrinio-password"] = "65be35238b14de4cd0afc0edf364efc3";

                Config.Set("parameters", JsonConvert.SerializeObject(algorithmParameters));
            }

            AlgorithmRunner.RunLocalBacktest(parameters.Algorithm, parameters.Statistics, parameters.AlphaStatistics, parameters.Language);
        }

        private static TestCaseData[] GetRegressionTestParameters()
        {
            // find all regression algorithms in Algorithm.CSharp
            var regressionAlgorithms =
                from type in typeof(BasicTemplateAlgorithm).Assembly.GetTypes()
                where typeof(IRegressionAlgorithmDefinition).IsAssignableFrom(type)
                where !type.IsAbstract                          // non-abstract
                where type.GetConstructor(new Type[0]) != null  // has default ctor
                select type;

            return regressionAlgorithms.SelectMany(ra =>
            {
                // create instance to fetch statistics and other regression languages
                var instance = (IRegressionAlgorithmDefinition)Activator.CreateInstance(ra);

                // generate test parameters
                return instance.Languages.Select(lang => new AlgorithmStatisticsTestParameters(ra.Name, instance.ExpectedStatistics, lang));
            })
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

            public AlgorithmStatisticsTestParameters(string algorithm, Dictionary<string, string> statistics, Language language)
            {
                Algorithm = algorithm;
                Statistics = statistics;
                Language = language;
            }
        }
    }
}
