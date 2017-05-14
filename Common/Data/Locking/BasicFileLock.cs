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

namespace QuantConnect.Data
{
    /// <summary>
    /// Implements basic data file locking mechanism
    /// </summary>
    public class BasicFileLock : IDataFileLock
    {
        // constant prefix used by basic file lock to mark the lock files
        private const string lockPrefix = "locked";
        private static Random _rnd = new Random((int)DateTime.Now.Ticks);
        private string _lockPath;
        private bool _acquired = false;

        /// <summary>
        /// Creates new instant of BasicFileLock 
        /// </summary>
        /// <param name="path">Lock path</param>
        /// <param name="useTempFolder">true, if temporary folder should be used for locks</param>
        /// <param name="acquire">true, if lock should be acquired</param>
        public BasicFileLock(string path, bool useTempFolder = false, bool acquire = true)
        {
            _lockPath = !useTempFolder ? path : Path.Combine(Path.GetTempPath(), Path.GetFileName(path));

            if (acquire)
            {
                Acquire();
            }
        }

        /// <summary>
        /// Acquires data file lock and blocks if necessary 
        /// </summary>
        public void Acquire()
        {
            if (_acquired) return;

            while (!TryAcquire())
            {
                Thread.Sleep((int)(DataFileLocks.MaxRetryPeriod.Milliseconds * _rnd.NextDouble()));
            }

            _acquired = true;
        }

        /// <summary>
        /// Releases data file lock and blocks if necessary  
        /// </summary>
        public void Release()
        {
            if (!_acquired) return;

            while (!TryRelease())
            {
                Thread.Sleep((int)(DataFileLocks.MaxRetryPeriod.Milliseconds * _rnd.NextDouble()));
            }

            _acquired = false;
        }

        /// <summary>
        /// Tries to acquire data file lock and returns false, if attempt failed
        /// </summary>
        /// <returns>true if acquired, or false otherwise</returns>
        public bool TryAcquire()
        {
            try
            {
                if (_acquired)
                    return false;

                var result = AcquireLock(_lockPath);

                if (result)
                    _acquired = true;

                return result;
            }
            catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to release data file lock and returns false, if attempt failed
        /// </summary>
        /// <returns>true if acquired, or false otherwise</returns>
        public bool TryRelease()
        {
            try
            {
                if (!_acquired)
                    return false;

                var result = ReleaseLock(_lockPath);

                if (result)
                    _acquired = false;

                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns name of the lock object
        /// </summary>
        public string LockName
        {
            get
            {
                return _lockPath;
            }
        }

        private bool AcquireLock(string path)
        {
            RemoveExpiredLocks(path);

            var oldLockPath = GetLockName(path);

            if (oldLockPath == null)
            {
                var name = GenerateLockName(path);

                return DataFileLocks.CreateLock(name);
            }
            else
                return false;
        }
        private bool ReleaseLock(string path)
        {
            RemoveExpiredLocks(path);

            var oldLockPath = GetLockName(path);

            if (oldLockPath != null)
            {
                return DataFileLocks.RemoveLock(oldLockPath);
            }
            else
                return false;
        }

        private string GetLockName(string path)
        {
            return DataFileLocks.GetLock(path, "locked");
        }

        private bool IsLockExpired(string path)
        {
            return DataFileLocks.IsLockExpired(path, "locked");
        }
        private string GenerateLockName(string path)
        {
            return DataFileLocks.GenerateLockName(path, "locked");
        }

        private bool RemoveExpiredLocks(string path)
        {
            if (IsLockExpired(path))
            {

                return DataFileLocks.RemoveLock(GetLockName(path));
            }

            return false;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Release();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
