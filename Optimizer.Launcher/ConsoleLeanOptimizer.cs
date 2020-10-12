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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.Optimizer.Launcher
{
    public class ConsoleLeanOptimizer: LeanOptimizer
    {
        private static string _workingDirectory = "../../../Launcher/bin/Debug/";

        public ConsoleLeanOptimizer(OptimizationNodePacket nodePacket) : base(nodePacket)
        {
        }

        public override void OnComplete()
        {
            var result = Strategy.Solution;
            var args = string.Join(",", result.ParameterSet.Keys.Select(a => $"{a}={result.ParameterSet[a]}"));
            Console.Write($"Result {result.Profit} was reached at point ({args})");
        }

        public override void Abort()
        {
            base.Abort();
        }

        protected override Task<string> RunLean(ParameterSet parameterSet)
        {
            string myPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string myDir = System.IO.Path.GetDirectoryName(myPath);

            string path = System.IO.Path.Combine(myDir, _workingDirectory, "QuantConnect.Lean.Launcher.exe");
            string parametersString = string.Join(",", parameterSet.Keys.Select(arg => $"{arg}:{parameterSet[arg]}"));

            string guid = Guid.NewGuid().ToString();
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = _workingDirectory,
                Arguments = $"--results-destination-folder \"{myDir}\" --algorithm-id \"{guid}\" --parameters {parametersString}"
            };

            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                NewResult(File.ReadAllText($"{guid}.json"), guid);
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return Task.FromResult(guid);
        }
    }
}
