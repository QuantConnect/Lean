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
 *
*/

using System;
using System.Threading;
using System.Collections.Generic;

namespace QuantConnect.Util
{
    /// <summary>
    /// Helper class to synchronize execution based on a string key
    /// </summary>
    public class KeyStringSynchronizer
    {
        private readonly Dictionary<string, string> _currentStrings = new ();

        /// <summary>
        /// Execute the given action synchronously with any other thread using the same key
        /// </summary>
        /// <param name="key">The synchronization key</param>
        /// <param name="singleExecution">True if execution should happen only once at the same time for multiple threads</param>
        /// <param name="action">The action to execute</param>
        public void Execute(string key, bool singleExecution, Action action)
        {
            ExecuteImplementation(key, singleExecution, action);
        }

        /// <summary>
        /// Execute the given function synchronously with any other thread using the same key
        /// </summary>
        /// <param name="key">The synchronization key</param>
        /// <param name="action">The function to execute</param>
        public T Execute<T>(string key, Func<T> action)
        {
            T result = default;
            ExecuteImplementation(key, singleExecution: false, () =>
            {
                result = action();
            });
            return result;
        }

        private void ExecuteImplementation(string key, bool singleExecution, Action action)
        {
            lock (key)
            {
                while (true)
                {
                    bool lockTaken = false;
                    string existingKey;
                    lock (_currentStrings)
                    {
                        if(!_currentStrings.TryGetValue(key, out existingKey))
                        {
                            _currentStrings[key] = existingKey = key;
                        }
                    }

                    try
                    {
                        lockTaken = Monitor.TryEnter(existingKey);
                        // this way we can handle reentry with no issues
                        if (lockTaken)
                        {
                            try
                            {
                                // happy case
                                action();
                                return;
                            }
                            finally
                            {
                                lock (_currentStrings)
                                {
                                    // even if we fail we need to release it
                                    _currentStrings.Remove(key);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(existingKey);
                        }
                    }

                    lock (existingKey)
                    {
                        // if we are here the thread that had the lock finished
                        if (!singleExecution)
                        {
                            // time to try again!
                            continue;
                        }
                        return;
                    }
                }
            }
        }
    }
}
