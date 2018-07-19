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

namespace QuantConnect
{
    /// <summary>
    /// Operating systems class for managing anything that is operation system specific.
    /// </summary>
    /// <remarks>Good design should remove the need for this function. Over time it should disappear.</remarks>
    public static class OS
    {
        private static PerformanceCounter _ramTotalCounter;
        private static PerformanceCounter _ramAvailableBytes;
        private static PerformanceCounter _cpuUsageCounter;

        /// <summary>
        /// Total Physical Ram on the Machine:
        /// </summary>
        private static PerformanceCounter RamTotalCounter
        {
            get
            {
                if (_ramTotalCounter == null)
                {
                    if (IsLinux)
                    {
                        _ramTotalCounter = new PerformanceCounter ("Mono Memory", "Total Physical Memory");
                    }
                    else
                    {
                        _ramTotalCounter = new PerformanceCounter("Memory", "Available Bytes");
                    }
                }
                return _ramTotalCounter;
            }
        }

        /// <summary>
        /// Memory free on the machine available for use:
        /// </summary>
        public static PerformanceCounter RamAvailableBytes
        {
            get
            {
                if (_ramAvailableBytes == null)
                {
                    if (IsLinux)
                    {
                        _ramAvailableBytes = new PerformanceCounter("Mono Memory", "Allocated Objects");
                    }
                    else
                    {
                        _ramAvailableBytes = new PerformanceCounter("Memory", "Available Bytes");
                    }
                }
                return _ramAvailableBytes;
            }
        }

        /// <summary>
        /// Total CPU usage as a percentage
        /// </summary>
        public static PerformanceCounter CpuUsage
        {
            get
            {
                if (_cpuUsageCounter == null)
                {
                    _cpuUsageCounter = new PerformanceCounter("Process", "% Processor Time",
                        IsWindows ? Process.GetCurrentProcess().ProcessName : Process.GetCurrentProcess().Id.ToString());
                }
                return _cpuUsageCounter;
            }
        }

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
        public static bool IsWindows
        {
            get
            {
                return !IsLinux;
            }
        }


        /// <summary>
        /// Character Separating directories in this OS:
        /// </summary>
        public static string PathSeparation
        {
            get
            {
                return Path.DirectorySeparatorChar.ToString();
            }
        }

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
        /// Gets the total RAM used by application in MB
        /// </summary>
        public static long ApplicationMemoryUsed
        {
            get
            {
                var proc = Process.GetCurrentProcess();
                return (proc.PrivateMemorySize64 / (1024*1024));
            }
        }

        /// <summary>
        /// Get the RAM remaining on the machine:
        /// </summary>
        public static long TotalPhysicalMemory {
            get {
                return (long)(RamTotalCounter.NextValue() / (1024*1024));
            }
        }

        /// <summary>
        /// Get the RAM used on the machine:
        /// </summary>
        public static long TotalPhysicalMemoryUsed
        {
            get
            {
                return GC.GetTotalMemory(false) / (1024*1024);
            }
        }

        /// <summary>
        /// Gets the RAM remaining on the machine
        /// </summary>
        private static long FreePhysicalMemory
        {
            get { return TotalPhysicalMemory - TotalPhysicalMemoryUsed; }
        }

        /// <summary>
        /// Gets the statistics of the machine, including CPU% and RAM
        /// </summary>
        public static Dictionary<string, string> GetServerStatistics()
        {
            return new Dictionary<string, string>
            {
                {"CPU Usage",            CpuUsage.NextValue().ToString("0.0") + "%"},
                {"Used RAM (MB)",        TotalPhysicalMemoryUsed.ToString()},
                {"Total RAM (MB)",        TotalPhysicalMemory.ToString()},
                {"Used Disk Space (MB)", DriveSpaceUsed.ToString() },
                {"Total Disk Space (MB)", DriveTotalSpace.ToString() },
                { "Hostname", Environment.MachineName },
                {"LEAN Version", "v" + Globals.Version}
            };
        }
    } // End OS Class
} // End QC Namespace
