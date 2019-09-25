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
using System.Collections.Concurrent;
using System.IO;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default file provider functionality that does not attempt to retrieve any data
    /// </summary>
    public class DefaultDataProvider : IDataProvider, IDisposable
    {
        /// <summary>
        /// We keep a cache of the missing directories so we don't check disk every time
        /// </summary>
        /// <remarks>Using the directory string hash code as key since we don't want to recuperate the string,
        /// and this will reduce memory usage.</remarks>
        private static readonly ConcurrentDictionary<int, bool> MissingDirectoriesCache
            = new ConcurrentDictionary<int, bool>();

        /// <summary>
        /// Retrieves data from disc to be used in an algorithm
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string key)
        {
            try
            {
                return new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception exception)
            {
                if (exception is DirectoryNotFoundException
                    || exception is FileNotFoundException)
                {
                    try
                    {
                        // lets check if the parent directory is not present, if this is the case lets
                        // log the directory was not found, versus the file, to avoid spamming the log
                        var directoryName = new FileInfo(key).Directory.FullName;
                        var cacheKey = directoryName.GetHashCode();
                        bool directoryIsMissing;
                        if (!MissingDirectoriesCache.TryGetValue(cacheKey, out directoryIsMissing) || directoryIsMissing)
                        {
                            if (!directoryIsMissing)
                            {
                                // wasn't in the cache, `TryGetValue` will set the bool to its default value: false
                                MissingDirectoriesCache[cacheKey] = directoryIsMissing = !Directory.Exists(directoryName);
                            }

                            if (directoryIsMissing)
                            {
                                Log.Error($"DefaultDataProvider.Fetch(): The specified directory was not found: {directoryName}");
                                return null;
                            }
                        }
                    }
                    catch
                    {
                        // pass, just in case, we don't want to lose original exception
                    }
                    Log.Error($"DefaultDataProvider.Fetch(): The specified file was not found: {key}");
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// The stream created by this type is passed up the stack to the IStreamReader
        /// The stream is closed when the StreamReader that wraps this stream is disposed</summary>
        public void Dispose()
        {
            //
        }
    }
}
