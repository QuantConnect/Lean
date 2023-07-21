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

using System.IO;
using NUnit.Framework;
using System.Diagnostics;
using QuantConnect.Logging;
using QuantConnect.Configuration;

namespace QuantConnect.Tests
{
    internal static class TestProcess
    {
        // Update in config.json to specify the alternate path to python.exe
        private static readonly string _pythonLocation = Config.Get("python-location", "python");

        public static void RunPythonProcess(string args, out Process process, int timeout = 1000 * 45)
        {
            RunProcess(_pythonLocation, args, out process, timeout);
        }

        public static void RunProcess(string targetProcess, string args, out Process process, int timeout = 1000 * 45)
        {
            Log.Trace($"TestProcess.RunProcess(): running '{targetProcess}' args {args}");

            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo(targetProcess, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            process = new Process
            {
                StartInfo = startInfo,
            };

            // real-time stream process output
            process.OutputDataReceived += DebugLog;
            process.ErrorDataReceived += DebugLog;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            if (!process.WaitForExit(timeout))
            {
                process.Kill();
                Assert.Fail($"Timeout waiting for process to exit. Timeout: {timeout}ms");
            }

            process.OutputDataReceived -= DebugLog;
            process.ErrorDataReceived -= DebugLog;
        }

        private static void DebugLog(object sender, DataReceivedEventArgs data)
        {
            if (!string.IsNullOrEmpty(data.Data))
            {
                Log.Debug($"ProcessOutput: {data.Data}");
            }
        }
    }
}
