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
using System.Reflection;
using Log = QuantConnect.Logging.Log;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Operating systems class for managing anything that is operation system specific.
    /// </summary>
    /// <remarks>Good design should remove the need for this function. Over time it should disappear.</remarks>
    public static class OS
    {
        private static PerformanceCounter _cpuUsageCounter;

        /// <summary>
        /// Global Flag :: Operating System
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        /// <summary>
        /// Global Flag :: Operating System
        /// </summary>
        public static bool IsWindows => !IsLinux;

        /// <summary>
        /// Character Separating directories in this OS:
        /// </summary>
        public static string PathSeparation => Path.DirectorySeparatorChar.ToStringInvariant();

        /// <summary>
        /// Get the drive space remaining on windows and linux in MB
        /// </summary>
        public static long DriveSpaceRemaining
        {
            get
            {
                var d = GetDrive();
                return d.AvailableFreeSpace / (1024 * 1024);
            }
        }

        /// <summary>
        /// Get the drive space remaining on windows and linux in MB
        /// </summary>
        public static long DriveSpaceUsed
        {
            get
            {
                var d = GetDrive();
                return (d.TotalSize - d.AvailableFreeSpace) / (1024 * 1024);
            }
        }

        /// <summary>
        /// Total space on the drive
        /// </summary>
        public static long DriveTotalSpace
        {
            get
            {
                var d = GetDrive();
                return d.TotalSize / (1024 * 1024);
            }
        }

        /// <summary>
        /// Get the drive.
        /// </summary>
        /// <returns></returns>
        private static DriveInfo GetDrive()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var drive = Path.GetPathRoot(assembly.Location);
            return new DriveInfo(drive);
        }

        /// <summary>
        /// Gets the amount of private memory allocated for the current process (includes both managed and unmanaged memory).
        /// </summary>
        public static long ApplicationMemoryUsed
        {
            get
            {
                var proc = Process.GetCurrentProcess();
                return proc.PrivateMemorySize64 / (1024 * 1024);
            }
        }

        /// <summary>
        /// Get the RAM used on the machine:
        /// </summary>
        public static long TotalPhysicalMemoryUsed => GC.GetTotalMemory(false) / (1024 * 1024);

        /// <summary>
        /// Total CPU usage as a percentage
        /// </summary>
        public static decimal CpuUsage
        {
            get
            {
                if (_cpuUsageCounter == null)
                {
                    try
                    {
                        _cpuUsageCounter = new PerformanceCounter(
                            "Process",
                            "% Processor Time",
                            IsWindows ? Process.GetCurrentProcess().ProcessName : Process.GetCurrentProcess().Id.ToStringInvariant());
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                        return 0;
                    }
                }

                return (decimal) _cpuUsageCounter.NextValue();
            }
        }

        /// <summary>
        /// Gets the statistics of the machine, including CPU% and RAM
        /// </summary>
        public static Dictionary<string, string> GetServerStatistics()
        {
            return new Dictionary<string, string>
            {
                { "CPU Usage", Invariant($"{CpuUsage:0.0}%")},
                { "Used RAM (MB)", TotalPhysicalMemoryUsed.ToStringInvariant() },
                { "Total RAM (MB)", "" },
                { "Used Disk Space (MB)", DriveSpaceUsed.ToStringInvariant() },
                { "Total Disk Space (MB)", DriveTotalSpace.ToStringInvariant() },
                { "Hostname", Environment.MachineName },
                { "LEAN Version", $"v{Globals.Version}"}
            };
        }
    }
}
