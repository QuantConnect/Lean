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
using System.Threading;

namespace QuantConnect.Data
{
    /// <summary>
    /// Module contains common methods and data constants used to implement data file locks
    /// </summary>
    public static class DataFileLocks
    {
        // Maximum locking period. After that period any lock is automatically release. 
        // This is done to minimize impact of erroneous code (if there is any) on other actors
        public readonly static TimeSpan MaxLockingPeriod = TimeSpan.FromMinutes(5);

        // We implement Ethernet style contention algorithm: if lock is not available, actor tries to 
        // acquire the lock again in random period of time between 0 and MaxRetryPeriod
        public readonly static TimeSpan MaxRetryPeriod = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Returns true if lock is expired
        /// </summary>
        /// <param name="path">Lock path</param>
        /// <param name="prefix">Lock prefix</param>
        /// <returns></returns>
        public static bool IsLockExpired(string path, string prefix)
        {
            var lockPath = GetLock(path, prefix);
            if (lockPath == null)
                return false;

            // when file is missing File.GetLastAccessTime() returns 12:00 midnight, January 1, 1601 A.D. (C.E.)
            var lastAccessTime = File.GetLastAccessTime(lockPath);
            var expired = lastAccessTime.Year > 1900 && lastAccessTime + MaxLockingPeriod < DateTime.Now;

            return expired;
        }

        /// <summary>
        /// Returns lock string if it exists
        /// </summary>
        /// <param name="path">Lock path</param>
        /// <param name="prefix">Lock prefix</param>
        /// <returns></returns>
        public static string GetLock(string path, string prefix)
        {
            var file = GenerateLockName(path, prefix);

            var exists = File.Exists(file);

            return exists ? file : null;
        }

        /// <summary>
        /// Creates an exclusive lock 
        /// </summary>
        /// <param name="lockPath">Lock path</param>
        /// <returns>True, if successful</returns>
        public static bool CreateLock(string lockPath)
        {
            try
            {
                using (var stream = new FileStream(lockPath, FileMode.CreateNew,
                        FileAccess.ReadWrite, FileShare.None, 4096))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(Process.GetCurrentProcess());
                        writer.WriteLine(Thread.CurrentThread.ManagedThreadId);
                        writer.Flush();
                        stream.Flush();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Removes exclusive lock
        /// </summary>
        /// <param name="lockPath">Lock path</param>
        /// <returns>True, if successful</returns>
        public static bool RemoveLock(string lockPath)
        {
            try
            {
                var renamedFile = lockPath.Replace(Path.GetExtension(lockPath),".deleting");

                File.Delete(renamedFile);

                File.Move(lockPath, renamedFile);

                File.Delete(renamedFile);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Generates lock name form its prefix and a number (if any)
        /// </summary>
        /// <param name="path">Lock path</param>
        /// <param name="prefix">Lock prefix</param>
        /// <param name="number">Lock number</param>
        /// <returns></returns>
        public static string GenerateLockName(string path, string prefix, int number = -1)
        {
            var numberString = number != -1 ? "_" + number.ToString() : string.Empty;

            return path + "." + prefix + numberString;
        }

        /// <summary>
        /// Parses lock name and extracts its number or -1 if there is none
        /// </summary>
        /// <param name="lockName">Lock name</param>
        /// <returns>Lock number or -1 if there is none</returns>
        private static int ParseLockName(string lockName)
        {
            int result = -1;
            var extention = Path.GetExtension(lockName);
            var parts = extention.Split(new char[] { '_' });

            if (parts.Length != 2)
            {
                return result;
            }

            int.TryParse(parts[1], out result);

            return result;
        }
    }
}
