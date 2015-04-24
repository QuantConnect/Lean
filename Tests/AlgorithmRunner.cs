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

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Provides methods for running an algorithm and testing it's performance metrics
    /// </summary>
    public static class AlgorithmRunner
    {
        public static void RunLocalBacktest(string algorithm, Dictionary<string, string> expectedStatistics)
        {
            // set the configuration up
            Config.Set("algorithm-type-name", algorithm);
            Config.Set("local", "true");
            Config.Set("live-mode", "false");
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");

            // run the algorithm in its own thread
            Task.Factory.StartNew(() => Lean.Engine.Engine.Main(null)).Wait();

            var consoleResultHandler = (ConsoleResultHandler)Lean.Engine.Engine.ResultHandler;
            var statistics = consoleResultHandler.FinalStatistics;

            foreach (var stat in expectedStatistics)
            {
                Assert.AreEqual(stat.Value, statistics[stat.Key]);
            }
        }
    }
}
