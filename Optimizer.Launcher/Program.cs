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
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using QuantConnect.Util;

namespace QuantConnect.Optimizer.Launcher
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                var packet = new OptimizationNodePacket
                {
                    OptimizationId = Guid.NewGuid().ToString(),
                    OptimizationStrategy = Config.Get("optimization-strategy", "QuantConnect.Optimizer.GridSearchOptimizationStrategy"),
                    Criterion =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(Config.Get("optimization-criterion", "{\"name\":\"TotalPerformance.TradeStatistics.TotalProfit\", \"direction\": \"max\"}")),
                    OptimizationParameters = 
                        JsonConvert.DeserializeObject<Dictionary<string, JObject>>(Config.Get("parameters", "{}"))
                        .Select( arg => new OptimizationParameter(arg.Key, arg.Value.Value<decimal>("min"), arg.Value.Value<decimal>("max"), arg.Value.Value<decimal>("step")))
                        .ToHashSet(),
                    MaximumConcurrentBacktests = Config.GetInt("maximum-concurrent-backtests", Environment.ProcessorCount)
                };
                var optimizer = new ConsoleLeanOptimizer(packet);

                optimizer.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }
    }
}
