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
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Provides methods for running an algorithm and testing it's performance metrics
    /// </summary>
    public static class AlgorithmRunner
    {
        public static void RunLocalBacktest(string algorithm, Dictionary<string, string> expectedStatistics)
        {
            var statistics = new Dictionary<string, string>();

            Composer.Instance.Reset();

            try
            {
                using (Log.LogHandler = new CompositeLogHandler(new ILogHandler[]
                {
                    new ConsoleLogHandler(),
                    new FileLogHandler("regression.log")
                }))
                using (var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance))
                using (var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance))
                {


                    Console.WriteLine("Running " + algorithm + "...");

                    // set the configuration up
                    Config.Set("algorithm-type-name", algorithm);
                    Config.Set("live-mode", "false");
                    Config.Set("environment", "");
                    Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
                    Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
                    Config.Set("api-handler", "QuantConnect.Api.Api");

                    // run the algorithm in its own thread

                    var engine = new Lean.Engine.Engine(systemHandlers, algorithmHandlers, false);
                    Task.Factory.StartNew(() =>
                    {
                        string algorithmPath;
                        var job = systemHandlers.JobQueue.NextJob(out algorithmPath);
                        engine.Run(job, algorithmPath);
                    }).Wait();

                    var consoleResultHandler = (ConsoleResultHandler)algorithmHandlers.Results;
                    statistics = consoleResultHandler.FinalStatistics;
                }
            }
            catch (Exception ex)
            {
                Log.LogHandler.Error("{0} {1}", ex.Message, ex.StackTrace);
            }

            foreach (var stat in expectedStatistics)
            {
                Assert.AreEqual(true, statistics.ContainsKey(stat.Key), "Missing key: " + stat.Key);
                Assert.AreEqual(stat.Value, statistics[stat.Key], "Failed on " + stat.Key);
            }
        }
    }
}
