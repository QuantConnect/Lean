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
using System.IO;
using System.Threading;
using System.Reflection;
using QuantConnect.Util;
using System.Diagnostics;
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Operating systems class for managing anything that is operation system specific.
    /// </summary>
    /// <remarks>Good design should remove the need for this function. Over time it should disappear.</remarks>
    public static class OS
    {
        /// <summary>
        /// CPU performance counter measures percentage of CPU used in a background thread.
        /// </summary>
        private static CpuPerformance CpuPerformanceCounter;

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
                if(CpuPerformanceCounter != null)
                {
                    return (decimal)CpuPerformanceCounter.CpuPercentage;
                }
                return 0m;
            }
        }

        /// <summary>
        /// Gets the statistics of the machine, including CPU% and RAM
        /// </summary>
        public static Dictionary<string, string> GetServerStatistics()
        {
            return new Dictionary<string, string>
            {
                { Messages.OS.CPUUsageKey, Invariant($"{CpuUsage:0.0}%")},
                { Messages.OS.UsedRAMKey, TotalPhysicalMemoryUsed.ToStringInvariant() },
                { Messages.OS.TotalRAMKey, "" },
                { Messages.OS.HostnameKey, Environment.MachineName },
                { Messages.OS.LEANVersionKey, $"v{Globals.Version}"}
            };
        }

        /// <summary>
        /// Initializes the OS internal resources
        /// </summary>
        public static void Initialize()
        {
            CpuPerformanceCounter = new CpuPerformance();
        }

        /// <summary>
        /// Disposes of the OS internal resources
        /// </summary>
        public static void Dispose()
        {
            CpuPerformanceCounter.DisposeSafely();
        }

        /// <summary>
        /// Calculates the CPU usage in a background thread
        /// </summary>
        private class CpuPerformance : IDisposable
        {
            private readonly CancellationTokenSource _cancellationToken;
            private readonly Thread _cpuThread;

            /// <summary>
            /// CPU usage as a percentage (0-100)
            /// </summary>
            /// <remarks>Float to avoid any atomicity issues</remarks>
            public float CpuPercentage { get; private set; }

            /// <summary>
            /// Initializes an instance of the class and starts a new thread.
            /// </summary>
            public CpuPerformance()
            {
                _cancellationToken = new CancellationTokenSource();
                _cpuThread = new Thread(CalculateCpu) { IsBackground = true, Name = "CpuPerformance" };
                _cpuThread.Start();
            }

            /// <summary>
            /// Event loop that calculates the CPU percentage the process is using
            /// </summary>
            private void CalculateCpu()
            {
                var process = Process.GetCurrentProcess();
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var startTime = DateTime.UtcNow;
                    var startCpuUsage = process.TotalProcessorTime;

                    if (_cancellationToken.Token.WaitHandle.WaitOne(startTime.GetSecondUnevenWait(5000)))
                    {
                        return;
                    }

                    var endTime = DateTime.UtcNow;
                    var endCpuUsage = process.TotalProcessorTime;

                    var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                    var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                    var cpuUsageTotal = cpuUsedMs / totalMsPassed;

                    CpuPercentage = (float)cpuUsageTotal * 100;
                }
            }

            /// <summary>
            /// Stops the execution of the task
            /// </summary>
            public void Dispose()
            {
                _cpuThread.StopSafely(TimeSpan.FromSeconds(5), _cancellationToken);
                _cancellationToken.DisposeSafely();
            }
        }
    }
}
