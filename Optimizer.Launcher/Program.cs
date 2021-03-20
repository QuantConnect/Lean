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

using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace QuantConnect.Optimizer.Launcher
{
    public class Program
    {
        public static void Main()
        {
            var endedEvent = new ManualResetEvent(false);
            
            try
            {
                Log.DebuggingEnabled = Config.GetBool("debug-mode");
                Log.FilePath = Path.Combine(Config.Get("results-destination-folder"), "log.txt");
                Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

                var optimizationStrategyName = Config.Get("optimization-strategy",
                    "QuantConnect.Optimizer.GridSearchOptimizationStrategy");
                var packet = new OptimizationNodePacket
                {
                    OptimizationId = Guid.NewGuid().ToString(),
                    OptimizationStrategy = optimizationStrategyName,
                    OptimizationStrategySettings = (OptimizationStrategySettings)JsonConvert.DeserializeObject(Config.Get(
                        "optimization-strategy-settings", 
                        "{\"$type\":\"QuantConnect.Optimizer.Strategies.OptimizationStrategySettings, QuantConnect.Optimizer\"}"), new JsonSerializerSettings(){TypeNameHandling = TypeNameHandling.All}),
                    Criterion = JsonConvert.DeserializeObject<Target>(Config.Get("optimization-criterion", "{\"target\":\"Statistics.TotalProfit\", \"extremum\": \"max\"}")),
                    Constraints = JsonConvert.DeserializeObject<List<Constraint>>(Config.Get("constraints", "[]")).AsReadOnly(),
                    OptimizationParameters = JsonConvert.DeserializeObject<HashSet<OptimizationParameter>>(Config.Get("parameters", "[]")),
                    MaximumConcurrentBacktests = Config.GetInt("maximum-concurrent-backtests", Environment.ProcessorCount / 2)
                };

                var optimizer = new ConsoleLeanOptimizer(packet);

                optimizer.Start();

                optimizer.Ended += (s, e) =>
                {
                    optimizer.DisposeSafely();
                    endedEvent.Set();
                };
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            // Wait until the optimizer has stopped running before exiting
            endedEvent.WaitOne();
        }
    }
}
