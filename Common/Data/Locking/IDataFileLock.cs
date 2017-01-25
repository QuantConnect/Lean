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

namespace QuantConnect.Data
{
    /// <summary>
    /// Data file lock interface
    /// </summary>
    interface IDataFileLock : IDisposable
    {
        /// <summary>
        /// Returns name of the lock object
        /// </summary>
        string LockName { get; }

        /// <summary>
        /// Tries to acquire data file lock and returns false, if attempt failed
        /// </summary>
        /// <returns>true if acquired, or false otherwise</returns>
        bool TryAcquire();

        /// <summary>
        /// Acquires data file lock and blocks if necessary 
        /// </summary>
        void Acquire();


        /// <summary>
        /// Tries to release data file lock and returns false, if attempt failed
        /// </summary>
        /// <returns>true if acquired, or false otherwise</returns>
        bool TryRelease();

        /// <summary>
        /// Releases data file lock and blocks if necessary 
        /// </summary>
        void Release();
    }
}
