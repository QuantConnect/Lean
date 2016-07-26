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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Handles launching and killing the IB Controller script
    /// </summary>
    /// <remarks>
    /// Requires TWS or IB Gateway and IBController installed to run
    /// </remarks>
    public static class InteractiveBrokersGatewayRunner
    {
        // process that's running the IB Controller script
        private static int ScriptProcessID;

        /// <summary>
        /// Starts the interactive brokers gateway using values from configuration
        /// </summary>
        public static void StartFromConfiguration()
        {
            Start(Config.Get("ib-controller-dir"),
                Config.Get("ib-tws-dir"),
                Config.Get("ib-user-name"),
                Config.Get("ib-password"),
                Config.GetBool("ib-use-tws")
                );
        }

        /// <summary>
        /// Starts the IB Gateway
        /// </summary>
        /// <param name="ibControllerDirectory">Directory to the IB controller installation</param>
        /// <param name="twsDirectory"></param>
        /// <param name="userID">The log in user id</param>
        /// <param name="password">The log in password</param>
        /// <param name="useTws">True to use Trader Work Station, false to just launch the API gateway</param>
        public static void Start(string ibControllerDirectory, string twsDirectory, string userID, string password, bool useTws = false)
        {
            var useTwsSwitch = useTws ? "TWS" : "GATEWAY";
            var batchFilename = Path.Combine("InteractiveBrokers", "run-ib-controller.bat");
            var bashFilename = Path.Combine("InteractiveBrokers", "run-ib-controller.sh");

            try
            {
                var file = OS.IsWindows ? batchFilename : bashFilename;
                var arguments = string.Format("{0} {1} {2} {3} {4} {5}", file, ibControllerDirectory, twsDirectory, userID, password, useTwsSwitch);

                Log.Trace("InteractiveBrokersGatewayRunner.Start(): Launching IBController for account " + userID + "...");

                var processStartInfo = OS.IsWindows ? new ProcessStartInfo("cmd.exe", "/C " + arguments) : new ProcessStartInfo("bash", arguments);

                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = false;
                var process = Process.Start(processStartInfo);
                ScriptProcessID = process.Id;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Stops the IB Gateway
        /// </summary>
        public static void Stop()
        {
            if (ScriptProcessID == 0)
            {
                return;
            }

            try
            {
                Log.Trace("InteractiveBrokersGatewayRunner.Stop(): Stopping IBController...");

                // we need to materialize this ienumerable since if we start killing some of them
                // we may leave some daemon processes hanging
                foreach (var process in GetSpawnedProcesses(ScriptProcessID).ToList())
                {
                    // kill all spawned processes
                    process.Kill();
                }

                ScriptProcessID = 0;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        private static IEnumerable<Process> GetSpawnedProcesses(int id)
        {
            // loop over all the processes and return those that were spawned by the specified processed ID
            return Process.GetProcesses().Where(x =>
            {
                try
                {
                    var parent = ProcessExtensions.Parent(x);
                    if (parent != null)
                    {
                        return parent.Id == id;
                    }
                }
                catch
                {
                    return false;
                }
                return false;
            });
        }

        //http://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
        private static class ProcessExtensions
        {
            private static string FindIndexedProcessName(int pid)
            {
                var processName = Process.GetProcessById(pid).ProcessName;
                var processesByName = Process.GetProcessesByName(processName);
                string processIndexdName = null;

                for (var index = 0; index < processesByName.Length; index++)
                {
                    processIndexdName = index == 0 ? processName : processName + "#" + index;
                    var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                    if ((int)processId.NextValue() == pid)
                    {
                        return processIndexdName;
                    }
                }

                return processIndexdName;
            }

            private static Process FindPidFromIndexedProcessName(string indexedProcessName)
            {
                var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
                return Process.GetProcessById((int)parentId.NextValue());
            }

            public static Process Parent(Process process)
            {
                return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
            }
        }
    }
}