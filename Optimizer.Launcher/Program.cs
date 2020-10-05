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
using QuantConnect.Optimizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QuantConnect.Optimizer.Launcher
{
    public class Program
    {
        private static string _workingDirectory = "../../../Launcher/bin/Debug/";
        public static void Main(string[] args)
        {
            try
            {
                string myPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string myDir = System.IO.Path.GetDirectoryName(myPath);

                string path = System.IO.Path.Combine(myDir, _workingDirectory, "QuantConnect.Lean.Launcher.exe");

                var packet = new OptimizationNodePacket()
                {
                    OptimizationStrategy = Config.Get("optimization-strategy", "GridSearch"),
                    OptimizationManager = Config.Get("optimization-manager", "BruteForceOptimizer"),
                    Criterion =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(Config.Get("optimization-criterion", "{\"name\":\"TotalPerformance.TradeStatistics.TotalProfit\", \"direction\": \"max\"}")),
                    OptimizationParameters = 
                        JsonConvert.DeserializeObject<Dictionary<string, OptimizationParameter>>(Config.Get("parameters", "{}"))
                };
                var chaser = new LeanOptimizer(packet);
                chaser.Abort();
                Console.ReadKey();

                // Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = _workingDirectory,
                    Arguments = $"--results-destination-folder \"{myDir}\" --parameters \"ema-fast\":1"
                };

                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }
    }
}
