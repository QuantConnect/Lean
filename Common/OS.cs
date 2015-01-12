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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.IO;
using System.Diagnostics;

namespace QuantConnect 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Operating systems class for managing anything that is operation system specific.
    /// </summary>
    /// <remarks>Good design should remove the need for this function. Over time it should disappear.</remarks>
    public static class OS 
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private static PerformanceCounter _ramTotalCounter;
        private static PerformanceCounter _ramAvailableBytes;

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

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
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
                var drives = DriveInfo.GetDrives();
                //User 1 GB Maximum As Cache
                if (drives.Length <= 0) return 1024;
                var d = drives[0];
                return d.AvailableFreeSpace / (1024 * 1024);
            }
        }


        /// <summary>
        /// Get the RAM remaining on the machine:
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
        /// Get the RAM remaining on the machine:
        /// </summary>
        public static long TotalPhysicalMemoryUsed
        {
            get 
            {
                return GC.GetTotalMemory(false) / (1024*1024);
            }
        }

    } // End OS Class
} // End QC Namespace
